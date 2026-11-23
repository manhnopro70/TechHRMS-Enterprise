using Microsoft.EntityFrameworkCore;

namespace TechHRMS.Api;

public static partial class OperationEndpoints
{
    public static void MapOperationsEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        MapNotificationsAndLeave(api);
        MapTimesheets(api);
        MapPayroll(api);
        MapLifecycle(api);
    }
    private static IResult Invalid(string error) => Results.BadRequest(new { error });
    private static IResult Conflict(string error) => Results.Conflict(new { error });
    private static void MapNotificationsAndLeave(RouteGroupBuilder api)
    {
        api.MapGet("/notifications", async (HttpContext ctx, HrDb db) =>
        {
            var employeeId = Access.EmployeeId(ctx.User);
            var query = db.AppNotifications.AsNoTracking().Where(x => x.EmployeeId == employeeId);
            var unreadCount = await query.CountAsync(x => !x.IsRead);
            var items = await query.OrderBy(x => x.IsRead).ThenByDescending(x => x.CreatedAtUtc).Take(200)
                .Select(x => new { x.Id, x.Title, x.Message, x.Link, x.IsRead, x.CreatedAtUtc }).ToListAsync();
            return Results.Ok(new { items, unreadCount });
        });
        api.MapPut("/notifications/{id:int}/read", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            var employeeId = Access.EmployeeId(ctx.User);
            var count = await db.AppNotifications.Where(x => x.Id == id && x.EmployeeId == employeeId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));
            return count == 0 ? Results.NotFound() : Results.Ok(new { message = "Đã đánh dấu đã đọc." });
        });
        api.MapPut("/notifications/read-all", async (HttpContext ctx, HrDb db) =>
        {
            var employeeId = Access.EmployeeId(ctx.User);
            await db.AppNotifications.Where(x => x.EmployeeId == employeeId && !x.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));
            return Results.Ok(new { message = "Đã đọc tất cả thông báo." });
        });
        api.MapGet("/leave-balances", async Task<IResult> (HttpContext ctx, HrDb db, int? year, int? employeeId) =>
        {
            var selectedYear = year ?? DateTime.UtcNow.Year;
            if (selectedYear is < 2000 or > 2100) return Invalid("Năm phải trong khoảng 2000–2100.");
            var query = await Operations.ScopedEmployees(db, ctx.User);
            if (employeeId.HasValue)
            {
                if (!await Access.CanViewEmployee(db, ctx.User, employeeId.Value)) return Results.Forbid();
                query = query.Where(x => x.Id == employeeId);
            }
            var employees = await query.OrderBy(x => x.FullName).Select(x => new { x.Id, x.FullName }).ToListAsync();
            var ids = employees.Select(x => x.Id).ToList();
            var policies = await db.LeaveEntitlements.AsNoTracking().Where(x => ids.Contains(x.EmployeeId) && x.Year == selectedYear)
                .ToDictionaryAsync(x => x.EmployeeId, x => x.Entitlement);
            var from = new DateOnly(selectedYear, 1, 1); var to = new DateOnly(selectedYear, 12, 31);
            var requests = await db.WorkRequests.AsNoTracking().Where(x => ids.Contains(x.EmployeeId) && x.Type == "Leave"
                && x.EndDate >= from && x.StartDate <= to && (x.Status == "Pending" || x.Status == "Approved")).ToListAsync();
            return Results.Ok(employees.Select(e =>
            {
                var entitlement = policies.GetValueOrDefault(e.Id, Operations.DefaultLeaveDays);
                var own = requests.Where(r => r.EmployeeId == e.Id).ToList();
                var used = own.Where(r => r.Status == "Approved").Sum(r => Operations.DaysInYear(r, selectedYear));
                var pending = own.Where(r => r.Status == "Pending").Sum(r => Operations.DaysInYear(r, selectedYear));
                return new { employeeId = e.Id, employee = e.FullName, year = selectedYear, entitlement, used, pending,
                    available = entitlement - used - pending };
            }));
        });
        api.MapPut("/leave-balances/{employeeId:int}/{year:int}", async Task<IResult> (int employeeId, int year,
            LeaveEntitlementInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            if (year is < 2000 or > 2100 || input.Entitlement is < 0 or > 366) return Invalid("Hạn mức cần từ 0–366 ngày; năm 2000–2100.");
            await using var transaction = await Operations.BeginAsync(db);
            if (!await db.Employees.AnyAsync(x => x.Id == employeeId)) return Results.NotFound();
            var balance = await Operations.Balance(db, employeeId, year);
            if (input.Entitlement < balance.used + balance.pending) return Invalid("Hạn mức không được thấp hơn tổng phép đã dùng và đang chờ duyệt.");
            var policy = await db.LeaveEntitlements.SingleOrDefaultAsync(x => x.EmployeeId == employeeId && x.Year == year);
            if (policy is null) db.LeaveEntitlements.Add(new LeaveEntitlement { EmployeeId = employeeId, Year = year, Entitlement = input.Entitlement });
            else policy.Entitlement = input.Entitlement;
            Access.Log(db, ctx.User, "SetEntitlement", "Leave", $"{employeeId}:{year}:{input.Entitlement}");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { message = "Đã cập nhật hạn mức phép." });
        });
    }
}
public record LeaveEntitlementInput(int Entitlement);
