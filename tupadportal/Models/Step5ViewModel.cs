// ViewModels/Step5ViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace tupadportal.ViewModels
{
    public class Step5ViewModel
    {
        public int ApplicantId { get; set; }


        [Required(ErrorMessage = "Interested in Skills Training is required.")]
        [Display(Name = "Interested in Skills Training")]
        public bool InterestedInSkillsTraining { get; set; }

        [Display(Name = "Skills Training Needed")]
        public string SkillsTrainingNeeded { get; set; } = "";

        [Required(ErrorMessage = "Batch is required.")]
        [Display(Name = "Batch")]
        public int BatchId { get; set; }

        
    }
}
