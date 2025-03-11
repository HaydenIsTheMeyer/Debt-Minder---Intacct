using System.Diagnostics;
using Debt_Minder___Intacct.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;
using System.Text.RegularExpressions; // For ISession

namespace Debt_Minder___Intacct.Controllers
{
    public class HomeController : Controller
    {
        public static Display.Response res { get; set; }
        public static pdfResponse.Response Pdfres { get; set; }

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {

            if (res == null)
            {
                await RequestDisplay.GenerateXmlRequest("GDcmwVjESX0kuTSBAz6va1kkfSW4pRg2SnROVXcXJLk0gQM_r3up1H0k");

            }

            var groupedDocuments = res.Operation.Result.Data.SODocuments
                .GroupBy(doc => doc.CustomerName)
                .Select(group => new
                {
                    CustomerName = group.Key,
                    TotalDueSum = group.Sum(doc => doc.TotalDue),
                    CustomerTotalDue = group.First().CustomerTotalDue,
                    NumberOfDocuments = group.Count(),
                    DocId = group.First().DOCID
                });

            var lines = new List<HomeDisplay>();
            foreach (var line in groupedDocuments)
            {
                lines.Add(new HomeDisplay
                {
                    CustomerName = line.CustomerName,
                    TotalDue = line.CustomerTotalDue,
                    DocTotal = line.TotalDueSum,
                    NoDocs = line.NumberOfDocuments
                });
            }

            return View(lines);
        }

        [HttpGet]
        public IActionResult GeneratePdf(string customers)
        {
            List<string> names = customers.Split(',').ToList();
            List<string> docIds = GetDocIdsByCustomers(res, names);

            if (names == null || names.Count < 1)
            {
                return BadRequest("Invalid request parameters.");
            }

            RequestGenerate.GenerateXmlRequest("6bmXR67n3qiPGo5t9_nLTHqzqI4bH_m4_-K4duDCjxqObffpy1yKQ6iP", docIds);

            List<pdfResponse.Result> pdfResults = Pdfres.Operation.Result;
            Dictionary<string, List<byte[]>> customerPdfFiles = new Dictionary<string, List<byte[]>>();

            foreach (string customer in names)
            {
                List<byte[]> pdfList = new List<byte[]>();
                List<string> ids = GetDocIdsByCustomer(res, customer);

                var pdfDataList2 = pdfResults
                    .Where(r => r.Data?.Sodocument != null && ids.Contains(r.Data.Sodocument.Docid))
                    .Select(r => new
                    {
                        PdfData = r.Data.Sodocument.Pdfdata,
                        DocId = r.Data.Sodocument.Docid
                    })
                    .ToList();

                foreach (var pdf in pdfDataList2)
                {
                    // Decode the base64 string into a byte array
                    byte[] pdfBytes = Convert.FromBase64String(pdf.PdfData);
                    pdfList.Add(pdfBytes);
                }

                customerPdfFiles[customer] = pdfList;
            }

            // Create a ZIP file from the customer's PDF data
            byte[] zipData = CreateZipFile(customerPdfFiles, pdfResults);
            return File(zipData, "application/zip", "Customer_Reports.zip");
        }

        private byte[] CreateZipFile(Dictionary<string, List<byte[]>> customerPdfFiles, List<pdfResponse.Result> pdfResults)
        {
            // Input validation
            if (customerPdfFiles == null || !customerPdfFiles.Any() || pdfResults == null || !pdfResults.Any())
            {
                throw new ArgumentException("Customer PDF files or PDF results are empty or null.");
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    int index = 0;

                    foreach (var customerPdf in customerPdfFiles)
                    {
                        string customerFolder = customerPdf.Key.Replace(" ", "_").Trim();
                        if (string.IsNullOrEmpty(customerFolder))
                        {
                            customerFolder = "Unknown_Customer_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                        }

                        // Ensure we don't exceed pdfResults count
                        foreach (var pdfBytes in customerPdf.Value)
                        {
                            if (index >= pdfResults.Count)
                            {
                                throw new InvalidOperationException("Mismatch between customer PDFs and pdfResults count.");
                            }

                            // Use DocId from pdfResults for the filename
                            string docId = pdfResults[index].Data?.Sodocument?.Docid;
                            if (string.IsNullOrEmpty(docId))
                            {
                                docId = $"Unnamed_Doc_{index}";
                            }

                            string fileName = $"{customerFolder}/{docId}.pdf";
                            var entry = archive.CreateEntry(fileName);

                            using (var entryStream = entry.Open())
                            {
                                if (pdfBytes == null || pdfBytes.Length == 0)
                                {
                                    throw new InvalidOperationException($"PDF data for {fileName} is null or empty.");
                                }

                                entryStream.Write(pdfBytes, 0, pdfBytes.Length);
                            }

                            index++;
                        }
                    }
                } // Ensure ZipArchive is fully disposed before accessing memoryStream

                // Return the byte array after the archive is fully written
                return memoryStream.ToArray();
            }
        }

        public IActionResult Details(string customerName, string ColumnName)
        {
            if (ColumnName == "Email")
            {
                var documents = res.Operation.Result.Data.SODocuments
                                .Where(doc => doc.CustomerName == customerName)
                                .Select(doc => new EmailViewModel
                                {
                                    CustomerName = doc.CustomerName,
                                    Email1 = doc.EMAIL1,
                                    Email2 = doc.EMAIL2
                                });

                var model = new EmailViewModel
                {
                    CustomerName = customerName,
                    Email1 = documents.ElementAt(0).Email1,
                    Email2 = documents.ElementAt(0).Email2
                };
                return View("Email", model);
            }
            else
            {
                var documents = res.Operation.Result.Data.SODocuments
                                .Where(doc => doc.CustomerName == customerName)
                                .Select(doc => new DocumentDetail
                                {
                                    DOCNo = doc.DOCNO, // Adjust properties as needed
                                    DOCID = doc.DOCID,
                                    EXTERNALREFNO = doc.EXTERNALREFNO ?? "N/A",
                                    ORIGDOCDATE = doc.ORIGDOCDATE, // Replace with actual date prop
                                    TotalDue = doc.TotalDue
                                }).ToList();

                var model = new CustomerDetailsViewModel
                {
                    CustomerName = customerName,
                    Documents = documents
                };
                return View("Details", model);

            }
        }




        [HttpGet]
        public IActionResult GetPdf(string sessionKey, string fileName)
        {
            try
            {
                string? pdfDataJson = HttpContext.Session.GetString(sessionKey);
                if (string.IsNullOrEmpty(pdfDataJson))
                {
                    return NotFound("PDF data not found in session.");
                }

                var pdfList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PdfViewModel>>(pdfDataJson);
                var pdf = pdfList.FirstOrDefault(p => p.FileName == fileName);
                if (pdf == null || string.IsNullOrEmpty(pdf.Base64Data))
                {
                    return NotFound("PDF not found.");
                }

                byte[] pdfBytes = Convert.FromBase64String(pdf.Base64Data);
                return File(pdfBytes, "application/pdf", $"{fileName}.pdf");
            }
            catch (Exception ex)
            {
                return Content($"Error retrieving PDF: {ex.Message}");
            }
        }

        public List<string> GetDocIdsByCustomers(Display.Response response, List<string> customerNames)
        {
            return response?.Operation?.Result?.Data?.SODocuments?
                .Where(doc => customerNames.Contains(doc.CustomerName, StringComparer.OrdinalIgnoreCase))
                .Select(doc => doc.DOCID)
                .ToList() ?? new List<string>();
        }

        public List<string> GetDocIdsByCustomer(Display.Response response, string customerName)
        {
            return response?.Operation?.Result?.Data?.SODocuments?
                .Where(doc => doc.CustomerName.Equals(customerName, StringComparison.OrdinalIgnoreCase))
                .Select(doc => doc.DOCID)
                .ToList() ?? new List<string>();
        }
        // ... (Privacy and Error methods unchanged)





    }



    public class PdfViewModel
    {
        public string FileName { get; set; }
        public string? Base64Data { get; set; }
    }

    public class GenerateResultViewModel
    {
        public List<string> SelectedCustomers { get; set; }
        public List<PdfViewModel> PdfFiles { get; set; }
        public string SessionKey { get; set; }
    }
}