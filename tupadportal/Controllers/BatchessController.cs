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
    [Authorize]
    public class BatchessController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFluentEmail _fluentEmail;
        private readonly UserManager<ApplicationUser> _userManager;

        public BatchessController(ApplicationDbContext context, IFluentEmail fluentEmail, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _fluentEmail = fluentEmail;
            _userManager = userManager;
        }

        // GET: Batches
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Batches.Include(b => b.Applicants);  // Updated to load Applicants instead of Address
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Batches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Batches == null)
            {
                return NotFound();
            }

            var batch = await _context.Batches
                .Include(b => b.Applicants)  // Include Applicants instead of Address
                .FirstOrDefaultAsync(m => m.BatId == id);
            if (batch == null)
            {
                return NotFound();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DetailsPartial", batch);
            }

            return View(batch);
        }

        public IActionResult Create()
        {
            ViewBag.Barangays = new SelectList(_context.Addresses.Select(a => new { a.Add_Id, a.Barangay }).ToList(), "Add_Id", "Barangay");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial");
            }
            return View();
        }


        // POST: Batches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BatId,BatchName,DateTime")] Batch batch)  // Removed AddressId
        {
            if (ModelState.IsValid)
            {
                _context.Add(batch);
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Batch successfully created!" });
                }
                TempData["SuccessMessage"] = "Batch successfully created!";
                return RedirectToAction(nameof(Index));
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial", batch);
            }
            return View(batch);
        }

        // GET: Batches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Batches == null)
            {
                return NotFound();
            }

            var batch = await _context.Batches.FindAsync(id);
            if (batch == null)
            {
                return NotFound();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", batch);
            }
            return View(batch);
        }

        // POST: Batches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BatId,BatchName,DateTime")] Batch batch)  // Removed AddressId
        {
            if (id != batch.BatId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(batch);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BatchExists(batch.BatId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Batch successfully updated!" });
                }
                TempData["SuccessMessage"] = "Batch successfully updated!";
                return RedirectToAction(nameof(Index));
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", batch);
            }
            return View(batch);
        }

        // GET: Batches/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Batches == null)
            {
                return NotFound();
            }

            var batch = await _context.Batches
                .Include(b => b.Applicants)  // Include Applicants instead of Address
                .FirstOrDefaultAsync(m => m.BatId == id);
            if (batch == null)
            {
                return NotFound();
            }

            return View(batch);
        }

        // POST: Batches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Batches == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Batches' is null.");
            }
            var batch = await _context.Batches.FindAsync(id);
            if (batch != null)
            {
                _context.Batches.Remove(batch);
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Batch successfully deleted!" });
                }
                TempData["SuccessMessage"] = "Batch successfully deleted!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Batches/GetApplicants/5
        public async Task<IActionResult> GetApplicants(int? id)
        {
            if (id == null || _context.Batches == null)
            {
                return NotFound();
            }

            var batch = await _context.Batches
                .Include(b => b.Applicants)
                .FirstOrDefaultAsync(b => b.BatId == id);

            if (batch == null)
            {
                return NotFound();
            }

            var applicants = batch.Applicants;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ApplicantsPartial", applicants);
            }

            return View(applicants);
        }

        // GET: Batches/GetApplicantDetails/5
        public async Task<IActionResult> GetApplicantDetails(int? id)
        {
            if (id == null || _context.Applicants == null)
            {
                return NotFound();
            }

            var applicant = await _context.Applicants
                .Include(a => a.Address)  // No changes here since Applicant still has Address
                .FirstOrDefaultAsync(a => a.ApplicantId == id);

            if (applicant == null)
            {
                return NotFound();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ApplicantDetailsPartial", applicant);
            }

            return View(applicant);
        }

        private bool BatchExists(int id)
        {
            return (_context.Batches?.Any(e => e.BatId == id)).GetValueOrDefault();
        }
    }
}
