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
        public string AdminEmail { get; set; }
    }

    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<Worker> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<Worker> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
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
                _logger.LogError($"Error sending mail: {ex.Message}");
                var errorSubject = "Attn: Error sending mail for Spec Monitoring Service";
                var errorBody = $"Hi,\nThis is a notification to inform you that a mail with the following details failed to send by the monitoring service.\n\nSubject: {subject}\n\nBody: {body}\\n\\nHere is the error message: {{ex.Message}}\nThanks";
                SendEmail(errorSubject, errorBody, "admin");
            }
        }

        public void SendEmail(string subject, string body, string admin)
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

                string[] emailAddresses = _emailSettings.AdminEmail.Split(',');
                foreach (string emailAddress in emailAddresses)
                {
                    mailMessage.To.Add(emailAddress);

                    smtpClient.Send(mailMessage);
                }


            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending mail: {ex.Message}");
                var errorSubject = "Attn: Error sending mail for Spec Monitoring Service";
                var errorBody = $"Hi,\nThis is a notification to inform you that a mail with the following details failed to send by the monitoring service.\n\nSubject: {subject}\n\nBody: {body}\n\nHere is the error message: {ex.Message}\nThanks";
                SendEmail(errorSubject, errorBody, "admin");
            }
        }
    }
}
