using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        // TODO: Implement actual email sending logic (e.g., using SMTP, SendGrid, etc.)
        _logger.LogInformation($"[EmailService] Password reset link for {toEmail}: {resetLink}");
        return Task.CompletedTask;
    }
}
