using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MedLinkPortal.Services
{
    public class EmailSettings
    {
        public string SenderName  { get; set; } = "MedLink";
        public string SenderEmail { get; set; } = "";
        public string Host        { get; set; } = "smtp.gmail.com";
        public int    Port        { get; set; } = 587;
        public string Username    { get; set; } = "";
        public string Password    { get; set; } = "";
    }

    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<EmailSettings> options, ILogger<EmailSender> logger)
        {
            _settings = options.Value;
            _logger   = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            // Build the MIME message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            try
            {
                // Connect with STARTTLS on port 587
                await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);

                // Gmail App Password — strip spaces if present
                var password = _settings.Password?.Replace(" ", "") ?? "";
                await smtp.AuthenticateAsync(_settings.Username, password);

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(quit: true);

                _logger.LogInformation("✅ Email sent to {Email} | Subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Email FAILED to {Email} | Subject: {Subject} | Host: {Host}:{Port} | User: {User}",
                    toEmail, subject, _settings.Host, _settings.Port, _settings.Username);

                // Disconnect cleanly even on error
                if (smtp.IsConnected)
                    await smtp.DisconnectAsync(quit: false);

                throw; // Re-throw so caller sees the real error message
            }
        }
    }
}
