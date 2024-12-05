using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using tupadportal.Data;
using tupadportal.Models;
using tupadportal.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace tupadportal.Controllers
{
    [Authorize]
    public class AdminAnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminAnnouncementsController> _logger;

        public AdminAnnouncementsController(ApplicationDbContext context, ILogger<AdminAnnouncementsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Announcements
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Announcements
                .Include(a => a.Address)
                .OrderByDescending(a => a.CreatedDate); // Order by CreatedDate descending

            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay");
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Announcements/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Announcements == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .Include(a => a.Address)
                .FirstOrDefaultAsync(m => m.AnnouncementsId == id);
            if (announcement == null)
            {
                return NotFound();
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_DetailsPartial", announcement);
            }

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
            // Server-side validation to ensure AddressId exists
            if (!_context.Addresses.Any(a => a.Add_Id == announcement.AddressId))
            {
                ModelState.AddModelError("AddressId", "Selected address does not exist.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(announcement);
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Announcement successfully created!";
                    if (Request.IsAjaxRequest())
                    {
                        // Optionally return the newly created announcement as a partial view
                        return PartialView("_AnnouncementCard", announcement);
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error creating announcement.");
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", announcement.AddressId);
            if (Request.IsAjaxRequest())
            {
                return PartialView("_CreatePartial", announcement);
            }
            // Set success message
            TempData["SuccessMessage"] = "Announcement successfully created!";
            return View(announcement);
        }

        // GET: Announcements/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Announcements == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", announcement.AddressId);
            if (Request.IsAjaxRequest())
            {
                return PartialView("_EditPartial", announcement);
            }
            return View(announcement);
        }

        // POST: Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AnnouncementsId,Title,Description,Read,AddressId,CreatedDate")] Announcement announcement)
        {
            if (id != announcement.AnnouncementsId)
            {
                return NotFound();
            }

            // Server-side validation to ensure AddressId exists
            if (!_context.Addresses.Any(a => a.Add_Id == announcement.AddressId))
            {
                ModelState.AddModelError("AddressId", "Selected address does not exist.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(announcement);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Announcement successfully updated!";
                    if (Request.IsAjaxRequest())
                    {
                        // Optionally return the updated announcement as a partial view
                        return PartialView("_AnnouncementCard", announcement);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.AnnouncementsId))
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
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", announcement.AddressId);
            if (Request.IsAjaxRequest())
            {
                return PartialView("_EditPartial", announcement);
            }
           
            return View(announcement);
        }


        [HttpPost]
        public IActionResult DeleteSelected(string selectedIds)
        {
            if (string.IsNullOrEmpty(selectedIds))
            {
                TempData["ErrorMessage"] = "No announcements selected for deletion.";
                return RedirectToAction("Index");
            }

            var ids = selectedIds.Split(',').Select(id => int.Parse(id)).ToList();
            var announcements = _context.Announcements.Where(a => ids.Contains(a.AnnouncementsId)).ToList();

            if (announcements.Count == 0)
            {
                TempData["ErrorMessage"] = "Selected announcements not found.";
                return RedirectToAction("Index");
            }

            _context.Announcements.RemoveRange(announcements);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Selected announcements have been deleted successfully.";
            return RedirectToAction("Index");
        }





        // GET: Announcements/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Announcements == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .Include(a => a.Address)
                .FirstOrDefaultAsync(m => m.AnnouncementsId == id);
            if (announcement == null)
            {
                return NotFound();
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_DeletePartial", announcement);
            }

            return View(announcement);
        }

        // POST: Announcements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Announcements == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Announcements' is null.");
            }
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Announcement successfully deleted!";
            if (Request.IsAjaxRequest())
            {
                // Optionally return a success status
                return Ok();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AnnouncementExists(int id)
        {
            return (_context.Announcements?.Any(e => e.AnnouncementsId == id)).GetValueOrDefault();
        }
    }
}
