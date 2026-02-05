using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class NewsletterService
{
    private readonly BarberDarioDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<NewsletterService> _logger;

    public NewsletterService(
        BarberDarioDbContext context,
        EmailService emailService,
        ILogger<NewsletterService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Subscribe a customer to the newsletter
    /// </summary>
    public async Task<bool> SubscribeAsync(Guid customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);

        if (customer == null)
        {
            throw new ArgumentException("Kunde nicht gefunden");
        }

        if (customer.NewsletterSubscribed)
        {
            _logger.LogWarning("Customer {CustomerId} is already subscribed", customerId);
            return false; // Already subscribed
        }

        customer.NewsletterSubscribed = true;
        customer.NewsletterSubscribedAt = DateTime.UtcNow;
        customer.NewsletterUnsubscribedAt = null;

        // Generate unsubscribe token if not exists
        if (string.IsNullOrEmpty(customer.UnsubscribeToken))
        {
            customer.UnsubscribeToken = Guid.NewGuid().ToString();
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Customer {CustomerId} subscribed to newsletter", customerId);

        return true;
    }

    /// <summary>
    /// Unsubscribe a customer from the newsletter using their token
    /// </summary>
    public async Task<bool> UnsubscribeAsync(string unsubscribeToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.UnsubscribeToken == unsubscribeToken);

        if (customer == null)
        {
            throw new ArgumentException("Ungültiger Abmelde-Link");
        }

        if (!customer.NewsletterSubscribed)
        {
            _logger.LogWarning("Customer {CustomerId} is not subscribed", customer.Id);
            return false; // Already unsubscribed
        }

        customer.NewsletterSubscribed = false;
        customer.NewsletterUnsubscribedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Customer {CustomerId} unsubscribed from newsletter", customer.Id);

        return true;
    }

    /// <summary>
    /// Send a newsletter to all subscribed customers
    /// </summary>
    public async Task SendNewsletterAsync(Guid newsletterId)
    {
        var newsletter = await _context.Newsletters
            .FirstOrDefaultAsync(n => n.Id == newsletterId);

        if (newsletter == null)
        {
            throw new ArgumentException("Newsletter nicht gefunden");
        }

        if (newsletter.Status == NewsletterStatus.Sent)
        {
            throw new InvalidOperationException("Newsletter wurde bereits versendet");
        }

        // Get all subscribed customers
        var subscribers = await _context.Customers
            .Where(c => c.NewsletterSubscribed)
            .ToListAsync();

        if (subscribers.Count == 0)
        {
            _logger.LogWarning("No subscribers found for newsletter {NewsletterId}", newsletterId);
            newsletter.Status = NewsletterStatus.Failed;
            await _context.SaveChangesAsync();
            return;
        }

        newsletter.Status = NewsletterStatus.Sending;
        newsletter.RecipientCount = subscribers.Count;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Starting to send newsletter {NewsletterId} to {Count} subscribers", newsletterId, subscribers.Count);

        var successCount = 0;
        var failureCount = 0;

        foreach (var customer in subscribers)
        {
            var recipient = new NewsletterRecipient
            {
                NewsletterId = newsletterId,
                CustomerId = customer.Id,
                Email = customer.Email
            };

            _context.NewsletterRecipients.Add(recipient);

            try
            {
                // Add unsubscribe link to HTML content
                var htmlWithUnsubscribe = AddUnsubscribeLink(newsletter.HtmlContent, customer.UnsubscribeToken!);

                await _emailService.SendNewsletterEmailAsync(
                    customer.Email,
                    customer.FirstName,
                    newsletter.Subject,
                    htmlWithUnsubscribe
                );

                recipient.Sent = true;
                recipient.SentAt = DateTime.UtcNow;
                successCount++;

                _logger.LogInformation("Newsletter sent to {Email}", customer.Email);
            }
            catch (Exception ex)
            {
                recipient.Failed = true;
                recipient.ErrorMessage = ex.Message;
                failureCount++;

                _logger.LogError(ex, "Failed to send newsletter to {Email}", customer.Email);
            }

            // Save after each email to track progress
            await _context.SaveChangesAsync();
        }

        newsletter.Status = failureCount == 0 ? NewsletterStatus.Sent : NewsletterStatus.Sent;
        newsletter.SentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Finished sending newsletter {NewsletterId}. Success: {SuccessCount}, Failed: {FailureCount}",
            newsletterId,
            successCount,
            failureCount
        );
    }

    private string AddUnsubscribeLink(string htmlContent, string unsubscribeToken)
    {
        var unsubscribeUrl = $"https://gentlelink-skinbloom.vercel.app/unsubscribe?token={unsubscribeToken}";
        var unsubscribeLink = $@"
<div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd;'>
    <p style='font-size: 12px; color: #999;'>
        Sie erhalten diese Email, weil Sie Newsletter abonniert haben.<br>
        <a href='{unsubscribeUrl}' style='color: #999; text-decoration: underline;'>Hier klicken zum Abmelden</a>
    </p>
</div>";

        // Insert before closing body tag
        return htmlContent.Replace("</body>", $"{unsubscribeLink}</body>");
    }
}
