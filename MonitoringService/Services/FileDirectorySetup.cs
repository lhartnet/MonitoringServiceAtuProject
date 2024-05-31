using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService.Services
{
    public class FileDirectorySetup
    {
        //private void EnsureDirectoriesExist()
        //{
        //    CreateDirectoryIfNotExists(_ongoingFolderPath);
        //    CreateDirectoryIfNotExists(_approvedFolderPath);
        //    CreateDirectoryIfNotExists(_approvedCsvPath);
        //}

        //private void CreateDirectoryIfNotExists(string path)
        //{
        //    if (!Directory.Exists(path))
        //    {
        //        Directory.CreateDirectory(path);
        //        _logger.LogInformation($"Created directory at: {path}");
        //    }
        //}

        private readonly ILogger<FileDirectorySetup> _logger;

        public FileDirectorySetup(ILogger<FileDirectorySetup> logger)
        {
            _logger = logger;
        }

        public void EnsureDirectoriesExist(params string[] paths)
        {
            foreach (var path in paths)
            {
                CreateDirectoryIfNotExists(path);
            }
        }

        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation($"Created directory at: {path}");
            }
        }
    }
}
