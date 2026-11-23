using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Security.Claims;

namespace TechHRMS.Api;

public class LeaveEntitlement
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Entitlement { get; set; } = 12;
}
public class WorkRequestDetail
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public decimal? Hours { get; set; }
}
public class AppNotification
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Link { get; set; } = "dashboard";
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
public class ProjectTimesheet
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int ProjectId { get; set; }
    public DateOnly WorkDate { get; set; }
    public decimal Hours { get; set; }
    public bool Billable { get; set; }
    public string Note { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public string ReviewNote { get; set; } = "";
    public int? ReviewedById { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
public class PayrollPeriod
{
    public int Id { get; set; }
    public string Month { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public int CreatedById { get; set; }
    public int? ApprovedById { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
}
public class PayrollItem
{
    public int Id { get; set; }
    public int PeriodId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public decimal BaseSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal Bonus { get; set; }
    public decimal Deductions { get; set; }
    public string Note { get; set; } = "";
}
public class OffboardingCase
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly LastWorkingDate { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "InProgress";
    public DateTime? CompletedAtUtc { get; set; }
}
public class OffboardingTask
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string Title { get; set; } = "";
    public bool Done { get; set; }
}
public class CandidateHire
{
    public int Id { get; set; }
    public int CandidateId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime HiredAtUtc { get; set; } = DateTime.UtcNow;
}
public class OperationLock
{
    public int Id { get; set; }
    public long Revision { get; set; }
}
public partial class HrDb
{
    public DbSet<LeaveEntitlement> LeaveEntitlements => Set<LeaveEntitlement>();
    public DbSet<WorkRequestDetail> WorkRequestDetails => Set<WorkRequestDetail>();
    public DbSet<AppNotification> AppNotifications => Set<AppNotification>();
    public DbSet<ProjectTimesheet> ProjectTimesheets => Set<ProjectTimesheet>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollItem> PayrollItems => Set<PayrollItem>();
    public DbSet<OffboardingCase> OffboardingCases => Set<OffboardingCase>();
    public DbSet<OffboardingTask> OffboardingTasks => Set<OffboardingTask>();
    public DbSet<CandidateHire> CandidateHires => Set<CandidateHire>();
    public DbSet<OperationLock> OperationLocks => Set<OperationLock>();
    partial void ConfigureOperations(ModelBuilder builder)
    {
        builder.Entity<LeaveEntitlement>().HasIndex(x => new { x.EmployeeId, x.Year }).IsUnique();
        builder.Entity<LeaveEntitlement>().HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkRequestDetail>().HasIndex(x => x.RequestId).IsUnique();
        builder.Entity<WorkRequestDetail>().HasOne<WorkRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AppNotification>().HasIndex(x => new { x.EmployeeId, x.IsRead, x.CreatedAtUtc });
        builder.Entity<AppNotification>().HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProjectTimesheet>().HasIndex(x => new { x.EmployeeId, x.ProjectId, x.WorkDate }).IsUnique();
        builder.Entity<ProjectTimesheet>().HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProjectTimesheet>().HasOne<TechProject>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PayrollPeriod>().HasIndex(x => x.Month).IsUnique();
        builder.Entity<PayrollItem>().HasIndex(x => new { x.PeriodId, x.EmployeeId }).IsUnique();
        builder.Entity<PayrollItem>().HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PayrollItem>().HasOne<PayrollPeriod>().WithMany().HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OffboardingCase>().HasIndex(x => x.EmployeeId).IsUnique();
        builder.Entity<OffboardingCase>().HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OffboardingTask>().HasOne<OffboardingCase>().WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<CandidateHire>().HasIndex(x => x.CandidateId).IsUnique();
        builder.Entity<CandidateHire>().HasIndex(x => x.EmployeeId).IsUnique();
        builder.Entity<CandidateHire>().HasOne<Candidate>().WithMany().HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CandidateHire>().HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OperationLock>().Property(x => x.Id).ValueGeneratedNever();
    }
}

public static class Operations
{
    public const int DefaultLeaveDays = 12; // Configurable demo allowance, not a statement of legal entitlement.
    public static async Task<IDbContextTransaction> BeginAsync(HrDb db)
    {
        var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Serialize demo workflow updates across instances. PostgreSQL row locks / SQLite write locks
            // protect aggregate limits (annual leave, daily hours) as well as state transitions.
            await db.Database.ExecuteSqlRawAsync("UPDATE \"OperationLocks\" SET \"Revision\" = \"Revision\" + 1 WHERE \"Id\" = 1");
            return transaction;
        }
        catch { await transaction.DisposeAsync(); throw; }
    }
    public static void Notify(HrDb db, int employeeId, string title, string message, string link) =>
        db.AppNotifications.Add(new AppNotification { EmployeeId = employeeId, Title = title, Message = message, Link = link });
    public static async Task NotifyApprovers(HrDb db, int employeeId, string title, string message, string link)
    {
        var departmentId = await db.Employees.Where(x => x.Id == employeeId).Select(x => x.DepartmentId).SingleAsync();
        var recipients = await db.Accounts.Where(a => a.IsActive && a.EmployeeId != employeeId && a.Employee!.Status != "Inactive"
            && (a.Role == "Admin" || a.Role == "HR" || (a.Role == "Manager" && a.Employee.DepartmentId == departmentId)))
            .Select(a => a.EmployeeId).Distinct().ToListAsync();
        foreach (var id in recipients) Notify(db, id, title, message, link);
    }
    public static async Task<IQueryable<Employee>> ScopedEmployees(HrDb db, ClaimsPrincipal user)
    {
        var query = db.Employees.AsNoTracking().AsQueryable();
        if (Access.IsHr(user)) return query;
        var own = Access.EmployeeId(user);
        if (!Access.IsManager(user)) return query.Where(x => x.Id == own);
        var department = await db.Employees.Where(x => x.Id == own).Select(x => x.DepartmentId).SingleAsync();
        return query.Where(x => x.DepartmentId == department);
    }
    public static int WorkingDays(DateOnly start, DateOnly end)
    {
        int total = 0;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) total++;
            if (day == end) break;
        }
        return total;
    }
    public static int DaysInYear(WorkRequest request, int year)
    {
        var start = request.StartDate > new DateOnly(year, 1, 1) ? request.StartDate : new DateOnly(year, 1, 1);
        var end = request.EndDate < new DateOnly(year, 12, 31) ? request.EndDate : new DateOnly(year, 12, 31);
        return start <= end ? WorkingDays(start, end) : 0;
    }
    public static async Task<(int entitlement, int used, int pending)> Balance(HrDb db, int employeeId, int year)
    {
        var entitlement = await db.LeaveEntitlements.Where(x => x.EmployeeId == employeeId && x.Year == year)
            .Select(x => (int?)x.Entitlement).SingleOrDefaultAsync() ?? DefaultLeaveDays;
        var from = new DateOnly(year, 1, 1); var to = new DateOnly(year, 12, 31);
        var requests = await db.WorkRequests.AsNoTracking().Where(x => x.EmployeeId == employeeId && x.Type == "Leave"
            && x.EndDate >= from && x.StartDate <= to && (x.Status == "Approved" || x.Status == "Pending")).ToListAsync();
        return (entitlement, requests.Where(x => x.Status == "Approved").Sum(x => DaysInYear(x, year)),
            requests.Where(x => x.Status == "Pending").Sum(x => DaysInYear(x, year)));
    }
}
