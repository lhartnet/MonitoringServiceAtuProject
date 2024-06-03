using log4net;
using MonitoringService.Domain.Models;
using MonitoringService.Interfaces;

namespace MonitoringService.Persistence
{
    /// <summary>
    /// A class which holds the logic involved with interacting with our SpecDetails database table
    /// </summary>
    public class SpecDbOperations
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SpecDbOperations));
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ISpecDetailsManagement _specDetailsManagement;
        private readonly IEmailService _emailService;

        public SpecDbOperations(IServiceScopeFactory serviceScopeFactory, ISpecDetailsManagement specDetailsManagement, IEmailService emailService)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _specDetailsManagement = specDetailsManagement;
            _emailService = emailService;
        }

        /// <summary>
        /// Retrieves a list of the file names from the database.
        /// </summary>
        /// <param name="Folder">The folder used to filter the database entries based on the "Ongoing" or "Approved" value.</param>
        /// <returns>A list of file names filtered by the specified folder.</returns>
        public List<string> GetFileNamesFromDatabase(string Folder)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            log.Info($"Retrieving list of {Folder} file names from the database...\n");
            return dbContext.SpecDetails.Where(s => s.Folder == Folder).Select(s => s.FileName).ToList();
        }

        /// <summary>
        /// Retrieves a list of the file objects from the database filtered by "ongoing" or "approved".
        /// </summary>
        /// <param name="Folder">The folder used to filter the database entries based on the "Ongoing" or "Approved" value.</param>
        /// <returns>A list of file names filtered by the specified folder.</returns>
        public List<SpecDetails> GetDatabaseEntries(string folder)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            log.Info($"Retrieving list of {folder} files from the database...\n");
            List<SpecDetails> entries = dbContext.SpecDetails.Where(s => s.Folder == folder).ToList();

            return entries;
        }

        /// <summary>
        /// Saves the SpecDetails objects to the database. Skips entries with missing information and admin is emailed.
        /// </summary>
        public void SaveToDatabase(List<SpecDetails> details)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            foreach (var specRowDocument in details)
            {
                if (_specDetailsManagement.AllSpecFieldsEntered(specRowDocument))
                {
                    dbContext.SpecDetails.Add(specRowDocument);
                    log.Info($"Added {specRowDocument.FileName} to the database...\n");
                }
                else
                {
                    log.Warn($"Skipping document {specRowDocument.FileName} due to missing information.");
                    var issue = $"There was an issue retrieving some information from spec {specRowDocument.FileName} in the {specRowDocument.Folder} folder. Please review to ensure spec is formatted correctly and fully complete and update the file.\n";
                    _emailService.SendAdminErrorMail(issue, specRowDocument.Folder);
                }
            }

            log.Info("Saving to database in progress.....\n");
            dbContext.SaveChanges();
        }
    }
}
