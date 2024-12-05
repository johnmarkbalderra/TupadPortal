// ViewModels/Step2ViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace tupadportal.ViewModels
{
    public class Step2ViewModel
    {
        public int ApplicantId { get; set; }

        [Required(ErrorMessage = "Barangay is required.")]
        [StringLength(255)]
        [Display(Name = "Barangay")]
        public string Barangay { get; set; } = "";

        [Required(ErrorMessage = "Address is required.")]
        [Display(Name = "Address")]
        public int AddressId { get; set; }

        [Required(ErrorMessage = "Municipality is required.")]
        [StringLength(255)]
        [Display(Name = "Municipality")]
        public string Municipality { get; set; } = "";

        [Required(ErrorMessage = "Contact Number is required.")]
        [StringLength(11)]
        [Display(Name = "Contact Number")]
        public string ContactNo { get; set; } = "";

        [Required(ErrorMessage = "ID Type is required.")]
        [StringLength(50)]
        [Display(Name = "ID Type")]
        public string IdType { get; set; } = "";

        [Required(ErrorMessage = "ID Number is required.")]
        [StringLength(50)]
        [Display(Name = "ID Number")]
        public string IdNumber { get; set; } = "";
    }
}
