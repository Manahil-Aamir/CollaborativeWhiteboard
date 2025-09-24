using Microsoft.EntityFrameworkCore;
using CollaborativeWhiteboard.Models;
using CollaborativeWhiteboard.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
          // API controllers only
builder.Services.AddRazorPages();           // Razor Pages
builder.Services.AddSignalR();              // SignalR

// Add Entity Framework with MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21))
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error"); // Razor Pages error
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Map Razor Pages first
app.MapRazorPages();           // Pages/Index.cshtml will handle "/"

// Map API controllers under /api
app.MapControllers();          // API endpoints like /api/Home/CreateSession

// Map SignalR hub
app.MapHub<WhiteboardHub>("/whiteboardhub");

app.Run();
