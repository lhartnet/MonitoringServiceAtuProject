using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitoringService.Services;

namespace MonitoringServiceTests.Services
{
    [TestClass]
    public class FileDirectorySetupTests
    {
        private FileDirectorySetup _fileDirectorySetup;

        [TestInitialize]
        public void Setup()
        {
            _fileDirectorySetup = new FileDirectorySetup();
        }

        [TestMethod]
        public void EnsureDirectoriesExist_CreatesDirectory_WhenDirectoryDoesNotExist()
        {
            // Arrange - define path and delete if it happens to exist
            var path = "test_directory";
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            // Act - use EnsureDirectoriesExist method to check if our test path exists
            _fileDirectorySetup.EnsureDirectoriesExist(path);

            // Assert - confirm our file directory exists
            Assert.IsTrue(Directory.Exists(path));
            // Delete our test directory
            Directory.Delete(path, true);
        }

    }
}