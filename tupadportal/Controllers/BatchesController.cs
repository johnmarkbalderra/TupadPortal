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
    public class BatchesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFluentEmail _fluentEmail;
        private readonly UserManager<ApplicationUser> _userManager;

        public BatchesController(ApplicationDbContext context, IFluentEmail fluentEmail, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _fluentEmail = fluentEmail;
            _userManager = userManager;
        }

        // GET: Batches
        public async Task<IActionResult> Index(int? barangayId)
        {
            var batchesQuery = _context.Batches
                .Include(b => b.Address)
                .Include(b => b.Applicants)
                .AsQueryable();

            // Apply filter only if barangayId has a value greater than 0
            if (barangayId.HasValue && barangayId.Value > 0)
            {
                batchesQuery = batchesQuery.Where(b => b.AddressId == barangayId);
            }

            var batches = await batchesQuery.ToListAsync();
            var barangays = await _context.Addresses.ToListAsync();

            ViewBag.BarangayId = new SelectList(barangays, "Add_Id", "Barangay", barangayId);

            return View(batches);
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

        // GET: Batches/Create
        public IActionResult Create()
        {
            ViewData["AddressId"] = new SelectList(_context.Addresses.Select(a => new { a.Add_Id, a.Barangay }).ToList(), "Add_Id", "Barangay");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial");
            }
            return View();
        }

        // POST: Batches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BatId,BatchName,Slot,DateTime,AddressId")] Batch batch)
        {
            if (ModelState.IsValid)
            {
                _context.Add(batch);
                await _context.SaveChangesAsync();

                // Notify Barangay users about the new batch
                var announcement = new Announcement
                {
                    Title = "New Batch Created",
                    Description = $"A batch named {batch.BatchName} has a {batch.Slot} slots Added by the admin.",
                    AddressId = batch.AddressId,
                    CreatedDate = DateTime.Now,
                    Read = false // Mark as unread for the barangay users
                };

                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();

                // Send email to Brgy users
                var address = await _context.Addresses.FindAsync(batch.AddressId);
                await SendEmailToBrgyUsers(address, batch.BatchName, batch.Slot);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Batch successfully created, and notification sent!" });
                }
                TempData["SuccessMessage"] = "Batch successfully created!";
                return RedirectToAction(nameof(Index));
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreatePartial", batch);
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", batch.AddressId);
            return View(batch);
        }

        

        // GET: Batches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Batches == null)
            {
                return NotFound();
            }

            // Ensure Address is included
            var batch = await _context.Batches
                                      .Include(b => b.Address)
                                      .FirstOrDefaultAsync(b => b.BatId == id);
            if (batch == null)
            {
                return NotFound();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditPartial", batch);
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", batch.AddressId);
            return View(batch);
        }


        // POST: Batches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BatId,BatchName,Slot,DateTime,AddressId")] Batch batch)
        {
            if (id != batch.BatId)
            {
                return Json(new { success = false, message = "Batch ID mismatch." });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Detach existing tracked entities to avoid tracking conflicts
                    var existingBatch = await _context.Batches
                        .AsNoTracking()
                        .Include(b => b.Address)
                        .FirstOrDefaultAsync(b => b.BatId == id);

                    if (existingBatch == null)
                    {
                        return Json(new { success = false, message = "Batch not found." });
                    }

                    existingBatch.Slot = batch.Slot;
                    _context.Entry(existingBatch).State = EntityState.Modified;

                    // Notify Barangay users about the slot update
                    var announcement = new Announcement
                    {
                        Title = "Slot Updated",
                        Description = $"A batch named {batch.BatchName} now has {batch.Slot} slots, updated by the admin.",
                        AddressId = existingBatch.AddressId,
                        CreatedDate = DateTime.Now,
                        Read = false // Mark as unread for the barangay users
                    };

                    _context.Announcements.Add(announcement);
                    await _context.SaveChangesAsync();

                    // Send email to Brgy users
                    await SendEmailToBrgyUsers(existingBatch.Address, batch.BatchName, batch.Slot);

                    // Return JSON response indicating success
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Slot updated successfully, and notification sent!" });
                    }
                    TempData["SuccessMessage"] = "Batch successfully created!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BatchExists(batch.BatId))
                    {
                        return Json(new { success = false, message = "Batch not found." });
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["AddressId"] = new SelectList(_context.Addresses, "Add_Id", "Barangay", batch.AddressId);
            return PartialView("_EditPartial", batch);
        }

        


        // GET: Batches/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Batches == null)
            {
                return NotFound();
            }

            var batch = await _context.Batches
                .Include(b => b.Address)
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
                return Problem("Entity set 'ApplicationDbContext.Batches'  is null.");
            }
            var batch = await _context.Batches.FindAsync(id);
            if (batch != null)
            {
                _context.Batches.Remove(batch);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // Helper Method: Send Email to Barangay Users about Slot Update
        private async Task SendEmailToBrgyUsers(Address address, string batchName, int slot)
        {
            var brgyUsers = await _context.Users
                .Where(u => u.AddressId == address.Add_Id)
                .ToListAsync();

            foreach (var brgyUser in brgyUsers)
            {
                if (await _userManager.IsInRoleAsync(brgyUser, "brgy"))
                {
                    await _fluentEmail
                        .To(brgyUser.Email)
                        .Subject("Slot Updated Notification")
                        .Body($"Dear {brgyUser.UserName},\n\nThe slot for the batch named {batchName} has been updated to {slot} slots for {address.Barangay}.\n\nBest Regards,\nTUPAD Portal")
                        .SendAsync();
                }
            }
        }

        private bool BatchExists(int id)
        {
          return (_context.Batches?.Any(e => e.BatId == id)).GetValueOrDefault();
        }
    }
}
