using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tupadportal.Data;
using tupadportal.Models;
using Microsoft.AspNetCore.Authorization;
using tupadportal.Extensions; // Include the extensions namespace

namespace tupadportal.Controllers
{
    [Authorize]
    [Route("AdminAttendances")]
    public class AdminAttendancesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminAttendancesController> _logger;

        public AdminAttendancesController(ApplicationDbContext context, ILogger<AdminAttendancesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var checklists = await _context.AttendanceChecklists
                                           .Include(c => c.Applicant)
                                           .ToListAsync();
            return View(checklists);
        }

        //[HttpGet("ApplicantAttendance/{applicantId}")]
        //public async Task<IActionResult> AdminApplicantAttendance(int applicantId)
        //{
        //    var attendances = await _context.Attendances
        //                                    .Include(a => a.Applicant)
        //                                    .Where(a => a.ApplicantId == applicantId)
        //                                    .ToListAsync();

        //    if (Request.IsAjaxRequest())
        //    {
        //        return PartialView("_AdminApplicantAttendance", attendances);
        //    }

        //    return View(attendances);
        //}
        // NEW: Action to return the partial view for Applicant Attendance
        [HttpGet("ApplicantAttendance/{applicantId}")]
        public async Task<IActionResult> AdminApplicantAttendance(int applicantId)
        {
            // Get the current logged-in user
            var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            // Find the applicant by applicantId
            var applicant = await _context.Applicants
                                           .Include(a => a.Attendances)
                                           .FirstOrDefaultAsync(a => a.ApplicantId == applicantId);

            if (applicant == null)
            {
                return NotFound("Applicant not found.");
            }

            // Fetch the attendance records for the applicant
            var attendances = applicant.Attendances.ToList();

            // Return the partial view with attendance data
            return PartialView("_AdminApplicantAttendance", new
            {
                Applicant = new
                {
                    applicant.FirstName,
                    applicant.LastName,
                    applicant.Barangay
                },
                Attendances = attendances
            });
        }



        // Other actions...
    }
}
