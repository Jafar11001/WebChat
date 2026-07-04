using WebChat.DAL;
using WebChat.Hubs;
using WebChat.Services;
using Microsoft.EntityFrameworkCore;
using WebChat.DAL;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(
                options =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
                });


//M8 : Adding Entity Framework Core with SQL database provider for data access.
builder.Services.AddScoped<MessageService>();

//M5: Adding SignalR services to enable real-time communication between clients and server.
builder.Services.AddSignalR();

//M6: Adding controllers support for MVC/API endpoints.
builder.Services.AddControllersWithViews();

//M7: Adding OpenAPI/Swagger support for API documentation in development.
builder.Services.AddOpenApi();

var app = builder.Build();

//M1: Enabling HTTPS redirection for secure HTTP → HTTPS routing.
app.UseHttpsRedirection();

//M2: Enabling static file serving from wwwroot (HTML, CSS, JS).
app.UseStaticFiles();

//M3: Enabling default file mapping (e.g., index.html when hitting "/").
app.UseDefaultFiles();

//M4: Enabling routing middleware for endpoint matching.
app.UseRouting();

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