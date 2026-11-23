using Microsoft.EntityFrameworkCore;

namespace TechHRMS.Api;

public static partial class HrEndpoints
{
    private static void MapWork(RouteGroupBuilder api)
    {
        api.MapGet("/requests", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var workers = await Operations.ScopedEmployees(db, ctx.User);
            var allowedIds = workers.Select(x => x.Id);
            var records = await (from r in db.WorkRequests.AsNoTracking()
                join e in db.Employees on r.EmployeeId equals e.Id
                join detail in db.WorkRequestDetails on r.Id equals detail.RequestId into details
                from d in details.DefaultIfEmpty()
                where allowedIds.Contains(r.EmployeeId)
                orderby r.CreatedAtUtc descending
                select new { request = r, employee = e.FullName, hours = d == null ? (decimal?)null : d.Hours }).ToListAsync();
            return Results.Ok(records.Select(x => new { x.request.Id, x.request.EmployeeId, x.employee,
                x.request.Type, x.request.StartDate, x.request.EndDate, x.request.Reason, x.request.Status,
                x.request.ReviewNote, x.request.CreatedAtUtc, x.hours,
                days = Operations.WorkingDays(x.request.StartDate, x.request.EndDate) }));
        });

        api.MapPost("/requests", async Task<IResult> (WorkRequestInput input, HttpContext ctx, HrDb db) =>
        {
            if (!AllowedRequestTypes.Contains(input.Type)) return Bad("Loại đơn không hợp lệ.");
            if (input.StartDate.Year is < 2000 or > 2100 || input.EndDate.Year > 2100 || input.EndDate < input.StartDate
                || input.EndDate.DayNumber - input.StartDate.DayNumber > 45)
                return Bad("Khoảng ngày không hợp lệ (năm 2000–2100, tối đa 46 ngày).");
            var reason = (input.Reason ?? "").Trim();
            if (reason.Length is < 5 or > 500) return Bad("Lý do phải có 5–500 ký tự.");
            if (input.Type == "OT" && (input.StartDate != input.EndDate || input.Hours is null or <= 0 or > 12
                || decimal.Round(input.Hours.Value, 2) != input.Hours))
                return Bad("Đơn OT cần một ngày và số giờ lớn hơn 0, tối đa 12 giờ (tối đa 2 chữ số thập phân).");
            if (input.Type != "OT" && input.Hours.HasValue) return Bad("Số giờ chỉ áp dụng cho đơn OT.");
            var days = Operations.WorkingDays(input.StartDate, input.EndDate);
            if (input.Type == "Leave" && (input.StartDate.Year != input.EndDate.Year || days == 0))
                return Bad("Đơn nghỉ phép cần ngày làm việc từ thứ Hai đến thứ Sáu trong cùng một năm.");
            var employeeId = Access.EmployeeId(ctx.User);
            await using var transaction = await Operations.BeginAsync(db);
            if (!await db.Employees.AnyAsync(x => x.Id == employeeId && x.Status != "Inactive")) return Results.Forbid();
            var overlap = db.WorkRequests.Where(x => x.EmployeeId == employeeId
                && (x.Status == "Pending" || x.Status == "Approved")
                && x.StartDate <= input.EndDate && x.EndDate >= input.StartDate);
            if (await overlap.AnyAsync(x => x.Type == input.Type || (input.Type != "OT" && x.Type != "OT")))
                return Results.Conflict(new { error = "Đã có đơn trùng loại hoặc trùng lịch nghỉ/WFH trong khoảng ngày này." });
            if (input.Type == "Leave")
            {
                var balance = await Operations.Balance(db, employeeId, input.StartDate.Year);
                if (days > balance.entitlement - balance.used - balance.pending)
                    return Bad("Số ngày vượt quá phép khả dụng. Phép của đơn đang chờ duyệt được tạm giữ.");
            }
            var item = new WorkRequest { EmployeeId = employeeId, Type = input.Type,
                StartDate = input.StartDate, EndDate = input.EndDate, Reason = reason };
            db.WorkRequests.Add(item);
            await db.SaveChangesAsync();
            db.WorkRequestDetails.Add(new WorkRequestDetail { RequestId = item.Id, Hours = input.Type == "OT" ? input.Hours : null });
            Access.Log(db, ctx.User, "Create", "Request", $"{item.Id}:{input.Type}");
            await Operations.NotifyApprovers(db, employeeId, "Đơn mới cần phê duyệt", $"Có đơn {input.Type} mới đang chờ xử lý.", "requests");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { item.Id });
        });

        api.MapPost("/requests/{id:int}/decision", async Task<IResult> (int id, DecisionInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User) && !Access.IsManager(ctx.User)) return Results.Forbid();
            if (input.Status is not ("Approved" or "Rejected")) return Bad("Quyết định không hợp lệ.");
            var note = (input.Note ?? "").Trim();
            if (note.Length > 300 || (input.Status == "Rejected" && note.Length < 3))
                return Bad("Khi từ chối cần lý do từ 3–300 ký tự.");
            await using var transaction = await Operations.BeginAsync(db);
            var item = await db.WorkRequests.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.Status != "Pending") return Results.Conflict(new { error = "Đơn đã được xử lý hoặc hủy." });
            if (item.EmployeeId == Access.EmployeeId(ctx.User)) return Bad("Không thể tự duyệt đơn của mình.");
            if (!await Access.CanManageEmployee(db, ctx.User, item.EmployeeId)) return Results.Forbid();
            if (input.Status == "Approved" && item.Type == "Leave")
            {
                for (var year = item.StartDate.Year; year <= item.EndDate.Year; year++)
                {
                    var balance = await Operations.Balance(db, item.EmployeeId, year);
                    if (balance.used + Operations.DaysInYear(item, year) > balance.entitlement)
                        return Bad("Số phép không đủ. HR cần kiểm tra hạn mức trước khi phê duyệt.");
                }
            }
            item.Status = input.Status; item.ReviewedById = Access.EmployeeId(ctx.User); item.ReviewNote = note;
            Access.Log(db, ctx.User, input.Status, "Request", id.ToString());
            Operations.Notify(db, item.EmployeeId, input.Status == "Approved" ? "Đơn đã được duyệt" : "Đơn bị từ chối",
                $"Đơn {item.Type} #{item.Id}: " + (note.Length == 0 ? "Đã cập nhật kết quả." : note), "requests");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { item.Id, item.Status });
        });
        api.MapPost("/requests/{id:int}/cancel", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            await using var transaction = await Operations.BeginAsync(db);
            var item = await db.WorkRequests.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.EmployeeId != Access.EmployeeId(ctx.User)) return Results.Forbid();
            if (item.Status != "Pending") return Results.Conflict(new { error = "Chỉ có thể hủy đơn đang chờ duyệt." });
            item.Status = "Cancelled";
            Access.Log(db, ctx.User, "Cancel", "Request", id.ToString());
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { item.Id, item.Status });
        });

        api.MapGet("/skills", async Task<IResult> (HttpContext ctx, HrDb db, int? employeeId) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var query = db.Skills.Include(x => x.Employee).AsQueryable();
            if (employeeId.HasValue)
            {
                if (!await Access.CanViewEmployee(db, ctx.User, employeeId.Value)) return Results.Forbid();
                query = query.Where(x => x.EmployeeId == employeeId);
            }
            else if (Access.IsManager(ctx.User))
            {
                var dept = await db.Employees.Where(x => x.Id == myId).Select(x => x.DepartmentId).FirstOrDefaultAsync();
                query = query.Where(x => x.Employee!.DepartmentId == dept);
            }
            else if (!Access.IsHr(ctx.User)) query = query.Where(x => x.EmployeeId == myId);
            return Results.Ok(await query.OrderBy(x => x.Name).Select(x => new { x.Id, x.EmployeeId,
                employee = x.Employee!.FullName, x.Name, x.Level, x.Certificate }).ToListAsync());
        });

        api.MapPost("/skills", async Task<IResult> (SkillInput input, HttpContext ctx, HrDb db) =>
        {
            var owner = input.EmployeeId == 0 ? Access.EmployeeId(ctx.User) : input.EmployeeId;
            if (owner != Access.EmployeeId(ctx.User) && !Access.IsHr(ctx.User)) return Results.Forbid();
            if (!await db.Employees.AnyAsync(x => x.Id == owner)) return Bad("Không tìm thấy nhân viên.");
            var name = (input.Name ?? "").Trim();
            if (name.Length is < 2 or > 80 || input.Level is not ("Beginner" or "Intermediate" or "Advanced" or "Senior"))
                return Bad("Kỹ năng hoặc cấp độ không hợp lệ.");
            if ((input.Certificate ?? "").Length > 150) return Bad("Tên chứng chỉ quá dài.");
            var item = new Skill { EmployeeId = owner, Name = name, Level = input.Level, Certificate = (input.Certificate ?? "").Trim() };
            db.Skills.Add(item);
            Access.Log(db, ctx.User, "Create", "Skill", name);
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });

        api.MapDelete("/skills/{id:int}", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            var item = await db.Skills.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.EmployeeId != Access.EmployeeId(ctx.User) && !Access.IsHr(ctx.User)) return Results.Forbid();
            db.Skills.Remove(item);
            Access.Log(db, ctx.User, "Delete", "Skill", id.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Đã xóa kỹ năng." });
        });
    }
}

public record WorkRequestInput(string Type, DateOnly StartDate, DateOnly EndDate, string Reason, decimal? Hours = null);
public record DecisionInput(string Status, string? Note);
public record SkillInput(int EmployeeId, string Name, string Level, string? Certificate);
