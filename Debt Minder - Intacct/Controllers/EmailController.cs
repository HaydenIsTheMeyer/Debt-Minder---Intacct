using Debt_Minder___Intacct.Models;
using Microsoft.AspNetCore.Mvc;

namespace Debt_Minder___Intacct.Controllers
{
    public class EmailController : Controller
    {
        public IActionResult Email(string customerName)
        {
            var documents = HomeController.res.Operation.Result.Data.SODocuments
                 .Where(doc => doc.CustomerId == customerName)
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
    }
}
