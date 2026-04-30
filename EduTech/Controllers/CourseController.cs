using BLL.Service.Abstraction;
using ClosedXML.Excel;
using DAL.Entities;
using DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduTech.Controllers
{
    [Authorize]
    public class CourseController : Controller
    {
        private readonly ICourseService _courseService;

        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        // GET: Course
        public async Task<IActionResult> Index()
        {
            var courses = await _courseService.GetAllAsync();
            return View(courses);
        }

        // GET: Course/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var course = await _courseService.GetByIdWithStudentsAsync(id);
            if (course == null)
                return NotFound();

            return View(course);
        }

        // GET: Course/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course)
        {
            if (ModelState.IsValid)
            {
                await _courseService.CreateAsync(course);
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: Course/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var course = await _courseService.GetByIdAsync(id);
            if (course == null)
                return NotFound();

            return View(course);
        }

        // POST: Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course)
        {
            if (id != course.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                await _courseService.UpdateAsync(course);
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: Course/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _courseService.GetByIdWithStudentsAsync(id);
            if (course == null)
                return NotFound();

            return View(course);
        }

        // POST: Course/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _courseService.DeleteAsync(id);
            if (!result)
            {
                TempData["Error"] = "لا يمكن حذف هذه الدورة لأنها تحتوي على طلاب مسجلين.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Course/ExportStudents/5
        public async Task<IActionResult> ExportStudents(int id)
        {
            var course = await _courseService.GetByIdWithStudentsAsync(id);
            if (course == null)
                return NotFound();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("الطلاب");
            worksheet.RightToLeft = true;

            // Headers
            worksheet.Cell(1, 1).Value = "#";
            worksheet.Cell(1, 2).Value = "كود الطالب";
            worksheet.Cell(1, 3).Value = "الاسم الكامل";
            worksheet.Cell(1, 4).Value = "رقم الهاتف";
            worksheet.Cell(1, 5).Value = "البريد الإلكتروني";
            worksheet.Cell(1, 6).Value = "الجنس";

            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#6f42c1");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data rows
            int row = 2;
            foreach (var student in course.Students)
            {
                worksheet.Cell(row, 1).Value = row - 1;
                worksheet.Cell(row, 2).Value = student.StudentCode;
                worksheet.Cell(row, 3).Value = student.FullName;
                worksheet.Cell(row, 4).Value = student.Phone ?? "—";
                worksheet.Cell(row, 5).Value = student.Email ?? "—";
                worksheet.Cell(row, 6).Value = student.Gender == Gender.Male ? "ذكر" : "أنثى";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"Students_{course.CourseName}_{DateTime.Now:yyyy-MM-dd}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
