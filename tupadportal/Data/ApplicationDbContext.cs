using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using tupadportal.Models;

namespace tupadportal.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define DbSets for your entities
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<Applicants> Applicants { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AttendanceChecklist> AttendanceChecklists { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed roles
            var admin = new IdentityRole("admin") { NormalizedName = "ADMIN" };
            var brgy = new IdentityRole("brgy") { NormalizedName = "BRGY" };
            var peso = new IdentityRole("peso") { NormalizedName = "PESO" };
            modelBuilder.Entity<IdentityRole>().HasData(admin, brgy, peso);

            // Applicants and Address relationship without cascade delete
            modelBuilder.Entity<Applicants>()
                .HasOne(a => a.Address)
                .WithMany(a => a.Applicants)
                .HasForeignKey(a => a.AddressId)
                .OnDelete(DeleteBehavior.NoAction); // Disable cascade delete

            // Applicants and Batch relationship
            modelBuilder.Entity<Applicants>()
                .HasOne(a => a.Batch)
                .WithMany(b => b.Applicants)
                .HasForeignKey(a => a.BatchId)
                .OnDelete(DeleteBehavior.NoAction);

            // ApplicationUser and Address
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(au => au.Address)
                .WithMany(a => a.Users)
                .HasForeignKey(au => au.AddressId);

            // Remove the Batch and Address relationship
            // The line below is removed:
            modelBuilder.Entity<Batch>()
                .HasOne(b => b.Address)
                .WithMany(a => a.Batches)
                .HasForeignKey(b => b.AddressId);

            // Attendance and Applicants
            modelBuilder.Entity<Attendance>()
                .HasOne(at => at.Applicant)
                .WithMany(a => a.Attendances)
                .HasForeignKey(at => at.ApplicantId);

            // AttendanceChecklist and Applicants
            modelBuilder.Entity<AttendanceChecklist>()
                .HasOne(ac => ac.Applicant)
                .WithMany(a => a.AttendanceChecklists)
                .HasForeignKey(ac => ac.ApplicantId);

            // Announcement and Address
            modelBuilder.Entity<Announcement>()
                .HasOne(an => an.Address)
                .WithMany(a => a.Announcements)
                .HasForeignKey(an => an.AddressId);

            // Configure the Signature property in Attendance
            modelBuilder.Entity<Attendance>()
                .Property(a => a.Signature)
                .HasColumnType("nvarchar(max)");
        }
    }
}
