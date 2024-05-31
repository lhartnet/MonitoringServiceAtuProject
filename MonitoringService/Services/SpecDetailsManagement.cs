using MonitoringService.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService.Services
{
    public class SpecDetailsManagement
    {
        public void SetSpecProperties(SpecDetails data, string property, string value)
        {
            switch (property)
            {
                case "Title":
                    data.Title = value;
                    break;
                case "Author":
                    data.Author = value;
                    break;
                case "Revision":
                    data.Revision = value;
                    break;
                case "Date":
                    data.Date = value;
                    break;
                case "Area":
                    data.Area = value;
                    break;
                case "Purpose":
                    data.Purpose = value;
                    break;
                case "Description":
                    data.Description = value;
                    break;
                case "FileName":
                    data.FileName = value;
                    break;
                case "Folder":
                    data.Folder = value;
                    break;

            }
        }

        public bool AllSpecFieldsEntered(SpecDetails spec)
        {
            return !string.IsNullOrEmpty(spec.Title)
                   && !string.IsNullOrEmpty(spec.Author)
                   && !string.IsNullOrEmpty(spec.Revision)
                   && !string.IsNullOrEmpty(spec.Date)
                   && !string.IsNullOrEmpty(spec.Area)
                   && !string.IsNullOrEmpty(spec.Purpose)
                   && !string.IsNullOrEmpty(spec.Description)
                   && !string.IsNullOrEmpty(spec.FileName)
                   && !string.IsNullOrEmpty(spec.Folder);
        }

    }
}
