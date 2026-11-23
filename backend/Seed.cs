using Microsoft.EntityFrameworkCore;
using System.Data;

namespace TechHRMS.Api;

public static class Seed
{
    public static async Task InitializeAsync(IServiceProvider services, IWebHostEnvironment environment, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDb>();
        // DEMO ONLY: a first-run schema bootstrap. Introduce versioned EF migrations before real production use.
        await db.Database.EnsureCreatedAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await AccountRules.LockWritesAsync(db);
        if (await db.Accounts.AnyAsync()) return;

        var isDemo = environment.IsDevelopment() || string.Equals(config["SEED_SAMPLE_DATA"], "true", StringComparison.OrdinalIgnoreCase);
        var adminEmail = (config["SEED_ADMIN_EMAIL"] ?? "admin@techhrms.local").ToLowerInvariant();
        var adminPassword = config["SEED_ADMIN_PASSWORD"];
        if (!environment.IsDevelopment() && !Passwords.IsValid(adminPassword))
            throw new InvalidOperationException("Production requires SEED_ADMIN_PASSWORD (12 to 128 characters) on first start.");

        var engineering = new Department { Code = "ENG", Name = "Engineering" };
        var product = new Department { Code = "PRD", Name = "Product & Design" };
        var people = new Department { Code = "PPL", Name = "People Operations" };
        var operations = new Department { Code = "OPS", Name = "IT Operations" };
        db.Departments.AddRange(engineering, product, people, operations);
        await db.SaveChangesAsync();

        var employees = new List<Employee>
        {
            new() { FullName = "Quản trị viên TechHRMS", Email = adminEmail, DepartmentId = people.Id, JobTitle = "System Administrator", WorkMode = "Hybrid", JoinedAt = new DateOnly(2022, 3, 1) },
        };
        if (isDemo)
        {
            employees.AddRange([
                new Employee { FullName = "Nguyễn Thu Hà", Email = "hr@techhrms.local", DepartmentId = people.Id, JobTitle = "HR Specialist", WorkMode = "Hybrid", JoinedAt = new DateOnly(2023, 2, 15) },
                new Employee { FullName = "Trần Đức Minh", Email = "manager@techhrms.local", DepartmentId = engineering.Id, JobTitle = "Engineering Manager", WorkMode = "Hybrid", JoinedAt = new DateOnly(2021, 6, 18) },
                new Employee { FullName = "Lê Hoàng Nam", Email = "employee@techhrms.local", DepartmentId = engineering.Id, JobTitle = "Frontend Developer", WorkMode = "Remote", JoinedAt = new DateOnly(2024, 1, 8) },
                new Employee { FullName = "Phạm Thùy Linh", Email = "linh@techhrms.local", DepartmentId = engineering.Id, JobTitle = "Backend Developer", WorkMode = "Hybrid", JoinedAt = new DateOnly(2023, 7, 21) },
                new Employee { FullName = "Đỗ Thanh Phong", Email = "phong@techhrms.local", DepartmentId = engineering.Id, JobTitle = "DevOps Engineer", WorkMode = "Remote", IsBench = true, JoinedAt = new DateOnly(2022, 11, 7) },
                new Employee { FullName = "Vũ Mai Anh", Email = "anh@techhrms.local", DepartmentId = product.Id, JobTitle = "UI/UX Designer", WorkMode = "Hybrid", JoinedAt = new DateOnly(2024, 4, 12) },
                new Employee { FullName = "Hoàng Quốc Bảo", Email = "bao@techhrms.local", DepartmentId = operations.Id, JobTitle = "IT Support", WorkMode = "Onsite", JoinedAt = new DateOnly(2023, 9, 4) },
            ]);
        }
        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();

        db.Accounts.Add(new Account { EmployeeId = employees[0].Id, Email = adminEmail, PasswordHash = Passwords.Hash(adminPassword ?? "LocalDemo@2026!"), Role = "Admin" });
        var demoPassword = environment.IsDevelopment() ? "LocalDemo@2026!" : config["SEED_DEMO_PASSWORD"];
        if (isDemo && Passwords.IsValid(demoPassword))
        {
            foreach (var (index, role) in new [] { (1,"HR"), (2,"Manager"), (3,"Employee") })
                db.Accounts.Add(new Account { EmployeeId = employees[index].Id, Email = employees[index].Email, PasswordHash = Passwords.Hash(demoPassword!), Role = role });
        }
        if (isDemo)
        {
            db.Skills.AddRange([
                new Skill { EmployeeId = employees[3].Id, Name = "React", Level = "Advanced", Certificate = "" },
                new Skill { EmployeeId = employees[3].Id, Name = "TypeScript", Level = "Advanced" },
                new Skill { EmployeeId = employees[4].Id, Name = "C# / .NET", Level = "Advanced" },
                new Skill { EmployeeId = employees[5].Id, Name = "Docker", Level = "Senior", Certificate = "AWS Certified" },
                new Skill { EmployeeId = employees[6].Id, Name = "Figma", Level = "Senior" },
            ]);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.WorkRequests.AddRange([
                new WorkRequest { EmployeeId = employees[3].Id, Type = "WFH", StartDate = today.AddDays(3), EndDate = today.AddDays(3), Reason = "Làm việc tại nhà" },
                new WorkRequest { EmployeeId = employees[4].Id, Type = "Leave", StartDate = today.AddDays(7), EndDate = today.AddDays(8), Reason = "Nghỉ phép cá nhân" },
                new WorkRequest { EmployeeId = employees[5].Id, Type = "OT", StartDate = today.AddDays(2), EndDate = today.AddDays(2), Reason = "Hỗ trợ đợt triển khai" },
            ]);
            db.Assets.AddRange([
                new ItAsset { AssetTag = "LAP-001", Name = "Dell Latitude 7440", Status = "Assigned", AssignedToEmployeeId = employees[3].Id },
                new ItAsset { AssetTag = "LAP-002", Name = "MacBook Pro 14", Status = "Assigned", AssignedToEmployeeId = employees[4].Id },
                new ItAsset { AssetTag = "MON-003", Name = "Dell UltraSharp 27", Status = "Available" },
            ]);
            var portal = new TechProject { Code = "PROJ-01", Name = "TechHRMS Portal", Status = "Active" };
            var mobile = new TechProject { Code = "PROJ-02", Name = "Mobile Commerce", Status = "Active" };
            db.Projects.AddRange(portal, mobile);
            await db.SaveChangesAsync();
            db.Allocations.AddRange([
                new ProjectAllocation { TechProjectId = portal.Id, EmployeeId = employees[3].Id, Percent = 70 },
                new ProjectAllocation { TechProjectId = portal.Id, EmployeeId = employees[4].Id, Percent = 80 },
                new ProjectAllocation { TechProjectId = mobile.Id, EmployeeId = employees[3].Id, Percent = 20 },
            ]);
            db.Candidates.AddRange([
                new Candidate { FullName = "Nguyễn Thanh Vy", Email = "vy@example.test", Position = "React Developer", Stage = "Applied" },
                new Candidate { FullName = "Phan Quốc Việt", Email = "viet@example.test", Position = "DevOps Engineer", Stage = "Interview" },
                new Candidate { FullName = "Mai Thu Trang", Email = "trang@example.test", Position = "QA Engineer", Stage = "TechnicalTest" },
            ]);
            db.Objectives.AddRange([
                new Objective { EmployeeId = employees[3].Id, Title = "Hoàn thành giao diện nhân sự", Quarter = "Q4-2026", Progress = 65 },
                new Objective { EmployeeId = employees[4].Id, Title = "Nâng tỷ lệ test API lên 90%", Quarter = "Q4-2026", Progress = 45 },
            ]);
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

}
