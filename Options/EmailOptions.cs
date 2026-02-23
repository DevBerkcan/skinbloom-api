namespace BarberDario.Api.Options;

public class EmailOptions
{
    public string SmtpServer { get; set; } = "smtp.ionos.de";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Skinbloom Aesthetics";
    public string BaseUrl { get; set; } = "https://skinbloom.runasp.net";
}
