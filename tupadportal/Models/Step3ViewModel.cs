// ViewModels/Step3ViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace tupadportal.ViewModels
{
    public class Step3ViewModel
    {
        public int ApplicantId { get; set; }

        [StringLength(50)]
        [Display(Name = "Occupation")]
        public string Occupation { get; set; } = "";

        [Display(Name = "Specify Occupation")]
        public string OccupationSpecify { get; set; } = "";

        [Display(Name = "Monthly Income")]
        public int? MonthlyIncome { get; set; }

        [Required(ErrorMessage = "Beneficiary Type is required.")]
        [StringLength(30)]
        [Display(Name = "Beneficiary Type")]
        public string BeneficiaryType { get; set; } = "";

        [StringLength(50)]
        [Display(Name = "Dependent")]
        public string Dependent { get; set; } = "";
    }
}
