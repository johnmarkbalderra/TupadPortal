using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tupadportal.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public DateTime? TimeInAM { get; set; }
        public DateTime? TimeOutAM { get; set; }

        public string? Signature { get; set; } // Signature property

        public string? PicturePath { get; set; }

        // Foreign key for Applicants
        public int ApplicantId { get; set; }

        // Navigation property
        [ForeignKey("ApplicantId")]
        public Applicants? Applicant { get; set; }
    }
}
