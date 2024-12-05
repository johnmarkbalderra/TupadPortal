// File: Ap/Controllers/AddressesController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using tupadportal.Data;
using tupadportal.Models;
using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace tupadportal.Controllers
{
    [Authorize(Roles = "admin")]
    public class AddressesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFluentEmail _fluentEmail;
        private readonly UserManager<ApplicationUser> _userManager;

        public AddressesController(ApplicationDbContext context, IFluentEmail fluentEmail, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _fluentEmail = fluentEmail;
            _userManager = userManager;
        }

        // GET: Addresses/Index
        public async Task<IActionResult> Index()
        {
            var addresses = await _context.Addresses
                .Include(a => a.Applicants)
                .ThenInclude(app => app.Batch) // Ensure Batch information is included
                .ToListAsync();

            return View(addresses);
        }

        // GET: Addresses/Search
        // Handles AJAX search queries
        public async Task<IActionResult> Search(string searchQuery)
        {
            var addresses = from a in _context.Addresses select a;

            if (!string.IsNullOrEmpty(searchQuery))
            {
                addresses = addresses.Where(a => a.Barangay.Contains(searchQuery) || a.Municipality.Contains(searchQuery));
            }

            var filteredAddresses = await addresses
                .Include(a => a.Applicants)
                .ThenInclude(app => app.Batch)
                .ToListAsync();

            return PartialView("_AddressListPartial", filteredAddresses);
        }

        // GET: Addresses/GetDetails
        public async Task<IActionResult> GetDetails(int addressId)
        {
            var address = await _context.Addresses
                .Include(a => a.Batches)
                .ThenInclude(b => b.Applicants)
                .FirstOrDefaultAsync(a => a.Add_Id == addressId);

            if (address == null)
            {
                return NotFound();
            }

            return PartialView("_AddressDetailsPartial", address);
        }


        // GET: Addresses/AddressListPartial
        // Returns the address list partial view
        public async Task<IActionResult> AddressListPartial()
        {
            var addresses = await _context.Addresses
                .Include(a => a.Applicants)
                .ThenInclude(app => app.Batch)
                .ToListAsync();

            return PartialView("_AddressListPartial", addresses);
        }

        

        public async Task<IActionResult> ApplicantsList(string barangay, int? batchId)
        {
            // Define the query to retrieve all applicants with their batch and address information
            IQueryable<Applicants> applicantsQuery = _context.Applicants
                .Include(a => a.Batch)
                .Include(a => a.Address);

            // Apply the filter if a specific barangay is provided
            if (!string.IsNullOrEmpty(barangay))
            {
                applicantsQuery = applicantsQuery.Where(a => a.Address.Barangay == barangay);
            }

            // Apply the batch filter if a batchId is provided
            if (batchId.HasValue)
            {
                applicantsQuery = applicantsQuery.Where(a => a.BatchId == batchId.Value);
            }

            // Execute the query and return the filtered list of applicants
            var applicants = await applicantsQuery.ToListAsync();

            // Pass the list of batches related to the barangay to the view
            var batchList = await _context.Batches
                .Where(b => b.Applicants.Any(a => a.Address.Barangay == barangay))
                .ToListAsync();

            ViewBag.BatchList = new SelectList(batchList, "BatId", "BatchName");

            return PartialView("_ApplicantsListPartial", applicants); // Update this to return a partial view
        }


        // GET: Addresses/Details/5
        // Returns the details of an address in a modal
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest("Address ID is required.");
            }

            var address = await _context.Addresses
                .Include(a => a.Applicants)
                .ThenInclude(app => app.Batch)
                .FirstOrDefaultAsync(a => a.Add_Id == id);

            if (address == null)
            {
                return NotFound();
            }

            return PartialView("_AddressDetailsPartial", address);
        }

        // GET: Addresses/Edit/5
        // Returns the edit address partial view for adding a slot
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest("Address ID is required.");
            }

            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
            {
                return NotFound();
            }

            return PartialView("_EditAddressPartial", address);
        }

        // POST: Addresses/Edit/5
        // Handles the AJAX form submission for editing an address (adding a slot)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Add_Id, Address address)
        {
            if (Add_Id != address.Add_Id)
            {
                return Json(new { success = false, message = "Address ID mismatch." });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Only update the Slot property to prevent unintended changes
                    var existingAddress = await _context.Addresses.FindAsync(Add_Id);
                    if (existingAddress == null)
                    {
                        return Json(new { success = false, message = "Address not found." });
                    }

                    
                    await _context.SaveChangesAsync();

                    // Send email to Brgy users
                    await SendEmailToBrgyUsers(existingAddress);

                    // Return JSON response indicating success
                    return Json(new { success = true, message = "Slot updated successfully, and notification sent!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AddressExists(address.Add_Id))
                    {
                        return Json(new { success = false, message = "Address not found." });
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                // If the model state is invalid, return validation errors
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, validationErrors = errors });
            }
        }



        // POST: AdminApplicants/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var applicant = await _context.Applicants
                                          .Include(a => a.Address)
                                          .FirstOrDefaultAsync(a => a.ApplicantId == id);
            if (applicant == null)
            {
                return NotFound();
            }

            applicant.Approved = true;
            _context.Update(applicant);
            await _context.SaveChangesAsync();

            // Create an announcement
            var announcement = new Announcement
            {
                Title = "Applicant Approved",
                Description = $"Applicant {applicant.FirstName} {applicant.LastName} has been approved.",
                CreatedDate = DateTime.Now,
                AddressId = applicant.AddressId,
                Read = false
            };
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            // Add to AttendanceChecklist
            var currentDate = DateTime.Today;
            await AddToAttendanceChecklist(id, currentDate);

            // Send email to Brgy user
            await SendEmailToBrgyUser(applicant);

            TempData["SuccessMessage"] = "Applicant successfully Approved!";
            return RedirectToAction(nameof(Index));
        }

        private async Task SendEmailToBrgyUser(Applicants applicant)
        {
            var brgyUsers = await _context.Users
                .Where(u => u.AddressId == applicant.AddressId)
                .ToListAsync();

            foreach (var brgyUser in brgyUsers)
            {
                if (await _userManager.IsInRoleAsync(brgyUser, "brgy"))
                {
                    await _fluentEmail
                        .To(brgyUser.Email)
                        .Subject("Applicant Approval Notification")
                        .Body($"Dear {brgyUser.UserName},\n\nThe applicant {applicant.FirstName} {applicant.LastName} has been approved.\n\nBest Regards,\nTUPAD Portal")
                        .SendAsync();
                }
            }
        }

        // POST: AdminApplicants/Disapprove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disapprove(int id)
        {
            var applicant = await _context.Applicants.FindAsync(id);
            if (applicant == null)
            {
                return NotFound();
            }

            applicant.Approved = false;
            _context.Update(applicant);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Applicant successfully Disapproved!";
            return RedirectToAction(nameof(Index));
        }

        // Helper Method: Send Email to Barangay Users about Slot Update
        private async Task SendEmailToBrgyUsers(Address address)
        {
            // Find the Brgy users responsible for this address
            var brgyUsers = await _context.Users
                .Where(u => u.AddressId == address.Add_Id)
                .ToListAsync();

            foreach (var brgyUser in brgyUsers)
            {
                // Check if the user is in the 'brgy' role
                if (await _userManager.IsInRoleAsync(brgyUser, "brgy"))
                {
                    // Compose and send the email using FluentEmail
                    await _fluentEmail
                        .To(brgyUser.Email)
                        .Subject("Slot Updated Notification")
                        .Body($"Dear {brgyUser.UserName},\n\nThe slot for {address.Barangay} has been updated by the admin.\n\nBest Regards,\nTUPAD Portal")
                        .SendAsync();
                }
            }
        }

        // Helper Method: Add Applicant to Attendance Checklist
        private async Task AddToAttendanceChecklist(int applicantId, DateTime startDate)
        {
            // Check if already in AttendanceChecklist for the current period
            var checklist = await _context.AttendanceChecklists
                .FirstOrDefaultAsync(c => c.ApplicantId == applicantId && c.StartDate <= startDate && c.EndDate >= startDate);

            if (checklist == null)
            {
                checklist = new AttendanceChecklist
                {
                    ApplicantId = applicantId,
                    StartDate = startDate,
                    EndDate = startDate.AddDays(9),
                    DaysCheckedSerialized = "false,false,false,false,false,false,false,false,false,false", // Initial days unchecked
                };
                _context.AttendanceChecklists.Add(checklist);
            }

            await _context.SaveChangesAsync();
        }

        // Helper Method: Remove Applicant from Attendance Checklist
        private async Task RemoveFromAttendanceChecklist(int applicantId)
        {
            var checklist = await _context.AttendanceChecklists
                .FirstOrDefaultAsync(c => c.ApplicantId == applicantId);

            if (checklist != null)
            {
                _context.AttendanceChecklists.Remove(checklist);
                await _context.SaveChangesAsync();
            }
        }

        // Helper Method: Check if Address Exists
        private bool AddressExists(int id)
        {
            return _context.Addresses.Any(e => e.Add_Id == id);
        }

        // Helper Method: Check if Applicant Exists
        private bool ApplicantExists(int id)
        {
            return (_context.Applicants?.Any(e => e.ApplicantId == id)).GetValueOrDefault();
        }
    }
}
