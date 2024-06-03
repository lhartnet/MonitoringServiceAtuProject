using log4net;
using MonitoringService.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MonitoringService.Services
{
    /// <summary>
    /// Class to hold logic for checking if new files have been added to our folders
    /// </summary>
    public class NewFileManagment
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NewFileManagment));
        private readonly IEmailService _emailService;

        public NewFileManagment(IEmailService emailService)
        {
            _emailService = emailService;
        }

        /// <summary>
        /// Get a list of file names in the path. Email admin if there's an issue reading the file names.
        /// </summary>
        /// <param name="filePath">The directory to search for files</param>
        /// <returns>The list of files in that location as an array of strings</returns>
        public string[] CheckFolderContents(string filePath)
        {
            try
            {
                var files = Directory.GetFiles(filePath);
                log.Info($"Retrieved files in folder {filePath}:");
                return files.ToArray();
            }
            catch (Exception ex)
            {
                log.Error($"Error reading folder contents for {filePath}: {ex.Message}");
                var issue =
                    $"There was an issue attempting to read the folder contents for {filePath} by the monitoring service. Here is the exception message:\n{ex.Message}\n\nPlease review.";
                _emailService.SendAdminErrorMail(issue, filePath);
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Compare the list of files in the folder to the list of items in the database, filtered by the folder type.
        /// This allows us to retrieve a list of files that don't exist in the database, and so have been added since our last run.
        /// </summary>
        /// <param name="currentFiles">The list of files in our folder</param>
        /// <param name="existingFiles">The list of files in our database for that folder</param>
        /// <returns>The list of files in that location as an array of strings</returns>
        public string[] CompareFolderContents(string[] currentFiles, List<string> existingFiles)
        {
            List<string> newFiles = new ();

            // Compare just the filename in the folder to the filename in the database and store the full file path of files that are NOT in the database.
            // We need the full filepath for a method later on.
            foreach (var currentFile in currentFiles)
            {
                var fileName = Path.GetFileName(currentFile);
                if (!existingFiles.Contains(fileName))
                {
                    newFiles.Add(currentFile);
                }
            }

            if (newFiles.Any())
            {
                log.Info("New files added since last run:");
                foreach (string newFile in newFiles)
                {
                    log.Info($"File added: {newFile}");
                }
                return newFiles.ToArray();
            }
            else
            {
                log.Info("\n\nNo new files added since last run.\n");
                return Array.Empty<string>();
            }
        }
    }
}
