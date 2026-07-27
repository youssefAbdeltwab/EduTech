using BLL.Service.Abstraction;
using DAL.Entities;
using DAL.Enums;
using DAL.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace EduTech.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly ICourseService _courseService;

        public InventoryController(IInventoryService inventoryService, ICourseService courseService)
        {
            _inventoryService = inventoryService;
            _courseService = courseService;
        }

        private static SelectList TypeSelectList(InventoryItemType? selected = null)
        {
            var items = Enum.GetValues<InventoryItemType>()
                .Select(t => new { Value = (int)t, Text = t.GetDisplayName() });

            return new SelectList(items, "Value", "Text", selected.HasValue ? (int)selected.Value : null);
        }

        private static SelectList CategorySelectList(InventoryCategory? selected = null)
        {
            var items = Enum.GetValues<InventoryCategory>()
                .Select(c => new { Value = (int)c, Text = c.GetDisplayName() });

            return new SelectList(items, "Value", "Text", selected.HasValue ? (int)selected.Value : null);
        }

        private static SelectList ConditionSelectList(AssetCondition? selected = null)
        {
            var items = Enum.GetValues<AssetCondition>()
                .Select(c => new { Value = (int)c, Text = c.GetDisplayName() });

            return new SelectList(items, "Value", "Text", selected.HasValue ? (int)selected.Value : null);
        }

        // GET: Inventory?type=&category=&courseId=
        public async Task<IActionResult> Index(InventoryItemType? type, InventoryCategory? category, int? courseId)
        {
            var items = await _inventoryService.GetAllAsync(type, category, courseId);
            var lowStock = await _inventoryService.GetLowStockAsync();

            ViewBag.SelectedType = type;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedCourseId = courseId;
            ViewBag.Types = TypeSelectList(type);
            ViewBag.Categories = CategorySelectList(category);
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", courseId);
            ViewBag.LowStockIds = lowStock.Select(i => i.Id).ToHashSet();
            ViewBag.LowStockCount = lowStock.Count;

            return View(items);
        }

        // GET: Inventory/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Types = TypeSelectList();
            ViewBag.Categories = CategorySelectList();
            ViewBag.Conditions = ConditionSelectList();
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName");
            return View();
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryItem item)
        {
            if (item.ItemType != InventoryItemType.Asset)
                item.Condition = null;

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _inventoryService.CreateAsync(item, userId);
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Types = TypeSelectList(item.ItemType);
            ViewBag.Categories = CategorySelectList(item.Category);
            ViewBag.Conditions = ConditionSelectList(item.Condition);
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", item.CourseId);
            return View(item);
        }

        // GET: Inventory/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _inventoryService.GetByIdAsync(id);
            if (item == null)
                return NotFound();

            ViewBag.Types = TypeSelectList(item.ItemType);
            ViewBag.Categories = CategorySelectList(item.Category);
            ViewBag.Conditions = ConditionSelectList(item.Condition);
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", item.CourseId);
            return View(item);
        }

        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InventoryItem item)
        {
            if (id != item.Id)
                return NotFound();

            if (item.ItemType != InventoryItemType.Asset)
                item.Condition = null;

            if (ModelState.IsValid)
            {
                var existing = await _inventoryService.GetByIdAsync(id);
                if (existing == null)
                    return NotFound();

                await _inventoryService.UpdateAsync(item);
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Types = TypeSelectList(item.ItemType);
            ViewBag.Categories = CategorySelectList(item.Category);
            ViewBag.Conditions = ConditionSelectList(item.Condition);
            ViewBag.Courses = new SelectList(await _courseService.GetAllAsync(), "Id", "CourseName", item.CourseId);
            return View(item);
        }

        // GET: Inventory/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _inventoryService.GetByIdAsync(id);
            if (item == null)
                return NotFound();

            return View(item);
        }

        // POST: Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _inventoryService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
