using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

public class EmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Fire-and-forget: do NOT await SMTP — it can take 30-60s on cold start
        // and would block registration/forgot-password responses.
        _ = Task.Run(async () =>
        {
            try
            {
                using var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
                {
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                    EnableSsl = true,
                    Timeout   = 30_000  // 30s max — don't hang forever
                };

                var mailMessage = new MailMessage
                {
                    From       = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject    = subject,
                    Body       = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent to {Email} — subject: {Subject}", email, subject);
            }
            catch (Exception ex)
            {
                // Never crash the caller — just log
                _logger.LogError(ex, "Failed to send email to {Email}", email);
            }
        });

        return Task.CompletedTask;  // Returns immediately — registration is instant
    }
}

public class EmailSettings
{
    public string SenderName  { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Host        { get; set; } = string.Empty;
    public int    Port        { get; set; }
    public string Username    { get; set; } = string.Empty;
    public string Password    { get; set; } = string.Empty;
}
