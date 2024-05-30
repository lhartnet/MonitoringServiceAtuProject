using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; }
        public string SenderPassword { get; set; }
        public string RecipientEmail { get; set; }
    }

    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public void SendEmail(string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient(_emailSettings.SmtpServer)
                {
                    Port = _emailSettings.SmtpPort,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false,
                };

                string[] emailAddresses = _emailSettings.RecipientEmail.Split(',');
                foreach (string emailAddress in emailAddresses)
                {
                    mailMessage.To.Add(emailAddress);

                    smtpClient.Send(mailMessage);
                }

                
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error sending email: {ex.Message}", ex);
            }
        }
    }
}
