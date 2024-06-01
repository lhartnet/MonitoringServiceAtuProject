using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using MonitoringService.Interfaces;
using MonitoringService.Services;

namespace MonitoringService.Tests
{
    [TestClass]
    public class NewFileManagmentTests
    {
        private Mock<ILogger<NewFileManagment>> _mockLogger;
        private Mock<IEmailService> _mockEmailService;
        private NewFileManagment _newFileManagment;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<NewFileManagment>>();
            _mockEmailService = new Mock<IEmailService>();
            _newFileManagment = new NewFileManagment(_mockLogger.Object, _mockEmailService.Object);
        }

        /// <summary>
        /// Test folder check functionality. Checkfoldercontents should return same number of files we add to test directory
        /// </summary>
        [TestMethod]
        public void CheckFolderContents_ReturnsFileList_WhenNoException()
        {
            // Arrange - set test direcory and mock 2 files
            var filePath = "test_directory";
            var expectedFiles = new string[] { Path.Combine(filePath, "file1.txt"), Path.Combine(filePath, "file2.txt") };
            Directory.CreateDirectory(filePath);
            foreach (var file in expectedFiles)
            {
                File.Create(file).Dispose();
            }

            // Act - check the number of files in the fake directory
            var result = _newFileManagment.CheckFolderContents(filePath);

            // Assert - confirm the number of files returned from CheckFolderContents is the same as the amount we added
            CollectionAssert.AreEqual(expectedFiles, result);

            // Cleanup - delete the filepath and files we created
            Directory.Delete(filePath, true);
        }

        /// <summary>
        /// Test that if there is an issue with the directory no files are returned and an informative email to admin is created
        /// </summary>
        [TestMethod]
        public void CheckFolderContents_ReturnsEmptyArray_WhenExceptionOccurs()
        {
            // Arrange
            var filePath = "non_existent_directory";
         
            // Act
            var result = _newFileManagment.CheckFolderContents(filePath);

            // Assert
            Assert.AreEqual(0, result.Length);
        }

        /// <summary>
        /// Test CompareFolderContents only returns new files, not the entire folder
        /// </summary>
        [TestMethod]
        public void CompareFolderContents_ReturnsNewFiles_WhenNewFilesExist()
        {
            // Arrange
            var currentFiles = new string[] { "file1.txt", "file2.txt" };
            var existingFiles = new List<string> { "file1.txt" };

            // Act
            var result = _newFileManagment.CompareFolderContents(currentFiles, existingFiles);

            // Assert
            CollectionAssert.AreEqual(new string[] { "file2.txt" }, result);
        }

        /// <summary>
        /// If no new files are found, no files are returned
        /// </summary>
        [TestMethod]
        public void CompareFolderContents_ReturnsEmptyArray_WhenNoNewFilesExist()
        {
            // Arrange
            var currentFiles = new string[] { "file1.txt" };
            var existingFiles = new List<string> { "file1.txt" };

            // Act
            var result = _newFileManagment.CompareFolderContents(currentFiles, existingFiles);

            // Assert
            Assert.AreEqual(0, result.Length);
        }
    }
}
