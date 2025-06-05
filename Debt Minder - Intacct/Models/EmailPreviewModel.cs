namespace Debt_Minder___Intacct.Models
{
    using System.ComponentModel.DataAnnotations;

    public class EmailPreviewModel
    {
        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(255, ErrorMessage = "Subject cannot exceed 255 characters.")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Body is required.")]
        public string Body { get; set; }
    }
}
