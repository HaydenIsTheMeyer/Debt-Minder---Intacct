using Microsoft.AspNetCore.Mvc;
using Debt_Minder___Intacct.Models;
using System.Data;

namespace Debt_Minder___Intacct.Controllers
{
    public class EmailPreviewController : Controller
    {
        public IActionResult EmailPreview()
        {
            EmailPreviewModel model = new EmailPreviewModel();
            model.Body = string.Empty;
            model.Subject = string.Empty;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendEmails(bool internalOnly = false)
        {
            var message = await EmailPollingService.PollForEmailsAsync(internalOnly);
            //TempData["EmailResult"] = message;
            return View();
        }

        public IActionResult EmailSummary()
        {
            ViewBag.ResultMessage = TempData["EmailResult"];
            return View(); // your EmailSummary.cshtml will show the message
        }


        [HttpPost]
        public async Task<IActionResult> SendEmails()
        {
            try
            {


                string FirstName = "Admin";//DatabaseEngine.GetAgentFirstName(lblUser.Text.Trim());




                string month = DateTime.Now.ToString("MMM");
                string selectedfolder = $@"C:\\PollingLogs";//(string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\InvoiceRun\EmailSettings", "InvoicePath", null);

                string folderPath = $@"{selectedfolder}\{month} Invoices";


                if (Directory.Exists(folderPath))
                {
                    string[] files = Directory.GetDirectories(folderPath);
                    int EmailCount = files.Length;



                    // DialogResult result = MessageBox.Show($"{EmailCount} Customers to be mailed. \n{FirstName} are you sure about this?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);


                   
                    
                       // string internalResult = frmMessageBox.Show($@"Who do you want to email {FirstName}?", "Customer", "Internal", "Both");
                        //DialogResult internalResult = MessageBox.Show($"{FirstName} would you like to CC internally?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        bool Internal = false;


                    DatabaseEngine.DeleteInternalEmailSelection();



                        foreach (string file in files)
                        {

                            // Find the position of the last backslash
                            string FolderName = Path.GetFileName(file.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                            string reciepents = GetEmailReciepients(FolderName);

                            if (reciepents != "")
                            {

                                DatabaseEngine.InsertEmailLog($@"{folderPath}\{FolderName}", reciepents, "p", "Pending");
                            }
                            else
                            {
                              //  MessageBox.Show("This Session has expired, please log out to continue", "Session Expired", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            }



                        }
                        string res = await EmailPollingService.PollForEmailsAsync(Internal);

                    return null;


                    

                }
                else
                {
                    // MessageBox.Show($@"Please generate documents before emailing", "Error - 007", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }


            }
            catch (Exception ex)
            {

                //  MessageBox.Show($@"An Error has occured - {ex.Message}", "Error - 006", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
        }
        public static string GetEmailReciepients(string FolderName)
        {

            // this is to get the client email address

            DataTable dtEmailReciepients = new DataTable();
            string Items = "";

            //   int CustomerId = DatabaseEngine.GetClientDCLinkByAccount(FolderName);

            dtEmailReciepients = null; //DatabaseEngine.GetClientEmail(CustomerId);
            if (dtEmailReciepients.Rows.Count > 0)
            {
                Items = dtEmailReciepients.Rows[0][0].ToString();

            }


            return Items;
        }

        [HttpPost]
        public IActionResult GenerateEmailHtml(string content)
        {
            try
            {
                // Call your existing method to generate HTML from the content
                string generatedHtml = LoadEmailPreview(content, "Default");
                return Json(new { html = generatedHtml });
            }
            catch (Exception ex)
            {
                // Log the error as needed
                return Json(new { html = "<p>Error generating HTML</p>" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveEmail(EmailPreviewModel model)
        {
            if (ModelState.IsValid)
            {
                string body = "test";// recieve body as parameter
                DatabaseEngine.InsertEmailTemplate("Default", "Test Subject", body, true);
                // Handle the save logic (e.g., store the email or send it)
                // For now, redirect to a success page or back to the form
                string resopnse = await EmailPollingService.PollForEmailsAsync(false);
                return RedirectToAction("Index", "Home");
            }
            return View("EmailPreview", model);
        }

        public string LoadEmailPreview(string Body, string LayoutType)
        {
            try
            {
                string layout = string.Empty;

                if (!string.IsNullOrEmpty(Body))
                {
                    DllInitializer.InitializeDll();
                    if (DllInitializer.EmailEngine != null)
                    {
                        try
                        {
                            layout = DllInitializer.EmailEngine.GetEmailTemplate("Kiteview Admin", "CASH", LayoutType, Body);
                        }
                        catch (Exception ex)
                        {
                          //  MessageBox.Show($@"An Error Occurred - {ex.Message}", "Error - 00089", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    try
                    {
                        DllInitializer.InitializeDll();
                        layout = DllInitializer.EmailEngine.GetEmailTemplate(SessionEngine.Username, "CASH", LayoutType, "");
                    }
                    catch (Exception ex)
                    {
                      //  MessageBox.Show($@"An Error Occurred - {ex.Message}", "Error - 00089", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                if (!string.IsNullOrEmpty(layout))
                {
                    return layout;
                    //if (webView21.CoreWebView2 == null)
                    //{
                    //    if (_webView2Environment == null)
                    //    {
                    //        string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DebtMinder", "WebView2");
                    //        Directory.CreateDirectory(userDataFolder); // Ensure the directory exists
                    //        _webView2Environment = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, userDataFolder);
                    //    }
                    //    await webView21.EnsureCoreWebView2Async(_webView2Environment);
                    //}
                    //webView21.CoreWebView2.NavigateToString(layout);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($@"An Error Occurred - {ex.Message}", "Error Loading Layout - 00028", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
        }
    }
}
