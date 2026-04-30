using BLL.Service.Abstraction;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduTech.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ICourseService _courseService;
        private readonly IAttendanceSyncManager _syncManager;
        private readonly IAttendanceSessionService _sessionService;
        private readonly IConfiguration _configuration;

        public AttendanceController(
            IAttendanceService attendanceService,
            ICourseService courseService,
            IAttendanceSyncManager syncManager,
            IAttendanceSessionService sessionService,
            IConfiguration configuration)
        {
            _attendanceService = attendanceService;
            _courseService = courseService;
            _syncManager = syncManager;
            _sessionService = sessionService;
            _configuration = configuration;
        }

        // GET: Attendance?courseId=1&month=4&year=2026
        public async Task<IActionResult> Index(int? courseId, int? month, int? year)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var courses = await _courseService.GetAllAsync();
            ViewBag.Courses = new SelectList(courses, "Id", "CourseName", courseId);
            ViewBag.SelectedCourseId = courseId ?? 0;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.GoogleFormUrl = _configuration["GoogleSheets:FormUrl"] ?? "";

            List<Attendance> attendances = new();

            if (courseId.HasValue && courseId.Value > 0)
            {
                attendances = await _attendanceService.GetByCourseMonthAsync(courseId.Value, selectedMonth, selectedYear);
                ViewBag.ActiveSession = await _sessionService.GetActiveSessionAsync(courseId.Value);
            }

            return View(attendances);
        }

        // POST: Attendance/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int courseId, int month, int year, List<Attendance> attendances)
        {
            await _attendanceService.SaveAsync(attendances);
            TempData["Success"] = "تم حفظ الحضور بنجاح.";
            return RedirectToAction(nameof(Index), new { courseId, month, year });
        }

        // POST: Attendance/SyncGoogleData
        [HttpPost]
        public async Task<IActionResult> SyncGoogleData()        {
            try
            {
                var result = await _syncManager.ProcessPendingAttendanceAsync();

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"تمت المزامنة بنجاح: {result.ValidRecords} سجل جديد، {result.SkippedRecords} تم تخطيه.",
                        validRecords = result.ValidRecords,
                        skippedRecords = result.SkippedRecords,
                        processedRows = result.ProcessedRows,
                        errors = result.Errors
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "حدثت أخطاء أثناء المزامنة: " + string.Join(" | ", result.Errors)
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"خطأ في الاتصال: {ex.Message}"
                });
            }
        }

        // POST: Attendance/GenerateSession
        [HttpPost]
        public async Task<IActionResult> GenerateSession(int courseId)
        {
            var session = await _sessionService.GenerateSessionAsync(courseId);
            return Json(new
            {
                success = true,
                sessionId = session.Id,
                pin = session.SessionPIN,
                createdAt = session.CreatedAt.ToString("HH:mm")
            });
        }

        // POST: Attendance/DeactivateSession
        [HttpPost]
        public async Task<IActionResult> DeactivateSession(int sessionId)
        {
            await _sessionService.DeactivateSessionAsync(sessionId);
            return Json(new { success = true });
        }

        // GET: Attendance/DebugSheet - shows raw sheet data for debugging
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DebugSheet()
        {
            try
            {
                var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"] ?? "";
                var sheetsService = (BLL.Services.GoogleSheetsService)
                    HttpContext.RequestServices.GetRequiredService<IGoogleSheetsService>();

                // Fetch raw data to see actual column structure
                var rawData = await sheetsService.GetRawDataAsync(spreadsheetId, 10);

                var rawRows = rawData?.Select((row, idx) => new
                {
                    rowIndex = idx + 1,
                    columnCount = row.Count,
                    values = row.Select(cell => cell?.ToString() ?? "").ToList()
                }).ToList();

                return Json(new
                {
                    spreadsheetId,
                    totalRows = rawData?.Count ?? 0,
                    expectedFormat = "A=Timestamp, B=StudentCode, C=CourseId, D=SessionPIN",
                    rows = rawRows
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
