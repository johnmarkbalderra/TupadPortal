using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using tupadportal.Data;
using tupadportal.Models;
using tupadportal.ViewModels;

namespace tupadportal.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var applicants = await _context.Applicants
                .Include(a => a.Address)
                .Include(a => a.Batch)
                .ToListAsync();

            var barangays = applicants
                .Select(a => a.Address.Barangay)
                .Distinct()
                .ToList();

            var batches = await _context.Batches.ToListAsync();

            var model = new DashboardViewModel
            {
                Applicants = applicants,
                Barangays = barangays,
                Batches = batches,
                TotalApplicants = applicants.Count,
                ApprovedApplicants = applicants.Count(a => a.Approved)
            };

            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> FilterByBarangay(string barangay)
        {
            var applicants = await _context.Applicants
                .Include(a => a.Address)
                .Where(a => a.Address.Barangay == barangay)
                .ToListAsync();

            var approvedCount = applicants.Count(a => a.Approved);
            var totalCount = applicants.Count;

            return Json(new { approvedCount, totalCount });
        }

        [HttpGet]
        public async Task<IActionResult> FilterByBatch(int batchId)
        {
            var applicants = await _context.Applicants
                .Include(a => a.Batch)
                .Where(a => a.BatchId == batchId)
                .ToListAsync();

            return PartialView("_BatchApplicantsPartial", applicants);
        }
    }
}
