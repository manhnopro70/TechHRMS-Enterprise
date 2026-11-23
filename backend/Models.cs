using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace TechHRMS.Api;

public partial class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}

public partial class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string WorkMode { get; set; } = "Hybrid";
    public string Status { get; set; } = "Active";
    public bool IsBench { get; set; }
    public DateOnly JoinedAt { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}

public partial class Account
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Employee";
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
}

public partial class WorkRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Type { get; set; } = "Leave";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int? ReviewedById { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public partial class Skill
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Name { get; set; } = "";
    public string Level { get; set; } = "Beginner";
    public string? Certificate { get; set; }
}

public partial class ItAsset
{
    public int Id { get; set; }
    public string AssetTag { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Available";
    public int? AssignedToEmployeeId { get; set; }
    public Employee? AssignedToEmployee { get; set; }
}

public partial class TechProject
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Active";
}

public partial class ProjectAllocation
{
    public int Id { get; set; }
    public int TechProjectId { get; set; }
    public TechProject? TechProject { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int Percent { get; set; }
}

public partial class Candidate
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Position { get; set; } = "";
    public string Stage { get; set; } = "Applied";
    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
}

public partial class Objective
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Title { get; set; } = "";
    public string Quarter { get; set; } = "Q1";
    public int Progress { get; set; }
}

public partial class AuditLog
{
    public int Id { get; set; }
    public int ActorEmployeeId { get; set; }
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
}


// V3: separate operational tables; no sensitive salary, identity-document or banking fields are stored.
public partial class EmploymentContract
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string ContractCode { get; set; } = "";
    public string ContractType { get; set; } = "FixedTerm";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string Notes { get; set; } = "";
}

public partial class OnboardingTask
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "HR";
    public string OwnerRole { get; set; } = "HR";
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? CompletedAtUtc { get; set; }
}

public partial class TimeEntry
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public string Mode { get; set; } = "Onsite";
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public string Note { get; set; } = "";
}

public partial class TrainingCourse
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "Technical";
    public int DurationHours { get; set; }
    public string Status { get; set; } = "Published";
}

public partial class TrainingEnrollment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int EmployeeId { get; set; }
    public string Status { get; set; } = "Enrolled";
    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public partial class HrDb(DbContextOptions<HrDb> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<WorkRequest> WorkRequests => Set<WorkRequest>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<ItAsset> Assets => Set<ItAsset>();
    public DbSet<TechProject> Projects => Set<TechProject>();
    public DbSet<ProjectAllocation> Allocations => Set<ProjectAllocation>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Objective> Objectives => Set<Objective>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EmploymentContract> EmploymentContracts => Set<EmploymentContract>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<TrainingCourse> TrainingCourses => Set<TrainingCourse>();
    public DbSet<TrainingEnrollment> TrainingEnrollments => Set<TrainingEnrollment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<EmploymentContract>().HasIndex(x => x.ContractCode).IsUnique();
        builder.Entity<TimeEntry>().HasIndex(x => new { x.EmployeeId, x.WorkDate }).IsUnique();
        builder.Entity<TrainingCourse>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<TrainingEnrollment>().HasIndex(x => new { x.CourseId, x.EmployeeId }).IsUnique();
        builder.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<Employee>().HasIndex(x => x.Email).IsUnique();
        builder.Entity<Account>().HasIndex(x => x.Email).IsUnique();
        builder.Entity<Account>().HasIndex(x => x.EmployeeId).HasDatabaseName("UX_Accounts_EmployeeId").IsUnique();
        builder.Entity<ItAsset>().HasIndex(x => x.AssetTag).IsUnique();
        builder.Entity<TechProject>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<ProjectAllocation>().HasIndex(x => new { x.TechProjectId, x.EmployeeId }).IsUnique();
        builder.Entity<Account>().HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ItAsset>().HasOne(x => x.AssignedToEmployee).WithMany().HasForeignKey(x => x.AssignedToEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProjectAllocation>().HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProjectAllocation>().HasOne(x => x.TechProject).WithMany().HasForeignKey(x => x.TechProjectId).OnDelete(DeleteBehavior.Cascade);
        ConfigureOperations(builder);
    }
    partial void ConfigureOperations(ModelBuilder builder);
}
