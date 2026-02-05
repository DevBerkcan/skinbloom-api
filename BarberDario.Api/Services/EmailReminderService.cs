using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class EmailReminderService
{
    private readonly BarberDarioDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<EmailReminderService> _logger;

    public EmailReminderService(
        BarberDarioDbContext context,
        EmailService emailService,
        ILogger<EmailReminderService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Send reminders for bookings that are due in the next 24 hours
    /// This should be called by a background job (e.g., Hangfire)
    /// </summary>
    public async Task SendUpcomingBookingRemindersAsync()
    {
        _logger.LogInformation("Starting to send booking reminders...");

        var now = DateTime.UtcNow;
        var reminderWindowStart = now.AddHours(23); // 23 hours from now
        var reminderWindowEnd = now.AddHours(25);   // 25 hours from now (1 hour window)

        // Find confirmed bookings in the next 24 hours that haven't had a reminder sent
        var bookingsToRemind = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .Include(b => b.Bundle)
            .Where(b =>
                b.Status == BookingStatus.Confirmed &&
                b.ReminderSentAt == null &&
                b.BookingDate >= DateOnly.FromDateTime(reminderWindowStart) &&
                b.BookingDate <= DateOnly.FromDateTime(reminderWindowEnd)
            )
            .ToListAsync();

        // Filter by time (DateOnly doesn't have time component, so we need to check the actual datetime)
        var filteredBookings = bookingsToRemind.Where(b =>
        {
            var bookingDateTime = b.BookingDate.ToDateTime(b.StartTime);
            return bookingDateTime >= reminderWindowStart && bookingDateTime <= reminderWindowEnd;
        }).ToList();

        _logger.LogInformation("Found {Count} bookings to send reminders for", filteredBookings.Count);

        var successCount = 0;
        var failureCount = 0;

        foreach (var booking in filteredBookings)
        {
            try
            {
                // Send reminder based on booking type
                if (booking.ServiceId.HasValue && booking.Service != null)
                {
                    await _emailService.SendReminderEmailAsync(booking, booking.Customer, booking.Service);
                }
                else if (booking.BundleId.HasValue && booking.Bundle != null)
                {
                    // TODO: Implement bundle reminder email
                    _logger.LogWarning("Bundle reminder email not yet implemented for booking {BookingId}", booking.Id);
                    continue;
                }
                else
                {
                    _logger.LogWarning("Booking {BookingId} has neither service nor bundle", booking.Id);
                    continue;
                }

                // Mark reminder as sent
                booking.ReminderSentAt = DateTime.UtcNow;

                // Log the email
                _context.EmailLogs.Add(new EmailLog
                {
                    BookingId = booking.Id,
                    RecipientEmail = booking.Customer.Email,
                    EmailType = EmailType.Reminder,
                    SentAt = DateTime.UtcNow,
                    Status = EmailStatus.Sent,
                    Subject = $"Erinnerung: Termin morgen um {booking.StartTime:HH:mm} Uhr"
                });

                await _context.SaveChangesAsync();

                successCount++;
                _logger.LogInformation(
                    "Reminder sent for booking {BookingId} to {Email}",
                    booking.Id,
                    booking.Customer.Email
                );
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(
                    ex,
                    "Failed to send reminder for booking {BookingId}",
                    booking.Id
                );

                // Log the failure
                _context.EmailLogs.Add(new EmailLog
                {
                    BookingId = booking.Id,
                    RecipientEmail = booking.Customer.Email,
                    EmailType = EmailType.Reminder,
                    SentAt = DateTime.UtcNow,
                    Status = EmailStatus.Failed,
                    ErrorMessage = ex.Message
                });

                await _context.SaveChangesAsync();
            }
        }

        _logger.LogInformation(
            "Finished sending reminders. Success: {SuccessCount}, Failed: {FailureCount}",
            successCount,
            failureCount
        );
    }

    /// <summary>
    /// Send follow-up emails for completed bookings
    /// This should be called by a background job
    /// </summary>
    public async Task SendFollowUpEmailsAsync()
    {
        _logger.LogInformation("Starting to send follow-up emails...");

        var now = DateTime.UtcNow;
        var followUpWindowStart = now.AddDays(-2); // 2 days ago
        var followUpWindowEnd = now.AddDays(-1);   // 1 day ago (1 day window)

        // Find completed bookings from yesterday that haven't had a follow-up sent
        var bookingsForFollowUp = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .Include(b => b.Bundle)
            .Where(b =>
                b.Status == BookingStatus.Completed &&
                b.BookingDate >= DateOnly.FromDateTime(followUpWindowStart) &&
                b.BookingDate <= DateOnly.FromDateTime(followUpWindowEnd)
            )
            .ToListAsync();

        // Filter out bookings that already have a follow-up email
        var bookingsNeedingFollowUp = new List<Booking>();
        foreach (var booking in bookingsForFollowUp)
        {
            var hasFollowUp = await _context.EmailLogs
                .AnyAsync(e =>
                    e.BookingId == booking.Id &&
                    e.EmailType == EmailType.FollowUp &&
                    e.Status == EmailStatus.Sent
                );

            if (!hasFollowUp)
            {
                bookingsNeedingFollowUp.Add(booking);
            }
        }

        _logger.LogInformation("Found {Count} bookings to send follow-ups for", bookingsNeedingFollowUp.Count);

        var successCount = 0;
        var failureCount = 0;

        foreach (var booking in bookingsNeedingFollowUp)
        {
            try
            {
                // Send follow-up based on booking type
                if (booking.ServiceId.HasValue && booking.Service != null)
                {
                    await _emailService.SendFollowUpAsync(booking, booking.Customer, booking.Service);
                }
                else if (booking.BundleId.HasValue && booking.Bundle != null)
                {
                    // TODO: Implement bundle follow-up email
                    _logger.LogWarning("Bundle follow-up email not yet implemented for booking {BookingId}", booking.Id);
                    continue;
                }
                else
                {
                    _logger.LogWarning("Booking {BookingId} has neither service nor bundle", booking.Id);
                    continue;
                }

                // Log the email
                _context.EmailLogs.Add(new EmailLog
                {
                    BookingId = booking.Id,
                    RecipientEmail = booking.Customer.Email,
                    EmailType = EmailType.FollowUp,
                    SentAt = DateTime.UtcNow,
                    Status = EmailStatus.Sent,
                    Subject = "Wie war Ihre Behandlung?"
                });

                await _context.SaveChangesAsync();

                successCount++;
                _logger.LogInformation(
                    "Follow-up sent for booking {BookingId} to {Email}",
                    booking.Id,
                    booking.Customer.Email
                );
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(
                    ex,
                    "Failed to send follow-up for booking {BookingId}",
                    booking.Id
                );

                // Log the failure
                _context.EmailLogs.Add(new EmailLog
                {
                    BookingId = booking.Id,
                    RecipientEmail = booking.Customer.Email,
                    EmailType = EmailType.FollowUp,
                    SentAt = DateTime.UtcNow,
                    Status = EmailStatus.Failed,
                    ErrorMessage = ex.Message
                });

                await _context.SaveChangesAsync();
            }
        }

        _logger.LogInformation(
            "Finished sending follow-ups. Success: {SuccessCount}, Failed: {FailureCount}",
            successCount,
            failureCount
        );
    }
}
