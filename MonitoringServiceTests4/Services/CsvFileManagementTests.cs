using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonitoringService.Domain.Models;
using MonitoringService.Services;
using System.Globalization;
using System.Text;

namespace MonitoringServiceTests.Services
{
    [TestClass]
    public class CsvFileManagementTests
    {
        private Mock<IOptions<ConfigurableSettings>> _mockOptions;
        private CsvFileManagement _csvFileManagement;
        private string _testDirectory;

        /// <summary>
        /// Create test directory for csv file to go and mock retrieving settings from appsettings.json
        /// Initialise _csvFileManagement
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
           _mockOptions = new Mock<IOptions<ConfigurableSettings>>();

            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            _mockOptions.Setup(o => o.Value).Returns(new ConfigurableSettings { ApprovedCsv = _testDirectory });

            _csvFileManagement = new CsvFileManagement(_mockOptions.Object);
        }

        /// <summary>
        /// Delete test directory after test runs
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        /// <summary>
        /// Test csv file is created as expected, filled with SpecDetails properties and is stored in test directory
        /// </summary>
        [TestMethod]
        public void CreateAndSaveCsvFile_CreatesCsvFile()
        {
            // Arrange - Set up the SpecDetails to be added to the csv
            var fileDetails = new List<SpecDetails>
            {
                new() { Title = "Title1", Author = "Author1", Revision = "1", Date = "2024-06-01", Area = "Area1", Purpose = "Purpose1", Description = "Description1" },
                new() { Title = "Title2", Author = "Author2", Revision = "2", Date = "2024-06-01", Area = "Area2", Purpose = "Purpose2", Description = "Description2" }
            };

            // Act - Run method CreateAndSaveCsvFile using the mock details above
            _csvFileManagement.CreateAndSaveCsvFile(fileDetails);

            // Assert - check file name concatination, file path and mock csv file with same info as above
            var todayDate = DateTime.Now.ToString("yyyyMd", CultureInfo.InvariantCulture);
            var expectedFileName = $"bvlib_{todayDate}.csv";
            var expectedFilePath = Path.Combine(_testDirectory, expectedFileName);

            Assert.IsTrue(File.Exists(expectedFilePath));

            var csvContent = File.ReadAllText(expectedFilePath);
            var expectedContent = new StringBuilder();
            expectedContent.AppendLine("Title,Author,Revision,Date,Area,Purpose,Description");
            expectedContent.AppendLine("Title1,Author1,1,2024-06-01,Area1,Purpose1,Description1");
            expectedContent.AppendLine("Title2,Author2,2,2024-06-01,Area2,Purpose2,Description2");

            // Check the expected outcome is the same as the result from act above
            Assert.AreEqual(expectedContent.ToString(), csvContent);
         }
    }
}
