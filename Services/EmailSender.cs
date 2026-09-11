using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

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

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    Credentials    = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl      = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                var mail = new MailMessage
                {
                    From       = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject    = subject,
                    Body       = htmlMessage,
                    IsBodyHtml = true
                };

                mail.To.Add(email);

                await client.SendMailAsync(mail);

                _logger.LogInformation("Email sent to {Email} — Subject: {Subject}", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} — Subject: {Subject}", email, subject);
                throw; // re-throw so caller can handle / surface the error
            }
        }
    }
}
