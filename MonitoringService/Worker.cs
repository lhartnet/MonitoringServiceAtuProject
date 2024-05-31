using Microsoft.Extensions.Options;
using MonitoringService.Persistence;
using MonitoringService.Services;
using MonitoringService.Domain.Models;

namespace MonitoringService
{
    /// <summary>
    /// Main functional class of this montioring service
    /// </summary>
    public class Worker : BackgroundService
    {
        // Load in all required services, files, parameters
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
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly List<string> _previousOngoingFiles;
        private readonly List<string> _previousApprovedFiles;

        // Worker class constructor
        public Worker(ILogger<Worker> logger, IOptions<ConfigurableSettings> folderSettings, FileDirectorySetup fileDirectorySetup, NewFileManagment newFileManagment, ParsePdfs parsePdfs, CsvFileManagement csvFileManagement, SpecDetailsManagement specDetailsManagement, SpecDbOperations specDbOperations, EmailService emailService, IServiceScopeFactory serviceScopeFactory)
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
            _previousOngoingFiles = _specDbOperations.GetFileNamesFromDatabase("Ongoing");
            _previousApprovedFiles = _specDbOperations.GetFileNamesFromDatabase("Approved");
            _emailService = emailService;
            _fileDirectorySetup.EnsureDirectoriesExist(_ongoingFolderPath, _approvedFolderPath, _approvedCsvPath);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {

                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    // Get list of new files in ongoing folder
                    _logger.LogInformation("Checking for files in ongoing folder: {folder}", _ongoingFolderPath);
                    var ongoingFiles = _newFileManagment.CheckFolderContents(_ongoingFolderPath);
                    _logger.LogInformation("Found " + ongoingFiles.Length + "file in ongoing folder\n");

                    _logger.LogInformation("Comparing ongoing files...");
                    string[] newOngoingFiles = _newFileManagment.CompareFolderContents(ongoingFiles, _previousOngoingFiles);
                    _logger.LogInformation(newOngoingFiles.Length + " new file(s) found in ongoing folder\n");

                    // Get list of new files in approved folder
                    _logger.LogInformation("Checking for files in approved folder: {folder}", _approvedFolderPath);
                    var approvedFiles = _newFileManagment.CheckFolderContents(_approvedFolderPath);
                    _logger.LogInformation("Found " + approvedFiles.Length + " file(s) in approved folder\n");

                    _logger.LogInformation("Comparing approved files...");
                    string[] newApprovedFiles = _newFileManagment.CompareFolderContents(approvedFiles, _previousApprovedFiles);
                    _logger.LogInformation(newApprovedFiles.Length + " new file(s) found in approved folder\n");

                    List<SpecDetails> listOngoingFiles = new List<SpecDetails>();
                    List<SpecDetails> listApprovedFiles = new List<SpecDetails>();

                    List<SpecDetails> emptyListOngoingFiles = new List<SpecDetails>();
                    List<SpecDetails> emptyListApprovedFiles = new List<SpecDetails>();

                    // ONGOING FILES: If the pdf has all the information needed for the specDetails object save it to the database and e-mail recipeients about them.
                    // If there is information missing from the pdf for the specDetails object do not save this to the database and email admin about the issue.
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
                            _logger.LogInformation($"{listOngoingFiles.Count} new ongoing files. Preparing to notify and save to database.\n");
                            _emailService.SendNewFilesEmail(listOngoingFiles,"Ongoing");
                            _specDbOperations.SaveToDatabase(listOngoingFiles);
                        }

                        if (emptyListOngoingFiles.Count > 0)
                        {
                            _logger.LogWarning($"{emptyListOngoingFiles} new files in ongoing folder have missing data. Please review.\n");
                            List<string> fileNames = new List<string>();
                            foreach (SpecDetails spec in emptyListOngoingFiles)
                            {
                                fileNames.Add(spec.FileName);
                            }

                            var fileNameString = string.Join(",", fileNames);

                            var issue =
                                $"Please review the following specs in the ongoing folder:\n{fileNameString}\n\nSome data is missing.";

                            _emailService.SendAdminErrorMail(issue, "Ongoing");
                        }

                    }

                    // APPROVED FILES: If the pdf has all the information needed for the specDetails object save it to the database and e-mail recipients about them.
                    // If there is information missing from the pdf for the specDetails object do not save this to the database and email admin about the issue.
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
                            _logger.LogInformation($"{listApprovedFiles.Count} new approved files. Preparing to notify and save to database.\n");
                            _emailService.SendNewFilesEmail(listApprovedFiles, "Approved");
                            _specDbOperations.SaveToDatabase(listApprovedFiles);
                            _csvFileManagement.CreateAndSaveCsvFile(listApprovedFiles);
                        }

                        if (emptyListApprovedFiles.Count > 0)
                        {
                            _logger.LogWarning($"{emptyListApprovedFiles} new files in approved folder have missing data. Please review.\n");
                            List<string> fileNames = new List<string>();
                            foreach (SpecDetails spec in emptyListApprovedFiles)
                            {
                                fileNames.Add(spec.FileName);
                            }

                            var fileNameString = string.Join(",", fileNames);

                            var issue =
                                $"Please review the following specs in the approved folder:\n{fileNameString}\n\nSome data is missing.";

                            _emailService.SendAdminErrorMail(issue, "Approved");
                        }

                    }

                }
                await Task.Delay(_delayBetweenRuns, stoppingToken);
            }
        }



    }
}


