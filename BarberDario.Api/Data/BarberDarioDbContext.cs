using BarberDario.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Data;

public class BarberDarioDbContext : DbContext
{
    public BarberDarioDbContext(DbContextOptions<BarberDarioDbContext> options)
        : base(options)
    {
    }

    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }
    public DbSet<ServiceBundle> ServiceBundles { get; set; }
    public DbSet<ServiceBundleItem> ServiceBundleItems { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BusinessHours> BusinessHours { get; set; }
    public DbSet<BlockedTimeSlot> BlockedTimeSlots { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<Newsletter> Newsletters { get; set; }
    public DbSet<NewsletterRecipient> NewsletterRecipients { get; set; }
    public DbSet<Waitlist> Waitlists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ServiceCategory Configuration
        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DisplayOrder);
        });

        // Service Configuration
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.HasIndex(e => e.IsActive);

            // Category relationship
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ServiceBundle Configuration
        modelBuilder.Entity<ServiceBundle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.OriginalPrice).HasPrecision(10, 2);
            entity.Property(e => e.BundlePrice).HasPrecision(10, 2);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(e => e.TermsAndConditions).HasMaxLength(2000);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DisplayOrder);
            entity.HasIndex(e => new { e.ValidFrom, e.ValidUntil });
        });

        // ServiceBundleItem Configuration
        modelBuilder.Entity<ServiceBundleItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Bundle)
                .WithMany(b => b.BundleItems)
                .HasForeignKey(e => e.BundleId)
                .OnDelete(DeleteBehavior.Cascade); // Delete items when bundle is deleted

            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict); // Don't delete service when item is deleted

            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasIndex(e => e.BundleId);
            entity.HasIndex(e => e.ServiceId);
            entity.HasIndex(e => new { e.BundleId, e.DisplayOrder });
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
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); // Nullable - booking can be for service OR bundle

            entity.HasOne(e => e.Bundle)
                .WithMany(b => b.Bookings)
                .HasForeignKey(e => e.BundleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); // Nullable - booking can be for service OR bundle

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
        // Seed Service Categories
        var hyaluronCategoryId = Guid.Parse("CAT00001-0001-0001-0001-000000000001");
        var botoxCategoryId = Guid.Parse("CAT00002-0002-0002-0002-000000000002");
        var skinCategoryId = Guid.Parse("CAT00003-0003-0003-0003-000000000003");
        var advancedCategoryId = Guid.Parse("CAT00004-0004-0004-0004-000000000004");

        var categories = new[]
        {
            new ServiceCategory
            {
                Id = hyaluronCategoryId,
                Name = "Hyaluron Behandlungen",
                Description = "Faltenunterspritzung und Volumenaufbau mit Hyaluronsäure",
                Icon = "💉",
                Color = "#000000",
                DisplayOrder = 1,
                IsActive = true
            },
            new ServiceCategory
            {
                Id = botoxCategoryId,
                Name = "Botox Behandlungen",
                Description = "Muskelentspannende Behandlungen mit Botulinum",
                Icon = "✨",
                Color = "#1F2937",
                DisplayOrder = 2,
                IsActive = true
            },
            new ServiceCategory
            {
                Id = skinCategoryId,
                Name = "Hautbehandlungen",
                Description = "Gesichtsbehandlungen, Peelings und Facials",
                Icon = "🌟",
                Color = "#374151",
                DisplayOrder = 3,
                IsActive = true
            },
            new ServiceCategory
            {
                Id = advancedCategoryId,
                Name = "Advanced Treatments",
                Description = "PRP, Microneedling und spezielle Therapien",
                Icon = "🔬",
                Color = "#4B5563",
                DisplayOrder = 4,
                IsActive = true
            }
        };
        modelBuilder.Entity<ServiceCategory>().HasData(categories);

        // Services will be inserted via SQL script (SkinbloomServices_Complete_25.sql)
        // No service seed data here to avoid conflicts

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
            new Setting { Key = "MAX_ADVANCE_BOOKING_DAYS", Value = "90", Description = "Wie viele Tage im Voraus kann gebucht werden" },
            new Setting { Key = "MIN_ADVANCE_BOOKING_HOURS", Value = "24", Description = "Mindestvorlauf für Buchungen in Stunden" },
            new Setting { Key = "REMINDER_HOURS_BEFORE", Value = "24", Description = "Wann vor dem Termin wird die Erinnerung gesendet (Stunden)" },
            new Setting { Key = "ADMIN_EMAIL", Value = "info@skinbloom-aesthetics.ch", Description = "Admin Email-Adresse" },
            new Setting { Key = "BUSINESS_NAME", Value = "Skinbloom Aesthetics", Description = "Name des Geschäfts" },
            new Setting { Key = "BUSINESS_ADDRESS", Value = "Elisabethenstrasse 41, 4051 Basel", Description = "Geschäftsadresse" },
            new Setting { Key = "BUSINESS_PHONE", Value = "+41 78 241 87 04", Description = "Telefonnummer" },
            new Setting { Key = "TIMEZONE", Value = "Europe/Zurich", Description = "Zeitzone des Geschäfts" }
        };
        modelBuilder.Entity<Setting>().HasData(settings);
    }
}
