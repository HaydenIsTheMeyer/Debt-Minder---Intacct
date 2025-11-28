using DM_Middle_Ware;
using System.Data;

namespace Debt_Minder___Intacct.Controllers
{
    public class EmailPollingService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailPollingService> _logger;



        public static async Task<string> PollForEmailsAsync(bool internalOnly)
        {
            var pendingEmails = DatabaseEngine.GetPendingEmails();

            if (pendingEmails.Rows.Count == 0)
                return "All invoices have already been emailed.";

            var template = DatabaseEngine.GetActiveEmailTemplate();
            if (template.Rows.Count == 0)
                return "No active email template found.";

            string subject = template.Rows[0]["Subject"].ToString();
            string bodyTemplate = template.Rows[0]["Body"].ToString();

            int numEmails = 0;
            bool success = true;

            foreach (DataRow row in pendingEmails.Rows)
            {
                try
                {
                    var month = DateTime.Now.ToString("MMM");
                    var recipients = row["Reciepients"].ToString();
                    var folderPath = row["FolderPath"].ToString();
                   // var selectedFolder = $@"C:\\TargetFolder";//_config["EmailSettings:InvoicePath"]; // Replacing registry

                    //var stdPath = Path.Combine(selectedFolder, $"{month} Invoices");
                    //var accountLength = folderPath.Length - stdPath.Length - 1;
                    //var customerAccount = folderPath.Substring(folderPath.Length - accountLength);

                    var internalRecipients = GetInternalRecipients();
                    DllInitializer.InitializeDll();
                    var finalBody = DllInitializer.EmailEngine.GetEmailTemplate(SessionEngine.Username, "CASH", EmailEngine.LayoutType, bodyTemplate, "");

                    EmailEngine.Initialize();
                    if (internalOnly)
                        EmailEngine.SendLogEmail(internalRecipients, "", $"Test - {subject}", finalBody, folderPath);
                    else
                        EmailEngine.SendLogEmail(recipients, internalRecipients, $"Test - {subject}", finalBody, folderPath);

                    foreach (var file in Directory.GetFiles(folderPath))
                    {
                        var orderNum = Path.GetFileNameWithoutExtension(file);
                        DatabaseEngine.UpdateInvoiceLogAfterEmail(orderNum, "y", "Success");
                    }

                    DatabaseEngine.UpdateEmailLog(folderPath, "y", "Success");
                    numEmails++;
                }
                catch (Exception ex)
                {
                   // _logger.LogError(ex, "Failed to send email for folder: {Folder}", row["FolderPath"]);
                    foreach (var file in Directory.GetFiles(row["FolderPath"].ToString()))
                    {
                        var orderNum = Path.GetFileNameWithoutExtension(file);
                        DatabaseEngine.UpdateInvoiceLogAfterEmail(orderNum, "f", $"Failed - {ex.Message}");
                    }

                    DatabaseEngine.UpdateEmailLog(row["FolderPath"].ToString(), "f", $"Failed - {ex.Message}");
                    success = false;
                    break;
                }
            }

            DatabaseEngine.ClearInvoiceSelection();
            DatabaseEngine.ClearEmailSelection();
            DatabaseEngine.ClearStatementSelection();
            DatabaseEngine.DeleteInternalEmailSelection();

            return success ? $"{numEmails} Emails Sent Successfully" : "Errors occurred during email sending.";
        }

        private static string GetInternalRecipients()
        {
            var result = new List<string>();
            var internalEmails = DatabaseEngine.GetSelectedInternalEmails();

            foreach (DataRow row in internalEmails.Rows)
            {
                result.Add(row["Reciepient"].ToString());
            }

            return string.Join(";", result);
        }
    }

}
