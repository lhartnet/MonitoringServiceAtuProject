using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService.Services
{
    public class NewFileManagment
    {
        private readonly ILogger<NewFileManagment> _logger;
        private readonly EmailService _emailService;

        public NewFileManagment(ILogger<NewFileManagment> logger, EmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        public string[] CheckFolderContents(string filePath)
        {
            try
            {
                string[] files = Directory.GetFiles(filePath);
                _logger.LogInformation($"Retrieved files in folder {filePath}:");
                return files.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading folder contents for {filePath}: {ex.Message}");
                var issue =
                    $"There was an issue attempting to read the folder contents for {filePath} by the monitoring service. Here is the exception message:\n{ex.Message}\n\nPlease review.";
                _emailService. SendAdminErrorMail(filePath, issue, filePath);
                return new string[0];
            }
        }

        public string[] CompareFolderContents(string[] currentFiles, List<string> existingFiles)
        {


            List<string> newFiles = new List<string>();

            foreach (string currentFile in currentFiles)
            {
                string fileName = Path.GetFileName(currentFile);
                if (!existingFiles.Contains(fileName))
                {
                    newFiles.Add(currentFile);
                }
            }

            if (newFiles.Any())
            {
                _logger.LogInformation("New files added since last run:");
                foreach (string newFile in newFiles)
                {
                    _logger.LogInformation("File added: {file}", newFile);
                }
                return newFiles.ToArray();
            }
            else
            {
                _logger.LogInformation("No new files added since last run.");
                return new string[0];
            }
        }

    }
}
