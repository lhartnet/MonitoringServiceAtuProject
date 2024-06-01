using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitoringService.Domain.Models;
using MonitoringService.Services;

namespace MonitoringServiceTests.Services
{
    [TestClass]
    public class SpecDetailsManagementTests
    {
        private SpecDetailsManagement _specDetailsManagement;

        [TestInitialize]
        public void Setup()
        {
            _specDetailsManagement = new SpecDetailsManagement();
        }

        /// <summary>
        /// Test that values being set to the SpecDetails object are being stored as expected
        /// </summary>
        [TestMethod]
        public void SetSpecProperties_SetsPropertyValuesCorrectly()
        {
            // Arrange
            var specDetails = new SpecDetails();

            // Act
            _specDetailsManagement.SetSpecProperties(specDetails, "Title", "Sample Title");
            _specDetailsManagement.SetSpecProperties(specDetails, "Author", "Sample Author");
            _specDetailsManagement.SetSpecProperties(specDetails, "Revision", "1");
            _specDetailsManagement.SetSpecProperties(specDetails, "Date", "01/01/2024");
            _specDetailsManagement.SetSpecProperties(specDetails, "Area", "Sample Area");
            _specDetailsManagement.SetSpecProperties(specDetails, "Purpose", "Sample Purpose");
            _specDetailsManagement.SetSpecProperties(specDetails, "Description", "Sample Description");
            _specDetailsManagement.SetSpecProperties(specDetails, "FileName", "sample.pdf");
            _specDetailsManagement.SetSpecProperties(specDetails, "Folder", "Sample Folder");

            // Assert
            Assert.AreEqual("Sample Title", specDetails.Title);
            Assert.AreEqual("Sample Author", specDetails.Author);
            Assert.AreEqual("1", specDetails.Revision);
            Assert.AreEqual("01/01/2024", specDetails.Date);
            Assert.AreEqual("Sample Area", specDetails.Area);
            Assert.AreEqual("Sample Purpose", specDetails.Purpose);
            Assert.AreEqual("Sample Description", specDetails.Description);
            Assert.AreEqual("sample.pdf", specDetails.FileName);
            Assert.AreEqual("Sample Folder", specDetails.Folder);
        }

        /// <summary>
        /// Ensure that when all values are entered we get a true value
        /// </summary>
        [TestMethod]
        public void AllSpecFieldsEntered_ReturnsTrue_WhenAllFieldsAreFilled()
        {
            // Arrange
            var specDetails = new SpecDetails
            {
                Title = "Sample Title",
                Author = "Sample Author",
                Revision = "1",
                Date = "01/01/2024",
                Area = "Sample Area",
                Purpose = "Sample Purpose",
                Description = "Sample Description",
                FileName = "sample.pdf",
                Folder = "Sample Folder"
            };

            // Act
            var result = _specDetailsManagement.AllSpecFieldsEntered(specDetails);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Ensure that if a field is NOT filled out we get a false value returned
        /// </summary>
        [TestMethod]
        public void AllSpecFieldsEntered_ReturnsFalse_WhenAnyFieldIsEmpty()
        {
            // Arrange
            var specDetails = new SpecDetails
            {
                Title = "Sample Title",
                Author = "Sample Author",
                Revision = "1",
                Date = "01/01/2024",
                Area = "Sample Area",
                Purpose = "Sample Purpose",
                Description = "",
                FileName = "sample.pdf",
                Folder = "Sample Folder"
            };

            // Act
            var result = _specDetailsManagement.AllSpecFieldsEntered(specDetails);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
