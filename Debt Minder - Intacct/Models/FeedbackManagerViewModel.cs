namespace Debt_Minder___Intacct.Models
{
    public class FeedbackManagerViewModel
    {
        public string CustomerName { get; set; }

        public string CustomerId { get; set; }
        public List<string> TypeOptions { get; set; }
        public List<string> ReferenceOptions { get; set; }
        public List<FeedbackHistoryItem> FeedbackHistory { get; set; }
        public string Type { get; set; }
        public string ReferenceSelection { get; set; }
        public string Outstanding { get; set; }
        public string PTPAmount { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }
    }

    public class FeedbackHistoryItem
    {
        public bool Display { get; set; }
        public int Id { get; set; }
        public string Type { get; set; }
        public string Code { get; set; }
        public string Reference { get; set; }
        public string OutstandingAmount { get; set; }
        public string PTPAmount { get; set; }
        public string ContactDate { get; set; }
    }
}
