using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tupadportal.Models
{
    public class Announcement
    {
        [Key]
        public int AnnouncementsId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        public bool Read { get; set; }

        // Foreign key for Address
        public int AddressId { get; set; }

        // Navigation property
        [ForeignKey("AddressId")]
        public Address? Address { get; set; }

        // CreatedDate column
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
