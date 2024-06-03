using Microsoft.Extensions.Options;
using MonitoringService.Domain.Models;
using System.Globalization;
using System.Text;
using MonitoringService.Interfaces;

namespace MonitoringService.Services
{
    /// <summary>
    /// Class to hold logic for managing the csv file for new approved files.
    /// </summary>
    public class CsvFileManagement
    {
        private readonly ILogging _logger;
        private readonly string _approvedCsvPath;

        public CsvFileManagement(ILogging logging, IOptions<ConfigurableSettings> folderSettings)
        {
            _logger = logging;
            _approvedCsvPath = folderSettings.Value.ApprovedCsv;
        }

        /// <summary>
        /// Creates a csv file populated with spec information and saved to location set in appsettings.json
        /// This file is consumed by another windows service and must follow a strict naming convention.
        /// "bvlib_" followed by the date in format yyyymd so no preceding 0s are present in filename
        /// </summary>
        public void CreateAndSaveCsvFile(List<SpecDetails> fileDetails)
        {
            string todayDate = DateTime.Now.ToString("yyyyMd", CultureInfo.InvariantCulture);
            string csvFileName = $"bvlib_{todayDate}.csv";
            string csvFilePath = Path.Combine(_approvedCsvPath, csvFileName);

            var csvContent = new StringBuilder();
            csvContent.AppendLine("Title,Author,Revision,Date,Area,Purpose,Description");

            foreach (var detail in fileDetails)
            {
                csvContent.AppendLine($"{detail.Title},{detail.Author},{detail.Revision},{detail.Date},{detail.Area},{detail.Purpose},{detail.Description}");
            }

            File.WriteAllText(csvFilePath, csvContent.ToString());
            _logger.LogInformation($"\nCSV file created at {csvFilePath}");
        }
    }
}
