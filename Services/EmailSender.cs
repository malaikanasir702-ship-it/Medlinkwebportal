using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace MedLinkPortal.Services
{
    public class EmailSettings
    {
        public string SenderName  { get; set; } = "MedLink";
        public string SenderEmail { get; set; } = "onboarding@resend.dev";

        // Resend API key — set via Railway env var: EmailSettings__ResendApiKey
        public string ResendApiKey { get; set; } = "";

        // Fallback SMTP (kept for reference, not used)
        public string Host     { get; set; } = "smtp.gmail.com";
        public int    Port     { get; set; } = 587;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings        _settings;
        private readonly IHttpClientFactory   _http;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(
            IOptions<EmailSettings> options,
            IHttpClientFactory http,
            ILogger<EmailSender> logger)
        {
            _settings = options.Value;
            _http     = http;
            _logger   = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            // ── Resend API ──────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(_settings.ResendApiKey))
            {
                await SendViaResendAsync(toEmail, subject, htmlBody);
                return;
            }

            // ── Fallback: MailKit SMTP ──────────────────────────────────────
            await SendViaSmtpAsync(toEmail, subject, htmlBody);
        }

        // ── Resend ────────────────────────────────────────────────────────────
        private async Task SendViaResendAsync(string toEmail, string subject, string htmlBody)
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ResendApiKey}");

            var payload = new
            {
                from    = $"{_settings.SenderName} <{_settings.SenderEmail}>",
                to      = new[] { toEmail },
                subject = subject,
                html    = htmlBody
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.resend.com/emails", content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Resend API error {Status}: {Body}", (int)response.StatusCode, body);
                throw new Exception($"Resend API error {(int)response.StatusCode}: {body}");
            }

            _logger.LogInformation("✅ Email sent via Resend to {Email} | Subject: {Subject}", toEmail, subject);
        }

        // ── SMTP fallback (MailKit) ────────────────────────────────────────────
        private async Task SendViaSmtpAsync(string toEmail, string subject, string htmlBody)
        {
            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                await smtp.ConnectAsync(_settings.Host, _settings.Port,
                    MailKit.Security.SecureSocketOptions.StartTls);

                var password = _settings.Password?.Replace(" ", "") ?? "";
                await smtp.AuthenticateAsync(_settings.Username, password);

                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(MimeKit.MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                var builder = new MimeKit.BodyBuilder { HtmlBody = htmlBody };
                message.Body = builder.ToMessageBody();

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(quit: true);

                _logger.LogInformation("✅ Email sent via SMTP to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ SMTP email failed to {Email}", toEmail);
                if (smtp.IsConnected) await smtp.DisconnectAsync(quit: false);
                throw;
            }
        }
    }
}
