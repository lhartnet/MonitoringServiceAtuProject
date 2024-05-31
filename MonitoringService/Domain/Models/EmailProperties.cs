﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService.Domain.Models
{
    public class EmailProperties
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
    }
}