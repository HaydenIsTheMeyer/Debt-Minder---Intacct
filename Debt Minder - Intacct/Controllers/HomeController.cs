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
using Aspose.Words.Lists;
using System.Reflection.PortableExecutable;

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

        public static string SessionId = "cKJmmiycuLi2rXNH09ASANHyuLes4nCiZsdx5QsRtq1zR9PQEg-W9b62";

        public async Task<IActionResult> Index2()
        {

            if (res == null)
            {
                await RequestDisplay.GenerateXmlRequest(SessionId);

            }


            DataTable dtDebtorsContact = new DataTable();
            dtDebtorsContact = DatabaseEngine.GetDebtorsContact();


            var groupedDocuments = res.Operation.Result.Data.SODocuments
                .GroupBy(doc => doc.CustomerId)
                .Select(group => new
                {
                    CustomerId = group.Key,
                    CustomerName = group.First().CustomerName,
                    TotalDueSum = group.Sum(doc => doc.TotalDue),
                    CustomerTotalDue = group.First().CustomerTotalDue,
                    NumberOfDocuments = group.Count(),
                    DocId = group.First().DOCID

                });

            var lines = from doc in groupedDocuments
                        join contact in dtDebtorsContact.AsEnumerable()
                        on doc.CustomerId equals contact.Field<string>("CustomerId").ToString() into contactGroup
                        from contact in contactGroup.DefaultIfEmpty()

                        select new HomeDisplay
                        {
                            CustomerId = doc.CustomerId,
                            CustomerName = doc.CustomerName,
                            TotalDue = doc.CustomerTotalDue,
                            DocTotal = doc.TotalDueSum,
                            NoDocs = doc.NumberOfDocuments,
                            Contacted = contact != null ? contact.Field<string>("Contacted") : "",
                            Action = contact != null ? contact.Field<string>("Action") : "",
                            RowClass =  contact != null  && contact.Field<int>("ActionDate") == 1 
           ? "table-danger"
           : ""



                        };



            return View(lines.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> GeneratePdf(string customerIds)
        {
            List<string> Custids = customerIds.Split(',').ToList();
            if (Custids == null || Custids.Count < 1)
            {
                return BadRequest("Invalid request parameters.");
            }

            List<string> docIds = GetDocIdsByCustomers(res, Custids);
            RequestGenerate.GenerateXmlRequest(SessionId, docIds);

            List<pdfResponse.Result> pdfResults = Pdfres.Operation.Result;
            Dictionary<string, List<(byte[] PdfBytes, string DocId)>> customerPdfFiles = new Dictionary<string, List<(byte[], string)>>();


            foreach (string id in Custids)
            {
                await RequestAging.CreateRequestXml(SessionId, id);

                await RequestStatement.CreateRequestXml(SessionId, id);




                List<(byte[], string)> pdfList = new List<(byte[], string)>();
                List<string> ids = GetDocIdsByCustomer(res, id);

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

                DataTable dtAttachments = new DataTable();
                dtAttachments = DatabaseEngine.GetAttachments(id);

                foreach (DataRow row in dtAttachments.Rows)
                {
                    byte[] FileData = (byte[])row["FileData"];
                    string FileName = row["FileName"].ToString();
                    pdfList.Add((FileData, FileName));
                }

                customerPdfFiles[id] = pdfList;
            }

            // Create a ZIP file from the customer's PDF data
            byte[] zipData = CreateZipFile(customerPdfFiles);
            return File(zipData, "application/zip", "Customer_Reports.zip");
        }

        [HttpPost]
        public async Task<IActionResult> EmailPdf(string customers)
        {
            List<string> CustomerIds = customers.Split(',').ToList();
            if (CustomerIds == null || CustomerIds.Count < 1)
            {
                return BadRequest("Invalid request parameters.");
            }

            List<string> docIds = GetDocIdsByCustomers(res, CustomerIds);
            RequestGenerate.GenerateXmlRequest(SessionId, docIds);

            List<pdfResponse.Result> pdfResults = Pdfres.Operation.Result;
            Dictionary<string, List<(byte[] PdfBytes, string DocId)>> customerPdfFiles = new Dictionary<string, List<(byte[], string)>>();


            foreach (string customerId in CustomerIds)
            {
                await RequestAging.CreateRequestXml(SessionId, customerId);

                await RequestStatement.CreateRequestXml(SessionId, customerId);

                List<(byte[], string)> pdfList = new List<(byte[], string)>();
                List<string> ids = GetDocIdsByCustomer(res, customerId);

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
                byte[] customerFolder = CreateCustomerFolder(customerId, pdfList);
                ExtractFolderFromBytes(customerFolder, @"C:\TargetFolder");
                string reciepients = StatementRes.Operation.Result.Invoices[0].Email1;
                DatabaseEngine.InsertEmailLog($@"C:\TargetFolder\{customerId.Replace(" ", "_").Trim()}", "hm@kiteview.co.za"/*reciepients*/, "p", "Pending");
                // send email at this point
            }

            var redirectUrl = Url.Action("EmailPreview", "EmailPreview");
            return Json(new { redirect = redirectUrl });            // Create a ZIP file from the customer's PDF data
            // byte[] zipData = CreateZipFile(customerPdfFiles);
            //return RedirectToAction("EmailPreview", "EmailPreview");//File(zipData, "application/zip", "Customer_Reports.zip");
        }



        public byte[] CreateCustomerFolder(string CustomerId, List<(byte[] PdfBytes, string DocId)> customerPdfFiles)
        {

            if (customerPdfFiles == null || !customerPdfFiles.Any())
            {
                throw new ArgumentException("Customer PDF files are empty or null.");
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
                {

                    string customerFolder = CustomerId.Replace(" ", "_").Trim();
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


        [HttpPost]
        public IActionResult Details(string customerName, string ColumnName)
        {
            return null;

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

        public List<string> GetDocIdsByCustomers(Display.Response response, List<string> customerIds)
        {
            return response?.Operation?.Result?.Data?.SODocuments?
                .Where(doc => customerIds.Contains(doc.CustomerId, StringComparer.OrdinalIgnoreCase))
                .Select(doc => doc.DOCID)
                .ToList() ?? new List<string>();
        }

        public List<string> GetDocIdsByCustomer(Display.Response response, string customerId)
        {
            return response?.Operation?.Result?.Data?.SODocuments?
                .Where(doc => doc.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase))
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
                {"in1-30", statementDetails.Element("in1-30")?.Value },
                {"in31-60", statementDetails.Element("in31-60")?.Value },
                {"in61-90", statementDetails.Element("in61-90")?.Value }



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
            Document doc = new Document("18ar_statement.doc");

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


        //public void PopulateAdditionalDocs()
        //{

        //    DataTable dtAdditionalDocs = new DataTable();
        //    dtAdditionalDocs = DatabaseEngine.GetAdditionalDocs();

        //    string month = DateTime.Now.ToString("MMM");
        //    string selectedfolder = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\EmailSettings", "InvoicePath", null);

        //    string folderPath = $@"{selectedfolder}\{month} Invoices";



        //    foreach (DataRow row in dtAdditionalDocs.Rows)
        //    {
        //        string Account = row["Account"].ToString();

        //        string clientPath = folderPath + $@"\" + Account;
        //        string sourcePath = row["FilePath"].ToString();
        //        string Reference = row["Reference"].ToString();


        //        if (Directory.Exists(clientPath))
        //        {

        //            if (row["DocType"].ToString().Trim() == "Header")
        //            {

        //                if (File.Exists(sourcePath))
        //                {

        //                    CopyFile(sourcePath, clientPath);


        //                }

        //            }
        //            else
        //            {

        //                string InvPath = Path.Combine(clientPath, $@"{Reference.Trim()}.pdf");
        //                string CombinePath = Path.Combine(clientPath, $@"{Reference.Trim()} + POD.pdf");

        //                foreach (string ItemPath in sourcePath.Split(','))
        //                {
        //                    if (File.Exists(InvPath) && File.Exists(ItemPath))
        //                    {
        //                        if (ItemPath.Contains(".pdf"))
        //                        {
        //                            CombinePdfs(InvPath, ItemPath, CombinePath);

        //                        }
        //                    }
        //                }

        //            }

        //        }
        //    }


        //}

        //public static void CombinePdfs(string pdf1Path, string pdf2Path, string outputPdfPath)
        //{
        //    try
        //    {
        //        // Open the first PDF document
        //        PdfDocument pdf1 = PdfReader.Open(pdf1Path, PdfDocumentOpenMode.Import);

        //        // Open the second PDF document
        //        PdfDocument pdf2 = PdfReader.Open(pdf2Path, PdfDocumentOpenMode.Import);

        //        // Create a new PDF document to hold the combined content
        //        PdfDocument outputPdf = new PdfDocument();

        //        // Add all pages from the first PDF
        //        for (int i = 0; i < pdf1.PageCount; i++)
        //        {
        //            outputPdf.AddPage(pdf1.Pages[i]);
        //        }

        //        // Add all pages from the second PDF
        //        for (int i = 0; i < pdf2.PageCount; i++)
        //        {
        //            outputPdf.AddPage(pdf2.Pages[i]);
        //        }

        //        // Save the combined PDF
        //        outputPdf.Save(outputPdfPath);

        //        // Delete the first PDF file after combining
        //        if (File.Exists(pdf1Path))
        //        {
        //            File.Delete(pdf1Path);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($@"An error occurred - {ex.Message}", "Error Combining files - 001", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}


        //public static void CopyFile(string sourceFilePath, string destinationDirectory)
        //{
        //    try
        //    {
        //        // Check if source file exists
        //        if (File.Exists(sourceFilePath))
        //        {
        //            // Ensure the destination directory exists
        //            if (!Directory.Exists(destinationDirectory))
        //            {
        //                Directory.CreateDirectory(destinationDirectory);
        //            }

        //            // Get the file name from the source file path
        //            string fileName = Path.GetFileName(sourceFilePath);

        //            // Create the full destination file path
        //            string destinationFilePath = Path.Combine(destinationDirectory, fileName);

        //            // Copy the file to the destination
        //            File.Copy(sourceFilePath, destinationFilePath, overwrite: true);

        //            Console.WriteLine($"File copied successfully to {destinationFilePath}");
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        //Console.WriteLine($"Error copying file: {ex.Message}");
        //        MessageBox.Show($@"An error occured - {ex.Message}", "Error Copying files - 004", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}
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