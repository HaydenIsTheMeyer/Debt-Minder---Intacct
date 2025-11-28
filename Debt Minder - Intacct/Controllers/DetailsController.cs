using Debt_Minder___Intacct.Models;
using Microsoft.AspNetCore.Mvc;

namespace Debt_Minder___Intacct.Controllers
{
    public class DetailsController : Controller
    {
        public IActionResult Details(string customerName)
        {
            var documents = HomeController.res.Operation.Result.Data.SODocuments
                .Where(doc => doc.CustomerId == customerName)
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
}
