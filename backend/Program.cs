using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using TechHRMS.Api;

var builder = WebApplication.CreateBuilder(args);

var postgres = builder.Configuration.GetConnectionString("Postgres");
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(postgres))
    throw new InvalidOperationException("Production requires ConnectionStrings__Postgres.");
builder.Services.AddDbContext<HrDb>(options =>
{
    if (!string.IsNullOrWhiteSpace(postgres)) options.UseNpgsql(postgres);
    else options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=techhrms.db");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "techhrms_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var id = ctx.Principal is null ? 0 : Access.EmployeeId(ctx.Principal);
            var db = ctx.HttpContext.RequestServices.GetRequiredService<HrDb>();
            var activeAccount = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeId == id && x.IsActive);
            if (activeAccount is null || !ctx.Principal!.IsInRole(activeAccount.Role))
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto,
        // Configure KnownProxies/KnownNetworks for the actual hosting platform.
    });
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=()";
    await next();
});

app.Use(async (ctx, next) =>
{
    // Reject cross-site state-changing browser requests. The Vite dev proxy is explicitly permitted.
    if (ctx.Request.Method is not ("GET" or "HEAD" or "OPTIONS") && ctx.Request.Headers.TryGetValue("Origin", out var originValues))
    {
        if (!Uri.TryCreate(originValues.ToString(), UriKind.Absolute, out var origin))
        {
            ctx.Response.StatusCode = 403; return;
        }
        var matchesHost = origin.Authority.Equals(ctx.Request.Host.Value, StringComparison.OrdinalIgnoreCase);
        var localDev = app.Environment.IsDevelopment() && origin.Host == "localhost" && origin.Port == 5173;
        if (!matchesHost && !localDev) { ctx.Response.StatusCode = 403; return; }
    }
    await next();
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", name = "TechHRMS API", utc = DateTime.UtcNow }));
app.MapGet("/api/health/ready", async (HrDb db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "ready", database = "connected" })
        : Results.Json(new { status = "not-ready" }, statusCode: 503));

app.MapPost("/api/auth/login", async (LoginInput input, HttpContext ctx, HrDb db) =>
{
    if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        return Results.BadRequest(new { error = "Vui lòng nhập email và mật khẩu." });
    var email = input.Email.Trim().ToLowerInvariant();
    var account = await db.Accounts.Include(x => x.Employee).ThenInclude(x => x!.Department)
        .FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
    if (account is null || account.Employee?.Status == "Inactive" || !Passwords.Verify(input.Password, account.PasswordHash))
        return Results.Json(new { error = "Email hoặc mật khẩu không chính xác." }, statusCode: 401);
    var claims = new []
    {
        new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
        new Claim(ClaimTypes.Name, account.Employee?.FullName ?? account.Email),
        new Claim(ClaimTypes.Email, account.Email),
        new Claim(ClaimTypes.Role, account.Role),
        new Claim("employeeId", account.EmployeeId.ToString())
    };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Ok(new { id = account.EmployeeId, name = account.Employee?.FullName, email = account.Email,
        role = account.Role, jobTitle = account.Employee?.JobTitle, department = account.Employee?.Department?.Name });
}).RequireRateLimiting("login");

app.MapGet("/api/auth/me", async (HttpContext ctx, HrDb db) =>
{
    var id = Access.EmployeeId(ctx.User);
    var account = await db.Accounts.Include(x => x.Employee).ThenInclude(x => x!.Department)
        .FirstOrDefaultAsync(x => x.EmployeeId == id && x.IsActive);
    if (account is null) return Results.Unauthorized();
    return Results.Ok(new { id, name = account.Employee?.FullName, email = account.Email, role = account.Role,
        jobTitle = account.Employee?.JobTitle, department = account.Employee?.Department?.Name });
}).RequireAuthorization();

app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { message = "Đã đăng xuất." });
}).RequireAuthorization();

app.MapHrEndpoints();

if (app.Environment.IsProduction()) app.MapFallbackToFile("index.html");
await Seed.InitializeAsync(app.Services, app.Environment, app.Configuration);
await EnterpriseSchema.InitializeAsync(app.Services, app.Environment, app.Configuration);
app.Run();

public record LoginInput(string Email, string Password);
