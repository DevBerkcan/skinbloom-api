using BarberDario.Api.Data;
using BarberDario.Api.Options;
using BarberDario.Api.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Skinbloom API",
        Version = "v1",
        Description = "Skinbloom API"
    });
});

// Add Database Context
builder.Services.AddDbContext<SkinbloomDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Options
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

// Add HttpClient for external APIs
builder.Services.AddHttpClient();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("EmailOptions"));
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<BlockedTimeSlotService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ReminderService>();
builder.Services.AddScoped<ManualBookingService>();
builder.Services.AddScoped<EmployeeService>();


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

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SkinbloomDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();