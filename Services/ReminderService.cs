using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class ReminderService
{
    private readonly SkinbloomDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        SkinbloomDbContext context,
        EmailService emailService,
        ILogger<ReminderService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Sends reminder emails for bookings that are tomorrow
    /// This method is called daily by Hangfire
    /// </summary>
    //public async Task SendDailyRemindersAsync()
    //{
    //    var now = DateTime.UtcNow;
    //    var tomorrow = DateOnly.FromDateTime(now.AddDays(1));

    //    _logger.LogInformation("Starting daily reminder job for date: {Date}", tomorrow);

    //    // Get all confirmed bookings for tomorrow that haven't received a reminder yet
    //    var bookingsToRemind = await _context.Bookings
    //        .Include(b => b.Customer)
    //        .Include(b => b.Service)
    //        .Where(b =>
    //            b.BookingDate == tomorrow &&
    //            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) &&
    //            !b.ReminderSentAt.HasValue
    //        )
    //        .ToListAsync();

    //    _logger.LogInformation("Found {Count} bookings to send reminders for", bookingsToRemind.Count);

    //    int successCount = 0;
    //    int failureCount = 0;

    //    foreach (var booking in bookingsToRemind)
    //    {
    //        try
    //        {
    //            // Send reminder email
    //            await _emailService.SendReminderEmailAsync(booking.Id);

    //            // Mark reminder as sent
    //            booking.ReminderSentAt = DateTime.UtcNow;

    //            // Log email
    //            _context.EmailLogs.Add(new EmailLog
    //            {
    //                BookingId = booking.Id,
    //                RecipientEmail = booking.Customer.Email,
    //                EmailType = EmailType.Reminder,
    //                SentAt = DateTime.UtcNow,
    //                Status = EmailStatus.Sent
    //            });

    //            successCount++;
    //            _logger.LogInformation(
    //                "Reminder sent for booking {BookingId} to {Email}",
    //                booking.Id,
    //                booking.Customer.Email
    //            );
    //        }
    //        catch (Exception ex)
    //        {
    //            failureCount++;
    //            _logger.LogError(
    //                ex,
    //                "Failed to send reminder for booking {BookingId} to {Email}",
    //                booking.Id,
    //                booking.Customer.Email
    //            );

    //            // Log email failure
    //            _context.EmailLogs.Add(new EmailLog
    //            {
    //                BookingId = booking.Id,
    //                RecipientEmail = booking.Customer.Email,
    //                EmailType = EmailType.Reminder,
    //                SentAt = DateTime.UtcNow,
    //                Status = EmailStatus.Failed,
    //                ErrorMessage = ex.Message
    //            });
    //        }
    //    }

    //    // Save all changes
    //    await _context.SaveChangesAsync();

    //    _logger.LogInformation(
    //        "Daily reminder job completed. Success: {Success}, Failed: {Failed}",
    //        successCount,
    //        failureCount
    //    );
    //}
}
