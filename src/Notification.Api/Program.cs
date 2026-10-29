using Microsoft.EntityFrameworkCore;
using Notification.Api.Data;
using Notification.Api.Providers;
using Notification.Api.Repositories;
using Notification.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Notifications") ?? "Data Source=notifications.db"));
builder.Services.AddScoped<INotificationRepository, EfNotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailNotificationProvider, SimulatedEmailNotificationProvider>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();

var app = builder.Build();
// For this small assessment, ensure the SQLite schema exists at startup. Production deployments
// should use reviewed EF Core migrations instead of EnsureCreated.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.EnsureCreatedAsync();
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseExceptionHandler();
app.MapControllers();
app.Run();

public partial class Program { }
