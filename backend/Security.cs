using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;

namespace TechHRMS.Api;

public static class Passwords
{
    private const int Iterations = 600_000;
    public static bool IsValid(string? value) => value is { Length: >= 12 and <= 128 } && !string.IsNullOrWhiteSpace(value);
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2_sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    public static bool Verify(string? password, string stored)
    {
        if (password is null || password.Length > 128) return false;
        try
        {
            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2_sha256") return false;
            var iterations = int.Parse(parts[1], CultureInfo.InvariantCulture);
            if (iterations < 100_000 || iterations > 1_000_000) return false;
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            if (salt.Length != 16 || expected.Length != 32) return false;
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException) { return false; }
        catch (OverflowException) { return false; }
    }
}

public static class AccountRules
{
    public static readonly string[] Roles = ["Admin", "HR", "Manager", "Employee"];
    public static async Task LockWritesAsync(HrDb db)
    {
        // Call within a serializable transaction. SQLite serializes writers; this also
        // coordinates account changes with the offboarding flow on PostgreSQL.
        if (db.Database.IsNpgsql()) await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(47424001)");
    }
    public static async Task<bool> CanDeactivateAsync(HrDb db, int employeeId)
    {
        var target = await db.Accounts.FirstOrDefaultAsync(x => x.EmployeeId == employeeId);
        return target is null || target.Role != "Admin" || !target.IsActive ||
            await db.Accounts.AnyAsync(x => x.Id != target.Id && x.Role == "Admin" && x.IsActive && x.Employee!.Status != "Inactive");
    }
    public static object Session(Account account) => new
    {
        id = account.EmployeeId,
        name = account.Employee?.FullName ?? account.Email,
        email = account.Email,
        role = account.Role,
        jobTitle = account.Employee?.JobTitle ?? "",
        department = account.Employee?.Department?.Name ?? "",
        mustChangePassword = account.MustChangePassword
    };
    public static async Task SignInAsync(HttpContext ctx, Account account)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, account.Employee?.FullName ?? account.Email),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, account.Role),
            new Claim("employeeId", account.EmployeeId.ToString(CultureInfo.InvariantCulture)),
            new Claim("securityStamp", account.SecurityStamp),
            new Claim("mustChangePassword", account.MustChangePassword ? "true" : "false")
        };
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    }
    public static async Task<Account?> CurrentAsync(HttpContext ctx, HrDb db) =>
        await db.Accounts.Include(x => x.Employee).ThenInclude(x => x!.Department)
            .FirstOrDefaultAsync(x => x.Id == Access.AccountId(ctx.User) && x.IsActive && x.Employee!.Status != "Inactive");
}

public static class Access
{
    public static int AccountId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
    public static int EmployeeId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst("employeeId")?.Value, out var id) ? id : 0;
    public static bool IsHr(ClaimsPrincipal user) => user.IsInRole("Admin") || user.IsInRole("HR");
    public static bool IsManager(ClaimsPrincipal user) => user.IsInRole("Manager");
    public static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("Admin");
    public static async Task<bool> CanViewEmployee(HrDb db, ClaimsPrincipal user, int employeeId)
    {
        if (IsHr(user) || EmployeeId(user) == employeeId) return true;
        if (!IsManager(user)) return false;
        var ownDept = await db.Employees.Where(x => x.Id == EmployeeId(user)).Select(x => x.DepartmentId).FirstOrDefaultAsync();
        return await db.Employees.AnyAsync(x => x.Id == employeeId && x.DepartmentId == ownDept);
    }
    public static async Task<bool> CanManageEmployee(HrDb db, ClaimsPrincipal user, int employeeId)
    {
        if (IsHr(user)) return true;
        if (!IsManager(user) || EmployeeId(user) == employeeId) return false;
        var ownDept = await db.Employees.Where(x => x.Id == EmployeeId(user)).Select(x => x.DepartmentId).FirstOrDefaultAsync();
        return await db.Employees.AnyAsync(x => x.Id == employeeId && x.DepartmentId == ownDept);
    }
    public static void Log(HrDb db, ClaimsPrincipal user, string action, string entity, string details) =>
        db.AuditLogs.Add(new AuditLog { ActorEmployeeId = EmployeeId(user), Action = action, Entity = entity, Details = details });
}
