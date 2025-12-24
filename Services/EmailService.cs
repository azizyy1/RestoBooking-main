using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RestoBooking.Services
{
    public class EmailSettings
    {
        [Required]
        public string Host { get; set; } = null!;

        [Range(1, 65535)]
        public int Port { get; set; }

        public bool EnableSSL { get; set; }

        [Required, EmailAddress]
        public string UserName { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required, EmailAddress]
        public string From { get; set; } = null!;
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly IHostEnvironment _environment;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger, IHostEnvironment environment)
        {
            _settings = settings.Value;
            _logger = logger;
            _environment = environment;
        }

        public async Task SendEmail(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogWarning("Email recipient is missing; skipping send for subject {Subject}", subject);
                return;
            }

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password),
                EnableSsl = _settings.EnableSSL,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            var mail = new MailMessage(_settings.From, to, subject, body)
            {
                IsBodyHtml = true
            };

            try
            {
                await client.SendMailAsync(mail);
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx,
                    "Failed to send email to {Recipient} via {Host}:{Port}. Verify SMTP credentials, SSL requirements, and app passwords.",
                    to, _settings.Host, _settings.Port);

                if (_environment.IsProduction())
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient} via {Host}:{Port}", to, _settings.Host, _settings.Port);

                if (!_environment.IsProduction())
                {
                    throw;
                }
            }
        }
    }
}
