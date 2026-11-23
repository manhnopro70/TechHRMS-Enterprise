using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
builder.Services.AddDataProtection().SetApplicationName("TechHRMS.Enterprise").PersistKeysToDbContext<HrDb>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "techhrms_csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "techhrms_session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = async ctx =>
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsJsonAsync(new { error = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", code = "AUTH_REQUIRED" });
    };
    options.Events.OnRedirectToAccessDenied = async ctx =>
    {
        ctx.Response.StatusCode = 403;
        await ctx.Response.WriteAsJsonAsync(new { error = "Bạn không có quyền thực hiện thao tác này.", code = "FORBIDDEN" });
    };
    options.Events.OnValidatePrincipal = async ctx =>
    {
        var accountId = ctx.Principal is null ? 0 : Access.AccountId(ctx.Principal);
        var db = ctx.HttpContext.RequestServices.GetRequiredService<HrDb>();
        var account = await db.Accounts.AsNoTracking().Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == accountId);
        if (account is null || !account.IsActive || account.Employee is null || account.Employee.Status == "Inactive" ||
            ctx.Principal!.FindFirst("securityStamp")?.Value != account.SecurityStamp || !ctx.Principal.IsInRole(account.Role))
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
    options.OnRejected = async (ctx, cancel) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new { error = "Bạn đã thử quá nhiều lần. Vui lòng đợi một phút rồi thử lại." }, cancel);
    };
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("password", context => RateLimitPartition.GetFixedWindowLimiter(
        Access.AccountId(context.User).ToString(),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});
var app = builder.Build();
if (app.Environment.IsProduction())
{
    var forwarding = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto, ForwardLimit = 1 };
    // Render terminates TLS at its own reverse proxy and advertises RENDER=true.
    // On other hosts, trust only explicitly supplied proxy IP addresses.
    var managedProxy = builder.Configuration["RENDER"] == "true" || builder.Configuration["TRUST_PROXY_HEADERS"] == "true";
    if (managedProxy) { forwarding.KnownIPNetworks.Clear(); forwarding.KnownProxies.Clear(); }
    else foreach (var value in (builder.Configuration["TRUSTED_PROXY_IPS"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        if (IPAddress.TryParse(value.Trim(), out var address)) forwarding.KnownProxies.Add(address);
    app.UseForwardedHeaders(forwarding);
    app.UseHsts();
}
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception ex) when (ex is DbUpdateException or DbUpdateConcurrencyException ||
        ex is PostgresException { SqlState: "40001" or "40P01" or "23505" } ||
        ex is SqliteException { SqliteErrorCode: 5 or 6 or 19 })
    {
        if (ctx.Response.HasStarted) throw;
        app.Logger.LogWarning(ex, "Concurrent or conflicting update {Path}", ctx.Request.Path);
        ctx.Response.Clear(); ctx.Response.StatusCode = 409;
        await ctx.Response.WriteAsJsonAsync(new { error = "Dữ liệu vừa thay đổi hoặc bị trùng. Hãy tải lại trang và thử lại.", code = "DATA_CONFLICT" });
    }
    catch (BadHttpRequestException)
    {
        if (ctx.Response.HasStarted) throw;
        ctx.Response.Clear(); ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsJsonAsync(new { error = "Dữ liệu gửi lên không hợp lệ. Hãy kiểm tra các trường thông tin." });
    }
    catch (Exception ex)
    {
        if (ctx.Response.HasStarted) throw;
        app.Logger.LogError(ex, "Unhandled request failure {Path}", ctx.Request.Path);
        ctx.Response.Clear(); ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = "Không thể xử lý yêu cầu lúc này. Vui lòng thử lại hoặc liên hệ quản trị viên.", code = "SERVER_ERROR" });
    }
});
app.Use(async (ctx, next) =>
{
    // OnStarting also runs for errors handled by the outer exception middleware,
    // even when that middleware clears a partially prepared response.
    ctx.Response.OnStarting(() =>
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=()";
        ctx.Response.Headers["X-Frame-Options"] = "DENY";
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.Headers.CacheControl = ctx.Request.Path == "/api/auth/csrf" ? "no-cache, no-store" : "no-store";
        return Task.CompletedTask;
    });
    await next();
});
app.UseStatusCodePages(async context =>
{
    if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
        await context.HttpContext.Response.WriteAsJsonAsync(new { error = context.HttpContext.Response.StatusCode switch
        {
            404 => "Không tìm thấy dữ liệu hoặc chức năng này.",
            401 => "Vui lòng đăng nhập lại.",
            403 => "Bạn không có quyền thực hiện thao tác này.",
            _ => "Yêu cầu không hợp lệ."
        }});
});
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api") && ctx.User.Identity?.IsAuthenticated == true &&
        ctx.User.FindFirst("mustChangePassword")?.Value == "true" &&
        !new[] { "/api/auth/me", "/api/auth/profile", "/api/auth/change-password", "/api/auth/logout", "/api/auth/csrf" }.Contains(ctx.Request.Path.Value))
    {
        ctx.Response.StatusCode = 403;
        await ctx.Response.WriteAsJsonAsync(new { error = "Bạn cần đổi mật khẩu tạm trước khi sử dụng hệ thống.", code = "PASSWORD_CHANGE_REQUIRED" });
        return;
    }
    if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Request.Method is not ("GET" or "HEAD" or "OPTIONS"))
    {
        try { await ctx.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(ctx); }
        catch (AntiforgeryValidationException)
        {
            ctx.Response.StatusCode = 403;
            await ctx.Response.WriteAsJsonAsync(new { error = "Phiên xác thực biểu mẫu đã thay đổi. Vui lòng thử lại.", code = "CSRF_INVALID" });
            return;
        }
    }
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", name = "TechHRMS API", version = "4.0", utc = DateTime.UtcNow }));
app.MapGet("/api/health/ready", async (HrDb db) => await db.Database.CanConnectAsync()
    ? Results.Ok(new { status = "ready", database = "connected" }) : Results.Json(new { status = "not-ready" }, statusCode: 503));
app.MapAccountEndpoints();
app.MapHrEndpoints();
app.MapOperationsEndpoints();
app.MapFallback("/api/{**path}", () => Results.NotFound(new { error = "Đường dẫn API không tồn tại.", code = "API_NOT_FOUND" }));
if (app.Environment.IsProduction()) app.MapFallbackToFile("index.html");
// Existing V3 tables stay intact. Apply account columns before querying Accounts through the new model.
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<HrDb>().Database.EnsureCreatedAsync();
await AccountSchema.InitializeAsync(app.Services);
await Seed.InitializeAsync(app.Services, app.Environment, app.Configuration);
await EnterpriseSchema.InitializeAsync(app.Services, app.Environment, app.Configuration);
await OperationsSchema.InitializeAsync(app.Services, app.Environment, app.Configuration);
app.Run();
