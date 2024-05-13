namespace MonitoringService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _ongoingFolderPath;
        private readonly string _approvedFolderPath;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _ongoingFolderPath = @"C:\Users\lhartnet\OneDrive - Analog Devices, Inc\Documents\Contemporary Software Development";
            _approvedFolderPath = @"C:\Users\lhartnet\OneDrive - Analog Devices, Inc\Documents\Contemporary Software Development";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckFolderContents(_ongoingFolderPath);
                await CheckFolderContents(_approvedFolderPath);
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(10000, stoppingToken);
            }
        }

        private async Task CheckFolderContents(string filePath)
        {
            try
            {
                string[] files = Directory.GetFiles(filePath);

                _logger.LogInformation($"Files in folder {filePath}:");
                foreach (string file in files)
                {
                    _logger.LogInformation($"- {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading folder contents for {filePath}: {ex.Message}");
            }
        }
    }
}
