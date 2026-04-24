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

        public AttendanceController(IAttendanceService attendanceService, ICourseService courseService)
        {
            _attendanceService = attendanceService;
            _courseService = courseService;
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

            List<Attendance> attendances = new();

            if (courseId.HasValue && courseId.Value > 0)
            {
                attendances = await _attendanceService.GetByCourseMonthAsync(courseId.Value, selectedMonth, selectedYear);
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
    }
}
