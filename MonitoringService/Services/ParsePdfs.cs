using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using MonitoringService.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService.Services
{
    public class ParsePdfs
    {
        //private readonly ILogger<ParsePdfs> _logger;
        private readonly Logging _logger;
        private readonly EmailService _emailService;
        private readonly SpecDetailsManagement _specDetailsManagement;

        public ParsePdfs(Logging logging, EmailService emailService, SpecDetailsManagement specDetailsManagement)
        {
            _logger = logging;
            _emailService = emailService;
            _specDetailsManagement = specDetailsManagement;
        }

        public SpecDetails ExtractSpecData(string pdfPath, string folder)
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
                     _logger.LogSpecData(pdfData);
                    return pdfData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading PDF file {pdfPath}: {ex.Message}");
                var issue = $"There was an issue extracting data from {pdfPath}. The exception message is as follows:\n{ex.Message} Please review.";
                _emailService.SendAdminErrorMail(Path.GetFileName(pdfPath), issue, folder);
                return null;
            }
        }

        public SpecDetails ParseSpecData(string pdfText, string fileName, string folder)
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
                                sectionContent.Append(" ");
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
