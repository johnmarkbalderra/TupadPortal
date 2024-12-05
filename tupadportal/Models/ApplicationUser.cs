using FluentEmail.Core.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace tupadportal.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Position { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool Active { get; set; }
        public string Role { get; set; } = "";

        // Foreign key for Address
        public int AddressId { get; set; }

        // Navigation property
        [ForeignKey("AddressId")]
        public Address? Address { get; set; }
    }
}
