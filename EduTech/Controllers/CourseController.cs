using BLL.Service.Abstraction;
using DAL.Entities;
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
    }
}
