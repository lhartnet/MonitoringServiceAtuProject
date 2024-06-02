using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using MonitoringService.Domain.Models;
using System.Text;
using MonitoringService.Interfaces;

namespace MonitoringService.Services
{
    /// <summary>
    /// Class to handle extracting information from the PDFs
    /// </summary>
    public class ParsePdfs
    {
        private readonly ILogging _logger;
        private readonly IEmailService _emailService;
        private readonly ISpecDetailsManagement _specDetailsManagement;

        public ParsePdfs(ILogging logging, IEmailService emailService, ISpecDetailsManagement specDetailsManagement)
        {
            _logger = logging;
            _emailService = emailService;
            _specDetailsManagement = specDetailsManagement;
        }

        /// <summary>
        /// Open the pdf file and read the entire document. Call ParseSpecData to extract the information.
        /// </summary>
        /// <param name="pdfPath">The location and name of the file we want to get data from</param>
        /// <param name="folder">string indicating if this is an ongoing or approved folder item</param>
        /// <returns>Return SpecDetails object with information from pdf set to it</returns>
        public SpecDetails ExtractSpecData(string pdfPath, string folder)
        {
            try
            {
                var fileName = Path.GetFileName(pdfPath);
                using var reader = new PdfReader(pdfPath);
                using var pdfDoc = new PdfDocument(reader);
                StringBuilder textBuilder = new ();
                for (var i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    textBuilder.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
                }
                var pdfText = textBuilder.ToString();
                var pdfData = ParseSpecData(pdfText, fileName, folder);
                _logger.LogSpecData(pdfData);
                return pdfData;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading PDF file {pdfPath}: {ex.Message}");
                var issue = $"There was an issue extracting data from {pdfPath}. The exception message is as follows:\n{ex.Message} Please review.";
                _emailService.SendAdminErrorMail(issue, folder);
                return null;
            }
        }

        /// <summary>
        /// Extract the specDetails properties from the pdf and call SetSpecProperties to store them.
        /// Read the title, then keep reading until the next title is reached for the value.
        /// </summary>
        /// <param name="pdfText">The read pdf to be interpreted</param>
        /// <param name="fileName">The name of the file being read as a string</param>
        /// <param name="folder">The type of folder the file is in as a string</param>
        /// <returns>Return the SpecDetails object with info extracted from pdf</returns>
        public SpecDetails ParseSpecData(string pdfText, string fileName, string folder)
        {
            var data = new SpecDetails();
            var lines = pdfText.Split('\n');

            string? currentSection = null;
            var sectionContent = new StringBuilder();

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
                            _specDetailsManagement.SetSpecProperties(data, currentSection, sectionContent.ToString().Trim());
                        }
                        currentSection = trimmedLine;
                        sectionContent.Clear();
                        break;

                    default:
                        if (currentSection != null)
                        {
                            if (sectionContent.Length > 0)
                            {
                                sectionContent.Append(' ');
                            }
                            sectionContent.Append(trimmedLine);
                        }
                        break;
                }
            }

            if (currentSection != null)
            {
                _specDetailsManagement.SetSpecProperties(data, currentSection, sectionContent.ToString().Trim());
            }

            _specDetailsManagement.SetSpecProperties(data, "FileName", fileName);
            _specDetailsManagement.SetSpecProperties(data, "Folder", folder);

            return data;
        }
    }
}
