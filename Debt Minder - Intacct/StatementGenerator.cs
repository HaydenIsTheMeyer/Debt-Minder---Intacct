using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Data;
using Debt_Minder___Intacct.Controllers;

namespace Debt_Minder___Intacct
{
    public static class StatementGenerator
    {
        public static byte[] GenerateStatement()
        {
            ResponseAging.ArAging Aging = HomeController.AgingRes.Operation.Result.Data.ArAging;
            string xmlString = StatementMapper.MapResponseToCustomStatement(HomeController.StatementRes, Aging);
            XDocument xmlDoc = XDocument.Parse(xmlString);

            // Extract single fields
            var customer = xmlDoc.Descendants("Customer").First();
            var statementDetails = xmlDoc.Descendants("StatementDetails").First();

            var mailMergeData = new Dictionary<string, string>
            {
                { "CUSTOMERID", customer.Element("CustomerID")?.Value ?? string.Empty },
                { "DISPLAYPRINTAS", customer.Element("DisplayPrintAs")?.Value ?? string.Empty },
                { "DISPLAYADDR1", customer.Element("DisplayAddr1")?.Value ?? string.Empty },
                { "DISPLAYADDR2", customer.Element("DisplayAddr2")?.Value ?? string.Empty },
                { "DISPLAYCITY", customer.Element("DisplayCity")?.Value ?? string.Empty },
                { "DISPLAYSTATE", customer.Element("DisplayState")?.Value ?? string.Empty },
                { "DISPLAYZIP", customer.Element("DisplayZip")?.Value ?? string.Empty },
                { "STATEMENTDATE", statementDetails.Element("StatementDate")?.Value ?? string.Empty },
                { "TotalDue", statementDetails.Element("TotalDue")?.Value ?? string.Empty },
                { "Current", statementDetails.Element("Current")?.Value ?? string.Empty },
                { "in1-30", statementDetails.Element("in1-30")?.Value ?? string.Empty },
                { "in31-60", statementDetails.Element("in31-60")?.Value ?? string.Empty },
                { "in61-90", statementDetails.Element("in61-90")?.Value ?? string.Empty }
            };

            // Extract table data
            var entries = xmlDoc.Descendants("Entry").Select(entry => new Entry
            {
                PRENTRY_WHENCREATED = entry.Element("WhenCreated")?.Value ?? string.Empty,
                PRENTRY_RECORDID = entry.Element("RecordID")?.Value ?? string.Empty,
                PRENTRY_TOTALENTERED = entry.Element("TotalEntered")?.Value ?? string.Empty,
                PRENTRY_TOTALPAID = entry.Element("TotalPaid")?.Value ?? string.Empty,
                PRENTRY_BALANCE = entry.Element("Balance")?.Value ?? string.Empty
            }).ToList();

            using (var richEdit = new RichEditDocumentServer())
            {
                // Load template
                richEdit.LoadDocument("18ar_statement.doc");

                // Prepare data table for table merge
                DataTable tableData = new DataTable("PRENTRY");
                tableData.Columns.Add("PRENTRY_WHENCREATED", typeof(string));
                tableData.Columns.Add("PRENTRY_RECORDID", typeof(string));
                tableData.Columns.Add("PRENTRY_TOTALENTERED", typeof(string));
                tableData.Columns.Add("PRENTRY_TOTALPAID", typeof(string));
                tableData.Columns.Add("PRENTRY_BALANCE", typeof(string));

                foreach (var entry in entries)
                {
                    tableData.Rows.Add(
                        entry.PRENTRY_WHENCREATED,
                        entry.PRENTRY_RECORDID,
                        entry.PRENTRY_TOTALENTERED,
                        entry.PRENTRY_TOTALPAID,
                        entry.PRENTRY_BALANCE
                    );
                }

                // Set up mail merge
                richEdit.Options.MailMerge.DataSource = tableData;
                richEdit.Options.MailMerge.ViewMergedData = true;
                SearchOptions searchOptions = new SearchOptions();
                // Perform mail merge for single fields
                foreach (var field in mailMergeData)
                {
                    richEdit.Document.Fields.Create(richEdit.Document.CreatePosition(0), $"MERGEFIELD {field.Key}").Update();
                    richEdit.Document.ReplaceAll($"«{field.Key}»", field.Value,searchOptions );
                }

                // Perform mail merge for table
              //  richEdit.Document.MailMerge(richEdit.Document, );

                // Save to PDF
                using (MemoryStream ms = new MemoryStream())
                {
                    richEdit.ExportToPdf(ms);
                    return ms.ToArray();
                }
            }
        }
    }

    public class Entry
    {
        public string PRENTRY_WHENCREATED { get; set; }
        public string PRENTRY_RECORDID { get; set; }
        public string PRENTRY_TOTALENTERED { get; set; }
        public string PRENTRY_TOTALPAID { get; set; }
        public string PRENTRY_BALANCE { get; set; }
    }
}