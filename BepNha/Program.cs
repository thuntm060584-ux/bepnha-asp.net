using CookingCommunity.Web.Data;
using CookingCommunity.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CookingDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("CookingDatabase")));
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<ImageStorage>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/auth/login"; o.AccessDeniedPath = "/errors/403";
    o.Cookie.Name = "CookingCommunity.Auth"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
    o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = true;
    o.Events.OnValidatePrincipal = c => c.HttpContext.RequestServices.GetRequiredService<SessionService>().Validate(c);
    o.Events.OnRedirectToLogin = c =>
    {
        if (c.Request.Headers["X-Requested-With"] == "XMLHttpRequest") c.Response.StatusCode = 401;
        else
        {
            var returnPath = c.Request.PathBase + c.Request.Path + c.Request.QueryString;
            if (returnPath.Length > 1 && c.Request.Path != "/auth/login")
                c.Response.Cookies.Append("CookingCommunity.ReturnUrl", returnPath, new CookieOptions
                {
                    HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax,
                    Secure = c.Request.IsHttps, MaxAge = TimeSpan.FromMinutes(5)
                });
            c.Response.Redirect("/auth/login");
        }
        return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    o.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(name => "Vui lòng nhập số hợp lệ.");
    o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/errors/500"); app.UseHsts(); }
app.UseStatusCodePagesWithReExecute("/errors/{0}");
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    await next();
});
app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CookingDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DatabaseSchemaUpdater.Apply(db);
    if (builder.Configuration.GetValue<bool>("Database:SeedDemo")) await DemoSeeder.Seed(db);
    if (args.Contains("--export-schema"))
    {
        var directory = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../../database"));
        Directory.CreateDirectory(directory); await File.WriteAllTextAsync(Path.Combine(directory, "schema.sql"), db.Database.GenerateCreateScript()); return;
    }
}
app.Run();
