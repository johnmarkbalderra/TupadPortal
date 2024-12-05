using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tupadportal.Data;
using tupadportal.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace tupadportal.Controllers
{
    [Authorize(Roles = "brgy")]
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Announcements
        public async Task<IActionResult> Index()
        {
            // Get the logged-in user's address (barangay)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users
                                     .Include(u => u.Address)
                                     .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Address == null)
            {
                return NotFound("User or user's address not found.");
            }

            // Get announcements for the user's barangay
            var announcements = await _context.Announcements
                                              .Include(a => a.Address)
                                              .Where(a => a.AddressId == user.AddressId)
                                              .OrderByDescending(a => a.CreatedDate)
                                              .ToListAsync();

            return View(announcements);
        }

        // GET: Announcements/Details/5
        // GET: Announcements/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest("Announcement ID is required.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users
                                     .Include(u => u.Address)
                                     .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.AddressId == null)
            {
                return NotFound("User or user's address not found.");
            }

            var announcement = await _context.Announcements
                .Include(a => a.Address)
                .FirstOrDefaultAsync(m => m.AnnouncementsId == id);

            if (announcement == null)
            {
                return NotFound("Announcement not found.");
            }

            // Check if the announcement belongs to the user's address
            if (announcement.AddressId != user.AddressId)
            {
                return Forbid("You are not authorized to view this announcement.");
            }

            // Mark as read if not already
            if (!announcement.Read)
            {
                announcement.Read = true;
                _context.Update(announcement);
                await _context.SaveChangesAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Return partial view for AJAX requests
                return PartialView("_DetailsPartial", announcement);
            }

            // For non-AJAX requests, return the standard view
            return View(announcement);
        }


        // GET: Announcements/Create
        public IActionResult Create()
        {
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay");
            return View();
        }

        // POST: Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnnouncementsId,Title,Description,Read,AddressId,CreatedDate")] Announcement announcement)
        {
            if (ModelState.IsValid)
            {
                _context.Add(announcement);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", announcement.AddressId);
            
            return View(announcement);
        }

        // Additional actions: Edit, Delete, etc.
    }
}
