using BarberDario.Api.Services;
using Hangfire;

namespace BarberDario.Api.BackgroundJobs;

public static class EmailJobsConfiguration
{
    /// <summary>
    /// Configure recurring email jobs for Hangfire
    /// Call this in Program.cs after Hangfire is initialized
    /// </summary>
    public static void ConfigureEmailJobs()
    {
        // Send booking reminders every hour
        // Checks for bookings in the next 24 hours that haven't had a reminder sent
        RecurringJob.AddOrUpdate<EmailReminderService>(
            "send-booking-reminders",
            service => service.SendUpcomingBookingRemindersAsync(),
            Cron.Hourly // Every hour
        );

        // Send follow-up emails every day at 10:00 AM
        // Sends follow-ups for completed bookings from yesterday
        RecurringJob.AddOrUpdate<EmailReminderService>(
            "send-follow-up-emails",
            service => service.SendFollowUpEmailsAsync(),
            Cron.Daily(10) // Every day at 10:00 AM
        );
    }
}
