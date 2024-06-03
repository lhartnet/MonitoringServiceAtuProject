using log4net;
using Microsoft.Extensions.Options;
using MonitoringService.Domain.Models;
using System.Globalization;
using System.Text;

namespace MonitoringService.Services
{
    /// <summary>
    /// Class to hold logic for managing the csv file for new approved files.
    /// </summary>
    public class CsvFileManagement
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CsvFileManagement));
        private readonly string _approvedCsvPath;

        public CsvFileManagement(IOptions<ConfigurableSettings> folderSettings)
        {
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
            log.Info($"\nCSV file created at {csvFilePath}");
        }
    }
}