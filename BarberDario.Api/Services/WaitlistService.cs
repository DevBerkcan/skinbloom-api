using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class WaitlistService
{
    private readonly BarberDarioDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<WaitlistService> _logger;

    public WaitlistService(
        BarberDarioDbContext context,
        EmailService emailService,
        ILogger<WaitlistService> _logger)
    {
        _context = context;
        _emailService = emailService;
        this._logger = _logger;
    }

    /// <summary>
    /// Add a customer to the waitlist for a specific date/time or flexible availability
    /// </summary>
    public async Task<Waitlist> AddToWaitlistAsync(
        Guid customerId,
        Guid? serviceId,
        Guid? bundleId,
        DateOnly? preferredDate,
        TimeOnly? preferredTimeFrom,
        TimeOnly? preferredTimeTo,
        string? notes)
    {
        // Validate: must have either service or bundle
        if (!serviceId.HasValue && !bundleId.HasValue)
        {
            throw new ArgumentException("Service oder Bundle muss angegeben werden");
        }

        if (serviceId.HasValue && bundleId.HasValue)
        {
            throw new ArgumentException("Nur Service ODER Bundle kann angegeben werden, nicht beides");
        }

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
        {
            throw new ArgumentException("Kunde nicht gefunden");
        }

        // Check if service/bundle exists
        if (serviceId.HasValue)
        {
            var service = await _context.Services.FindAsync(serviceId.Value);
            if (service == null || !service.IsActive)
            {
                throw new ArgumentException("Service nicht gefunden oder inaktiv");
            }
        }

        if (bundleId.HasValue)
        {
            var bundle = await _context.ServiceBundles.FindAsync(bundleId.Value);
            if (bundle == null || !bundle.IsActive)
            {
                throw new ArgumentException("Bundle nicht gefunden oder inaktiv");
            }
        }

        var waitlistEntry = new Waitlist
        {
            CustomerId = customerId,
            ServiceId = serviceId,
            BundleId = bundleId,
            PreferredDate = preferredDate,
            PreferredTimeFrom = preferredTimeFrom,
            PreferredTimeTo = preferredTimeTo,
            Notes = notes,
            Status = WaitlistStatus.Active
        };

        _context.Waitlists.Add(waitlistEntry);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Customer {CustomerId} added to waitlist for Service/Bundle {ServiceId}/{BundleId}",
            customerId, serviceId, bundleId
        );

        return waitlistEntry;
    }

    /// <summary>
    /// Notify waitlist entries when a slot becomes available (e.g., after cancellation)
    /// </summary>
    public async Task NotifyWaitlistForSlotAsync(
        Guid? serviceId,
        Guid? bundleId,
        DateOnly bookingDate,
        TimeOnly startTime)
    {
        // Find active waitlist entries matching this service/bundle and date/time
        var query = _context.Waitlists
            .Include(w => w.Customer)
            .Include(w => w.Service)
            .Include(w => w.Bundle)
            .Where(w => w.Status == WaitlistStatus.Active);

        if (serviceId.HasValue)
        {
            query = query.Where(w => w.ServiceId == serviceId.Value);
        }

        if (bundleId.HasValue)
        {
            query = query.Where(w => w.BundleId == bundleId.Value);
        }

        // Filter by date/time if specified
        query = query.Where(w =>
            !w.PreferredDate.HasValue || // Flexible on date
            w.PreferredDate == bookingDate
        );

        var waitlistEntries = await query
            .OrderBy(w => w.CreatedAt) // First come, first served
            .ToListAsync();

        // Filter by time range
        waitlistEntries = waitlistEntries.Where(w =>
            !w.PreferredTimeFrom.HasValue || // Flexible on time
            (startTime >= w.PreferredTimeFrom && startTime <= (w.PreferredTimeTo ?? TimeOnly.MaxValue))
        ).ToList();

        if (waitlistEntries.Count == 0)
        {
            _logger.LogInformation("No waitlist entries found for this slot");
            return;
        }

        _logger.LogInformation("Found {Count} waitlist entries to notify", waitlistEntries.Count);

        // Notify all matching waitlist entries
        foreach (var entry in waitlistEntries)
        {
            try
            {
                var serviceName = entry.Service?.Name ?? entry.Bundle?.Name ?? "Behandlung";

                await _emailService.SendWaitlistNotificationAsync(
                    entry.Customer,
                    serviceName,
                    bookingDate,
                    startTime
                );

                entry.Status = WaitlistStatus.Notified;
                entry.NotifiedAt = DateTime.UtcNow;

                _logger.LogInformation("Notified customer {CustomerId} on waitlist", entry.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify waitlist entry {WaitlistId}", entry.Id);
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Convert a waitlist entry to an actual booking
    /// </summary>
    public async Task<Waitlist> ConvertToBookingAsync(Guid waitlistId, Guid bookingId)
    {
        var entry = await _context.Waitlists.FindAsync(waitlistId);

        if (entry == null)
        {
            throw new ArgumentException("Wartelisten-Eintrag nicht gefunden");
        }

        if (entry.Status == WaitlistStatus.Converted)
        {
            throw new InvalidOperationException("Wartelisten-Eintrag wurde bereits konvertiert");
        }

        entry.Status = WaitlistStatus.Converted;
        entry.ConvertedAt = DateTime.UtcNow;
        entry.ConvertedToBookingId = bookingId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Waitlist entry {WaitlistId} converted to booking {BookingId}", waitlistId, bookingId);

        return entry;
    }

    /// <summary>
    /// Cancel/expire old waitlist entries
    /// This should be called by a background job periodically
    /// </summary>
    public async Task ExpireOldWaitlistEntriesAsync(int daysOld = 30)
    {
        var expireDate = DateTime.UtcNow.AddDays(-daysOld);

        var entriesToExpire = await _context.Waitlists
            .Where(w =>
                (w.Status == WaitlistStatus.Active || w.Status == WaitlistStatus.Notified) &&
                w.CreatedAt < expireDate
            )
            .ToListAsync();

        foreach (var entry in entriesToExpire)
        {
            entry.Status = WaitlistStatus.Expired;
            entry.ExpiredAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Expired {Count} old waitlist entries", entriesToExpire.Count);
    }
}
