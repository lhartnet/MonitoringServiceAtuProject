using log4net;

namespace MonitoringService.Services
{
    /// <summary>
    /// While unlikely to happen, a class to ensure the folders containing files exist. And if they don't, create them.
    /// </summary>
    public class FileDirectorySetup
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FileDirectorySetup));

        public FileDirectorySetup()
        {
        }

        /// <summary>
        /// Check if the folder path exists
        /// </summary>
        /// <param name="paths">The directories we're check for as an array of strings</param>
        public void EnsureDirectoriesExist(params string[] paths)
        {
            foreach (var path in paths)
            {
                CreateDirectoryIfNotExists(path);
            }
        }

        /// <summary>
        /// Create the directory if it doesn't exist to ensure we don't have any issues
        /// </summary>
        /// <param name="path">The directory to be created as a string</param>
        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                log.Info($"Created directory at: {path}");
            }
        }
    }
}