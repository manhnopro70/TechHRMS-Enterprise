using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace TechHRMS.Api;

public static class Passwords
{
    private const int Iterations = 210_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2_sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        try
        {
            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2_sha256") return false;
            var iterations = int.Parse(parts[1], CultureInfo.InvariantCulture);
            if (iterations < 100_000 || iterations > 1_000_000) return false;
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch { return false; }
    }
}

public static class Access
{
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
