using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace tupadportal.Models
{
    public class Batch
    {
        [Key]
        public int BatId { get; set; }

        [StringLength(100)]
        public string BatchName { get; set; } = "";

        public int Slot { get; set; }

        [Required]
        public DateTime DateTime { get; set; } = DateTime.Now;

        // Remove the Address foreign key and navigation property
        public int AddressId { get; set; }
        public Address? Address { get; set; }

        public ICollection<Applicants>? Applicants { get; set; }
    }
}
