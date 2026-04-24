using System.Diagnostics;
using BLL.Service.Abstraction;
using EduTech.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduTech.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly ICourseService _courseService;
        private readonly IPaymentService _paymentService;
        private readonly IExamService _examService;

        public HomeController(IStudentService studentService, ICourseService courseService, IPaymentService paymentService, IExamService examService)
        {
            _studentService = studentService;
            _courseService = courseService;
            _paymentService = paymentService;
            _examService = examService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var students = await _studentService.GetAllAsync();
            var courses = await _courseService.GetAllAsync();

            int currentMonth = DateTime.Now.Month;
            int currentYear = DateTime.Now.Year;
            var payments = await _paymentService.GetByMonthAsync(currentMonth, currentYear);

            int totalStudents = students.Count;
            int paidCount = payments.Count;
            int unpaidCount = totalStudents - paidCount;

            ViewBag.TotalStudents = totalStudents;
            ViewBag.PaidCount = paidCount;
            ViewBag.UnpaidCount = unpaidCount;
            ViewBag.CurrentMonth = currentMonth;
            ViewBag.CurrentYear = currentYear;
            ViewBag.Courses = courses;

            // Students whose last 5 exams are all failed
            var failingStudents = await _examService.GetFailingStudentsAsync();
            ViewBag.FailingStudents = failingStudents;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode)
        {
            var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = statusCode ?? Response.StatusCode,
                ErrorMessage = exceptionFeature?.Error?.Message
            };

            return View(model);
        }
    }
}
