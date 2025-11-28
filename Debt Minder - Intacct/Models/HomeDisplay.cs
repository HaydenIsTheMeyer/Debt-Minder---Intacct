using System.Xml.Serialization;

namespace Debt_Minder___Intacct.Models
{
    public class HomeDisplay
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }

        public decimal TotalDue { get; set; }

        public decimal DocTotal { get; set; }

        public int NoDocs { get; set; }

        public string Contacted { get; set; }

        public string Action { get; set; }

        public string RowClass { get; set; }
    }
}
