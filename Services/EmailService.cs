using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace RestoBooking.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public bool EnableSSL { get; set; }
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string From { get; set; } = null!;
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmail(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(_settings.From) || string.IsNullOrWhiteSpace(_settings.Host))
            {
                // Configuration incomplète : on ne bloque pas la réservation, mais on évite l'exception
                return;
            }

            if (string.IsNullOrWhiteSpace(to))
            {
                return;
            }

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password),
                EnableSsl = _settings.EnableSSL
            };

            var mail = new MailMessage(_settings.From, to, subject, body)
            {
                IsBodyHtml = true
            };

              try
            {
                await client.SendMailAsync(mail);
            }
            catch
            {
                // En production on loggerait l'erreur, mais on évite de bloquer la réservation
            }
        }
    }
}
