using Microsoft.EntityFrameworkCore;

namespace TechHRMS.Api;

public static partial class HrEndpoints
{
    private static readonly HashSet<string> ContractTypes = ["Probation", "FixedTerm", "Indefinite", "Internship", "Contractor"];
    private static readonly HashSet<string> ContractStatuses = ["Draft", "Active", "Expired", "Terminated"];
    private static readonly HashSet<string> OnboardingCategories = ["HR", "IT", "Training", "Operations"];
    private static readonly HashSet<string> TaskRoles = ["Employee", "Manager", "HR"];
    private static readonly HashSet<string> WorkModes = ["Onsite", "Remote", "Hybrid"];
    private static readonly HashSet<string> CourseCategories = ["Technical", "SoftSkill", "Compliance", "Leadership"];

    private static void MapEnterprise(RouteGroupBuilder api)
    {
        // A clear organization view without showing sensitive personal information.
        api.MapGet("/organization", async (HrDb db) => Results.Ok(await db.Departments.AsNoTracking()
            .Select(d => new { d.Id, d.Code, d.Name,
                total = db.Employees.Count(e => e.DepartmentId == d.Id),
                active = db.Employees.Count(e => e.DepartmentId == d.Id && e.Status == "Active"),
                remote = db.Employees.Count(e => e.DepartmentId == d.Id && e.WorkMode == "Remote") })
            .OrderBy(x => x.Name).ToListAsync()));

        api.MapGet("/contracts", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var query = db.EmploymentContracts.AsNoTracking().AsQueryable();
            if (!Access.IsHr(ctx.User)) query = query.Where(x => x.EmployeeId == myId);
            var list = await (from c in query join e in db.Employees on c.EmployeeId equals e.Id
                orderby c.StartDate descending
                select new { c.Id, c.EmployeeId, employee = e.FullName, c.ContractCode,
                    c.ContractType, c.StartDate, c.EndDate, c.Status, c.Notes }).ToListAsync();
            return Results.Ok(list);
        });
        api.MapPost("/contracts", async Task<IResult> (ContractCreate input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var code = (input.ContractCode ?? "").Trim().ToUpperInvariant();
            var note = (input.Notes ?? "").Trim();
            if (code.Length is < 4 or > 48 || note.Length > 500 || !ContractTypes.Contains(input.ContractType)
                || !ContractStatuses.Contains(input.Status) || input.StartDate.Year < 2000
                || (input.EndDate.HasValue && input.EndDate < input.StartDate)) return Bad("Thông tin hợp đồng không hợp lệ.");
            if (!await db.Employees.AnyAsync(x => x.Id == input.EmployeeId)) return Bad("Nhân viên không tồn tại.");
            if (await db.EmploymentContracts.AnyAsync(x => x.ContractCode == code)) return Bad("Mã hợp đồng đã tồn tại.");
            var item = new EmploymentContract { EmployeeId = input.EmployeeId, ContractCode = code,
                ContractType = input.ContractType, StartDate = input.StartDate, EndDate = input.EndDate, Status = input.Status, Notes = note };
            db.EmploymentContracts.Add(item);
            Access.Log(db, ctx.User, "Create", "Contract", code);
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });
        api.MapPut("/contracts/{id:int}/status", async Task<IResult> (int id, ContractStatusInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            if (!ContractStatuses.Contains(input.Status)) return Bad("Trạng thái hợp đồng không hợp lệ.");
            var item = await db.EmploymentContracts.FindAsync(id);
            if (item is null) return Results.NotFound();
            item.Status = input.Status;
            Access.Log(db, ctx.User, "UpdateStatus", "Contract", item.ContractCode);
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id, item.Status });
        });

        api.MapGet("/onboarding", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var query = db.OnboardingTasks.AsNoTracking().AsQueryable();
            if (Access.IsManager(ctx.User))
            {
                var dept = await db.Employees.Where(x => x.Id == myId).Select(x => x.DepartmentId).FirstOrDefaultAsync();
                query = query.Where(x => db.Employees.Any(e => e.Id == x.EmployeeId && e.DepartmentId == dept));
            }
            else if (!Access.IsHr(ctx.User)) query = query.Where(x => x.EmployeeId == myId);
            return Results.Ok(await (from t in query join e in db.Employees on t.EmployeeId equals e.Id
                orderby t.Status, t.DueDate
                select new { t.Id, t.EmployeeId, employee = e.FullName, t.Title, t.Category,
                    t.OwnerRole, t.DueDate, t.Status, t.CompletedAtUtc }).ToListAsync());
        });
        api.MapPost("/onboarding", async Task<IResult> (OnboardingCreate input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var title = (input.Title ?? "").Trim();
            if (title.Length is < 5 or > 200 || !OnboardingCategories.Contains(input.Category)
                || !TaskRoles.Contains(input.OwnerRole)) return Bad("Thông tin checklist không hợp lệ.");
            if (!await db.Employees.AnyAsync(x => x.Id == input.EmployeeId)) return Bad("Không tìm thấy nhân viên.");
            var item = new OnboardingTask { EmployeeId = input.EmployeeId, Title = title,
                Category = input.Category, OwnerRole = input.OwnerRole, DueDate = input.DueDate };
            db.OnboardingTasks.Add(item);
            Access.Log(db, ctx.User, "Create", "Onboarding", title);
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });
        api.MapPut("/onboarding/{id:int}/status", async Task<IResult> (int id, TaskStatusInput input, HttpContext ctx, HrDb db) =>
        {
            if (input.Status is not ("Pending" or "Done")) return Bad("Trạng thái không hợp lệ.");
            var item = await db.OnboardingTasks.FindAsync(id);
            if (item is null) return Results.NotFound();
            var actor = Access.EmployeeId(ctx.User);
            var allowed = Access.IsHr(ctx.User)
                || (item.OwnerRole == "Employee" && item.EmployeeId == actor)
                || (item.OwnerRole == "Manager" && Access.IsManager(ctx.User)
                    && await Access.CanManageEmployee(db, ctx.User, item.EmployeeId));
            if (!allowed) return Results.Forbid();
            item.Status = input.Status;
            item.CompletedAtUtc = input.Status == "Done" ? DateTime.UtcNow : null;
            Access.Log(db, ctx.User, "UpdateStatus", "Onboarding", id.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id, item.Status });
        });

        api.MapGet("/attendance", async Task<IResult> (HttpContext ctx, HrDb db, string? month) =>
        {
            var now = DateOnly.FromDateTime(DateTime.UtcNow);
            if (string.IsNullOrWhiteSpace(month)) month = now.ToString("yyyy-MM");
            if (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var from)) return Bad("Tháng phải có dạng YYYY-MM.");
            var until = from.AddMonths(1);
            var myId = Access.EmployeeId(ctx.User);
            var query = db.TimeEntries.AsNoTracking().Where(x => x.WorkDate >= from && x.WorkDate < until);
            if (Access.IsManager(ctx.User))
            {
                var dept = await db.Employees.Where(x => x.Id == myId).Select(x => x.DepartmentId).FirstOrDefaultAsync();
                query = query.Where(x => db.Employees.Any(e => e.Id == x.EmployeeId && e.DepartmentId == dept));
            }
            else if (!Access.IsHr(ctx.User)) query = query.Where(x => x.EmployeeId == myId);
            return Results.Ok(await (from t in query join e in db.Employees on t.EmployeeId equals e.Id
                orderby t.WorkDate descending, t.Id descending
                select new { t.Id, t.EmployeeId, employee = e.FullName, t.WorkDate,
                    t.Mode, t.CheckInUtc, t.CheckOutUtc, t.Note }).Take(400).ToListAsync());
        });
        api.MapPost("/attendance/check-in", async Task<IResult> (CheckInInput input, HttpContext ctx, HrDb db) =>
        {
            if (!WorkModes.Contains(input.Mode)) return Bad("Hình thức làm việc không hợp lệ.");
            var note = (input.Note ?? "").Trim();
            if (note.Length > 240) return Bad("Ghi chú không được vượt 240 ký tự.");
            var actor = Access.EmployeeId(ctx.User);
            if (!await db.Employees.AnyAsync(e => e.Id == actor && e.Status == "Active")) return Results.Forbid();
            var timeZoneId = ctx.RequestServices.GetRequiredService<IConfiguration>()["HRMS_TIME_ZONE"] ?? "Asia/Ho_Chi_Minh";
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
            catch (TimeZoneNotFoundException) { return Results.Problem("Múi giờ hệ thống chưa được cấu hình đúng.", statusCode: 500); }
            var nowUtc = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone));
            if (await db.TimeEntries.AnyAsync(x => x.EmployeeId == actor && x.WorkDate == today))
                return Bad("Bạn đã check-in hôm nay rồi.");
            var entry = new TimeEntry { EmployeeId = actor, WorkDate = today, CheckInUtc = nowUtc,
                Mode = input.Mode, Note = note };
            db.TimeEntries.Add(entry);
            Access.Log(db, ctx.User, "CheckIn", "Attendance", today.ToString("yyyy-MM-dd"));
            await db.SaveChangesAsync();
            return Results.Ok(new { entry.Id, entry.WorkDate, entry.CheckInUtc });
        });
        api.MapPost("/attendance/check-out", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var actor = Access.EmployeeId(ctx.User);
            var item = await db.TimeEntries.Where(x => x.EmployeeId == actor && x.CheckOutUtc == null)
                .OrderByDescending(x => x.CheckInUtc).FirstOrDefaultAsync();
            if (item is null) return Bad("Không tìm thấy phiên làm việc chưa check-out.");
            item.CheckOutUtc = DateTime.UtcNow;
            Access.Log(db, ctx.User, "CheckOut", "Attendance", item.Id.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id, item.CheckOutUtc });
        });

        api.MapGet("/courses", async (HrDb db) => Results.Ok(await db.TrainingCourses.AsNoTracking()
            .OrderBy(x => x.Title).Select(x => new { x.Id, x.Code, x.Title,
                x.Category, x.DurationHours, x.Status }).ToListAsync()));
        api.MapPost("/courses", async Task<IResult> (CourseCreate input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var title = (input.Title ?? "").Trim();
            var code = (input.Code ?? "").Trim().ToUpperInvariant();
            if (title.Length is < 5 or > 160 || code.Length is < 3 or > 32 || !CourseCategories.Contains(input.Category)
                || input.DurationHours is < 1 or > 500) return Bad("Thông tin khóa học không hợp lệ.");
            if (await db.TrainingCourses.AnyAsync(x => x.Code == code)) return Bad("Mã khóa học đã tồn tại.");
            var item = new TrainingCourse { Code = code, Title = title,
                Category = input.Category, DurationHours = input.DurationHours };
            db.TrainingCourses.Add(item);
            Access.Log(db, ctx.User, "Create", "Training", code);
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });
        api.MapGet("/enrollments", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var query = db.TrainingEnrollments.AsNoTracking().AsQueryable();
            if (!Access.IsHr(ctx.User)) query = query.Where(x => x.EmployeeId == myId);
            return Results.Ok(await (from e in query join c in db.TrainingCourses on e.CourseId equals c.Id
                join person in db.Employees on e.EmployeeId equals person.Id
                orderby e.EnrolledAtUtc descending
                select new { e.Id, e.CourseId, course = c.Title, e.EmployeeId, employee = person.FullName,
                    e.Status, e.EnrolledAtUtc, e.CompletedAtUtc }).ToListAsync());
        });
        api.MapPost("/courses/{id:int}/enroll", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            if (!await db.TrainingCourses.AnyAsync(x => x.Id == id && x.Status == "Published")) return Results.NotFound();
            var actor = Access.EmployeeId(ctx.User);
            if (await db.TrainingEnrollments.AnyAsync(x => x.CourseId == id && x.EmployeeId == actor))
                return Bad("Bạn đã đăng ký khóa học này rồi.");
            db.TrainingEnrollments.Add(new TrainingEnrollment { CourseId = id, EmployeeId = actor });
            Access.Log(db, ctx.User, "Enroll", "Training", id.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Đăng ký khóa học thành công." });
        });
        api.MapPut("/enrollments/{id:int}/status", async Task<IResult> (int id, EnrollmentStatusInput input, HttpContext ctx, HrDb db) =>
        {
            if (input.Status is not ("Enrolled" or "InProgress" or "Completed")) return Bad("Trạng thái không hợp lệ.");
            var item = await db.TrainingEnrollments.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.EmployeeId != Access.EmployeeId(ctx.User) && !Access.IsHr(ctx.User)) return Results.Forbid();
            item.Status = input.Status;
            item.CompletedAtUtc = input.Status == "Completed" ? DateTime.UtcNow : null;
            Access.Log(db, ctx.User, "UpdateStatus", "Training", id.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id, item.Status });
        });

        api.MapGet("/insights", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            if (!Access.IsHr(ctx.User) && !Access.IsManager(ctx.User)) return Results.Forbid();
            var workers = db.Employees.AsNoTracking().AsQueryable();
            if (Access.IsManager(ctx.User))
            {
                var dept = await workers.Where(x => x.Id == myId).Select(x => x.DepartmentId).FirstOrDefaultAsync();
                workers = workers.Where(x => x.DepartmentId == dept);
            }
            var ids = workers.Select(x => x.Id);
            var now = DateOnly.FromDateTime(DateTime.UtcNow);
            var from = now.AddDays(30);
            var monthStart = new DateOnly(now.Year, now.Month, 1);
            var nextMonth = monthStart.AddMonths(1);
            return Results.Ok(new
            {
                headcount = await workers.CountAsync(),
                active = await workers.CountAsync(x => x.Status == "Active"),
                hybrid = await workers.CountAsync(x => x.WorkMode == "Hybrid"),
                remote = await workers.CountAsync(x => x.WorkMode == "Remote"),
                onsite = await workers.CountAsync(x => x.WorkMode == "Onsite"),
                bench = await workers.CountAsync(x => x.IsBench),
                openRequests = await db.WorkRequests.CountAsync(x => ids.Contains(x.EmployeeId) && x.Status == "Pending"),
                attendanceThisMonth = await db.TimeEntries.CountAsync(x => ids.Contains(x.EmployeeId) && x.WorkDate >= monthStart && x.WorkDate < nextMonth),
                certificates = await db.Skills.CountAsync(x => ids.Contains(x.EmployeeId) && x.Certificate != null && x.Certificate != ""),
                expiringContracts = Access.IsHr(ctx.User)
                    ? await db.EmploymentContracts.CountAsync(x => x.Status == "Active" && x.EndDate != null && x.EndDate >= now && x.EndDate <= from) : 0,
                departments = await workers.GroupBy(x => x.Department!.Name)
                    .Select(g => new { name = g.Key, value = g.Count() }).OrderByDescending(x => x.value).ToListAsync()
            });
        });
    }
}

public record ContractCreate(int EmployeeId, string ContractCode, string ContractType, DateOnly StartDate, DateOnly? EndDate, string Status, string? Notes);
public record ContractStatusInput(string Status);
public record OnboardingCreate(int EmployeeId, string Title, string Category, string OwnerRole, DateOnly? DueDate);
public record TaskStatusInput(string Status);
public record CheckInInput(string Mode, string? Note);
public record CourseCreate(string Code, string Title, string Category, int DurationHours);
public record EnrollmentStatusInput(string Status);
