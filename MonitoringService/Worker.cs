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
        private readonly ParsePdfs _parsePdfs;
        private readonly SpecDetailsManagement _specDetailsManagement;
        private readonly SpecDbOperations _specDbOperations;
        private readonly CsvFileManagement _csvFileManagement;
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

        public Worker(ILogger<Worker> logger, IOptions<ConfigurableSettings> folderSettings, FileDirectorySetup fileDirectorySetup , NewFileManagment newFileManagment, ParsePdfs parsePdfs, CsvFileManagement csvFileManagement , SpecDetailsManagement specDetailsManagement, SpecDbOperations specDbOperations, EmailService emailService, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _ongoingFolderPath = folderSettings.Value.Ongoing;
            _approvedFolderPath = folderSettings.Value.Approved;
            _approvedCsvPath = folderSettings.Value.ApprovedCsv;
            _delayBetweenRuns = folderSettings.Value.MsBetweenRuns;
            _serviceScopeFactory = serviceScopeFactory;
            _fileDirectorySetup = fileDirectorySetup;
            _newFileManagment = newFileManagment;
            _parsePdfs = parsePdfs;
            _specDetailsManagement = specDetailsManagement;
            _specDbOperations = specDbOperations;
            _csvFileManagement = csvFileManagement;

            _previousOngoingFileNamesPath = "previousOngoingFiles.txt";
            _previousApprovedFileNamesPath = "previousApprovedFiles.txt";
            //_previousOngoingFiles = LoadPreviousFileNames(_previousOngoingFileNamesPath);
            _previousOngoingFiles = _specDbOperations.GetFileNamesFromDatabase("Ongoing");
            _previousApprovedFiles = _specDbOperations.GetFileNamesFromDatabase("Approved");
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
                                var fileData = _parsePdfs.ExtractSpecData(file, "Ongoing");
                                bool noEmptyFields = _specDetailsManagement.AllSpecFieldsEntered(fileData);
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
                            _specDbOperations.SaveToDatabase(listOngoingFiles);
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
                                var fileData = _parsePdfs.ExtractSpecData(file, "Approved");
                                bool noEmptyFields = _specDetailsManagement.AllSpecFieldsEntered(fileData);
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
                            _specDbOperations.SaveToDatabase(listApprovedFiles);
                            _csvFileManagement.CreateAndSaveCsvFile(listApprovedFiles);
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

        
        
    }
}


