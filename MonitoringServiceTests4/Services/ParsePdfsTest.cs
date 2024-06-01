using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonitoringService.Domain.Models;
using MonitoringService.Interfaces;
using MonitoringService.Services;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Layout;
using System.Text;
using iText.Forms.Form.Element;


namespace ParsePdfsTest.Services
{
    [TestClass]
    public class ParsePdfsTests
    {
        private Mock<ILogging> _mockLogger;
        private Mock<IEmailService> _mockEmailService;
        private Mock<ISpecDetailsManagement> _mockSpecDetailsManagement;
        private ParsePdfs _parsePdfs;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogging>();
            _mockEmailService = new Mock<IEmailService>();
            _mockSpecDetailsManagement = new Mock<ISpecDetailsManagement>();
            _parsePdfs = new ParsePdfs(_mockLogger.Object, _mockEmailService.Object, _mockSpecDetailsManagement.Object);
        }


        /// <summary>
        /// Ensure that ReturnsSpecDetails returns SpecDetails object when pdf format is valid
        /// </summary>
        [TestMethod]
        public void ExtractSpecData_ReturnsSpecDetails_WhenPdfIsValid()
        {
        
            // Arrange
            var pdfPath = Path.GetTempFileName();
            var folder = "Ongoing";
            CreateSamplePdf(pdfPath);

            // Act
            var result = _parsePdfs.ExtractSpecData(pdfPath, folder);

            // Assert
            Assert.IsNotNull(result);

            // Cleanup
            File.Delete(pdfPath);
         }


        // Create a pdf in correct format for testing
        private void CreateSamplePdf(string filePath)
        {

            using (var writer = new PdfWriter(filePath))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {

                document.Add(new Paragraph("Title"));
                document.Add(new Paragraph("Test Title"));
                document.Add(new Paragraph("Author"));
                document.Add(new Paragraph("Test Author"));
                document.Add(new Paragraph("Revision"));
                document.Add(new Paragraph("TA"));
                document.Add(new Paragraph("Date"));
                document.Add(new Paragraph("21/03/2024"));
                document.Add(new Paragraph("Area"));
                document.Add(new Paragraph("TestArea"));
                document.Add(new Paragraph("Purpose"));
                document.Add(new Paragraph("TestPurpose"));
                document.Add(new Paragraph("Description"));
                document.Add(new Paragraph("Test Description"));
            }
        }

        /// <summary>
        /// Ensure ExtractSpecData doesn't return a SpecDetails object when there is an issue with the file
        /// </summary>
        [TestMethod]
        public void ExtractSpecData_ReturnsNull_WhenExceptionOccurs()
        {
            // Arrange
            var pdfPath = "invalid.pdf";
            var folder = "folder";

            // Act
            var result = _parsePdfs.ExtractSpecData(pdfPath, folder);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// Check that ParseSpecData is reading the files when in the correct format
        /// </summary>
        [TestMethod]
        public void ParseSpecData_ReturnsSpecDetails_WhenTextIsValid()
        {
            // Arrange
            var pdfText = "Title\nSample Title\nAuthor\nSample Author";
            var fileName = "sample.pdf";
            var folder = "folder";

            // Act
            var result = _parsePdfs.ParseSpecData(pdfText, fileName, folder);

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
