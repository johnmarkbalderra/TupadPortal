using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace tupadportal.Models
{
    public class Applicants
    {
        [Key]
        public int ApplicantId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = "";

        [StringLength(50)]
        public string MiddleName { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = "";

        [StringLength(10)]
        public string ExtensionName { get; set; } = "";

        [Required]
        public DateTime Birthdate { get; set; }

        [Required]
        [StringLength(255)]
        public string Barangay { get; set; } = "";

        [Required]
        [StringLength(255)]
        public string Municipality { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string IdType { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string IdNumber { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string ContactNo { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string BankAccountType { get; set; } = "";

        [StringLength(50)]
        public string BankAccountNo { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string BeneficiaryType { get; set; } = "";

        [StringLength(50)]
        public string Occupation { get; set; } = "";

        public string OccupationSpecify { get; set; } = "";

        [Required]
        [StringLength(10)]
        public string Sex { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string CivilStatus { get; set; } = "";

        [Required]
        public int Age { get; set; }

        public int MonthlyIncome { get; set; }

        [StringLength(50)]
        public string Dependent { get; set; } = "";

        [Required]
        public bool InterestedInSkillsTraining { get; set; }

        public string SkillsTrainingNeeded { get; set; } = "";

        public bool Approved { get; set; } = false;

        [Required]
        public DateTime DateTime { get; set; } = DateTime.Now;

        // Foreign key for Batch
        public int BatchId { get; set; }

        // Navigation property
        [ForeignKey("BatchId")]
        public Batch? Batch { get; set; }

        public ICollection<Attendance>? Attendances { get; set; }
        public ICollection<AttendanceChecklist>? AttendanceChecklists { get; set; }

        // Foreign key for Address
        public int AddressId { get; set; }

        // Navigation property for Address
        [ForeignKey("AddressId")]
        public Address? Address { get; set; }
    }
}
