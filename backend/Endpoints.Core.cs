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
                assets = await db.Assets.CountAsync(x => Access.IsHr(ctx.User) || x.AssignedToEmployeeId == myId),
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
            if (!await db.Departments.AnyAsync(x => x.Id == input.DepartmentId)) return Bad("Phòng ban không tồn tại.");
            if (await db.Employees.AnyAsync(x => x.Email == email)) return Bad("Email đã tồn tại.");
            if (!AllowedWorkModes.Contains(input.WorkMode) || !AllowedEmployeeStatuses.Contains(input.Status)) return Bad("Trạng thái không hợp lệ.");
            var item = new Employee { FullName = name, Email = email, JobTitle = jobTitle, DepartmentId = input.DepartmentId,
                WorkMode = input.WorkMode, Status = input.Status, IsBench = input.IsBench };
            db.Employees.Add(item);
            Access.Log(db, ctx.User, "Create", "Employee", email);
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });

        api.MapPut("/employees/{id:int}", async Task<IResult> (int id, EmployeeInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var item = await db.Employees.FindAsync(id);
            if (item is null) return Results.NotFound();
            var name = (input.FullName ?? "").Trim();
            var email = (input.Email ?? "").Trim().ToLowerInvariant();
            var jobTitle = (input.JobTitle ?? "").Trim();
            if (name.Length is < 2 or > 120 || !LooksLikeEmail(email) || jobTitle.Length is < 2 or > 120)
                return Bad("Dữ liệu nhân viên không hợp lệ.");
            if (!await db.Departments.AnyAsync(x => x.Id == input.DepartmentId)) return Bad("Phòng ban không tồn tại.");
            if (await db.Employees.AnyAsync(x => x.Id != id && x.Email == email)) return Bad("Email đã tồn tại.");
            if (!AllowedWorkModes.Contains(input.WorkMode) || !AllowedEmployeeStatuses.Contains(input.Status)) return Bad("Trạng thái không hợp lệ.");
            item.FullName = name; item.Email = email; item.JobTitle = jobTitle; item.DepartmentId = input.DepartmentId;
            item.WorkMode = input.WorkMode; item.Status = input.Status; item.IsBench = input.IsBench;
            var account = await db.Accounts.FirstOrDefaultAsync(x => x.EmployeeId == id);
            if (account is not null) account.Email = email;
            Access.Log(db, ctx.User, "Update", "Employee", id.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });
    }

    private static readonly string[] AllowedWorkModes = ["Remote", "Hybrid", "Onsite"];
    private static readonly string[] AllowedEmployeeStatuses = ["Active", "OnLeave", "Inactive"];
    private static readonly string[] AllowedRequestTypes = ["Leave", "WFH", "OT"];
    private static readonly string[] AllowedStages = ["Applied", "TechnicalTest", "Interview", "Offer", "Hired", "Rejected"];
    private static bool LooksLikeEmail(string s) => s.Length is >= 5 and <= 180 && s.Contains('@') && !s.Contains(' ');
    private static IResult Bad(string message) => Results.BadRequest(new { error = message });
}

public record DepartmentInput(string Name, string Code);
public record EmployeeInput(string FullName, string Email, string JobTitle, int DepartmentId, string WorkMode, string Status, bool IsBench);
