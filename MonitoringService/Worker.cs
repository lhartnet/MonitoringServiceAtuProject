using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Options;
using System.Text;
using MonitoringService.Services;

namespace MonitoringService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _ongoingFolderPath;
        private readonly string _approvedFolderPath;
        private readonly EmailService _emailService;

        private readonly string _previousOngoingFileNamesPath;
        private readonly string _previousApprovedFileNamesPath;
        private List<string> _previousOngoingFiles;
        private List<string> _previousApprovedFiles;

        public Worker(ILogger<Worker> logger, IOptions<ConfigurableSettings> folderSettings, EmailService emailService)
        {
            _logger = logger;
            _ongoingFolderPath = folderSettings.Value.Ongoing;
            _approvedFolderPath = folderSettings.Value.Approved;
            _previousOngoingFileNamesPath = "previousOngoingFiles.txt";
            _previousApprovedFileNamesPath = "previousApprovedFiles.txt";
            _previousOngoingFiles = LoadPreviousFileNames(_previousOngoingFileNamesPath);
            _previousApprovedFiles = LoadPreviousFileNames(_previousApprovedFileNamesPath);
            _emailService = emailService;
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
                var ongoingFiles = CheckFolderContents(_ongoingFolderPath);

                _logger.LogInformation("Checking for files in approved folder: {folder}", _approvedFolderPath);
                var approvedFiles = CheckFolderContents(_approvedFolderPath);

                _logger.LogInformation("Comparing ongoing files...");
                string[] newOngoingFiles = CompareFolderContents(ongoingFiles, _previousOngoingFiles, _previousOngoingFileNamesPath);

                _logger.LogInformation("Comparing approved files...");
                string[] newApprovedFiles = CompareFolderContents(approvedFiles, _previousApprovedFiles, _previousApprovedFileNamesPath);

                List<SpecDetails> listOngoingFiles = new List<SpecDetails>();
                List<SpecDetails> listApprovedFiles = new List<SpecDetails>();

                if (newOngoingFiles.Length > 0)
                {
                    foreach (var file in newOngoingFiles)
                    {
                        if (Path.GetExtension(file) == ".pdf")
                        {
                            var fileData = ExtractSpecData(file);

                            listOngoingFiles.Add(fileData);
                        }
                    }

                    SendNewFilesEmail(listOngoingFiles, newOngoingFiles, "Ongoing");
                }

                if (newApprovedFiles.Length > 0)
                {
                    foreach (var file in newApprovedFiles)
                    {
                        if (Path.GetExtension(file) == ".pdf")
                        {
                            var fileData = ExtractSpecData(file);
                            listApprovedFiles.Add(fileData);
                        }
                    }

                    SendNewFilesEmail(listApprovedFiles, newApprovedFiles, "Approved");
                }

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

        private string[] CompareFolderContents(string[] currentFiles, List<string> existingFiles, string filePath)
        {
            var newFiles = currentFiles.Except(existingFiles).ToArray();
            if (newFiles.Any())
            {
                _logger.LogInformation("New files added since last run:");
                foreach (string newFile in newFiles)
                {
                    _logger.LogInformation("File added: {file}", Path.GetFileName(newFile));
                }
                //SavePreviousFileNames(filePath, currentFiles);
                return newFiles;
            }
            else
            {
                _logger.LogInformation("No new files added since last run.");
                return new string[0];
            }
        }

        private SpecDetails ExtractSpecData(string pdfPath)
        {
            try
            {
                var fileName = Path.GetFileName(pdfPath);
                using (PdfReader reader = new PdfReader(pdfPath))
                using (PdfDocument pdfDoc = new PdfDocument(reader))
                {
                    StringBuilder textBuilder = new StringBuilder();
                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        textBuilder.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
                    }
                    string pdfText = textBuilder.ToString();
                    var pdfData = ParseSpecData(pdfText, fileName);
                    LogSpecData(pdfData);
                    return pdfData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading PDF file {pdfPath}: {ex.Message}");
                throw new Exception($"Error extracting data from {pdfPath}: {ex.Message}");
            }
        }

        private SpecDetails ParseSpecData(string pdfText, string fileName)
        {
            var data = new SpecDetails();
            var lines = pdfText.Split('\n');

            string currentSection = null;
            StringBuilder sectionContent = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                switch (trimmedLine)
                {
                    case "Title":
                    case "Author":
                    case "Revision":
                    case "Date":
                    case "Area":
                    case "Purpose":
                    case "Description":
                        if (currentSection != null)
                        {
                            SetSpecProperties(data, currentSection, sectionContent.ToString().Trim());
                        }
                        currentSection = trimmedLine;
                        sectionContent.Clear();
                        break;

                    default:
                        if (currentSection != null)
                        {
                            if (sectionContent.Length > 0)
                            {
                                sectionContent.Append(" ");
                            }
                            sectionContent.Append(trimmedLine);
                        }
                        break;
                }
            }

            if (currentSection != null)
            {
                SetSpecProperties(data, currentSection, sectionContent.ToString().Trim());
            }

            SetSpecProperties(data, "FileName", fileName);

            return data;
        }

        private void SetSpecProperties(SpecDetails data, string property, string value)
        {
            switch (property)
            {
                case "Title":
                    data.Title = value;
                    break;
                case "Author":
                    data.Author = value;
                    break;
                case "Revision":
                    data.Revision = value;
                    break;
                case "Date":
                    data.Date = value;
                    break;
                case "Area":
                    data.Area = value;
                    break;
                case "Purpose":
                    data.Purpose = value;
                    break;
                case "Description":
                    data.Description = value;
                    break;
                case "FileName":
                    data.FileName = value;
                    break;

            }
        }

        private void LogSpecData(SpecDetails data)
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
        }

        private void SendNewFilesEmail(List<SpecDetails> fileDetails, string[] newFiles, string folder)
        {
            var subject = $"ATTN: New files in {folder} folder";
            var body = new StringBuilder();
            body.AppendLine("Hi,\nThe following new files were detected and require attention:\n");
            foreach (var newFile in fileDetails)
            {
                //var details = file
                //body.AppendLine(Path.GetFileName(newFile));
                //body.AppendLine("         Title: " +)
                body.AppendLine(newFile.FileName);
                body.AppendLine("Title:   " + newFile.Title);
                body.AppendLine("Purpose: " + newFile.Purpose);
                body.AppendLine("");
            }

            body.AppendLine("\nThanks");
            _emailService.SendEmail(subject, body.ToString());
        }
    }
}


