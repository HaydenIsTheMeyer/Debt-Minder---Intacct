using Microsoft.AspNetCore.Mvc;
using Debt_Minder___Intacct.Models;
using System.Data;

namespace Debt_Minder___Intacct.Controllers
{
    public class FeedbackManagerController : Controller
    {
        public IActionResult FeedbackManager(string customerName)
        {

            // Get Excuses
            DataTable dtExcuses = DatabaseEngine.GetExcuses();
            var Excuses = dtExcuses.AsEnumerable().Select(row => row.Field<string>("Excuse") ?? string.Empty).ToList();
            
            // Get References
            var References = HomeController.res.Operation.Result.Data.SODocuments
            .Where(doc => doc.CustomerId == customerName)
            .Select(doc => doc.DOCNO).ToList<string>();

            // Get History
            DataTable dtHistory = new DataTable();
            dtHistory = DatabaseEngine.GetContactHistory(customerName);

            var History = dtHistory
                            .AsEnumerable()
                            .Select(Hist => new FeedbackHistoryItem
                            {
                                Display = Convert.ToBoolean(Hist.Field<int>("Selected")),
                                Type = Hist.Field<string>("Type"),
                                Code = Hist.Field<string>("Code"),
                                Reference = Hist.Field<string?>("Reference"),
                                OutstandingAmount = Hist.Field<string>("OutstandingAmount"),
                                PTPAmount = Hist.Field<string>("Amount"),
                                ContactDate = Hist.Field<string>("ContactDate"),
                                Id = Hist.Field<int>("Id")

                            }).ToList();




            FeedbackManagerViewModel model = new FeedbackManagerViewModel();
            model.CustomerName = customerName;
            model.CustomerId = customerName;
            model.TypeOptions = Excuses;
            model.ReferenceOptions = References;
            model.FeedbackHistory = History;

            return View(model);
        }

        [HttpPost]
        public IActionResult SaveFeedback(FeedbackManagerViewModel model)
        {
            //if (string.IsNullOrEmpty(model.Type) || string.IsNullOrEmpty(model.ReferenceSelection) ||
            //    string.IsNullOrEmpty(model.Outstanding) || string.IsNullOrEmpty(model.PTPAmount) ||
            //    string.IsNullOrEmpty(model.Notes) || model.Date == default)
            //{
            //    ModelState.AddModelError("", "All fields are required.");
            //    return View("FeedbackManager", model);
            //}

            string notes = model.Notes == null ? "" : model.Notes;
            int ExcuseId = DatabaseEngine.GetExcuseId(model.Type);

            DatabaseEngine.InsertContactHistory(model.CustomerId, ExcuseId, model.ReferenceSelection, Convert.ToDouble(model.PTPAmount.Replace(" ", "")), Convert.ToDouble(model.Outstanding.Replace(" ", "")), model.Date, notes);

            return RedirectToAction("FeedbackManager", new { customerName = model.CustomerId });


        }

        public IActionResult Close()
        {


            return RedirectToAction("Index", "Home");


        }

        [HttpPost]
        public IActionResult DeleteFeedback(string Type, string CustomerId, DateTime Date)
        {
            int ExcuseId = DatabaseEngine.GetExcuseId(Type);
            // Your deletion logic here, for example:
            DatabaseEngine.DeleteContactHistory(CustomerId, ExcuseId, Date);

            return Json(new { success = true });
        }


        [HttpPost]
        public IActionResult UpdateCheckbox(string CustomerId, string ContactId)
        {
            // Your deletion logic here, for example:
            DatabaseEngine.InsertContactSelection(ContactId, CustomerId);

            return Json(new { success = true });
        }


    }
}
