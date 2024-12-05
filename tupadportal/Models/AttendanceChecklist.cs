using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace tupadportal.Models
{
    public class AttendanceChecklist
    {
        [Key]
        public int ChecklistId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string DaysCheckedSerialized { get; set; } = "";

        [NotMapped]
        public List<bool> DaysChecked
        {
            get => DaysCheckedSerialized.Split(',').Select(bool.Parse).ToList();
            set => DaysCheckedSerialized = string.Join(",", value);
        }

        // Foreign key for Applicants
        public int ApplicantId { get; set; }

        // Navigation property
        [ForeignKey("ApplicantId")]
        public Applicants? Applicant { get; set; }
    }
}
