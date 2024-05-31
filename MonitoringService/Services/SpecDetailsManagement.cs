using MonitoringService.Domain.Models;

namespace MonitoringService.Services
{
    /// <summary>
    /// Class to manage the specDetails class
    /// </summary>
    /// <param name="paths">The directories we're check for as an array of strings</param>
    public class SpecDetailsManagement
    {
        /// <summary>
        /// Set the values of the properties of the SpecDetails class
        /// </summary>
        /// <param name="data">The object holding the information to be saved</param>
        /// <param name="property">The property to be saved as a string</param>
        /// <param name="value">The value of the property to be set</param>
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

        /// <summary>
        /// Check if all fields have been filled out in a SpecDetails Object
        /// </summary>
        /// <param name="spec">The SpecDetails object we're checking</param>
        /// <returns>Return true if all fields have a value, return false if one of the properties is null or empty</returns>
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
