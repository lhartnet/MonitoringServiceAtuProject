namespace MonitoringService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _folderPath;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _folderPath = @"C:\Users\lhartnet\OneDrive - Analog Devices, Inc\Documents\Contemporary Software Development";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckFolderContents();
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(10000, stoppingToken);
            }
        }

        private async Task CheckFolderContents()
        {
            string[] files = Directory.GetFiles(_folderPath);

            _logger.LogInformation($"Files in folder {_folderPath}:");
            foreach (string file in files)
            {
                _logger.LogInformation($"- {Path.GetFileName(file)}");
            }
        }
    }
}
