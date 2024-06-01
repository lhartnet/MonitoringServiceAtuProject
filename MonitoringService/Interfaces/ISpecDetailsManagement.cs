using MonitoringService.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService.Interfaces
{
    public interface ISpecDetailsManagement
    {
        bool AllSpecFieldsEntered(SpecDetails specRowDocument);
        void SetSpecProperties(SpecDetails data, string propertyName, string value);
    }
}
