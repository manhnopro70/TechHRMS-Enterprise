using Microsoft.EntityFrameworkCore;

namespace TechHRMS.Api;

public static partial class HrEndpoints
{
    private static void MapWork(RouteGroupBuilder api)
    {
        api.MapGet("/requests", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var query = db.WorkRequests.Include(x => x.Employee).AsQueryable();
            if (Access.IsManager(ctx.User))
            {
                var dept = await db.Employees.Where(x => x.Id == myId).Select(x => x.DepartmentId).FirstOrDefaultAsync();
                query = query.Where(x => x.Employee!.DepartmentId == dept);
            }
            else if (!Access.IsHr(ctx.User)) query = query.Where(x => x.EmployeeId == myId);
            var list = await query.OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new { x.Id, x.EmployeeId, employee = x.Employee!.FullName, x.Type,
                    x.StartDate, x.EndDate, x.Reason, x.Status, x.ReviewNote, x.CreatedAtUtc })
                .ToListAsync();
            return Results.Ok(list);
        });

        api.MapPost("/requests", async Task<IResult> (WorkRequestInput input, HttpContext ctx, HrDb db) =>
        {
            if (!AllowedRequestTypes.Contains(input.Type)) return Bad("Loại đơn không hợp lệ.");
            if (input.EndDate < input.StartDate || input.EndDate.DayNumber - input.StartDate.DayNumber > 45)
                return Bad("Khoảng ngày không hợp lệ (tối đa 46 ngày). ");
            var reason = (input.Reason ?? "").Trim();
            if (reason.Length is < 5 or > 500) return Bad("Lý do phải có 5–500 ký tự.");
            var item = new WorkRequest { EmployeeId = Access.EmployeeId(ctx.User), Type = input.Type,
                StartDate = input.StartDate, EndDate = input.EndDate, Reason = reason };
            db.WorkRequests.Add(item);
            Access.Log(db, ctx.User, "Create", "Request", input.Type);
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });

        api.MapPost("/requests/{id:int}/decision", async Task<IResult> (int id, DecisionInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User) && !Access.IsManager(ctx.User)) return Results.Forbid();
            if (input.Status is not ("Approved" or "Rejected")) return Bad("Quyết định không hợp lệ.");
            var item = await db.WorkRequests.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.Status != "Pending") return Bad("Đơn đã được xử lý.");
            if (item.EmployeeId == Access.EmployeeId(ctx.User)) return Bad("Không thể tự duyệt đơn của mình.");
            if (!await Access.CanManageEmployee(db, ctx.User, item.EmployeeId)) return Results.Forbid();
            item.Status = input.Status;
            item.ReviewedById = Access.EmployeeId(ctx.User);
            item.ReviewNote = (input.Note ?? "").Trim()[..Math.Min((input.Note ?? "").Trim().Length, 300)];
            Access.Log(db, ctx.User, input.Status, "Request", id.ToString());
            await db.SaveChangesAsync();
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

public record WorkRequestInput(string Type, DateOnly StartDate, DateOnly EndDate, string Reason);
public record DecisionInput(string Status, string? Note);
public record SkillInput(int EmployeeId, string Name, string Level, string? Certificate);
