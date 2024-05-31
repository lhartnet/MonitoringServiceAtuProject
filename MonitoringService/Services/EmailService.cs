using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Text;
using MonitoringService.Domain.Models;

namespace MonitoringService.Services
{
    /// <summary>
    /// Class to hold logic for sending e-mails to users
    /// </summary>
    public class EmailService
    {
        private readonly EmailProperties.EmailSettings _emailSettings;
        private readonly ILogger<Worker> _logger;

        public EmailService(IOptions<EmailProperties.EmailSettings> emailSettings, ILogger<Worker> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Send e-mail to users listed in "RecipientEmail" or "AdminEmail" setting in appsettings.json.
        /// If multiple users are on the list they must be separated by a comma without spaces.
        /// We don't want the service to break when there is an issue, so if there's an issue sending a mail we attempt to e-mail the site admin about the issue.
        /// </summary>
        /// <param name="subject">The text to be set as the subject of the e-mail.</param>
        /// <param name="body">The text to be set as the body of the email.</param>
        /// <param name="emailTo">A string to identify if this mail should be sent to admin or regular recipents</param>
        public void SendEmail(string subject, string body, string emailTo)
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

                List<string> emailAddresses = new List<string>();
                if (emailTo == "Admin")
                {
                    emailAddresses = _emailSettings.AdminEmail.Split(',').ToList();
                }
                else
                {
                    emailAddresses = _emailSettings.RecipientEmail.Split(',').ToList();
                }

                foreach (string emailAddress in emailAddresses)
                {
                    mailMessage.To.Add(emailAddress);

                    smtpClient.Send(mailMessage);
                    _logger.LogInformation($"Email sent to {emailAddress}: {mailMessage.Subject}");
                }


            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending mail: {ex.Message}");
                var errorSubject = "Attn: Error sending mail for Spec Monitoring Service";
                var errorBody = $"Hi,\nThis is a notification to inform you that a mail with the following details failed to send by the monitoring service.\n\nSubject: {subject}\n\nBody: {body}\\n\\nHere is the error message: {{ex.Message}}\nThanks";
                SendEmail(errorSubject, errorBody, "Admin");
            }
        }


        /// <summary>
        /// Custom e-mail to let recipients know that new files have been found in the ongoing and/or approved folder
        /// </summary>
        /// <param name="fileDetails">The list of objects that were found in the folder</param>
        /// <param name="folder">String to identify if we're dealing with ongoing or approved folder</param>
        public void SendNewFilesEmail(List<SpecDetails> fileDetails, string folder)
        {
            var subject = $"ATTN: New files in {folder} folder";
            var body = new StringBuilder();
            body.AppendLine("Hi,\nThe following new files were detected and require attention:\n");
            foreach (var newFile in fileDetails)
            {
                body.AppendLine(newFile.FileName);
                body.AppendLine("Title:   " + newFile.Title);
                body.AppendLine("Purpose: " + newFile.Purpose);
                body.AppendLine("");
            }

            body.AppendLine("\nThanks");
            SendEmail(subject, body.ToString(), "Recipient");
        }

        /// <summary>
        /// Custom e-mail to let admin know that there has been an error with the service. This is to replace an exception being thrown.
        /// </summary>
        /// <param name="issue">Details of the specific problem</param>
        /// <param name="folder">String to identify if we're dealing with ongoing or approved folder</param>
        public void SendAdminErrorMail(string issue, string folder)
        {
            var subject = $"ATTN: Error with {folder} spec";
            var body = new StringBuilder();
            body.AppendLine("Hi,\nThere was an issue with the spec monitoring service.\n");
            body.AppendLine(issue);

            body.AppendLine("\nThanks");
            SendEmail(subject, body.ToString(), "Admin");
        }
    }
}
