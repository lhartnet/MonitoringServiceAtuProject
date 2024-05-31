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

        public List<string> GetFileNamesFromDatabase(string Folder)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            return dbContext.SpecDetails.Where(s => s.Folder == Folder).Select(s => s.FileName).ToList();
        }

        public List<SpecDetails> GetDatabaseEntries(string folder)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            List<SpecDetails> entries = new List<SpecDetails>();

            if (folder == "Ongoing")
            {
                entries = dbContext.SpecDetails.Where(s => s.Folder == "Ongoing").ToList();
            }
            else if (folder == "Approved")
            {
                entries = dbContext.SpecDetails.Where(s => s.Folder == "Approved").ToList();
            }

            List<SpecDetails> existingSpecs = new List<SpecDetails>();

            Console.WriteLine("Database Entries:");
            foreach (var entry in entries)
            {
                Console.WriteLine($"ID: {entry.Id}, Title: {entry.Title}, Author: {entry.Author}");
                existingSpecs.Add(entry);
            }

            return existingSpecs;
        }


        public void SaveToDatabase(List<SpecDetails> details)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            foreach (var specRowDocument in details)
            {
                if (_specDetailsManagement.AllSpecFieldsEntered(specRowDocument))
                {
                    dbContext.SpecDetails.Add(specRowDocument);
                }
                else
                {
                    _logger.LogWarning($"Skipping document {specRowDocument.FileName} due to missing information.");
                    var issue = $"There was an issue retrieving some information from spec {specRowDocument.FileName} in the {specRowDocument.Folder} folder. Please review to ensure spec is formatted correctly and fully complete and update the file.\n";
                    _emailService.SendAdminErrorMail(specRowDocument.FileName, issue, specRowDocument.Folder);
                }
            }

            dbContext.SaveChanges();
        }

    }
}
