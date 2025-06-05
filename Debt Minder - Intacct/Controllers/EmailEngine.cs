using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Data;
using Microsoft.Win32;

namespace Debt_Minder___Intacct.Controllers
{
    internal class EmailEngine
    {
        public static string smtpServer { get; set; }
        public static string port { get; set; }
        public static string username { get; set; }
        public static string password { get; set; }
        public static bool enableSSL { get; set; }

        public static string LayoutType { get; set; } = "Default";

        public static void EmailSender(string SmtpServer, string Port, string Username, string Password, bool EnableSSL)
        {
            smtpServer = SmtpServer;
            port = Port;
            username = Username;
            password = Password;
            enableSSL = EnableSSL;
        }

        public static bool SendEmail(string from, string to , string subject, string body)
        {
            //create the mail message
            MailMessage mail = new MailMessage();

            //set the addresses
            mail.From = new MailAddress(from);
            mail.To.Add(to);


            //set the content
            mail.Subject = subject;
            mail.IsBodyHtml = true;
            mail.Body = body/*body*/;
 
            //send the message
            SmtpClient smtp = new SmtpClient(smtpServer, Convert.ToInt32(port));
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(username, password);

            try
            {
                smtp.Send(mail);
                return true;
            }
            catch (Exception ex)
            {
               // MessageBox.Show($"Failed to send email: {ex.Message}");
                return false;
            }
        }

        public static void SendLogEmail(string to, string CC, string subject, string body, string folderPath)
        {
            // Create the mail message
            using (MailMessage mail = new MailMessage())
            {
                try
                {
                    // Set the addresses
                    mail.From = new MailAddress(username);

                    // Split recipients and add to the mail
                    string[] recipients = to.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string recipient in recipients)
                    {
                        mail.To.Add(recipient.Trim());
                    }

                    // Set the content
                    mail.Subject = subject;
                    mail.IsBodyHtml = true;
                    mail.Body = body;
                    if (!string.IsNullOrEmpty(CC))
                    {
                        string[] Copies = CC.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string Copy in Copies)
                        {
                            mail.CC.Add(Copy.Trim());
                        }
                        
                    }
                }
                catch (Exception ex)
                {

                   // MessageBox.Show($@"An Error Occured {ex.Message}", "Email Error - 001", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Add attachments
                try
                {
                    string[] files = Directory.GetFiles(folderPath);
                    foreach (string file in files)
                    {
                        Attachment attachment = new Attachment(file);
                        mail.Attachments.Add(attachment);
                    }

                    // Send the message using SMTP client
                    using (SmtpClient smtp = new SmtpClient(smtpServer, Convert.ToInt32(port)))
                    {
                        smtp.EnableSsl = true; // Enable SSL/TLS
                        smtp.Credentials = new NetworkCredential(username, password);

                        try
                        {
                            smtp.Send(mail);
                            Console.WriteLine("Email sent successfully.");
                        }
                        catch (Exception ex)
                        {
                         //   MessageBox.Show($"Failed to send email: {ex.Message}", "Email Error", MessageBoxButtons.OK,MessageBoxIcon.Error);
                            // Log exception details if needed
                        }
                    }
                }
                catch (Exception ex)
                {
                   // MessageBox.Show($"Failed to add attachments: {ex.Message}", "Email Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    // Clean up any created attachments
                    foreach (var attachment in mail.Attachments)
                    {
                        attachment.Dispose();
                    }
                    throw;
                }
            }
        }


        public static void Initialize()
        {

            try
            {
                // Populate with Active Email SMTP
                DataTable dtActiveEmail = new DataTable();
                dtActiveEmail = DatabaseEngine.GetActiveEmailSMTP();

                if (dtActiveEmail.Rows.Count > 0)
                {
                    username = dtActiveEmail.Rows[0]["Sender"].ToString();
                    smtpServer = dtActiveEmail.Rows[0]["Host"].ToString();
                    port = dtActiveEmail.Rows[0]["Port"].ToString();
                    password = CryptorEngine.Decrypt(dtActiveEmail.Rows[0]["Password"].ToString(), true);
                }
                else
                {
                    throw new Exception("No Active Email Template Selected");
                   // MessageBox.Show("No Active Email Template Selected", "Email Template Missing - 003", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception)
            {

                throw;
            }




        }



    }
}
