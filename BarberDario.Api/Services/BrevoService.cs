using BarberDario.Api.Data.Entities;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BarberDario.Api.Services;

public class BrevoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BrevoService> _logger;
    private readonly string _apiKey;

    public BrevoService(IConfiguration configuration, ILogger<BrevoService> logger, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Brevo");
        _logger = logger;
        _apiKey = configuration["Brevo:ApiKey"] ?? string.Empty;

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    public async Task<bool> CreateOrUpdateContactAsync(Customer customer)
    {
        try
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Brevo API key not configured. Skipping contact sync.");
                return false;
            }

            var contactData = new
            {
                email = customer.Email,
                attributes = new
                {
                    FIRSTNAME = customer.FirstName,
                    LASTNAME = customer.LastName,
                    SMS = customer.Phone
                },
                updateEnabled = true  // Update if contact already exists
            };

            var json = JsonSerializer.Serialize(contactData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.brevo.com/v3/contacts", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully synced customer {Email} to Brevo", customer.Email);
                return true;
            }
            else if ((int)response.StatusCode == 400)
            {
                // Contact might already exist with the same data - this is OK
                var responseBody = await response.Content.ReadAsStringAsync();
                if (responseBody.Contains("Contact already exist"))
                {
                    _logger.LogInformation("Customer {Email} already exists in Brevo", customer.Email);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to sync customer {Email} to Brevo. Status: {Status}, Body: {Body}",
                        customer.Email, response.StatusCode, responseBody);
                    return false;
                }
            }
            else
            {
                _logger.LogWarning("Failed to sync customer {Email} to Brevo. Status: {Status}",
                    customer.Email, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - we don't want Brevo sync failures to block bookings
            _logger.LogError(ex, "Error syncing customer {Email} to Brevo", customer.Email);
            return false;
        }
    }

    public async Task<bool> AddContactToListAsync(string email, int listId)
    {
        try
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Brevo API key not configured. Skipping list addition.");
                return false;
            }

            var requestData = new
            {
                emails = new[] { email }
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"https://api.brevo.com/v3/contacts/lists/{listId}/contacts/add", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Added customer {Email} to Brevo list {ListId}", email, listId);
                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to add customer {Email} to list. Status: {Status}, Body: {Body}",
                    email, response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding customer {Email} to Brevo list {ListId}", email, listId);
            return false;
        }
    }
}
