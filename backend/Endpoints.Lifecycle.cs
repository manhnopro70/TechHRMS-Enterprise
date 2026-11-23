using Microsoft.EntityFrameworkCore;

namespace TechHRMS.Api;

public static partial class OperationEndpoints
{
    private static void MapLifecycle(RouteGroupBuilder api)
    {
        api.MapGet("/offboarding", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var employees = await Operations.ScopedEmployees(db, ctx.User); var ids = employees.Select(x => x.Id);
            var cases = await (from c in db.OffboardingCases.AsNoTracking()
                join e in db.Employees on c.EmployeeId equals e.Id
                where ids.Contains(c.EmployeeId)
                orderby c.Id descending
                select new { item = c, employee = e.FullName }).ToListAsync();
            var caseIds = cases.Select(x => x.item.Id).ToList();
            var tasks = await db.OffboardingTasks.AsNoTracking().Where(x => caseIds.Contains(x.CaseId)).OrderBy(x => x.Id).ToListAsync();
            return Results.Ok(cases.Select(x => new { x.item.Id, x.item.EmployeeId, x.employee, x.item.LastWorkingDate,
                x.item.Reason, x.item.Status, x.item.CompletedAtUtc,
                checklist = tasks.Where(t => t.CaseId == x.item.Id).Select(t => new { t.Id, t.Title, t.Done }) }));
        });
        api.MapPost("/offboarding", async Task<IResult> (OffboardingInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var reason = (input.Reason ?? "").Trim();
            if (reason.Length is < 5 or > 500 || input.LastWorkingDate.Year is < 2000 or > 2100)
                return Invalid("Lý do cần từ 5–500 ký tự và ngày làm việc cuối hợp lệ.");
            if (input.EmployeeId == Access.EmployeeId(ctx.User)) return Invalid("Nhờ người quản trị khác lập quy trình nghỉ việc của bạn.");
            await using var transaction = await Operations.BeginAsync(db);
            var employee = await db.Employees.FindAsync(input.EmployeeId);
            if (employee is null) return Results.NotFound();
            if (employee.Status == "Inactive") return Invalid("Nhân viên đã ngừng làm việc.");
            if (!Access.IsAdmin(ctx.User) && await db.Accounts.AnyAsync(x => x.EmployeeId == input.EmployeeId && x.Role == "Admin"))
                return Results.Forbid();
            if (await db.OffboardingCases.AnyAsync(x => x.EmployeeId == input.EmployeeId)) return Conflict("Nhân viên đã có hồ sơ offboarding.");
            var item = new OffboardingCase { EmployeeId = input.EmployeeId, LastWorkingDate = input.LastWorkingDate, Reason = reason };
            db.OffboardingCases.Add(item); await db.SaveChangesAsync();
            db.OffboardingTasks.AddRange(
                new OffboardingTask { CaseId = item.Id, Title = "Xác nhận bàn giao công việc và tài liệu" },
                new OffboardingTask { CaseId = item.Id, Title = "Xác nhận bàn giao thiết bị, quyền truy cập ngoài hệ thống" },
                new OffboardingTask { CaseId = item.Id, Title = "HR xác nhận công, phép và các nghĩa vụ còn lại" });
            Operations.Notify(db, input.EmployeeId, "Checklist bàn giao", "HR đã mở quy trình bàn giao trước khi nghỉ việc.", "offboarding");
            Access.Log(db, ctx.User, "Start", "Offboarding", input.EmployeeId.ToString());
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { item.Id });
        });
        api.MapPut("/offboarding/{id:int}/tasks/{taskId:int}", async Task<IResult> (int id, int taskId,
            OffboardingTaskInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            await using var transaction = await Operations.BeginAsync(db);
            var item = await db.OffboardingCases.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.EmployeeId == Access.EmployeeId(ctx.User)) return Invalid("Không tự xác nhận bàn giao của mình.");
            if (!Access.IsAdmin(ctx.User) && await db.Accounts.AnyAsync(x => x.EmployeeId == item.EmployeeId && x.Role == "Admin"))
                return Results.Forbid();
            if (item.Status != "InProgress") return Conflict("Hồ sơ bàn giao đã hoàn tất.");
            var task = await db.OffboardingTasks.SingleOrDefaultAsync(x => x.Id == taskId && x.CaseId == id);
            if (task is null) return Results.NotFound();
            task.Done = input.Done;
            Access.Log(db, ctx.User, input.Done ? "CompleteTask" : "ReopenTask", "Offboarding", $"{id}:{taskId}");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { task.Id, task.Done });
        });
        api.MapPost("/offboarding/{id:int}/complete", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            await using var transaction = await Operations.BeginAsync(db);
            await AccountRules.LockWritesAsync(db);
            var item = await db.OffboardingCases.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.EmployeeId == Access.EmployeeId(ctx.User)) return Invalid("Không được tự khóa tài khoản bằng quy trình offboarding.");
            if (item.Status != "InProgress") return Conflict("Hồ sơ đã hoàn tất.");
            if (!Access.IsAdmin(ctx.User) && await db.Accounts.AnyAsync(x => x.EmployeeId == item.EmployeeId && x.Role == "Admin"))
                return Results.Forbid();
            var zoneId = ctx.RequestServices.GetRequiredService<IConfiguration>()["HRMS_TIME_ZONE"] ?? "Asia/Ho_Chi_Minh";
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, zoneId));
            if (item.LastWorkingDate > today) return Invalid("Chỉ hoàn tất từ ngày làm việc cuối cùng trở đi.");
            if (!await db.OffboardingTasks.AnyAsync(x => x.CaseId == id) || await db.OffboardingTasks.AnyAsync(x => x.CaseId == id && !x.Done))
                return Invalid("Cần xác nhận hoàn tất tất cả mục bàn giao trước.");
            if (!await AccountRules.CanDeactivateAsync(db, item.EmployeeId)) return Invalid("Không thể ngừng hoạt động quản trị viên cuối cùng.");
            var employee = await db.Employees.FindAsync(item.EmployeeId);
            if (employee is null) return Results.NotFound();
            employee.Status = "Inactive"; employee.IsBench = false;
            var accounts = await db.Accounts.Where(x => x.EmployeeId == item.EmployeeId).ToListAsync();
            foreach (var account in accounts) { account.IsActive = false; account.SecurityStamp = Guid.NewGuid().ToString("N"); }
            var assets = await db.Assets.Where(x => x.AssignedToEmployeeId == item.EmployeeId).ToListAsync();
            foreach (var asset in assets) { asset.AssignedToEmployeeId = null; asset.Status = "Available"; }
            var allocations = await db.Allocations.Where(x => x.EmployeeId == item.EmployeeId).ToListAsync();
            db.Allocations.RemoveRange(allocations);
            var pendingRequests = await db.WorkRequests.Where(x => x.EmployeeId == item.EmployeeId && x.Status == "Pending").ToListAsync();
            foreach (var request in pendingRequests) { request.Status = "Cancelled"; request.ReviewNote = "Đóng trong quy trình offboarding."; }
            item.Status = "Completed"; item.CompletedAtUtc = DateTime.UtcNow;
            Access.Log(db, ctx.User, "Complete", "Offboarding", $"Employee:{item.EmployeeId};Assets:{assets.Count};Allocations:{allocations.Count};Accounts:{accounts.Count}");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { item.Id, item.Status, releasedAssets = assets.Count, releasedAllocations = allocations.Count,
                message = "Đã khóa tài khoản HRM, thu hồi phân bổ và cập nhật tài sản đã bàn giao." });
        });
        api.MapPost("/candidates/{id:int}/hire", async Task<IResult> (int id, HireCandidateInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var title = (input.JobTitle ?? "").Trim();
            if (title.Length is < 2 or > 120 || input.WorkMode is not ("Hybrid" or "Onsite" or "Remote")
                || (input.JoinedAt.HasValue && input.JoinedAt.Value.Year is < 2000 or > 2100)) return Invalid("Chức danh, hình thức hoặc ngày vào làm không hợp lệ.");
            await using var transaction = await Operations.BeginAsync(db);
            var candidate = await db.Candidates.FindAsync(id);
            if (candidate is null) return Results.NotFound();
            if (candidate.Stage is not ("Offer" or "Hired")) return Invalid("Ứng viên cần ở vòng Offer hoặc Hired trước khi chuyển thành nhân viên.");
            if (await db.CandidateHires.AnyAsync(x => x.CandidateId == id)) return Conflict("Ứng viên đã được chuyển thành nhân viên.");
            if (!await db.Departments.AnyAsync(x => x.Id == input.DepartmentId)) return Invalid("Phòng ban không tồn tại.");
            var email = candidate.Email.Trim().ToLowerInvariant();
            if (await db.Employees.AnyAsync(x => x.Email == email) || await db.Accounts.AnyAsync(x => x.Email == email))
                return Conflict("Email đã thuộc hồ sơ/tài khoản hiện có. Không tạo hồ sơ trùng.");
            var employee = new Employee { FullName = candidate.FullName, Email = email, JobTitle = title,
                DepartmentId = input.DepartmentId, WorkMode = input.WorkMode, JoinedAt = input.JoinedAt ?? DateOnly.FromDateTime(DateTime.UtcNow) };
            db.Employees.Add(employee); await db.SaveChangesAsync();
            db.CandidateHires.Add(new CandidateHire { CandidateId = id, EmployeeId = employee.Id });
            candidate.Stage = "Hired";
            db.OnboardingTasks.AddRange(
                new OnboardingTask { EmployeeId = employee.Id, Title = "Hoàn thiện thông tin hồ sơ và đọc nội quy", Category = "HR", OwnerRole = "Employee", DueDate = employee.JoinedAt.AddDays(5) },
                new OnboardingTask { EmployeeId = employee.Id, Title = "Kiểm tra hồ sơ và cấp tài khoản HRM", Category = "HR", OwnerRole = "HR", DueDate = employee.JoinedAt },
                new OnboardingTask { EmployeeId = employee.Id, Title = "Bàn giao thiết bị và kiểm tra quyền truy cập", Category = "IT", OwnerRole = "HR", DueDate = employee.JoinedAt });
            Access.Log(db, ctx.User, "Hire", "Candidate", $"{id}:Employee:{employee.Id}");
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { employeeId = employee.Id, message = "Đã tạo hồ sơ và checklist onboarding. Admin có thể cấp tài khoản đăng nhập." });
        });
    }
}
public record OffboardingInput(int EmployeeId, DateOnly LastWorkingDate, string Reason);
public record OffboardingTaskInput(bool Done);
public record HireCandidateInput(int DepartmentId, string JobTitle, string WorkMode, DateOnly? JoinedAt = null);
