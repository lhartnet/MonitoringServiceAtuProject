namespace MonitoringService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _ongoingFolderPath;
        private readonly string _approvedFolderPath;

        private readonly string _previousOngoingFileNamesPath;
        private readonly string _previousApprovedFileNamesPath;
        private List<string> _previousOngoingFiles;
        private List<string> _previousApprovedFiles;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _ongoingFolderPath = @"C:\Users\lhartnet\OneDrive - Analog Devices, Inc\Documents\Contemporary Software Development";
            _approvedFolderPath = @"C:\Users\lhartnet\OneDrive - Analog Devices, Inc\Documents\Contemporary Software Development";
            _previousOngoingFileNamesPath = "previousOngoingFiles.txt"; 
            _previousApprovedFileNamesPath = "previousOngoingFiles.txt";
            _previousOngoingFiles = LoadPreviousFileNames(_previousOngoingFileNamesPath);
            _previousApprovedFiles = LoadPreviousFileNames(_previousApprovedFileNamesPath);
        }

        private List<string> LoadPreviousFileNames(string filePath)
        {
            if (File.Exists(filePath))
            {
                return new List<string>(File.ReadAllLines(filePath));
            }
            return new List<string>();
        }

        private void SavePreviousFileNames(string filePath, string[] files)
        {
            File.WriteAllLines(filePath, files);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                _logger.LogInformation("Checking for files in ongoing folder: {folder}", _ongoingFolderPath);
                var newOngoingFiles = CheckFolderContents(_ongoingFolderPath);

                _logger.LogInformation("Checking for files in approved folder: {folder}", _approvedFolderPath);
                 var newApprovedFiles = CheckFolderContents(_approvedFolderPath);

                 _logger.LogInformation("Comparing ongoing files...");
                CompareFolderContents(newOngoingFiles, _previousOngoingFiles, _previousOngoingFileNamesPath);

              
                await Task.Delay(10000, stoppingToken);
            }
        }

        private string[] CheckFolderContents(string filePath)
        {
            try
            {
                string[] files = Directory.GetFiles(filePath);

                _logger.LogInformation($"Files in folder {filePath}:");
                //foreach (string file in files)
                //{
                //    _logger.LogInformation($"- {Path.GetFileName(file)}");
                //}
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading folder contents for {filePath}: {ex.Message}");
                throw;
            }
        }

        private void CompareFolderContents(string[] currentFiles, List<string> existingFiles, string filePath)
        {
            var newFiles = currentFiles.Except(existingFiles);
            if (newFiles.Any())
            {
                _logger.LogInformation("New files added since last run:");
                foreach (string newFile in newFiles)
                {
                    _logger.LogInformation("File added: {file}", Path.GetFileName(newFile));
                }
                SavePreviousFileNames(filePath, currentFiles);
            }
            else
            {
                _logger.LogInformation("No new files added since last run.");

            }
        }
    }
}
