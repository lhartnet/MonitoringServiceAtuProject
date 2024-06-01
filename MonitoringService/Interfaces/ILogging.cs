using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonitoringService.Domain.Models;

namespace MonitoringService.Interfaces
{
    public interface ILogging
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogSpecData(SpecDetails specDetails);
    }
}
