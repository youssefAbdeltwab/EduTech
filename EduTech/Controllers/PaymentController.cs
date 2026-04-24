using BLL.Service.Abstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduTech.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IStudentService _studentService;
        private readonly ICourseService _courseService;

        public PaymentController(IPaymentService paymentService, IStudentService studentService, ICourseService courseService)
        {
            _paymentService = paymentService;
            _studentService = studentService;
            _courseService = courseService;
        }

        // GET: Payment?month=2&year=2026&courseId=1&status=paid
        public async Task<IActionResult> Index(int? month, int? year, int? courseId, string? status)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var students = await _studentService.GetAllAsync();

            // Filter by course if selected
            if (courseId.HasValue && courseId.Value > 0)
            {
                students = students.Where(s => s.CourseId == courseId.Value).ToList();
            }

            var payments = await _paymentService.GetByMonthAsync(selectedMonth, selectedYear);
            var paidStudentIds = payments.Select(p => p.StudentId).ToHashSet();

            // Filter by payment status
            if (status == "paid")
            {
                students = students.Where(s => paidStudentIds.Contains(s.Id)).ToList();
            }
            else if (status == "unpaid")
            {
                students = students.Where(s => !paidStudentIds.Contains(s.Id)).ToList();
            }

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedCourseId = courseId ?? 0;
            ViewBag.SelectedStatus = status ?? "all";
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", courseId);
            ViewBag.PaidStudentIds = paidStudentIds;

            return View(students);
        }

        // POST: Payment/Pay
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int studentId, int month, int year, int? courseId, string? status)
        {
            await _paymentService.CreatePaymentAsync(studentId, month, year);
            return RedirectToAction(nameof(Index), new { month, year, courseId, status });
        }
    }
}
