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
using Aspose.Words;
using Aspose.Words.MailMerging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static Debt_Minder___Intacct.Controllers.CustomStatement;
using DM_Middle_Ware;
using Microsoft.Win32;
using System.Data;
using System.Drawing;

namespace Debt_Minder___Intacct.Controllers
{
    public class HomeController : Controller
    {
        public static Display.Response res { get; set; }

        public static ResponseStatement.Response StatementRes { get; set; }

        public static ResponseAging.Response AgingRes { get; set; }

        public static pdfResponse.Response Pdfres { get; set; }

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public static string SessionId = "_tdXdiFiRQzjeV7lHYbWGIroDOK9v-rXeqDE1YtG43le5R2G1jTPrUDj";

        public async Task<IActionResult> Index()
        {

            if (res == null)
            {
                await RequestDisplay.GenerateXmlRequest(SessionId);

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
        public async Task<IActionResult> GeneratePdf(string customers)
        {
            List<string> names = customers.Split(',').ToList();
            if (names == null || names.Count < 1)
            {
                return BadRequest("Invalid request parameters.");
            }

            List<string> docIds = GetDocIdsByCustomers(res, names);
            RequestGenerate.GenerateXmlRequest(SessionId, docIds);

            List<pdfResponse.Result> pdfResults = Pdfres.Operation.Result;
            Dictionary<string, List<(byte[] PdfBytes, string DocId)>> customerPdfFiles = new Dictionary<string, List<(byte[], string)>>();

            await RequestAging.CreateRequestXml(SessionId);

            foreach (string customer in names)
            {
                await RequestStatement.CreateRequestXml(SessionId, customer);

                List<(byte[], string)> pdfList = new List<(byte[], string)>();
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
                    byte[] pdfBytes = Convert.FromBase64String(pdf.PdfData);
                    pdfList.Add((pdfBytes, pdf.DocId));
                }

                // Add the statement PDF with a special "Statement" identifier
                byte[] statementBytes = GenerateStatement();
                if (statementBytes != null)
                {
                    pdfList.Add((statementBytes, "Statement"));
                }

                customerPdfFiles[customer] = pdfList;
            }

            // Create a ZIP file from the customer's PDF data
            byte[] zipData = CreateZipFile(customerPdfFiles);
            return File(zipData, "application/zip", "Customer_Reports.zip");
        }

        [HttpPost]
        public async Task<IActionResult> EmailPdf(string customers)
        {
            List<string> names = customers.Split(',').ToList();
            if (names == null || names.Count < 1)
            {
                return BadRequest("Invalid request parameters.");
            }

            List<string> docIds = GetDocIdsByCustomers(res, names);
            RequestGenerate.GenerateXmlRequest(SessionId, docIds);

            List<pdfResponse.Result> pdfResults = Pdfres.Operation.Result;
            Dictionary<string, List<(byte[] PdfBytes, string DocId)>> customerPdfFiles = new Dictionary<string, List<(byte[], string)>>();

            await RequestAging.CreateRequestXml(SessionId);

            foreach (string customer in names)
            {
                await RequestStatement.CreateRequestXml(SessionId, customer);

                List<(byte[], string)> pdfList = new List<(byte[], string)>();
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
                    byte[] pdfBytes = Convert.FromBase64String(pdf.PdfData);
                    pdfList.Add((pdfBytes, pdf.DocId));
                }

                // Add the statement PDF with a special "Statement" identifier
                byte[] statementBytes = GenerateStatement();
                if (statementBytes != null)
                {
                    pdfList.Add((statementBytes, "Statement"));
                }

                //customerPdfFiles[customer] = pdfList;
                byte[] customerFolder = CreateCustomerFolder(customer, pdfList);
                ExtractFolderFromBytes(customerFolder, @"C:\TargetFolder");
                string reciepients = StatementRes.Operation.Result.Invoices[0].Email1;
                DatabaseEngine.InsertEmailLog($@"C:\TargetFolder\{customer.Replace(" ", "_").Trim()}", "hm@kiteview.co.za"/*reciepients*/, "p", "Pending");
                // send email at this point
            }

            return View("EmailPreview", "EmailPreview");
            // Create a ZIP file from the customer's PDF data
            // byte[] zipData = CreateZipFile(customerPdfFiles);
            //return RedirectToAction("EmailPreview", "EmailPreview");//File(zipData, "application/zip", "Customer_Reports.zip");
        }



        public byte[] CreateCustomerFolder(string CustomerName,  List<(byte[] PdfBytes, string DocId)> customerPdfFiles)
        {

            if (customerPdfFiles == null || !customerPdfFiles.Any())
            {
                throw new ArgumentException("Customer PDF files are empty or null.");
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
                {

                        string customerFolder = CustomerName.Replace(" ", "_").Trim();
                        if (string.IsNullOrEmpty(customerFolder))
                        {
                            customerFolder = "Unknown_Customer_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                        }

                        foreach (var (pdfBytes, docId) in customerPdfFiles)
                        {
                            string fileName;

                            // Determine the file name based on whether it is a statement or a customer PDF
                            if (docId == "Statement")
                            {
                                fileName = $"{customerFolder}/Statement.pdf";
                            }
                            else
                            {
                                fileName = $"{customerFolder}/{docId}.pdf";
                            }

                            var entry = archive.CreateEntry(fileName);
                            using (var entryStream = entry.Open())
                            {
                                if (pdfBytes == null || pdfBytes.Length == 0)
                                {
                                    throw new InvalidOperationException($"PDF data for {fileName} is null or empty.");
                                }

                                entryStream.Write(pdfBytes, 0, pdfBytes.Length);
                            }
                        }
                    
                }

                return memoryStream.ToArray();
            }
        }

        public void ExtractFolderFromBytes(byte[] zipBytes, string destinationFolderPath)
        {
            if (zipBytes == null || zipBytes.Length == 0)
                throw new ArgumentException("The zip byte array is null or empty.", nameof(zipBytes));

            if (string.IsNullOrWhiteSpace(destinationFolderPath))
                throw new ArgumentException("Destination folder path must be provided.", nameof(destinationFolderPath));

            // Ensure destination directory exists
            if (!Directory.Exists(destinationFolderPath))
                Directory.CreateDirectory(destinationFolderPath);

            // Create a temporary memory stream from the byte array
            using (var memoryStream = new MemoryStream(zipBytes))
            {
                // Extract the zip archive to the destination folder
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(destinationFolderPath, overwriteFiles: true);
                }
            }
        }

        private byte[] CreateZipFile(Dictionary<string, List<(byte[] PdfBytes, string DocId)>> customerPdfFiles)
        {
            if (customerPdfFiles == null || !customerPdfFiles.Any())
            {
                throw new ArgumentException("Customer PDF files are empty or null.");
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var customerPdf in customerPdfFiles)
                    {
                        string customerFolder = customerPdf.Key.Replace(" ", "_").Trim();
                        if (string.IsNullOrEmpty(customerFolder))
                        {
                            customerFolder = "Unknown_Customer_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                        }

                        foreach (var (pdfBytes, docId) in customerPdf.Value)
                        {
                            string fileName;

                            // Determine the file name based on whether it is a statement or a customer PDF
                            if (docId == "Statement")
                            {
                                fileName = $"{customerFolder}/Statement.pdf";
                            }
                            else
                            {
                                fileName = $"{customerFolder}/{docId}.pdf";
                            }

                            var entry = archive.CreateEntry(fileName);
                            using (var entryStream = entry.Open())
                            {
                                if (pdfBytes == null || pdfBytes.Length == 0)
                                {
                                    throw new InvalidOperationException($"PDF data for {fileName} is null or empty.");
                                }

                                entryStream.Write(pdfBytes, 0, pdfBytes.Length);
                            }
                        }
                    }
                }

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


        public static byte[] GenerateStatement()
        {
            License license = new License();
            license.SetLicense("Aspose.WordsProductFamily.lic");

            ResponseAging.ArAging Aging = AgingRes.Operation.Result.Data.ArAging;

            string xmlString = StatementMapper.MapResponseToCustomStatement(StatementRes, Aging);

            // Parse XML
            XDocument xmlDoc = XDocument.Parse(xmlString);

            // Extract single fields
            var customer = xmlDoc.Descendants("Customer").First();
            var statementDetails = xmlDoc.Descendants("StatementDetails").First();

            var mailMergeData = new Dictionary<string, string>
    {
        { "CUSTOMERID", customer.Element("CustomerID")?.Value },
        { "DISPLAYPRINTAS", customer.Element("DisplayPrintAs")?.Value },
        { "DISPLAYADDR1", customer.Element("DisplayAddr1")?.Value },
        { "DISPLAYADDR2", customer.Element("DisplayAddr2")?.Value },
        { "DISPLAYCITY", customer.Element("DisplayCity")?.Value },
        { "DISPLAYSTATE", customer.Element("DisplayState")?.Value },
        { "DISPLAYZIP", customer.Element("DisplayZip")?.Value },
        { "STATEMENTDATE", statementDetails.Element("StatementDate")?.Value },
        { "TotalDue", statementDetails.Element("TotalDue")?.Value },
        {"Current", statementDetails.Element("Current")?.Value },
        {"in1-30", statementDetails.Element("ThirtyDays")?.Value },
        {"in31-60", statementDetails.Element("SixtyDays")?.Value },
        {"in61-90", statementDetails.Element("NinetyDays")?.Value }



    };

            // Extract table data
            var entries = xmlDoc.Descendants("Entry").Select(entry => new Entry
            {
                PRENTRY_WHENCREATED = entry.Element("WhenCreated")?.Value,
                PRENTRY_RECORDID = entry.Element("RecordID")?.Value,
                PRENTRY_TOTALENTERED = entry.Element("TotalEntered")?.Value,
                PRENTRY_TOTALPAID = entry.Element("TotalPaid")?.Value,
                PRENTRY_BALANCE = entry.Element("Balance")?.Value
            }).ToList();

            // Load the Word document
            Document doc = new Document("ar_statement_18.doc");

            // Execute mail merge for single fields
            doc.MailMerge.Execute(mailMergeData.Select(kvp => kvp.Key).ToArray(),
                                 mailMergeData.Select(kvp => kvp.Value).ToArray());

            // Execute mail merge for table
            doc.MailMerge.ExecuteWithRegions(new CustomMailMergeDataSource(entries, "PRENTRY"));

            // Save the populated document to a memory stream as a PDF
            using (MemoryStream ms = new MemoryStream())
            {
                doc.Save(ms, SaveFormat.Pdf);
                Console.WriteLine("Document populated successfully.");

                // Return the byte array from the memory stream
                return ms.ToArray();
            }
        }
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

    public class Entry
    {
        public string PRENTRY_WHENCREATED { get; set; }
        public string PRENTRY_RECORDID { get; set; }
        public string PRENTRY_TOTALENTERED { get; set; }
        public string PRENTRY_TOTALPAID { get; set; }
        public string PRENTRY_BALANCE { get; set; }
    }
    public class CustomMailMergeDataSource : IMailMergeDataSource
    {
        private readonly List<Entry> _data;
        private readonly string _tableName;
        private int _currentIndex = -1;

        public CustomMailMergeDataSource(List<Entry> data, string tableName)
        {
            _data = data;
            _tableName = tableName;
        }

        public string TableName => _tableName;

        public bool MoveNext()
        {
            _currentIndex++;
            return _currentIndex < _data.Count;
        }

        public bool GetValue(string fieldName, out object fieldValue)
        {
            var currentRecord = _data[_currentIndex];
            var property = typeof(Entry).GetProperty(fieldName);
            if (property != null)
            {
                fieldValue = property.GetValue(currentRecord);
                return true;
            }
            fieldValue = null;
            return false;
        }

        public IMailMergeDataSource GetChildDataSource(string tableName)
        {
            return null; // No nested tables
        }


        

    }
}