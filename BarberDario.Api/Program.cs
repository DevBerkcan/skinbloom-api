using BarberDario.Api.Data;
using BarberDario.Api.Options;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Barber Dario API",
        Version = "v1",
        Description = "Booking System API for Barber Dario"
    });
});

// Add Database Context
builder.Services.AddDbContext<BarberDarioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Options
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

// Add HttpClient for external APIs
builder.Services.AddHttpClient();

// Add Application Services
builder.Services.AddScoped<BarberDario.Api.Services.AvailabilityService>();
builder.Services.AddScoped<BarberDario.Api.Services.BookingService>();
builder.Services.AddScoped<BarberDario.Api.Services.EmailService>();
builder.Services.AddScoped<BarberDario.Api.Services.AdminService>();
builder.Services.AddScoped<BarberDario.Api.Services.ReminderService>();
builder.Services.AddScoped<BarberDario.Api.Services.BrevoService>();

// Add Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// Add CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthorization();

// Hangfire Dashboard (only in development for security)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapControllers();

// Apply migrations and seed data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BarberDarioDbContext>();
    await db.Database.MigrateAsync();
}

// ✅ Schedule recurring jobs (DI-based, safe for IIS / Startup)
using (var scope = app.Services.CreateScope())
{
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    recurringJobs.AddOrUpdate<BarberDario.Api.Services.ReminderService>(
        "send-daily-reminders",
        service => service.SendDailyRemindersAsync(),
        Cron.Daily(9)  // Runs every day at 9:00 AM
    );
}

app.Run();
