using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace tupadportal.Models
{
    public class Address
    {
        [Key]
        public int Add_Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Barangay { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Municipality { get; set; } = "";


        [Required]
        public DateTime DateTime { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<ApplicationUser>? Users { get; set; }  // Linked users
        public ICollection<Applicants>? Applicants { get; set; }  // Linked applicants

        // Remove the link to Batch
        public ICollection<Batch>? Batches { get; set; }

        public ICollection<Announcement>? Announcements { get; set; }
    }
}
