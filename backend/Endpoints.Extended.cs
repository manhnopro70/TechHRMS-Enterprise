using Microsoft.EntityFrameworkCore;

namespace TechHRMS.Api;

public static partial class HrEndpoints
{
    private static void MapExtended(RouteGroupBuilder api)
    {
        api.MapGet("/assets", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var query = db.Assets.Include(x => x.AssignedToEmployee).AsQueryable();
            if (Access.IsManager(ctx.User))
            {
                var dept = await db.Employees.Where(x => x.Id == myId).Select(x => x.DepartmentId).FirstOrDefaultAsync();
                query = query.Where(x => x.AssignedToEmployee != null && x.AssignedToEmployee.DepartmentId == dept);
            }
            else if (!Access.IsHr(ctx.User)) query = query.Where(x => x.AssignedToEmployeeId == myId);
            return Results.Ok(await query.OrderBy(x => x.AssetTag).Select(x => new { x.Id, x.AssetTag, x.Name, x.Status,
                x.AssignedToEmployeeId, assignedTo = x.AssignedToEmployee == null ? "" : x.AssignedToEmployee.FullName }).ToListAsync());
        });

        api.MapPost("/assets", async Task<IResult> (AssetInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var tag = (input.AssetTag ?? "").Trim().ToUpperInvariant();
            var name = (input.Name ?? "").Trim();
            if (tag.Length is < 3 or > 30 || name.Length is < 3 or > 120) return Bad("Thông tin tài sản không hợp lệ.");
            if (await db.Assets.AnyAsync(x => x.AssetTag == tag)) return Bad("Mã tài sản đã tồn tại.");
            if (input.AssignedToEmployeeId.HasValue && !await db.Employees.AnyAsync(x => x.Id == input.AssignedToEmployeeId)) return Bad("Nhân viên không tồn tại.");
            var item = new ItAsset { AssetTag = tag, Name = name, AssignedToEmployeeId = input.AssignedToEmployeeId,
                Status = input.AssignedToEmployeeId.HasValue ? "Assigned" : "Available" };
            db.Assets.Add(item);
            Access.Log(db, ctx.User, "Create", "Asset", tag);
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });

        api.MapPut("/assets/{id:int}/assign", async Task<IResult> (int id, AssignAssetInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var item = await db.Assets.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (input.EmployeeId.HasValue && !await db.Employees.AnyAsync(x => x.Id == input.EmployeeId && x.Status == "Active"))
                return Bad("Không tìm thấy nhân viên đang hoạt động.");
            item.AssignedToEmployeeId = input.EmployeeId;
            item.Status = input.EmployeeId.HasValue ? "Assigned" : "Available";
            Access.Log(db, ctx.User, "Assign", "Asset", id.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });

        api.MapGet("/projects", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var query = db.Projects.AsQueryable();
            if (!Access.IsHr(ctx.User) && !Access.IsManager(ctx.User))
                query = query.Where(p => db.Allocations.Any(a => a.TechProjectId == p.Id && a.EmployeeId == myId));
            return Results.Ok(await query.OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name, x.Status,
                allocationCount = db.Allocations.Count(a => a.TechProjectId == x.Id) }).ToListAsync());
        });

        api.MapPost("/projects", async Task<IResult> (ProjectInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User) && !Access.IsManager(ctx.User)) return Results.Forbid();
            var code = (input.Code ?? "").Trim().ToUpperInvariant();
            var name = (input.Name ?? "").Trim();
            if (code.Length is < 3 or > 30 || name.Length is < 3 or > 120) return Bad("Thông tin dự án không hợp lệ.");
            if (await db.Projects.AnyAsync(x => x.Code == code)) return Bad("Mã dự án đã tồn tại.");
            var project = new TechProject { Code = code, Name = name, Status = "Active" };
            db.Projects.Add(project);
            Access.Log(db, ctx.User, "Create", "Project", code);
            await db.SaveChangesAsync();
            return Results.Ok(new { project.Id });
        });

        api.MapGet("/allocations", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var query = db.Allocations.Include(x => x.Employee).Include(x => x.TechProject).AsQueryable();
            if (Access.IsManager(ctx.User))
            {
                var dept = await db.Employees.Where(x => x.Id == myId).Select(x => x.DepartmentId).FirstOrDefaultAsync();
                query = query.Where(x => x.Employee!.DepartmentId == dept);
            }
            else if (!Access.IsHr(ctx.User)) query = query.Where(x => x.EmployeeId == myId);
            return Results.Ok(await query.OrderBy(x => x.TechProject!.Code)
                .Select(x => new { x.Id, x.EmployeeId, employee = x.Employee!.FullName,
                    x.TechProjectId, project = x.TechProject!.Name, x.Percent }).ToListAsync());
        });

        api.MapPost("/allocations", async Task<IResult> (AllocationInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User) && !Access.IsManager(ctx.User)) return Results.Forbid();
            if (input.Percent is < 1 or > 100) return Bad("Tỷ lệ phân bổ từ 1 đến 100%.");
            if (!await db.Projects.AnyAsync(x => x.Id == input.TechProjectId)) return Bad("Dự án không tồn tại.");
            if (!await db.Employees.AnyAsync(x => x.Id == input.EmployeeId && x.Status == "Active")) return Bad("Nhân viên không hợp lệ.");
            if (Access.IsManager(ctx.User) && !await Access.CanManageEmployee(db, ctx.User, input.EmployeeId))
                return Results.Forbid();
            if (await db.Allocations.AnyAsync(x => x.EmployeeId == input.EmployeeId && x.TechProjectId == input.TechProjectId))
                return Bad("Nhân viên đã được phân bổ vào dự án này.");
            var percent = await db.Allocations.Where(x => x.EmployeeId == input.EmployeeId).SumAsync(x => (int?)x.Percent) ?? 0;
            if (percent + input.Percent > 100) return Bad("Tổng phân bổ không được vượt quá 100%.");
            db.Allocations.Add(new ProjectAllocation { EmployeeId = input.EmployeeId, TechProjectId = input.TechProjectId, Percent = input.Percent });
            Access.Log(db, ctx.User, "Allocate", "Project", $"{input.EmployeeId}:{input.TechProjectId}");
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Đã phân bổ nhân sự." });
        });

        api.MapDelete("/allocations/{id:int}", async Task<IResult> (int id, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User) && !Access.IsManager(ctx.User)) return Results.Forbid();
            var item = await db.Allocations.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (Access.IsManager(ctx.User) && !await Access.CanManageEmployee(db, ctx.User, item.EmployeeId))
                return Results.Forbid();
            db.Allocations.Remove(item);
            Access.Log(db, ctx.User, "Remove", "Allocation", id.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Đã thu hồi phân bổ." });
        });

        api.MapGet("/candidates", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            return Results.Ok(await db.Candidates.OrderByDescending(x => x.AppliedAtUtc)
                .Select(x => new { x.Id, x.FullName, x.Email, x.Position, x.Stage, x.AppliedAtUtc }).ToListAsync());
        });

        api.MapPost("/candidates", async Task<IResult> (CandidateInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            var name = (input.FullName ?? "").Trim();
            var email = (input.Email ?? "").Trim().ToLowerInvariant();
            var position = (input.Position ?? "").Trim();
            if (name.Length is < 2 or > 120 || !LooksLikeEmail(email) || position.Length is < 2 or > 120)
                return Bad("Thông tin ứng viên không hợp lệ.");
            var item = new Candidate { FullName = name, Email = email, Position = position };
            db.Candidates.Add(item);
            Access.Log(db, ctx.User, "Create", "Candidate", email);
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });

        api.MapPut("/candidates/{id:int}/stage", async Task<IResult> (int id, StageInput input, HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsHr(ctx.User)) return Results.Forbid();
            if (!AllowedStages.Contains(input.Stage)) return Bad("Vòng tuyển dụng không hợp lệ.");
            var item = await db.Candidates.FindAsync(id);
            if (item is null) return Results.NotFound();
            item.Stage = input.Stage;
            Access.Log(db, ctx.User, "UpdateStage", "Candidate", id.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id });
        });

        api.MapGet("/objectives", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            var myId = Access.EmployeeId(ctx.User);
            var query = db.Objectives.Include(x => x.Employee).AsQueryable();
            if (Access.IsManager(ctx.User))
            {
                var dept = await db.Employees.Where(x => x.Id == myId).Select(x => x.DepartmentId).FirstOrDefaultAsync();
                query = query.Where(x => x.Employee!.DepartmentId == dept);
            }
            else if (!Access.IsHr(ctx.User)) query = query.Where(x => x.EmployeeId == myId);
            return Results.Ok(await query.OrderByDescending(x => x.Id)
                .Select(x => new { x.Id, x.EmployeeId, employee = x.Employee!.FullName, x.Title, x.Quarter, x.Progress }).ToListAsync());
        });

        api.MapPost("/objectives", async Task<IResult> (ObjectiveInput input, HttpContext ctx, HrDb db) =>
        {
            var owner = input.EmployeeId == 0 ? Access.EmployeeId(ctx.User) : input.EmployeeId;
            if (owner != Access.EmployeeId(ctx.User) && !Access.IsHr(ctx.User) && !await Access.CanManageEmployee(db, ctx.User, owner))
                return Results.Forbid();
            if (!await db.Employees.AnyAsync(x => x.Id == owner)) return Bad("Không tìm thấy nhân viên.");
            var title = (input.Title ?? "").Trim();
            var quarter = (input.Quarter ?? "").Trim();
            if (title.Length is < 5 or > 200 || quarter.Length is < 2 or > 15) return Bad("Mục tiêu/Quý không hợp lệ.");
            db.Objectives.Add(new Objective { EmployeeId = owner, Title = title, Quarter = quarter, Progress = 0 });
            Access.Log(db, ctx.User, "Create", "Objective", title);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Đã tạo OKR." });
        });

        api.MapPut("/objectives/{id:int}/progress", async Task<IResult> (int id, ProgressInput input, HttpContext ctx, HrDb db) =>
        {
            if (input.Progress is < 0 or > 100) return Bad("Tiến độ phải nằm trong 0–100%.");
            var item = await db.Objectives.FindAsync(id);
            if (item is null) return Results.NotFound();
            if (item.EmployeeId != Access.EmployeeId(ctx.User) && !Access.IsHr(ctx.User)
                && !await Access.CanManageEmployee(db, ctx.User, item.EmployeeId)) return Results.Forbid();
            item.Progress = input.Progress;
            Access.Log(db, ctx.User, "UpdateProgress", "Objective", id.ToString());
            await db.SaveChangesAsync();
            return Results.Ok(new { item.Id, item.Progress });
        });

        api.MapGet("/audit", async Task<IResult> (HttpContext ctx, HrDb db) =>
        {
            if (!Access.IsAdmin(ctx.User)) return Results.Forbid();
            var list = await db.AuditLogs.OrderByDescending(x => x.AtUtc).Take(150)
                .Select(x => new { x.Id, x.ActorEmployeeId, x.Action, x.Entity, x.Details, x.AtUtc }).ToListAsync();
            return Results.Ok(list);
        });
    }
}

public record AssetInput(string AssetTag, string Name, int? AssignedToEmployeeId);
public record AssignAssetInput(int? EmployeeId);
public record ProjectInput(string Code, string Name);
public record AllocationInput(int TechProjectId, int EmployeeId, int Percent);
public record CandidateInput(string FullName, string Email, string Position);
public record StageInput(string Stage);
public record ObjectiveInput(int EmployeeId, string Title, string Quarter);
public record ProgressInput(int Progress);
