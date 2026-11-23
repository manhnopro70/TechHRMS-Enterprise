using Microsoft.EntityFrameworkCore;

namespace TechHRMS.Api;

public static partial class OperationEndpoints
{
    private static void MapTimesheets(RouteGroupBuilder api)
    {
        api.MapGet("/timesheets", async Task<IResult> (HttpContext ctx, HrDb db, string? month) =>
        {
            month = string.IsNullOrWhiteSpace(month) ? DateTime.UtcNow.ToString("yyyy-MM") : month;
            if (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var rangeStart) || rangeStart.Year is < 2000 or > 2100)
                return Invalid("Tháng phải có dạng YYYY-MM, năm 2000–2100.");
            var until = rangeStart.AddMonths(1);
            var workers = await Operations.ScopedEmployees(db, ctx.User);
            var ids = workers.Select(x => x.Id);
            return Results.Ok(await (from t in db.ProjectTimesheets.AsNoTracking()
                join e in db.Employees on t.EmployeeId equals e.Id
                join p in db.Projects on t.ProjectId equals p.Id
                where ids.Contains(t.EmployeeId) && t.WorkDate >= rangeStart && t.WorkDate < until
                orderby t.WorkDate descending, t.Id descending
                select new { t.Id, t.EmployeeId, employee = e.FullName, t.ProjectId, project = p.Name,
                    t.WorkDate, t.Hours, t.Billable, t.Note, t.Status, t.ReviewNote }).ToListAsync());
        });
        api.MapPost("/timesheets", async Task<IResult> (TimesheetInput input, HttpContext ctx, HrDb db) =>
        {
            var actor = Access.EmployeeId(ctx.User);
            await using var transaction = await Operations.BeginAsync(db);
            var error = await ValidateTimesheet(db, actor, input, null, ctx);
            if (error is not null) return Invalid(error);
            var item = new ProjectTimesheet { EmployeeId = actor, ProjectId = input.ProjectId, WorkDate = input.WorkDate,
                Hours = input.Hours, Billable = input.Billable, Note = (input.Note ?? "").Trim() };
            db.ProjectTimesheets.Add(item);
            Access.Log(db, ctx.User, "Create", "Timesheet", $"{actor}:{input.ProjectId}:{input.WorkDate}");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { item.Id });
        });
        api.MapPut("/timesheets/{id:int}", async Task<IResult> (int id, TimesheetInput input, HttpContext ctx, HrDb db) =>
        {
            await using var transaction = await Operations.BeginAsync(db);
            var item = await db.ProjectTimesheets.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.EmployeeId != Access.EmployeeId(ctx.User)) return Results.Forbid();
            if (item.Status is not ("Draft" or "Rejected")) return Conflict("Chỉ được sửa giờ công nháp hoặc bị từ chối.");
            var error = await ValidateTimesheet(db, item.EmployeeId, input, id, ctx);
            if (error is not null) return Invalid(error);
            item.ProjectId = input.ProjectId; item.WorkDate = input.WorkDate; item.Hours = input.Hours;
            item.Billable = input.Billable; item.Note = (input.Note ?? "").Trim(); item.Status = "Draft";
            item.ReviewNote = ""; item.ReviewedById = null;
            Access.Log(db, ctx.User, "Update", "Timesheet", id.ToString());
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { item.Id });
        });
        api.MapDelete("/timesheets/{id:int}", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            await using var transaction = await Operations.BeginAsync(db);
            var item = await db.ProjectTimesheets.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.EmployeeId != Access.EmployeeId(ctx.User)) return Results.Forbid();
            if (item.Status is not ("Draft" or "Rejected")) return Conflict("Chỉ được xóa giờ công nháp hoặc bị từ chối.");
            db.ProjectTimesheets.Remove(item);
            Access.Log(db, ctx.User, "Delete", "Timesheet", id.ToString());
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { message = "Đã xóa giờ công." });
        });
        api.MapPost("/timesheets/{id:int}/submit", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            await using var transaction = await Operations.BeginAsync(db);
            var item = await db.ProjectTimesheets.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.EmployeeId != Access.EmployeeId(ctx.User)) return Results.Forbid();
            if (item.Status is not ("Draft" or "Rejected")) return Conflict("Giờ công đã được gửi hoặc duyệt.");
            var error = await ValidateTimesheet(db, item.EmployeeId,
                new TimesheetInput(item.ProjectId, item.WorkDate, item.Hours, item.Billable, item.Note), id, ctx);
            if (error is not null) return Invalid(error);
            item.Status = "Submitted"; item.ReviewNote = ""; item.ReviewedById = null;
            Access.Log(db, ctx.User, "Submit", "Timesheet", id.ToString());
            await Operations.NotifyApprovers(db, item.EmployeeId, "Giờ công cần phê duyệt", "Có bản ghi giờ công dự án mới đang chờ duyệt.", "timesheets");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { item.Id, item.Status });
        });
        api.MapPost("/timesheets/{id:int}/decision", async Task<IResult> (int id, DecisionInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User) && !Access.IsManager(ctx.User)) return Results.Forbid();
            var note = (input.Note ?? "").Trim();
            if (input.Status is not ("Approved" or "Rejected") || note.Length > 300 || (input.Status == "Rejected" && note.Length < 3))
                return Invalid("Chọn duyệt hoặc từ chối; từ chối cần lý do từ 3–300 ký tự.");
            await using var transaction = await Operations.BeginAsync(db);
            var item = await db.ProjectTimesheets.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.EmployeeId == Access.EmployeeId(ctx.User)) return Invalid("Không được tự duyệt giờ công của mình.");
            if (!await Access.CanManageEmployee(db, ctx.User, item.EmployeeId)) return Results.Forbid();
            if (item.Status != "Submitted") return Conflict("Bản ghi không còn chờ phê duyệt.");
            item.Status = input.Status; item.ReviewNote = note; item.ReviewedById = Access.EmployeeId(ctx.User);
            Access.Log(db, ctx.User, input.Status, "Timesheet", id.ToString());
            Operations.Notify(db, item.EmployeeId, "Kết quả duyệt giờ công", input.Status == "Approved" ? "Giờ công dự án đã được duyệt." : "Giờ công bị từ chối: " + note, "timesheets");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { item.Id, item.Status });
        });
    }
    private static async Task<string?> ValidateTimesheet(HrDb db, int employeeId, TimesheetInput input, int? ignoreId, HttpContext ctx)
    {
        var zoneId = ctx.RequestServices.GetRequiredService<IConfiguration>()["HRMS_TIME_ZONE"] ?? "Asia/Ho_Chi_Minh";
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, zoneId));
        if (input.WorkDate.Year < 2000 || input.WorkDate > today) return "Ngày ghi công phải từ năm 2000 đến hôm nay.";
        if (input.Hours is <= 0 or > 24 || decimal.Round(input.Hours, 2) != input.Hours) return "Số giờ cần lớn hơn 0, tối đa 24, có tối đa 2 chữ số thập phân.";
        if ((input.Note ?? "").Trim().Length is < 3 or > 500) return "Mô tả công việc cần từ 3–500 ký tự.";
        if (!await db.Allocations.AnyAsync(a => a.EmployeeId == employeeId && a.TechProjectId == input.ProjectId && a.TechProject!.Status == "Active"))
            return "Bạn cần được phân bổ vào dự án đang hoạt động trước khi ghi công.";
        if (await db.ProjectTimesheets.AnyAsync(x => x.EmployeeId == employeeId && x.ProjectId == input.ProjectId
            && x.WorkDate == input.WorkDate && (!ignoreId.HasValue || x.Id != ignoreId)))
            return "Đã có giờ công cho dự án trong ngày này. Hãy chỉnh sửa bản ghi hiện có.";
        var hours = await db.ProjectTimesheets.Where(x => x.EmployeeId == employeeId && x.WorkDate == input.WorkDate
            && (!ignoreId.HasValue || x.Id != ignoreId)).Select(x => x.Hours).ToListAsync();
        return hours.Sum() + input.Hours > 24 ? "Tổng giờ công của tất cả dự án trong một ngày không được vượt quá 24 giờ." : null;
    }
}
public record TimesheetInput(int ProjectId, DateOnly WorkDate, decimal Hours, bool Billable, string? Note);
