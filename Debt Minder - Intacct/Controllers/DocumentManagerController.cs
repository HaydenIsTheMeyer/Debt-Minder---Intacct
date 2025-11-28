using Microsoft.AspNetCore.Mvc;
using Debt_Minder___Intacct.Models;
using System.Data;

namespace Debt_Minder___Intacct.Controllers
{
    public class DocumentManagerController : Controller
    {
        public IActionResult DocumentManager(string customerName)
        {
            DocumentManagerViewModel viewModel = new DocumentManagerViewModel();
            viewModel.CustomerId = customerName;
            viewModel.CustomerName = customerName;


            DataTable dtHistory = new DataTable();
            dtHistory = DatabaseEngine.GetDocHistory(customerName);

            var res = dtHistory.AsEnumerable()
                .Select(row => new DocumentHistoryItem
                {
                    Id = row.Field<int>("Id"),
                    Type = row.Field<string>("DocType"),
                    FilePath = row.Field<string>("FilePath"),
                    Reference = row.Field<string>("Reference"),
                    User = row.Field<string>("User"),
                    UploadDate = row.Field<DateTime>("UploadDate").ToString(),
                    Include = Convert.ToBoolean(row.Field<int>("bInclude")),
                   

                    // Map other properties as needed
                })
                .ToList();


            var documents = HomeController.res.Operation.Result.Data.SODocuments
                .Where(doc => doc.CustomerId == customerName)
                .Select(doc => doc.DOCNO)
                .ToList();

            List<string> TypeOptions = new List<string>();
            TypeOptions.Add("POD");
            TypeOptions.Add("Header");

            viewModel.DocumentHistory  = res;
            viewModel.ReferenceOptions = documents;
            viewModel.TypeOptions = TypeOptions;


            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttachment(IFormFile file, string DocType, string Reference, string CustomerId)
        {

            // add validation

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();

            // Save fileBytes, DocType, etc. to database
            // ...
            int DocId = DatabaseEngine.InsertDocMapping(CustomerId, DocType, file.FileName, Reference, 1, 0);
            DatabaseEngine.InsertDocAttachment(DocId,file.FileName, file.ContentType, 100, fileBytes );

            return Ok("File uploaded");
        }

        [HttpGet]
        public IActionResult PreviewFile(int id)
        {
            DataTable dtFile = new DataTable();
            dtFile = DatabaseEngine.GetAttachmentById(id);

            if(dtFile.Rows.Count > 0)
            {
                byte[] FileData = (byte[])dtFile.Rows[0]["FileData"];
                string ContentType = dtFile.Rows[0]["ContentType"].ToString() ?? "application/octet-stream";
                string FileName = dtFile.Rows[0]["FileName"].ToString() ?? "Preview";

                if ( FileData == null)
                    return NotFound();

                Response.Headers.Add("Content-Disposition", $"inline; filename={FileName}");

                return File(FileData, ContentType);
            }
            else
            {
                return NotFound();

            }

        }

        [HttpPost]
        public IActionResult DeleteAttachment(int DocId)
        {

           DatabaseEngine.DeleteAttachmentHistory(DocId);

            return Json(new { success = true });
        }

        public IActionResult Close()
        {


            return RedirectToAction("Index", "Home");


        }


    }
}
