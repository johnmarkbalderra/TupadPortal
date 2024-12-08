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
                if (!int.TryParse(query.Get("applicantId"), out int applicantId))
                {
                    _logger.LogError("Invalid QR Code data: applicantId={applicantId}");
                    return BadRequest("Invalid QR Code data.");
                }

                var applicant = await _context.Applicants.FindAsync(applicantId);
                if (applicant == null)
                {
                    _logger.LogError("Applicant not found: applicantId={applicantId}");
                    return NotFound("Applicant not found.");
                }

                // Retrieve the specific StartDate from the checklist
                var checklist = await _context.AttendanceChecklists
                    .FirstOrDefaultAsync(c => c.ApplicantId == applicantId);

                if (checklist == null || checklist.StartDate == null)
                {
                    _logger.LogError("StartDate not found for applicantId={applicantId}");
                    return BadRequest("StartDate not found for the applicant.");
                }

                var startDate = checklist.StartDate.Date; // Use the specific StartDate
                var currentDate = DateTime.Now.Date; // Get today's date

                // Validate if StartDate matches the current date
                if (startDate != currentDate)
                {
                    _logger.LogWarning("StartDate does not match the current date. Attendance not recorded for applicantId={applicantId}");
                    return BadRequest("Attendance can only be recorded on the specified StartDate.");
                }

                // Fetch attendance record for the specific StartDate
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.ApplicantId == applicantId && a.Date == startDate);

                if (attendance == null)
                {
                    // Create new attendance record for StartDate with TimeInAM as current time
                    attendance = new Attendance
                    {
                        ApplicantId = applicantId,
                        Date = startDate,
                        TimeInAM = DateTime.Now, // Save the current time
                        TimeOutAM = null
                    };
                    _context.Attendances.Add(attendance);
                    _logger.LogInformation("TimeInAM saved: {time}", attendance.TimeInAM);
                }
                else
                {
                    var currentTime = DateTime.Now;

                    if (attendance.TimeInAM == null)
                    {
                        attendance.TimeInAM = currentTime; // Save the current time for TimeIn
                        _logger.LogInformation("Set TimeInAM: {time}", attendance.TimeInAM);
                    }
                    else if (attendance.TimeOutAM == null)
                    {
                        // Ensure at least 1 minute has passed since TimeInAM
                        var timeDifference = currentTime - attendance.TimeInAM.Value;
                        if (timeDifference.TotalMinutes < 1)
                        {
                            _logger.LogWarning("TimeOutAM cannot be saved within 1 minute of TimeInAM. TimeDifference={timeDifference}", timeDifference.TotalSeconds);
                            return BadRequest("You can only record TimeOutAM at least 1 minute after TimeInAM.");
                        }

                        attendance.TimeOutAM = currentTime; // Save the current time for TimeOut
                        _logger.LogInformation("Set TimeOutAM: {time}", attendance.TimeOutAM);
                    }
                    else
                    {
                        _logger.LogWarning("All attendance times already recorded for StartDate={startDate}");
                        return BadRequest("Attendance for the specified StartDate has already been fully recorded.");
                    }
                }

                await _context.SaveChangesAsync();
                await UpdateAttendanceChecklist(applicantId, startDate);

                _logger.LogInformation("Attendance marked successfully for applicantId={applicantId} on StartDate={startDate}");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing QR code message: {qrCodeMessage}", qrCodeMessageModel.qrCodeMessage);
                return StatusCode(500, "Internal server error. Please try again later.");
            }
        }


        private async Task UpdateAttendanceChecklist(int applicantId, DateTime qrCodeDate)
        {
            // Find the corresponding checklist for the applicant
            var checklist = await _context.AttendanceChecklists
                .FirstOrDefaultAsync(c => c.ApplicantId == applicantId);

            // If checklist does not exist, return
            if (checklist == null) return;

            // Calculate the index of the day in the checklist based on the QR code date
            int dayIndex = (qrCodeDate - checklist.StartDate).Days;

            // Check if the day index is valid (between 0 and 9)
            if (dayIndex >= 0 && dayIndex < 10)
            {
                // Find the attendance for the given applicant and date
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.ApplicantId == applicantId && a.Date.Date == qrCodeDate.Date);

                // If attendance is found and the required time-in/out is recorded
                if (attendance != null &&
                    attendance.TimeInAM.HasValue &&
                    attendance.TimeOutAM.HasValue)
                {
                    // Update the checklist days as checked for the corresponding day
                    var daysChecked = checklist.DaysChecked;
                    daysChecked[dayIndex] = true;
                    checklist.DaysChecked = daysChecked;
                }
            }

            // Save changes to the context
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
