// ViewModels/Step1ViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace tupadportal.ViewModels
{
    public class Step1ViewModel
    {
        public int ApplicantId { get; set; }

        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [StringLength(50)]
        [Display(Name = "Middle Name")]
        public string MiddleName { get; set; } = "";

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [StringLength(10)]
        [Display(Name = "Extension Name")]
        public string ExtensionName { get; set; } = "";

        [Required(ErrorMessage = "Birthdate is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Birthdate")]
        public DateTime Birthdate { get; set; }

        [Required(ErrorMessage = "Sex is required.")]
        [StringLength(10)]
        [Display(Name = "Sex")]
        public string Sex { get; set; } = "";

        [Required(ErrorMessage = "Civil Status is required.")]
        [StringLength(20)]
        [Display(Name = "Civil Status")]
        public string CivilStatus { get; set; } = "";
    }
}
