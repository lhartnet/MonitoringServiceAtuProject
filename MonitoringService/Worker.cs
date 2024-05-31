using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Options;
using System.Text;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using iText.Layout.Borders;
using MonitoringService.Persistence;
using MonitoringService.Services;
using MonitoringService.Domain.Models;

namespace MonitoringService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly FileDirectorySetup _fileDirectorySetup;
        private readonly NewFileManagment _newFileManagment;
        private readonly string _ongoingFolderPath;
        private readonly string _approvedFolderPath;
        private readonly string _approvedCsvPath;
        private readonly EmailService _emailService;
        private readonly int _delayBetweenRuns;
        //private readonly ApplicationContext _dbContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly string _previousOngoingFileNamesPath;
        private readonly string _previousApprovedFileNamesPath;
        private readonly List<string> _previousOngoingFiles;
        private readonly List<string> _previousApprovedFiles;

        public Worker(ILogger<Worker> logger, IOptions<ConfigurableSettings> folderSettings, FileDirectorySetup fileDirectorySetup , NewFileManagment newFileManagment, EmailService emailService, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _ongoingFolderPath = folderSettings.Value.Ongoing;
            _approvedFolderPath = folderSettings.Value.Approved;
            _approvedCsvPath = folderSettings.Value.ApprovedCsv;
            _delayBetweenRuns = folderSettings.Value.MsBetweenRuns;
            _serviceScopeFactory = serviceScopeFactory;
            _fileDirectorySetup = fileDirectorySetup;
            _newFileManagment = newFileManagment;

            _previousOngoingFileNamesPath = "previousOngoingFiles.txt";
            _previousApprovedFileNamesPath = "previousApprovedFiles.txt";
            //_previousOngoingFiles = LoadPreviousFileNames(_previousOngoingFileNamesPath);
            _previousOngoingFiles = GetFileNamesFromDatabase("Ongoing");
            _previousApprovedFiles = GetFileNamesFromDatabase("Approved");
            //_previousApprovedFiles = LoadPreviousFileNames(_previousApprovedFileNamesPath);
            _emailService = emailService;
            //_dbContext = context;
            _fileDirectorySetup.EnsureDirectoriesExist(_ongoingFolderPath, _approvedFolderPath, _approvedCsvPath);
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

        private List<string> GetFileNamesFromDatabase(string Folder)
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


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {

                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    _logger.LogInformation("Checking for files in ongoing folder: {folder}", _ongoingFolderPath);
                    var ongoingFiles = _newFileManagment.CheckFolderContents(_ongoingFolderPath);


                    _logger.LogInformation("Checking for files in approved folder: {folder}", _approvedFolderPath);
                    var approvedFiles = _newFileManagment.CheckFolderContents(_approvedFolderPath);


                    _logger.LogInformation("Comparing ongoing files...");
                    string[] newOngoingFiles = _newFileManagment.CompareFolderContents(ongoingFiles, _previousOngoingFiles);

                    _logger.LogInformation("Comparing approved files...");
                    string[] newApprovedFiles = _newFileManagment.CompareFolderContents(approvedFiles, _previousApprovedFiles);

                    List<SpecDetails> listOngoingFiles = new List<SpecDetails>();
                    List<SpecDetails> listApprovedFiles = new List<SpecDetails>();

                    List<SpecDetails> emptyListOngoingFiles = new List<SpecDetails>();
                    List<SpecDetails> emptyListApprovedFiles = new List<SpecDetails>();

                    if (newOngoingFiles.Length > 0)
                    {
                        foreach (var file in newOngoingFiles)
                        {
                            if (Path.GetExtension(file) == ".pdf")
                            {
                                var fileData = ExtractSpecData(file, "Ongoing");
                                bool noEmptyFields = AllSpecFieldsEntered(fileData);
                                if (noEmptyFields)
                                {
                                    listOngoingFiles.Add(fileData);
                                }
                                else
                                {
                                    emptyListOngoingFiles.Add(fileData);
                                }

                            }
                        }

                        if (listOngoingFiles.Count > 0)
                        {
                            _emailService.SendNewFilesEmail(listOngoingFiles, newOngoingFiles, "Ongoing");
                            SaveToDatabase(listOngoingFiles);
                        }

                        if (emptyListOngoingFiles.Count > 0)
                        {
                            List<string> fileNames = new List<string>();
                            foreach (SpecDetails spec in emptyListOngoingFiles)
                            {
                                fileNames.Add(spec.FileName);
                            }

                            var fileNameString = string.Join(",", fileNames);

                            var issue =
                                $"Please review the following specs in the ongoing folder:\n{fileNameString}\n\nSome data is missing.";

                            _emailService.SendAdminErrorMail(fileNameString, issue, "Ongoing");
                        }

                    }

                    if (newApprovedFiles.Length > 0)
                    {
                        foreach (var file in newApprovedFiles)
                        {
                            if (Path.GetExtension(file) == ".pdf")
                            {
                                var fileData = ExtractSpecData(file, "Approved");
                                bool noEmptyFields = AllSpecFieldsEntered(fileData);
                                if (noEmptyFields)
                                {
                                    listApprovedFiles.Add(fileData);
                                }
                                else
                                {
                                    emptyListApprovedFiles.Add(fileData);
                                }

                            }
                        }

                        if (listApprovedFiles.Count > 0)
                        {
                            _emailService.SendNewFilesEmail(listApprovedFiles, newApprovedFiles, "Ongoing");
                            SaveToDatabase(listApprovedFiles);
                            CreateAndSaveCsvFile(listApprovedFiles);
                        }

                        if (emptyListApprovedFiles.Count > 0)
                        {
                            List<string> fileNames = new List<string>();
                            foreach (SpecDetails spec in emptyListApprovedFiles)
                            {
                                fileNames.Add(spec.FileName);
                            }

                            var fileNameString = string.Join(",", fileNames);

                            var issue =
                                $"Please review the following specs in the approved folder:\n{fileNameString}\n\nSome data is missing.";

                            _emailService.SendAdminErrorMail(fileNameString, issue, "Approved");
                        }

                    }

                }
                await Task.Delay(_delayBetweenRuns, stoppingToken);
            }
        }

        


        private SpecDetails ExtractSpecData(string pdfPath, string folder)
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
                    var pdfData = ParseSpecData(pdfText, fileName, folder);
                    LogSpecData(pdfData);
                    return pdfData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading PDF file {pdfPath}: {ex.Message}");
                var issue = $"There was an issue extracting data from {pdfPath}. The exception message is as follows:\n{ex.Message} Please review.";
                _emailService.SendAdminErrorMail(Path.GetFileName(pdfPath), issue, folder );
                return null;
            }
        }

        private SpecDetails ParseSpecData(string pdfText, string fileName, string folder)
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
            SetSpecProperties(data, "Folder", folder);

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
                case "Folder":
                    data.Folder = value;
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
            _logger.LogInformation($"Folder: {data.Folder}");
        }

        

        //private void SaveToDatabase(List<SpecDetails> details)
        //{
        //    using var scope = _serviceScopeFactory.CreateScope();
        //    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        //    foreach (var document in details)
        //    {
        //        dbContext.SpecDetails.Add(document);
        //    }

        //    dbContext.SaveChanges();
        //}


        private void SaveToDatabase(List<SpecDetails> details)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            foreach (var specRowDocument in details)
            {
                if (AllSpecFieldsEntered(specRowDocument))
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

        private bool AllSpecFieldsEntered(SpecDetails spec)
        {
            return !string.IsNullOrEmpty(spec.Title)
                   && !string.IsNullOrEmpty(spec.Author)
                   && !string.IsNullOrEmpty(spec.Revision)
                   && !string.IsNullOrEmpty(spec.Date)
                   && !string.IsNullOrEmpty(spec.Area)
                   && !string.IsNullOrEmpty(spec.Purpose)
                   && !string.IsNullOrEmpty(spec.Description)
                   && !string.IsNullOrEmpty(spec.FileName)
                   && !string.IsNullOrEmpty(spec.Folder);
        }

       


        private void CreateAndSaveCsvFile(List<SpecDetails> fileDetails)
        {
            string todayDate = DateTime.Now.ToString("yyyyMd", CultureInfo.InvariantCulture);
            string csvFileName = $"bvlib_{todayDate}.csv";
            string csvFilePath = Path.Combine(_approvedCsvPath, csvFileName);

            var csvContent = new StringBuilder();
            csvContent.AppendLine("Title,Author,Revision,Date,Area,Purpose,Description");

            foreach (var detail in fileDetails)
            {
                csvContent.AppendLine($"{detail.Title},{detail.Author},{detail.Revision},{detail.Date},{detail.Area},{detail.Purpose},{detail.Description}");
            }

            File.WriteAllText(csvFilePath, csvContent.ToString());
            _logger.LogInformation($"CSV file created at {csvFilePath}");
        }
    }
}


