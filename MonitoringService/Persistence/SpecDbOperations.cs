﻿using iText.Layout.Borders;
using Microsoft.Extensions.DependencyInjection;
using MonitoringService.Domain.Models;
using MonitoringService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService.Persistence
{
    /// <summary>
    /// A class which holds the logic involved with interacting with our SpecDetails database table
    /// </summary>
    public class SpecDbOperations
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly SpecDetailsManagement _specDetailsManagement;
        private readonly EmailService _emailService;
        private readonly Logging _logger;

        public SpecDbOperations(IServiceScopeFactory serviceScopeFactory, SpecDetailsManagement specDetailsManagement, EmailService emailService, Logging logging)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _specDetailsManagement = specDetailsManagement;
            _emailService = emailService;
            _logger = logging;
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

            _logger.LogInformation($"Retrieving list of {Folder} file names from the database...\n");
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

            _logger.LogInformation($"Retrieving list of {folder} files from the database...\n");
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
                    _logger.LogInformation($"Added {specRowDocument.FileName} to the database...\n");
                }
                else
                {
                    _logger.LogWarning($"Skipping document {specRowDocument.FileName} due to missing information.");
                    var issue = $"There was an issue retrieving some information from spec {specRowDocument.FileName} in the {specRowDocument.Folder} folder. Please review to ensure spec is formatted correctly and fully complete and update the file.\n";
                    _emailService.SendAdminErrorMail(specRowDocument.FileName, issue, specRowDocument.Folder);
                }
            }

            _logger.LogInformation("Saving to database in progress.....\n");
            dbContext.SaveChanges();
        }

    }
}
