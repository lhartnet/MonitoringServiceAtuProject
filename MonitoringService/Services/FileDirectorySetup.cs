namespace MonitoringService.Services
{
    /// <summary>
    /// While unlikely to happen, a class to ensure the folders containing files exist. And if they don't, create them.
    /// </summary>
    /// <param name="issue">Details of the specific problem</param>
    /// <param name="folder">String to identify if we're dealing with ongoing or approved folder</param>
    public class FileDirectorySetup
    {

        private readonly ILogger<FileDirectorySetup> _logger;

        public FileDirectorySetup(ILogger<FileDirectorySetup> logger)
        {
            _logger = logger;
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
                _logger.LogInformation($"Created directory at: {path}");
            }
        }
    }
}
