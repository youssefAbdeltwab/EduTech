using BLL.Service.Abstraction;
using DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduTech.Controllers
{
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly ICourseService _courseService;
        private readonly IPaymentService _paymentService;
        private readonly IExamService _examService;

        public StudentController(IStudentService studentService, ICourseService courseService, IPaymentService paymentService, IExamService examService)
        {
            _studentService = studentService;
            _courseService = courseService;
            _paymentService = paymentService;
            _examService = examService;
        }

        // GET: Student
        public async Task<IActionResult> Index(int? search)
        {
            List<Student> students;
            if (search.HasValue)
            {
                var student = await _studentService.SearchAsync(search.Value);
                ViewData["Search"] = search;
                students = student != null ? new List<Student> { student } : new List<Student>();
            }
            else
            {
                students = await _studentService.GetAllAsync();
            }
            return View(students);
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var student = await _studentService.GetByIdAsync(id);
            if (student == null)
                return NotFound();

            ViewBag.Payments = await _paymentService.GetStudentPaymentsAsync(id);
            ViewBag.StudentExams = await _examService.GetStudentExamsAsync(id);
            return View(student);
        }

        // GET: Student/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName");
            return View();
        }

        // POST: Student/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            if (ModelState.IsValid)
            {
                await _studentService.CreateAsync(student);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", student.CourseId);
            return View(student);
        }

        // GET: Student/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _studentService.GetByIdAsync(id);
            if (student == null)
                return NotFound();

            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", student.CourseId);
            return View(student);
        }

        // POST: Student/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                await _studentService.UpdateAsync(student);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", student.CourseId);
            return View(student);
        }

        // GET: Student/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _studentService.GetByIdAsync(id);
            if (student == null)
                return NotFound();

            return View(student);
        }

        // POST: Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _studentService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
