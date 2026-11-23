using Microsoft.EntityFrameworkCore;

namespace TechHRMS.Api;

public static partial class OperationEndpoints
{
    private static object PayrollItemDto(PayrollItem item) => new
    {
        item.Id, item.PeriodId, item.EmployeeId, employee = item.EmployeeName,
        item.BaseSalary, item.Allowances, item.Bonus, item.Deductions,
        gross = item.BaseSalary + item.Allowances + item.Bonus,
        net = item.BaseSalary + item.Allowances + item.Bonus - item.Deductions, item.Note
    };
    private static object PayslipDto(PayrollItem item, PayrollPeriod period) => new
    {
        item.Id, item.PeriodId, item.EmployeeId, employee = item.EmployeeName,
        item.BaseSalary, item.Allowances, item.Bonus, item.Deductions,
        gross = item.BaseSalary + item.Allowances + item.Bonus,
        net = item.BaseSalary + item.Allowances + item.Bonus - item.Deductions,
        item.Note, period.Month, period.Title, period.PublishedAtUtc, currency = "VND", isDemo = true
    };
    private static void MapPayroll(RouteGroupBuilder api)
    {
        api.MapGet("/payroll/periods", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var periods = await db.PayrollPeriods.AsNoTracking().OrderByDescending(x => x.Month).ToListAsync();
            var items = await db.PayrollItems.AsNoTracking().ToListAsync();
            return Results.Ok(periods.Select(p =>
            {
                var own = items.Where(x => x.PeriodId == p.Id).ToList();
                var totalGross = own.Sum(x => x.BaseSalary + x.Allowances + x.Bonus);
                return new { p.Id, p.Month, p.Title, p.Status, itemCount = own.Count, totalGross,
                    totalNet = totalGross - own.Sum(x => x.Deductions), p.CreatedAtUtc, p.PublishedAtUtc,
                    p.CreatedById, p.ApprovedById };
            }));
        });
        api.MapPost("/payroll/periods", async Task<IResult> (PayrollPeriodInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var title = (input.Title ?? "").Trim();
            if (!DateOnly.TryParseExact(input.Month + "-01", "yyyy-MM-dd", out var month)
                || month.Year is < 2000 or > 2100 || title.Length is < 3 or > 120)
                return Invalid("Tên kỳ cần 3–120 ký tự, tháng YYYY-MM trong năm 2000–2100.");
            await using var transaction = await Operations.BeginAsync(db);
            if (await db.PayrollPeriods.AnyAsync(x => x.Month == input.Month)) return Conflict("Tháng này đã có kỳ lương.");
            var item = new PayrollPeriod { Month = input.Month, Title = title, CreatedById = Access.EmployeeId(ctx.User) };
            db.PayrollPeriods.Add(item);
            Access.Log(db, ctx.User, "Create", "PayrollPeriod", input.Month);
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { item.Id });
        });
        api.MapGet("/payroll/periods/{id:int}/items", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            if (!await db.PayrollPeriods.AnyAsync(x => x.Id == id)) return Results.NotFound();
            var items = await db.PayrollItems.AsNoTracking().Where(x => x.PeriodId == id).OrderBy(x => x.EmployeeName).ToListAsync();
            return Results.Ok(items.Select(PayrollItemDto));
        });
        api.MapPut("/payroll/periods/{id:int}/items/{employeeId:int}", async Task<IResult> (int id, int employeeId,
            PayrollItemInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var amounts = new[] { input.BaseSalary, input.Allowances, input.Bonus, input.Deductions };
            if (amounts.Any(x => x < 0 || x > 1_000_000_000_000m || decimal.Round(x, 2) != x)
                || input.Deductions > input.BaseSalary + input.Allowances + input.Bonus || (input.Note ?? "").Length > 500)
                return Invalid("Số tiền phải không âm, tối đa 1.000 tỷ và 2 chữ số thập phân; khấu trừ không vượt tổng thu nhập. Ghi chú tối đa 500 ký tự.");
            await using var transaction = await Operations.BeginAsync(db);
            var period = await db.PayrollPeriods.FindAsync(id);
            if (period is null) return Results.NotFound();
            if (period.Status != "Draft") return Conflict("Chỉ được sửa bảng lương nháp. Bảng đã phát hành được khóa.");
            var employee = await db.Employees.FindAsync(employeeId);
            if (employee is null) return Invalid("Không tìm thấy nhân viên.");
            var item = await db.PayrollItems.SingleOrDefaultAsync(x => x.PeriodId == id && x.EmployeeId == employeeId);
            if (item is null)
            {
                item = new PayrollItem { PeriodId = id, EmployeeId = employeeId };
                db.PayrollItems.Add(item);
            }
            item.EmployeeName = employee.FullName; item.BaseSalary = input.BaseSalary; item.Allowances = input.Allowances;
            item.Bonus = input.Bonus; item.Deductions = input.Deductions; item.Note = (input.Note ?? "").Trim();
            Access.Log(db, ctx.User, "UpdateItem", "Payroll", $"{id}:{employeeId}");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(PayrollItemDto(item));
        });
        api.MapDelete("/payroll/periods/{id:int}/items/{employeeId:int}", async Task<IResult> (int id, int employeeId,
            HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            await using var transaction = await Operations.BeginAsync(db);
            var period = await db.PayrollPeriods.FindAsync(id);
            if (period is null) return Results.NotFound();
            if (period.Status != "Draft") return Conflict("Chỉ được xóa dòng lương ở kỳ nháp.");
            var item = await db.PayrollItems.SingleOrDefaultAsync(x => x.PeriodId == id && x.EmployeeId == employeeId);
            if (item is null) return Results.NotFound();
            db.PayrollItems.Remove(item);
            Access.Log(db, ctx.User, "DeleteItem", "Payroll", $"{id}:{employeeId}");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { message = "Đã xóa dòng lương." });
        });
        api.MapPost("/payroll/periods/{id:int}/approve", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            await using var transaction = await Operations.BeginAsync(db);
            var period = await db.PayrollPeriods.FindAsync(id);
            if (period is null) return Results.NotFound();
            if (period.Status != "Draft") return Conflict("Kỳ lương không còn ở trạng thái nháp.");
            if (!await db.PayrollItems.AnyAsync(x => x.PeriodId == id)) return Invalid("Cần ít nhất một dòng lương trước khi duyệt.");
            period.Status = "Approved"; period.ApprovedById = Access.EmployeeId(ctx.User);
            Access.Log(db, ctx.User, "Approve", "Payroll", id.ToString());
            var admins = await db.Accounts.Where(x => x.Role == "Admin" && x.IsActive && x.Employee!.Status != "Inactive")
                .Select(x => x.EmployeeId).ToListAsync();
            foreach (var admin in admins) Operations.Notify(db, admin, "Kỳ lương chờ phát hành", $"Kỳ {period.Month} đã được kiểm tra, sẵn sàng phát hành.", "payroll");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { period.Id, period.Status });
        });
        api.MapPost("/payroll/periods/{id:int}/reopen", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            await using var transaction = await Operations.BeginAsync(db);
            var period = await db.PayrollPeriods.FindAsync(id);
            if (period is null) return Results.NotFound();
            if (period.Status != "Approved") return Conflict("Chỉ được mở lại kỳ đã duyệt nhưng chưa phát hành.");
            period.Status = "Draft"; period.ApprovedById = null;
            Access.Log(db, ctx.User, "Reopen", "Payroll", id.ToString());
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { period.Id, period.Status });
        });
        api.MapPost("/payroll/periods/{id:int}/publish", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsAdmin(ctx.User)) return Results.Forbid();
            await using var transaction = await Operations.BeginAsync(db);
            var period = await db.PayrollPeriods.FindAsync(id);
            if (period is null) return Results.NotFound();
            if (period.Status != "Approved") return Conflict("Chỉ được phát hành kỳ đã duyệt.");
            period.Status = "Published"; period.PublishedAtUtc = DateTime.UtcNow;
            var items = await db.PayrollItems.Where(x => x.PeriodId == id).ToListAsync();
            if (items.Count == 0) return Invalid("Kỳ lương chưa có dữ liệu.");
            foreach (var item in items)
                Operations.Notify(db, item.EmployeeId, "Phiếu lương mới", $"Phiếu lương minh họa tháng {period.Month} đã được phát hành.", "payroll");
            Access.Log(db, ctx.User, "Publish", "Payroll", id.ToString());
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { period.Id, period.Status });
        });
        api.MapGet("/payslips", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var employeeId = Access.EmployeeId(ctx.User);
            var isHr = Access.IsHr(ctx.User);
            var entries = await (from i in db.PayrollItems.AsNoTracking()
                join p in db.PayrollPeriods on i.PeriodId equals p.Id
                where p.Status == "Published" && (isHr || i.EmployeeId == employeeId)
                orderby p.Month descending, i.EmployeeName
                select new { item = i, period = p }).ToListAsync();
            return Results.Ok(entries.Select(x => PayslipDto(x.item, x.period)));
        });
        api.MapGet("/payslips/{id:int}", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            var employeeId = Access.EmployeeId(ctx.User); var isHr = Access.IsHr(ctx.User);
            var entry = await (from i in db.PayrollItems.AsNoTracking()
                join p in db.PayrollPeriods on i.PeriodId equals p.Id
                where i.Id == id && p.Status == "Published" && (isHr || i.EmployeeId == employeeId)
                select new { item = i, period = p }).SingleOrDefaultAsync();
            return entry is null ? Results.NotFound() : Results.Ok(PayslipDto(entry.item, entry.period));
        });
    }
}
public record PayrollPeriodInput(string Month, string Title);
public record PayrollItemInput(decimal BaseSalary, decimal Allowances, decimal Bonus, decimal Deductions, string? Note);
