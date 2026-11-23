using System.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace TechHRMS.Api;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        app.MapGet("/api/auth/csrf", (HttpContext ctx, IAntiforgery antiforgery) =>
        {
            ctx.Response.Headers.CacheControl = "no-cache, no-store";
            ctx.Response.Headers.Pragma = "no-cache";
            return Results.Ok(new { token = antiforgery.GetAndStoreTokens(ctx).RequestToken });
        });
        app.MapPost("/api/auth/login", async Task<IResult> (LoginInput input, HttpContext ctx, HrDb db) =>
        {
            if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 180 || string.IsNullOrWhiteSpace(input.Password) || input.Password.Length > 128)
                return Bad("Vui lòng nhập email và mật khẩu hợp lệ.");
            var email = input.Email.Trim().ToLowerInvariant();
            var account = await db.Accounts.Include(x => x.Employee).ThenInclude(x => x!.Department)
                .FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
            if (account is null || account.Employee is null || account.Employee.Status == "Inactive" || !Passwords.Verify(input.Password, account.PasswordHash))
                return Results.Json(new { error = "Email hoặc mật khẩu không chính xác, hoặc tài khoản đã bị khóa." }, statusCode: 401);
            account.LastLoginAtUtc = DateTime.UtcNow;
            Access.Log(db, PrincipalForAudit(account), "Login", "Account", account.Id.ToString());
            await db.SaveChangesAsync();
            await AccountRules.SignInAsync(ctx, account);
            return Results.Ok(AccountRules.Session(account));
        }).RequireRateLimiting("login");
        app.MapGet("/api/auth/me", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var account = await AccountRules.CurrentAsync(ctx, db);
            return account is null ? Results.Unauthorized() : Results.Ok(AccountRules.Session(account));
        }).RequireAuthorization();
        app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { message = "Đã đăng xuất." });
        }).RequireAuthorization();
        app.MapGet("/api/auth/profile", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var account = await AccountRules.CurrentAsync(ctx, db);
            if (account?.Employee is not Employee e) return Results.Unauthorized();
            return Results.Ok(new { e.Id, e.FullName, e.Email, account.Role, e.JobTitle, e.DepartmentId,
                department = e.Department?.Name ?? "", e.WorkMode, e.Status, e.IsBench, e.JoinedAt, account.MustChangePassword });
        }).RequireAuthorization();
        app.MapPut("/api/auth/profile", async Task<IResult> (ProfileInput input, HttpContext ctx, HrDb db) =>
        {
            var name = (input.FullName ?? "").Trim();
            if (name.Length is < 2 or > 120) return Bad("Họ và tên phải dài từ 2 đến 120 ký tự.");
            var account = await AccountRules.CurrentAsync(ctx, db);
            if (account?.Employee is null) return Results.Unauthorized();
            account.Employee.FullName = name;
            Access.Log(db, ctx.User, "UpdateOwnProfile", "Employee", account.EmployeeId.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Đã cập nhật hồ sơ cá nhân." });
        }).RequireAuthorization();
        app.MapPost("/api/auth/change-password", async Task<IResult> (ChangePasswordInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Passwords.IsValid(input.NewPassword)) return Bad("Mật khẩu mới phải dài từ 12 đến 128 ký tự.");
            if (input.CurrentPassword == input.NewPassword) return Bad("Mật khẩu mới phải khác mật khẩu hiện tại.");
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await AccountRules.LockWritesAsync(db);
            var account = await AccountRules.CurrentAsync(ctx, db);
            if (account is null) return Results.Unauthorized();
            if (!Passwords.Verify(input.CurrentPassword, account.PasswordHash)) return Bad("Mật khẩu hiện tại không chính xác.");
            account.PasswordHash = Passwords.Hash(input.NewPassword);
            account.MustChangePassword = false;
            account.SecurityStamp = Guid.NewGuid().ToString("N");
            Access.Log(db, ctx.User, "ChangePassword", "Account", account.Id.ToString());
            await db.SaveChangesAsync(); await tx.CommitAsync();
            await AccountRules.SignInAsync(ctx, account);
            return Results.Ok(AccountRules.Session(account));
        }).RequireAuthorization().RequireRateLimiting("password");

        var accounts = app.MapGroup("/api/accounts").RequireAuthorization(policy => policy.RequireRole("Admin"));
        accounts.MapGet("", async (HrDb db) => Results.Ok(await db.Accounts.AsNoTracking().OrderBy(x => x.Employee!.FullName)
            .Select(x => new { x.Id, x.EmployeeId, employeeName = x.Employee!.FullName, x.Email, x.Role,
                x.IsActive, x.MustChangePassword, x.CreatedAtUtc, x.LastLoginAtUtc }).ToListAsync()));
        accounts.MapPost("", async Task<IResult> (AccountCreateInput input, HttpContext ctx, HrDb db) =>
        {
            if (!AccountRules.Roles.Contains(input.Role) || !Passwords.IsValid(input.Password))
                return Bad("Vai trò không hợp lệ hoặc mật khẩu không đủ 12–128 ký tự.");
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await AccountRules.LockWritesAsync(db);
            var employee = await db.Employees.FindAsync(input.EmployeeId);
            if (employee is null || employee.Status == "Inactive") return Bad("Nhân viên không tồn tại hoặc đã ngừng làm việc.");
            if (await db.Accounts.AnyAsync(x => x.EmployeeId == employee.Id || x.Email == employee.Email))
                return Conflict("Nhân viên này đã có tài khoản. Hãy mở khóa hoặc đặt lại mật khẩu tài khoản hiện có.");
            var account = new Account { EmployeeId = employee.Id, Email = employee.Email, Role = input.Role,
                PasswordHash = Passwords.Hash(input.Password), MustChangePassword = true };
            db.Accounts.Add(account);
            Access.Log(db, ctx.User, "CreateAccount", "Account", $"Employee={employee.Id};Role={input.Role}");
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return Results.Ok(new { account.Id, message = "Đã cấp tài khoản. Nhân viên cần đổi mật khẩu khi đăng nhập lần đầu." });
        });
        accounts.MapPut("/{id:int}", async Task<IResult> (int id, AccountUpdateInput input, HttpContext ctx, HrDb db) =>
        {
            if (!AccountRules.Roles.Contains(input.Role)) return Bad("Vai trò không hợp lệ.");
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await AccountRules.LockWritesAsync(db);
            var account = await db.Accounts.Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == id);
            if (account is null) return Results.NotFound();
            if (id == Access.AccountId(ctx.User) && (!input.IsActive || input.Role != "Admin"))
                return Bad("Không thể tự khóa hoặc hạ quyền tài khoản đang sử dụng.");
            if (account.Role == "Admin" && (input.Role != "Admin" || !input.IsActive) && !await AccountRules.CanDeactivateAsync(db, account.EmployeeId))
                return Conflict("Cần giữ ít nhất một quản trị viên đang hoạt động.");
            if (input.IsActive && account.Employee?.Status == "Inactive")
                return Bad("Hãy cập nhật nhân viên sang trạng thái đang làm việc trước khi mở tài khoản.");
            account.Role = input.Role; account.IsActive = input.IsActive;
            account.SecurityStamp = Guid.NewGuid().ToString("N");
            Access.Log(db, ctx.User, "UpdateAccount", "Account", $"Id={id};Role={input.Role};Active={input.IsActive}");
            await db.SaveChangesAsync(); await tx.CommitAsync();
            if (id == Access.AccountId(ctx.User))
            {
                await db.Entry(account).Reference(x => x.Employee).LoadAsync();
                if (account.Employee is not null) await db.Entry(account.Employee).Reference(x => x.Department).LoadAsync();
                await AccountRules.SignInAsync(ctx, account);
            }
            return Results.Ok(new { message = "Đã cập nhật tài khoản và thu hồi các phiên đăng nhập cũ." });
        });
        accounts.MapPost("/{id:int}/reset-password", async Task<IResult> (int id, ResetPasswordInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Passwords.IsValid(input.Password)) return Bad("Mật khẩu tạm phải dài từ 12 đến 128 ký tự.");
            if (id == Access.AccountId(ctx.User)) return Bad("Hãy dùng Đổi mật khẩu trong tài khoản cá nhân để đổi mật khẩu của bạn.");
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await AccountRules.LockWritesAsync(db);
            var account = await db.Accounts.FindAsync(id);
            if (account is null) return Results.NotFound();
            account.PasswordHash = Passwords.Hash(input.Password); account.MustChangePassword = true;
            account.SecurityStamp = Guid.NewGuid().ToString("N");
            Access.Log(db, ctx.User, "ResetPassword", "Account", id.ToString());
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return Results.Ok(new { message = "Đã đặt mật khẩu tạm và thu hồi các phiên cũ. Người dùng phải đổi mật khẩu khi đăng nhập." });
        });
        accounts.MapPost("/{id:int}/revoke-sessions", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await AccountRules.LockWritesAsync(db);
            var account = await db.Accounts.FindAsync(id);
            if (account is null) return Results.NotFound();
            account.SecurityStamp = Guid.NewGuid().ToString("N");
            Access.Log(db, ctx.User, "RevokeSessions", "Account", id.ToString());
            await db.SaveChangesAsync(); await tx.CommitAsync();
            if (id == Access.AccountId(ctx.User)) await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { message = "Đã thu hồi toàn bộ phiên đăng nhập của tài khoản." });
        });
    }
    private static System.Security.Claims.ClaimsPrincipal PrincipalForAudit(Account account) => new(new System.Security.Claims.ClaimsIdentity([
        new System.Security.Claims.Claim("employeeId", account.EmployeeId.ToString())]));
    private static IResult Bad(string message) => Results.BadRequest(new { error = message });
    private static IResult Conflict(string message) => Results.Conflict(new { error = message });
}

public record LoginInput(string Email, string Password);
public record AccountCreateInput(int EmployeeId, string Role, string Password);
public record AccountUpdateInput(string Role, bool IsActive);
public record ResetPasswordInput(string Password);
public record ChangePasswordInput(string CurrentPassword, string NewPassword);
public record ProfileInput(string FullName);
public record ProvisionAccountInput(string Role, string Password);
