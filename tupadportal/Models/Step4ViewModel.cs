// ViewModels/Step4ViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace tupadportal.ViewModels
{
    public class Step4ViewModel
    {
        public int ApplicantId { get; set; }

        [Required(ErrorMessage = "Bank Account Type is required.")]
        [StringLength(50)]
        [Display(Name = "Bank Account Type")]
        public string BankAccountType { get; set; } = "";

        [StringLength(50)]
        [Display(Name = "Bank Account Number")]
        public string? BankAccountNo { get; set; }
    }
}