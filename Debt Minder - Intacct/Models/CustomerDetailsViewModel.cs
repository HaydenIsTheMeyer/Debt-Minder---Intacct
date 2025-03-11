namespace Debt_Minder___Intacct.Models
{
    public class CustomerDetailsViewModel
    {
        public string CustomerName { get; set; }
        public List<DocumentDetail> Documents { get; set; }
    }

    public class DocumentDetail
    {
        public string DOCNo { get; set; }
        public string DOCID { get; set; }
        public string? EXTERNALREFNO { get; set; }
        public string? ORIGDOCDATE { get; set; }
        public decimal TotalDue { get; set; }

        public decimal DocTotal { get; set; }
    }
}