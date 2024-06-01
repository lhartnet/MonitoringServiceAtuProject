using MonitoringService.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService.Interfaces
{
    public interface IEmailService
    {
        void SendAdminErrorMail(string issue, string filePath);
        void SendNewFilesEmail(List<SpecDetails> listOngoingFiles, string v);
    }
}
