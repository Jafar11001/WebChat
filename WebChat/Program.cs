using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Helpers;
using WebChat.DAL;
using WebChat.Entities;
using WebChat.Hubs;
using WebChat.Services;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(
                options =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
                });

builder.Services.AddAuthorization();

//M10: Adding ASP.NET Core Identity services for user authentication and management.
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Without this Identity happily allows two accounts on one address, and
    // FindByEmailAsync (which UserStore implements with SingleOrDefault) then
    // throws for that address instead of signing anyone in.
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

// This configures the cookie AddIdentity actually uses. The old
// AddAuthentication(...).AddCookie(...) set these paths on a different scheme
// that AddIdentity then overrode — so it never took effect.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // A rejected /api call must return a status code, not a 302 to an HTML
    // login page — otherwise fetch() sees "200 + login HTML" and can't tell it
    // was refused. Browser page navigations still redirect normally.
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

//M8 : Adding Entity Framework Core with SQL database provider for data access.
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<ConversationService>();

//M5: Adding SignalR services to enable real-time communication between clients and server.
builder.Services.AddSignalR();

//M6: Adding controllers support for MVC/API endpoints.
builder.Services.AddControllersWithViews();

//M7: Adding OpenAPI/Swagger support for API documentation in development.
builder.Services.AddOpenApi();

var app = builder.Build();

//M0: Seeding the Identity roles from RoleEnum on startup. Registration calls
//    AddToRoleAsync("Member"), which throws "Role MEMBER does not exist" if the
//    roles table is empty — so this can't depend on someone remembering to hit
//    /Account/CreateRole by hand after every database reset. Idempotent: it only
//    creates roles that are missing.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in Enum.GetNames<RoleEnum>())
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

//M1: Enabling HTTPS redirection for secure HTTP → HTTPS routing.
app.UseHttpsRedirection();

//M2: Enabling static file serving from wwwroot (HTML, CSS, JS).
app.UseStaticFiles();

//M3: Enabling default file mapping (e.g., index.html when hitting "/").
app.UseDefaultFiles();

//M4: Enabling routing middleware for endpoint matching.
app.UseRouting();

//M5: Enabling authentication middleware to handle user authentication.
app.UseAuthentication();

//M5: Enabling authorization middleware (only needed if you actually use auth policies).
app.UseAuthorization();



//M6: Mapping MVC controller routes.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//M7: Mapping attribute-based controllers (API controllers if used).
app.MapControllers();

//M8: Mapping SignalR hub endpoint for real-time chat communication.
app.MapHub<ChatHub>("/chathub");

//M9: Enabling OpenAPI endpoints in development environment.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();