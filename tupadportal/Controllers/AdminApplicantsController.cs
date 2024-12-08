using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using tupadportal.Data;
using tupadportal.Models;
using Microsoft.AspNetCore.Authorization;
using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;

namespace tupadportal.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminApplicantsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFluentEmail _fluentEmail;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminApplicantsController(ApplicationDbContext context, IFluentEmail fluentEmail, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _fluentEmail = fluentEmail;
            _userManager = userManager;
        }

       

        public async Task<IActionResult> Index(string sortColumn, string sortOrder)
        {
            var applicants = _context.Applicants
                .Include(a => a.Address)
                .Include(a => a.Batch)
                .AsQueryable();

            // Default sort order
            if (string.IsNullOrEmpty(sortOrder)) sortOrder = "asc";
            if (string.IsNullOrEmpty(sortColumn)) sortColumn = "LastName";

            // Apply sorting based on the sort column and order
            switch (sortColumn)
            {
                case "FirstName":
                    applicants = sortOrder == "asc" ? applicants.OrderBy(a => a.FirstName) : applicants.OrderByDescending(a => a.FirstName);
                    break;
                case "Barangay":
                    applicants = sortOrder == "asc" ? applicants.OrderBy(a => a.Barangay) : applicants.OrderByDescending(a => a.Barangay);
                    break;
                case "Approved":
                    applicants = sortOrder == "asc" ? applicants.OrderBy(a => a.Approved) : applicants.OrderByDescending(a => a.Approved);
                    break;
                default:
                    applicants = sortOrder == "asc" ? applicants.OrderBy(a => a.LastName) : applicants.OrderByDescending(a => a.LastName);
                    break;
            }

            // Store the sort information in ViewData to maintain state
            ViewData["SortColumn"] = sortColumn;
            ViewData["SortOrder"] = sortOrder;

            return View(await applicants.ToListAsync());
        }

        // GET: AdminApplicants/Search
        [HttpGet]
        public IActionResult Search(string searchQuery, string barangay, string batchId)
        {
            // Fetch all applicants
            var applicants = _context.Applicants.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                applicants = applicants.Where(a =>
                    a.FirstName.Contains(searchQuery) ||
                    a.LastName.Contains(searchQuery) ||
                    a.Barangay.Contains(searchQuery));
            }

            // Apply Barangay filter
            if (!string.IsNullOrWhiteSpace(barangay))
            {
                applicants = applicants.Where(a => a.Barangay == barangay);
            }

            // Apply Batch filter
            if (!string.IsNullOrWhiteSpace(batchId))
            {
                if (int.TryParse(batchId, out var parsedBatchId))
                {
                    applicants = applicants.Where(a => a.BatchId == parsedBatchId);
                }
            }

            // Return filtered results to partial view
            return PartialView("_ApplicantListPartial", applicants.ToList());
        }



        // GET: AdminApplicants/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Applicants == null)
            {
                return NotFound();
            }

            var applicants = await _context.Applicants
                .Include(a => a.Address)
                .Include(a => a.Batch)
                .FirstOrDefaultAsync(m => m.ApplicantId == id);
            if (applicants == null)
            {
                return NotFound();
            }

            return View(applicants);
        }

        // GET: AdminApplicants/Create
        public IActionResult Create()
        {
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay");
            ViewData["BatchId"] = new SelectList(_context.Batches, "BatId", "BatId");
            return View();
        }

        // POST: AdminApplicants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ApplicantId,FirstName,MiddleName,LastName,ExtensionName,Birthdate,Barangay,Municipality,IdType,IdNumber,ContactNo,BankAccountType,BankAccountNo,BeneficiaryType,Occupation,OccupationSpecify,Sex,CivilStatus,Age,MonthlyIncome,Dependent,InterestedInSkillsTraining,SkillsTrainingNeeded,Approved,DateTime,BatchId,AddressId")] Applicants applicants)
        {
            if (ModelState.IsValid)
            {
                _context.Add(applicants);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", applicants.AddressId);
            ViewData["BatchId"] = new SelectList(_context.Batches, "BatId", "BatId", applicants.BatchId);
            return View(applicants);
        }

        // GET: AdminApplicants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Applicants == null)
            {
                return NotFound();
            }

            var applicants = await _context.Applicants.FindAsync(id);
            if (applicants == null)
            {
                return NotFound();
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", applicants.AddressId);
            ViewData["BatchId"] = new SelectList(_context.Batches, "BatId", "BatId", applicants.BatchId);
            return View(applicants);
        }

        // POST: AdminApplicants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApplicantId,FirstName,MiddleName,LastName,ExtensionName,Birthdate,Barangay,Municipality,IdType,IdNumber,ContactNo,BankAccountType,BankAccountNo,BeneficiaryType,Occupation,OccupationSpecify,Sex,CivilStatus,Age,MonthlyIncome,Dependent,InterestedInSkillsTraining,SkillsTrainingNeeded,Approved,DateTime,BatchId,AddressId")] Applicants applicants)
        {
            if (id != applicants.ApplicantId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(applicants);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicantsExists(applicants.ApplicantId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", applicants.AddressId);
            ViewData["BatchId"] = new SelectList(_context.Batches, "BatId", "BatId", applicants.BatchId);
            return View(applicants);
        }

        // GET: AdminApplicants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Applicants == null)
            {
                return NotFound();
            }

            var applicants = await _context.Applicants
                .Include(a => a.Address)
                .Include(a => a.Batch)
                .FirstOrDefaultAsync(m => m.ApplicantId == id);
            if (applicants == null)
            {
                return NotFound();
            }

            return View(applicants);
        }

        // POST: AdminApplicants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Applicants == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Applicants' is null.");
            }
            var applicants = await _context.Applicants.FindAsync(id);
            if (applicants != null)
            {
                _context.Applicants.Remove(applicants);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: AdminApplicants/Approve/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Approve(int id)
        //{
        //    var applicant = await _context.Applicants
        //                                  .Include(a => a.Address)
        //                                  .FirstOrDefaultAsync(a => a.ApplicantId == id);
        //    if (applicant == null)
        //    {
        //        return NotFound();
        //    }

        //    applicant.Approved = true;
        //    _context.Update(applicant);
        //    await _context.SaveChangesAsync();

        //    // Create an announcement
        //    var announcement = new Announcement
        //    {
        //        Title = "Applicant Approved",
        //        Description = $"Applicant {applicant.FirstName} {applicant.LastName} has been approved.",
        //        CreatedDate = DateTime.Now,
        //        AddressId = applicant.AddressId,
        //        Read = false
        //    };
        //    _context.Announcements.Add(announcement);
        //    await _context.SaveChangesAsync();

        //    // Add to AttendanceChecklist
        //    var currentDate = DateTime.Today;
        //    await AddToAttendanceChecklist(id, currentDate);

        //    // Send email to Brgy user
        //    await SendEmailToBrgyUser(applicant);

        //    TempData["SuccessMessage"] = "Applicant successfully Approved!";
        //    return RedirectToAction(nameof(Index));
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, DateTime startDate)
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
            await AddToAttendanceChecklist(id, startDate);

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

        //private async Task AddToAttendanceChecklist(int applicantId, DateTime startDate)
        //{
        //    var checklist = await _context.AttendanceChecklists
        //                                  .FirstOrDefaultAsync(c => c.ApplicantId == applicantId && c.StartDate <= startDate && c.EndDate >= startDate);

        //    if (checklist == null)
        //    {
        //        checklist = new AttendanceChecklist
        //        {
        //            ApplicantId = applicantId,
        //            StartDate = startDate,
        //            EndDate = startDate.AddDays(9),
        //            DaysCheckedSerialized = "false,false,false,false,false,false,false,false,false,false",
        //        };
        //        _context.AttendanceChecklists.Add(checklist);
        //    }

        //    await _context.SaveChangesAsync();
        //}


        private async Task AddToAttendanceChecklist(int applicantId, DateTime startDate)
        {
            var checklist = await _context.AttendanceChecklists
                                          .FirstOrDefaultAsync(c => c.ApplicantId == applicantId && c.StartDate == startDate);

            if (checklist == null)
            {
                checklist = new AttendanceChecklist
                {
                    ApplicantId = applicantId,
                    StartDate = startDate,
                    EndDate = startDate.AddDays(9),
                    DaysCheckedSerialized = "false,false,false,false,false,false,false,false,false,false",
                };
                _context.AttendanceChecklists.Add(checklist);
            }

            await _context.SaveChangesAsync();
        }

        private bool ApplicantsExists(int id)
        {
            return (_context.Applicants?.Any(e => e.ApplicantId == id)).GetValueOrDefault();
        }
    }
}
