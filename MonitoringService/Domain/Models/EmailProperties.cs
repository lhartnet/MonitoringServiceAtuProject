﻿namespace MonitoringService.Domain.Models
{
    public class EmailProperties
    {
        /// <summary>
        /// This class represents the appsettings.json Email settings allowing them to be configurable from that file.
        /// </summary>
        public class EmailSettings
        {
            public string SmtpServer { get; set; }
            public int SmtpPort { get; set; }
            public string SenderEmail { get; set; }
            public string SenderPassword { get; set; }
            public string RecipientEmail { get; set; }
            public string AdminEmail { get; set; }
        }
    }
}
