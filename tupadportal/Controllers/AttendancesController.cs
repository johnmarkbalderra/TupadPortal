using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using tupadportal.Data;
using tupadportal.Models;
using Microsoft.AspNetCore.WebUtilities; // Added for QueryHelpers
using Microsoft.AspNetCore.Mvc.Rendering; // Add this line

namespace tupadportal.Controllers
{
    [Authorize(Roles = "brgy, admin")]
    [Route("Attendances")]
    public class AttendancesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendancesController> _logger;

        public AttendancesController(ApplicationDbContext context, ILogger<AttendancesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /*[HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var attendances = await _context.Attendances
                .Include(a => a.Applicant)
                .ToListAsync();
            return View(attendances);
        }*/

        [HttpGet("")]
        public async Task<IActionResult> Index(int? batchId)
        {
            // Get the current logged-in user
            var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.Include(u => u.Address).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Address == null)
            {
                return Unauthorized("User or address not found.");
            }

            // Get the barangay of the logged-in user
            var userBarangay = user.Address.Barangay;

            // Fetch available batches for the filter
            ViewBag.BatchId = new SelectList(await _context.Batches.ToListAsync(), "BatId", "BatchName", batchId);

            // Filter attendance checklists by batch and barangay
            var checklistsQuery = _context.AttendanceChecklists
                .Include(c => c.Applicant)
                .ThenInclude(a => a.Batch)
                .Where(c => c.Applicant.Barangay == userBarangay)
                .AsQueryable();

            // If a batch is selected, filter by batch
            if (batchId.HasValue)
            {
                checklistsQuery = checklistsQuery.Where(c => c.Applicant.BatchId == batchId.Value);
            }

            var checklists = await checklistsQuery.ToListAsync();
            return View(checklists);
        }

        // NEW: Action to return the partial view for Applicant Attendance
        [HttpGet("ApplicantAttendancePartial/{applicantId}")]
        public async Task<IActionResult> ApplicantAttendancePartial(int applicantId)
        {
            // Get the current logged-in user
            var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.Include(u => u.Address).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Address == null)
            {
                return Unauthorized("User or address not found.");
            }

            // Get the barangay of the logged-in user
            var userBarangay = user.Address.Barangay;

            // Find the applicant by applicantId
            var applicant = await _context.Applicants.Include(a => a.Address).FirstOrDefaultAsync(a => a.ApplicantId == applicantId);

            if (applicant == null)
            {
                return NotFound("Applicant not found.");
            }

            // Check if the applicant belongs to the same barangay as the logged-in user
            if (applicant.Barangay != userBarangay)
            {
                return Unauthorized("You do not have permission to view this applicant's attendance.");
            }

            // Fetch the attendance records for the applicant
            var attendances = await _context.Attendances
                .Include(a => a.Applicant)
                .Where(a => a.ApplicantId == applicantId)
                .ToListAsync();

            // Return the partial view with attendance data
            return PartialView("_ApplicantAttendancePartial", attendances);
        }


        [HttpGet("ApplicantAttendance/{applicantId}")]
        public async Task<IActionResult> ApplicantAttendance(int applicantId)
        {
            // Existing authorization and data retrieval logic...

            // Fetch the attendance records for the applicant
            var attendances = await _context.Attendances
                .Include(a => a.Applicant)
                .Where(a => a.ApplicantId == applicantId)
                .ToListAsync();

            // Check if the request is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Return partial view for AJAX requests
                return PartialView("_ApplicantAttendancePartial", attendances);
            }

            // Return full view for non-AJAX requests
            return View(attendances);
        }


        [HttpGet("DownloadQRCode/{applicantId}/{daysAhead?}")]
        public async Task<IActionResult> DownloadQRCode(int applicantId, int daysAhead = 0)
        {
            var applicant = await _context.Applicants.FindAsync(applicantId);
            if (applicant == null || !applicant.Approved)
            {
                return NotFound("Applicant not approved or does not exist.");
            }

            var date = DateTime.Now.AddDays(daysAhead).ToString("yyyy-MM-dd");
            var qrText = Url.Action("ScanQRCode", "Attendances", new { applicantId, date }, Request.Scheme);
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(qrCodeData))
            using (var bitmap = qrCode.GetGraphic(20))
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                var qrCodeImage = stream.ToArray();
                return File(qrCodeImage, "image/png", $"QRCode_{applicantId}.png");
            }
        }

        [HttpPost("MarkAttendanceByQRCode")]
        public async Task<IActionResult> MarkAttendanceByQRCode([FromBody] QRCodeMessageModel qrCodeMessageModel)
        {
            _logger.LogInformation("Received QR code message: {qrCodeMessage}", qrCodeMessageModel.qrCodeMessage);

            try
            {
                var url = new Uri(qrCodeMessageModel.qrCodeMessage);
                var query = System.Web.HttpUtility.ParseQueryString(url.Query);
                if (!int.TryParse(query.Get("applicantId"), out int applicantId) ||
                    !DateTime.TryParse(query.Get("date"), out DateTime date))
                {
                    _logger.LogError("Invalid QR Code data: applicantId={applicantId}, date={date}");
                    return BadRequest("Invalid QR Code data.");
                }

                var applicant = await _context.Applicants.FindAsync(applicantId);
                if (applicant == null)
                {
                    _logger.LogError("Applicant not found: applicantId={applicantId}");
                    return NotFound("Applicant not found.");
                }

                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.ApplicantId == applicantId && a.Date == date);

                var now = DateTime.Now;

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        ApplicantId = applicantId,
                        Date = date,
                        TimeInAM = now,
                        TimeOutAM = null
                    };
                    _context.Attendances.Add(attendance);
                }
                else
                {
                    if (attendance.TimeInAM == null)
                    {
                        attendance.TimeInAM = now;
                        _logger.LogInformation("Set TimeInAM: {time}", now);
                    }
                    else if (attendance.TimeOutAM == null && (now - attendance.TimeInAM.Value).TotalMinutes >= 1)
                    {
                        attendance.TimeOutAM = now;
                        _logger.LogInformation("Set TimeOutAM: {time}", now);
                    }
                    else
                    {
                        _logger.LogWarning("All attendance times already recorded or insufficient time gap: now={now}");
                        return BadRequest("Attendance for today has already been fully recorded or insufficient time gap between scans.");
                    }
                }

                await _context.SaveChangesAsync();
                await UpdateAttendanceChecklist(applicantId, date);

                _logger.LogInformation("Attendance marked successfully for applicantId={applicantId} on date={date}");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attendance for QR code message: {qrCodeMessage}", qrCodeMessageModel.qrCodeMessage);
                return StatusCode(500, "Internal server error. Please try again later.");
            }
        }

        private async Task UpdateAttendanceChecklist(int applicantId, DateTime date)
        {
            var checklist = await _context.AttendanceChecklists
                .FirstOrDefaultAsync(c => c.ApplicantId == applicantId);
            if (checklist == null) return;

            int dayIndex = (date - checklist.StartDate).Days;
            if (dayIndex >= 0 && dayIndex < 10)
            {
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.ApplicantId == applicantId && a.Date.Date == date.Date);

                if (attendance != null &&
                    attendance.TimeInAM.HasValue &&
                    attendance.TimeOutAM.HasValue)
                {
                    var daysChecked = checklist.DaysChecked;
                    daysChecked[dayIndex] = true;
                    checklist.DaysChecked = daysChecked;
                }
            }

            await _context.SaveChangesAsync();
        }

        [HttpPost("SaveSignature")]
        public async Task<IActionResult> SaveSignature([FromBody] SignatureModel model)
        {
            if (model == null || model.ApplicantId == 0 || string.IsNullOrEmpty(model.Signature))
            {
                return BadRequest("Invalid data.");
            }

            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.ApplicantId == model.ApplicantId && a.Date.Date == model.Date.Date);
            if (attendance == null)
            {
                return NotFound("Attendance record not found for the specified date.");
            }

            attendance.Signature = model.Signature;
            await _context.SaveChangesAsync();

            return Ok();
        }

        public class QRCodeMessageModel
        {
            public string? qrCodeMessage { get; set; }
        }
    }
}
