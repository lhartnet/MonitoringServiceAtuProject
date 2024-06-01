using MonitoringService.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonitoringService.Interfaces;

namespace MonitoringService.Services
{
    /// <summary>
    /// Class to manage logging, specifically where longer sections of logs are required
    /// </summary>
    public class Logging : ILogging
    {
        private readonly ILogger<Logging> _logger;

        public Logging(ILogger<Logging> logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogError(string message)
        {
            _logger.LogError(message);
        }

        /// <summary>
        /// Log out the information of the specdetail object
        /// </summary>
        /// <param name="data">The specDetails object to be logged</param>
        public void LogSpecData(SpecDetails data)
        {
            _logger.LogInformation("PDF Data Extracted:");
            _logger.LogInformation($"Title: {data.Title}");
            _logger.LogInformation($"Author: {data.Author}");
            _logger.LogInformation($"Revision: {data.Revision}");
            _logger.LogInformation($"Date: {data.Date}");
            _logger.LogInformation($"Area: {data.Area}");
            _logger.LogInformation($"Purpose: {data.Purpose}");
            _logger.LogInformation($"Description: {data.Description}");
            _logger.LogInformation($"File Name: {data.FileName}");
            _logger.LogInformation($"Folder: {data.Folder}");
        }
    }
}
