namespace tupadportal.Models
{
    public class SignatureModel
    {
        public int ApplicantId { get; set; }
        public DateTime Date { get; set; } // Added Date property
        public string? Signature { get; set; } // Base64-encoded signature image
    }
}
