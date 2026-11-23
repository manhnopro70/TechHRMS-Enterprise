using System.Data;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;

namespace TechHRMS.Api;

public static partial class HrEndpoints
{
    public static void MapHrEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        MapCore(api);
        MapWork(api);
        MapExtended(api);
        MapEnterprise(api);
    }

    private static void MapCore(RouteGroupBuilder api)
    {
        api.MapGet("/dashboard", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var myDepartment = await db.Employees.Where(x => x.Id == myId).Select(x => x.DepartmentId).FirstOrDefaultAsync();
            var mine = !Access.IsHr(ctx.User) && !Access.IsManager(ctx.User);
            var workforce = Access.IsHr(ctx.User) ? db.Employees.AsQueryable()
                : Access.IsManager(ctx.User) ? db.Employees.Where(x => x.DepartmentId == myDepartment)
                : db.Employees.Where(x => x.Id == myId);
            var requests = Access.IsHr(ctx.User) ? db.WorkRequests.AsQueryable()
                : Access.IsManager(ctx.User) ? db.WorkRequests.Where(x => x.Employee!.DepartmentId == myDepartment)
                : db.WorkRequests.Where(x => x.EmployeeId == myId);
            return Results.Ok(new
            {
                totalEmployees = await workforce.CountAsync(),
                departments = await db.Departments.CountAsync(),
                remoteEmployees = await workforce.CountAsync(x => x.WorkMode == "Remote"),
                benchEmployees = await workforce.CountAsync(x => x.IsBench),
                pendingRequests = await requests.CountAsync(x => x.Status == "Pending"),
                assets = Access.IsHr(ctx.User) ? await db.Assets.CountAsync() : await db.Assets.CountAsync(x => x.AssignedToEmployeeId == myId),
                workforceByDepartment = await workforce.GroupBy(x => x.Department!.Name)
                    .Select(g => new { name = g.Key, count = g.Count() }).OrderByDescending(x => x.count).ToListAsync(),
                portal = ctx.User.IsInRole("Admin") || ctx.User.IsInRole("HR") ? "HR & Admin"
                    : ctx.User.IsInRole("Manager") ? "Manager" : "Employee",
                isPersonal = mine
            });
        });

        api.MapGet("/departments", async Task<IResult> (HrDb db) =>
        {
            var list = await db.Departments.OrderBy(x => x.Name)
                .Select(d => new { d.Id, d.Name, d.Code, employeeCount = db.Employees.Count(x => x.DepartmentId == d.Id) })
                .ToListAsync();
            return Results.Ok(list);
        });

        api.MapPost("/departments", async Task<IResult> (DepartmentInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var code = (input.Code ?? "").Trim().ToUpperInvariant();
            var name = (input.Name ?? "").Trim();
            if (code.Length is < 2 or > 12 || name.Length is < 2 or > 100) return Bad("Mã hoặc tên phòng ban không hợp lệ.");
            if (await db.Departments.AnyAsync(x => x.Code == code)) return Bad("Mã phòng ban đã tồn tại.");
            var dep = new Department { Code = code, Name = name };
            db.Departments.Add(dep);
            Access.Log(db, ctx.User, "Create", "Department", code);
            await db.SaveChangesAsync();
            return Results.Ok(new { dep.Id });
        });

        api.MapGet("/employees", async Task<IResult> (HttpContext ctx, HrDb db, string? search, string? bench) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var query = db.Employees.Include(x => x.Department).AsQueryable();
            if (Access.IsManager(ctx.User))
            {
                var dept = await db.Employees.Where(x => x.Id == myId).Select(x => x.DepartmentId).FirstOrDefaultAsync();
                query = query.Where(x => x.DepartmentId == dept);
            }
            else if (!Access.IsHr(ctx.User)) query = query.Where(x => x.Id == myId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.FullName.Contains(term) || x.Email.Contains(term) || x.JobTitle.Contains(term));
            }
            if (bench == "true") query = query.Where(x => x.IsBench);
            var list = await query.OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName, x.Email, x.JobTitle, x.DepartmentId,
                    department = x.Department!.Name, x.WorkMode, x.Status, x.IsBench, x.JoinedAt })
                .ToListAsync();
            return Results.Ok(list);
        });

        api.MapPost("/employees", async Task<IResult> (EmployeeInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var name = (input.FullName ?? "").Trim();
            var email = (input.Email ?? "").Trim().ToLowerInvariant();
            var jobTitle = (input.JobTitle ?? "").Trim();
            if (name.Length is < 2 or > 120 || !LooksLikeEmail(email) || jobTitle.Length is < 2 or > 120)
                return Bad("Họ tên, email hoặc chức danh không hợp lệ.");
            if (!AllowedWorkModes.Contains(input.WorkMode) || !AllowedEmployeeStatuses.Contains(input.Status)) return Bad("Trạng thái không hợp lệ.");
            if (input.Account is not null)
            {
                if (!Access.IsAdmin(ctx.User)) return Results.Forbid();
                if (!AccountRules.Roles.Contains(input.Account.Role) || !Passwords.IsValid(input.Account.Password))
                    return Bad("Vai trò hoặc mật khẩu tài khoản không hợp lệ (12–128 ký tự).");
                if (input.Status == "Inactive") return Bad("Không thể cấp tài khoản cho nhân viên đã ngừng làm việc.");
            }
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await AccountRules.LockWritesAsync(db);
            if (!await db.Departments.AnyAsync(x => x.Id == input.DepartmentId)) return Bad("Phòng ban không tồn tại.");
            if (await db.Employees.AnyAsync(x => x.Email == email) || await db.Accounts.AnyAsync(x => x.Email == email))
                return Results.Conflict(new { error = "Email đã tồn tại." });
            var item = new Employee { FullName = name, Email = email, JobTitle = jobTitle, DepartmentId = input.DepartmentId,
                WorkMode = input.WorkMode, Status = input.Status, IsBench = input.IsBench };
            db.Employees.Add(item);
            if (input.Account is not null)
            {
                db.Accounts.Add(new Account { Employee = item, Email = email, Role = input.Account.Role,
                    PasswordHash = Passwords.Hash(input.Account.Password), MustChangePassword = true });
                Access.Log(db, ctx.User, "CreateAccount", "Account", $"Email={email};Role={input.Account.Role}");
            }
            Access.Log(db, ctx.User, "Create", "Employee", email);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return Results.Ok(new { item.Id, accountCreated = input.Account is not null });
        });

        api.MapPut("/employees/{id:int}", async Task<IResult> (int id, EmployeeInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            if (input.Account is not null) return Bad("Vui lòng dùng mục Tài khoản để cấp hoặc cập nhật quyền đăng nhập.");
            var name = (input.FullName ?? "").Trim();
            var email = (input.Email ?? "").Trim().ToLowerInvariant();
            var jobTitle = (input.JobTitle ?? "").Trim();
            if (name.Length is < 2 or > 120 || !LooksLikeEmail(email) || jobTitle.Length is < 2 or > 120)
                return Bad("Dữ liệu nhân viên không hợp lệ.");
            if (!AllowedWorkModes.Contains(input.WorkMode) || !AllowedEmployeeStatuses.Contains(input.Status)) return Bad("Trạng thái không hợp lệ.");
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await AccountRules.LockWritesAsync(db);
            var item = await db.Employees.FindAsync(id);
            if (item is null) return Results.NotFound();
            var account = await db.Accounts.FirstOrDefaultAsync(x => x.EmployeeId == id);
            if (account?.Role == "Admin" && !Access.IsAdmin(ctx.User)) return Results.Forbid();
            if (input.Status == "Inactive")
            {
                if (id == Access.EmployeeId(ctx.User)) return Bad("Không thể tự ngừng hoạt động hồ sơ đang sử dụng.");
                if (!await AccountRules.CanDeactivateAsync(db, id)) return Results.Conflict(new { error = "Cần giữ ít nhất một quản trị viên đang hoạt động." });
            }
            if (!await db.Departments.AnyAsync(x => x.Id == input.DepartmentId)) return Bad("Phòng ban không tồn tại.");
            if (await db.Employees.AnyAsync(x => x.Id != id && x.Email == email) ||
                await db.Accounts.AnyAsync(x => x.EmployeeId != id && x.Email == email)) return Results.Conflict(new { error = "Email đã tồn tại." });
            var securityChanged = item.Email != email || item.DepartmentId != input.DepartmentId || item.Status != input.Status;
            item.FullName = name; item.Email = email; item.JobTitle = jobTitle; item.DepartmentId = input.DepartmentId;
            item.WorkMode = input.WorkMode; item.Status = input.Status; item.IsBench = input.IsBench;
            if (account is not null)
            {
                account.Email = email;
                if (securityChanged) account.SecurityStamp = Guid.NewGuid().ToString("N");
                if (input.Status == "Inactive") account.IsActive = false;
            }
            Access.Log(db, ctx.User, "Update", "Employee", $"Id={id};Department={input.DepartmentId};Status={input.Status}");
            await db.SaveChangesAsync(); await tx.CommitAsync();
            if (account is not null && id == Access.EmployeeId(ctx.User))
            {
                await db.Entry(item).Reference(x => x.Department).LoadAsync();
                account.Employee = item;
                await AccountRules.SignInAsync(ctx, account);
            }
            return Results.Ok(new { item.Id });
        });
        api.MapPut("/departments/{id:int}", async Task<IResult> (int id, DepartmentInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var name = (input.Name ?? "").Trim(); var code = (input.Code ?? "").Trim().ToUpperInvariant();
            if (code.Length is < 2 or > 12 || name.Length is < 2 or > 100) return Bad("Mã hoặc tên phòng ban không hợp lệ.");
            var department = await db.Departments.FindAsync(id);
            if (department is null) return Results.NotFound();
            if (await db.Departments.AnyAsync(x => x.Id != id && x.Code == code)) return Results.Conflict(new { error = "Mã phòng ban đã tồn tại." });
            department.Name = name; department.Code = code;
            Access.Log(db, ctx.User, "Update", "Department", id.ToString());
            await db.SaveChangesAsync(); return Results.Ok(new { department.Id });
        });

    }

    private static readonly string[] AllowedWorkModes = ["Remote", "Hybrid", "Onsite"];
    private static readonly string[] AllowedEmployeeStatuses = ["Active", "OnLeave", "Inactive"];
    private static readonly string[] AllowedRequestTypes = ["Leave", "WFH", "OT"];
    private static readonly string[] AllowedStages = ["Applied", "TechnicalTest", "Interview", "Offer", "Hired", "Rejected"];
    private static bool LooksLikeEmail(string s) => s.Length is >= 5 and <= 180 && MailAddress.TryCreate(s, out var parsed) && parsed.Address == s && s.Contains('@');
    private static IResult Bad(string message) => Results.BadRequest(new { error = message });
}

public record DepartmentInput(string Name, string Code);
public record EmployeeInput(string FullName, string Email, string JobTitle, int DepartmentId, string WorkMode, string Status, bool IsBench, ProvisionAccountInput? Account = null);
