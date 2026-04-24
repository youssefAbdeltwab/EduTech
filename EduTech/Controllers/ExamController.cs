using BLL.Service.Abstraction;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduTech.Controllers
{
    [Authorize]
    public class ExamController : Controller
    {
        private readonly IExamService _examService;
        private readonly ICourseService _courseService;

        public ExamController(IExamService examService, ICourseService courseService)
        {
            _examService = examService;
            _courseService = courseService;
        }

        // GET: Exam
        public async Task<IActionResult> Index()
        {
            var exams = await _examService.GetAllAsync();
            return View(exams);
        }

        // GET: Exam/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var exam = await _examService.GetByIdWithStudentsAsync(id);
            if (exam == null)
                return NotFound();

            return View(exam);
        }

        // GET: Exam/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName");
            return View();
        }

        // POST: Exam/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exam exam)
        {
            if (ModelState.IsValid)
            {
                await _examService.CreateAsync(exam);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", exam.CourseId);
            return View(exam);
        }

        // GET: Exam/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var exam = await _examService.GetByIdAsync(id);
            if (exam == null)
                return NotFound();

            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", exam.CourseId);
            return View(exam);
        }

        // POST: Exam/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Exam exam)
        {
            if (id != exam.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                await _examService.UpdateAsync(exam);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", exam.CourseId);
            return View(exam);
        }

        // GET: Exam/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var exam = await _examService.GetByIdWithStudentsAsync(id);
            if (exam == null)
                return NotFound();

            return View(exam);
        }

        // POST: Exam/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _examService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // POST: Exam/SaveScores
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScores(int examId, List<StudentExam> studentExams)
        {
            await _examService.SaveScoresAsync(studentExams);
            TempData["Success"] = "تم حفظ الدرجات بنجاح.";
            return RedirectToAction(nameof(Details), new { id = examId });
        }
    }
}
