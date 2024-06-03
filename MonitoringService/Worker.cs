using log4net;
using Microsoft.Extensions.Options;
using MonitoringService.Persistence;
using MonitoringService.Services;
using MonitoringService.Domain.Models;
using MonitoringService.Interfaces;

namespace MonitoringService
{
    public class Worker : BackgroundService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Worker));
        private readonly FileDirectorySetup _fileDirectorySetup;
        private readonly NewFileManagment _newFileManagment;
        private readonly ParsePdfs _parsePdfs;
        private readonly ISpecDetailsManagement _specDetailsManagement;
        private readonly SpecDbOperations _specDbOperations;
        private readonly CsvFileManagement _csvFileManagement;
        private readonly string _ongoingFolderPath;
        private readonly string _approvedFolderPath;
        private readonly string _approvedCsvPath;
        private readonly IEmailService _emailService;
        private readonly int _delayBetweenRuns;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private List<string> _previousOngoingFiles;
        private List<string> _previousApprovedFiles;

        // Worker class constructor
        public Worker(IOptions<ConfigurableSettings> folderSettings, FileDirectorySetup fileDirectorySetup, NewFileManagment newFileManagment, ParsePdfs parsePdfs, CsvFileManagement csvFileManagement, ISpecDetailsManagement specDetailsManagement, SpecDbOperations specDbOperations, IEmailService emailService, IServiceScopeFactory serviceScopeFactory)
        {
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
            _emailService = emailService;

            _fileDirectorySetup.EnsureDirectoriesExist(_ongoingFolderPath, _approvedFolderPath, _approvedCsvPath);

            // Initialize file lists
            InitializeFileLists();
        }

        private void InitializeFileLists()
        {
            _previousOngoingFiles = _specDbOperations.GetFileNamesFromDatabase("Ongoing");
            _previousApprovedFiles = _specDbOperations.GetFileNamesFromDatabase("Approved");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    log.Info("\n\nWorker running at: " + DateTimeOffset.Now);

                    // Get list of new files in ongoing folder by comparing existing files to the ones currently in the folder
                    log.Info("");
                    log.Info("Checking for files in ongoing folder: " + _ongoingFolderPath);
                    var ongoingFiles = _newFileManagment.CheckFolderContents(_ongoingFolderPath);
                    log.Info("Found " + ongoingFiles.Length + " file(s) in ongoing folder\n");

                    log.Info("");
                    log.Info("Comparing ongoing files...");
                    string[] newOngoingFiles = _newFileManagment.CompareFolderContents(ongoingFiles, _previousOngoingFiles);
                    log.Info(newOngoingFiles.Length + " new file(s) found in ongoing folder\n");

                    // Get list of new files in approved folder by comparing existing files to the ones currently in the folder
                    log.Info("Checking for files in approved folder: " + _approvedFolderPath);
                    var approvedFiles = _newFileManagment.CheckFolderContents(_approvedFolderPath);
                    log.Info("Found " + approvedFiles.Length + " file(s) in approved folder\n");

                    log.Info("");
                    log.Info("Comparing approved files...");
                    string[] newApprovedFiles = _newFileManagment.CompareFolderContents(approvedFiles, _previousApprovedFiles);
                    log.Info(newApprovedFiles.Length + " new file(s) found in approved folder\n");

                    List<SpecDetails> listOngoingFiles = new ();
                    List<SpecDetails> listApprovedFiles = new ();

                    List<SpecDetails> emptyListOngoingFiles = new ();
                    List<SpecDetails> emptyListApprovedFiles = new ();

                    // Process new files
                    ProcessNewFiles(newOngoingFiles, listOngoingFiles, emptyListOngoingFiles, "Ongoing");
                    ProcessNewFiles(newApprovedFiles, listApprovedFiles, emptyListApprovedFiles, "Approved");

                    // Save processed files
                    SaveAndNotify(listOngoingFiles, "Ongoing");
                    SaveAndNotify(listApprovedFiles, "Approved");

                    // Update previous files lists 
                    UpdatePreviousFilesLists(listOngoingFiles, listApprovedFiles);
                }
                log.Info("\n\n\n\n##################################\n RUN HAS COMPLETE... NEXT RUN PENDING....\n##################################\n\n\n");
                await Task.Delay(_delayBetweenRuns, stoppingToken);
            }
        }

        // Extract and parse the information from the PDFs
        private void ProcessNewFiles(string[] newFiles, List<SpecDetails> validList, List<SpecDetails> emptyList, string folderType)
        {
            if (newFiles.Length > 0)
            {
                foreach (var file in newFiles)
                {
                    if (Path.GetExtension(file) == ".pdf")
                    {
                        var fileData = _parsePdfs.ExtractSpecData(file, folderType);
                        bool noEmptyFields = _specDetailsManagement.AllSpecFieldsEntered(fileData);
                        if (noEmptyFields)
                        {
                            validList.Add(fileData);
                        }
                        else
                        {
                            emptyList.Add(fileData);
                        }
                    }
                }

                if (emptyList.Count > 0)
                {
                    log.Warn(emptyList.Count + " new files in " + folderType + " folder have missing data. Please review.");
                    List<string> fileNames = emptyList.Select(spec => spec.FileName).ToList();
                    var fileNameString = string.Join(",", fileNames);
                    var issue = $"Please review the following specs in the {folderType} folder:\n{fileNameString}\n\nSome data is missing.";
                    _emailService.SendAdminErrorMail(issue, folderType);
                }
            }
        }

        private void SaveAndNotify(List<SpecDetails> fileList, string folderType)
        {
            if (fileList.Count > 0)
            {
                log.Info(fileList.Count + " new " + folderType + " files. Preparing to notify and save to database.");
                _emailService.SendNewFilesEmail(fileList, folderType);
                _specDbOperations.SaveToDatabase(fileList);
                if (folderType == "Approved")
                {
                    _csvFileManagement.CreateAndSaveCsvFile(fileList);
                }
            }
        }

        private void UpdatePreviousFilesLists(List<SpecDetails> ongoingFiles, List<SpecDetails> approvedFiles)
        {
            _previousOngoingFiles.AddRange(ongoingFiles.Select(f => f.FileName));
            _previousApprovedFiles.AddRange(approvedFiles.Select(f => f.FileName));
        }
    }
}
