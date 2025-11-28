namespace Debt_Minder___Intacct.Models
{
    public class DocumentManagerViewModel
    {

        public string CustomerName { get; set; }

        public string CustomerId { get; set; }
        public List<string> TypeOptions { get; set; }
        public List<string> ReferenceOptions { get; set; }
        public List<DocumentHistoryItem> DocumentHistory { get; set; }
        public string Type { get; set; }
        public string ReferenceSelection { get; set; }

        public string FilePath { get; set; }

    }

    public class DocumentHistoryItem
    {
        public bool Include { get; set; }
        public int Id { get; set; }
        public string Type { get; set; }
        public string FilePath { get; set; }
        public string Reference { get; set; }
        public string User { get; set; }
        public string UploadDate { get; set; }
    }
}
