using BarberDario.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Skinbloom.Api.Data.Entities;

namespace BarberDario.Api.Data;

public class SkinbloomDbContext : DbContext
{
    public SkinbloomDbContext(DbContextOptions<SkinbloomDbContext> options)
        : base(options)
    {
    }

    public DbSet<Service> Services { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BusinessHours> BusinessHours { get; set; }
    public DbSet<BlockedTimeSlot> BlockedTimeSlots { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }
    public DbSet<PageView> PageViews { get; set; }
    public DbSet<Conversion> Conversions { get; set; }
    public DbSet<AnalyticsSummary> AnalyticsSummaries { get; set; }
    public DbSet<LinkClick> LinkClicks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // In OnModelCreating
        modelBuilder.Entity<PageView>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PageUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ReferrerUrl).HasMaxLength(500);
            entity.HasIndex(e => e.ViewedAt);
            entity.HasIndex(e => e.SessionId);
        });

        modelBuilder.Entity<LinkClick>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LinkName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LinkUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.SessionId).HasMaxLength(100);
            entity.Property(e => e.ReferrerUrl).HasMaxLength(500);

            entity.HasIndex(e => e.LinkName);
            entity.HasIndex(e => e.ClickedAt);
        });

        modelBuilder.Entity<Conversion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConversionType).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Booking)
                  .WithMany()
                  .HasForeignKey(e => e.BookingId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.ConvertedAt);
        });

        modelBuilder.Entity<AnalyticsSummary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SummaryDate).IsUnique();
        });

        // ServiceCategory configuration
        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasOne(s => s.Category)
                  .WithMany(c => c.Services)
                  .HasForeignKey(s => s.Id)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Service Configuration
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.HasIndex(e => e.IsActive);
        });

        // Customer Configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Phone);

            entity.Ignore(e => e.FullName); // Computed property
        });

        // Booking Configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Service)
                .WithMany(s => s.Bookings)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => e.BookingDate);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.BookingDate, e.Status });

            entity.Ignore(e => e.BookingNumber); // Computed property
        });

        // BusinessHours Configuration
        modelBuilder.Entity<BusinessHours>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DayOfWeek).IsUnique();
            entity.Ignore(e => e.DayName); // Computed property
        });

        // BlockedTimeSlot Configuration
        modelBuilder.Entity<BlockedTimeSlot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.HasIndex(e => e.BlockDate);
        });

        // EmailLog Configuration
        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Booking)
                .WithMany(b => b.EmailLogs)
                .HasForeignKey(e => e.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.EmailType)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.RecipientEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);

            entity.HasIndex(e => e.BookingId);
            entity.HasIndex(e => e.EmailType);
        });

        // Setting Configuration
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Seed Data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Services
        var services = new[]
        {
            new Service
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Herrenschnitt",
                Description = "Klassischer Herrenhaarschnitt mit Styling",
                DurationMinutes = 30,
                Price = 35.00m,
                DisplayOrder = 1,
                IsActive = true
            },
            new Service
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Bart Trimmen",
                Description = "Professionelles Bart-Trimming und Konturenschneiden",
                DurationMinutes = 20,
                Price = 20.00m,
                DisplayOrder = 2,
                IsActive = true
            },
            new Service
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Komplettpaket",
                Description = "Haarschnitt + Bart Trimmen",
                DurationMinutes = 50,
                Price = 50.00m,
                DisplayOrder = 3,
                IsActive = true
            },
            new Service
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Kinder Haarschnitt",
                Description = "Haarschnitt für Kinder bis 12 Jahre",
                DurationMinutes = 30,
                Price = 25.00m,
                DisplayOrder = 4,
                IsActive = true
            }
        };
        modelBuilder.Entity<Service>().HasData(services);

        // Seed Business Hours
        var businessHours = new[]
        {
            new BusinessHours { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Sunday, OpenTime = new TimeOnly(0, 0), CloseTime = new TimeOnly(0, 0), IsOpen = false },
            new BusinessHours { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Monday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(18, 0), IsOpen = true },
            new BusinessHours { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Tuesday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(18, 0), IsOpen = true },
            new BusinessHours { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Wednesday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(18, 0), IsOpen = true },
            new BusinessHours { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Thursday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(20, 0), IsOpen = true },
            new BusinessHours { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Friday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(18, 0), IsOpen = true },
            new BusinessHours { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Saturday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(16, 0), IsOpen = true }
        };
        modelBuilder.Entity<BusinessHours>().HasData(businessHours);

        // Seed Settings
        var settings = new[]
        {
            new Setting { Key = "BOOKING_INTERVAL_MINUTES", Value = "15", Description = "Zeitintervall für Buchungen in Minuten" },
            new Setting { Key = "MAX_ADVANCE_BOOKING_DAYS", Value = "60", Description = "Wie viele Tage im Voraus kann gebucht werden" },
            new Setting { Key = "MIN_ADVANCE_BOOKING_HOURS", Value = "24", Description = "Mindestvorlauf für Buchungen in Stunden" },
            new Setting { Key = "REMINDER_HOURS_BEFORE", Value = "24", Description = "Wann vor dem Termin wird die Erinnerung gesendet (Stunden)" },
            new Setting { Key = "ADMIN_EMAIL", Value = "dario@barberdario.com", Description = "Admin Email-Adresse" },
            new Setting { Key = "BUSINESS_NAME", Value = "Barber Dario", Description = "Name des Geschäfts" },
            new Setting { Key = "BUSINESS_ADDRESS", Value = "Berliner Allee 43, 40212 Düsseldorf", Description = "Geschäftsadresse" },
            new Setting { Key = "TIMEZONE", Value = "Europe/Berlin", Description = "Zeitzone des Geschäfts" }
        };
        modelBuilder.Entity<Setting>().HasData(settings);
    }
}
