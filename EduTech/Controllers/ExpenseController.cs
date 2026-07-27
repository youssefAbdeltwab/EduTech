using System.Security.Claims;
using BLL.Service.Abstraction;
using DAL.Entities;
using DAL.Enums;
using DAL.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExpenseController : Controller
    {
        private readonly IExpenseService _expenseService;

        public ExpenseController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        private static SelectList CategorySelectList(ExpenseCategory? selected = null)
        {
            var items = Enum.GetValues<ExpenseCategory>()
                .Select(c => new { Value = (int)c, Text = c.GetDisplayName() });

            return new SelectList(items, "Value", "Text", selected.HasValue ? (int)selected.Value : null);
        }

        // GET: Expense?month=&year=&category=
        public async Task<IActionResult> Index(int? month, int? year, ExpenseCategory? category)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var expenses = await _expenseService.GetByMonthAsync(selectedMonth, selectedYear, category);

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedCategory = category;
            ViewBag.Categories = CategorySelectList(category);
            ViewBag.Total = expenses.Sum(e => e.Amount);

            return View(expenses);
        }

        // GET: Expense/Create
        public IActionResult Create()
        {
            ViewBag.Categories = CategorySelectList();
            return View();
        }

        // POST: Expense/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense)
        {
            if (expense.Date.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(Expense.Date), "لا يمكن أن يكون التاريخ في المستقبل");
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _expenseService.CreateAsync(expense, userId);
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = CategorySelectList(expense.Category);
            return View(expense);
        }

        // GET: Expense/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var expense = await _expenseService.GetByIdAsync(id);
            if (expense == null)
                return NotFound();

            ViewBag.Categories = CategorySelectList(expense.Category);
            return View(expense);
        }

        // POST: Expense/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Expense expense)
        {
            if (id != expense.Id)
                return NotFound();

            if (expense.Date.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(Expense.Date), "لا يمكن أن يكون التاريخ في المستقبل");
            }

            if (ModelState.IsValid)
            {
                var result = await _expenseService.UpdateAsync(expense);
                if (!result)
                    return NotFound();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = CategorySelectList(expense.Category);
            return View(expense);
        }

        // GET: Expense/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _expenseService.GetByIdAsync(id);
            if (expense == null)
                return NotFound();

            return View(expense);
        }

        // POST: Expense/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _expenseService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Expense/Dashboard?weekOffset=0&monthOffset=0&yearOffset=0
        public async Task<IActionResult> Dashboard(int weekOffset = 0, int monthOffset = 0, int yearOffset = 0)
        {
            var today = DateTime.Today;

            // ---- Week (Sat–Fri, the Egyptian work week) ----
            var weekStart = StartOfWeek(today).AddDays(weekOffset * 7);
            var weekEnd = weekStart.AddDays(7); // exclusive
            var weekExpenses = await _expenseService.GetByDateRangeAsync(weekStart, weekEnd);

            // ---- Month (calendar) ----
            var monthAnchor = new DateTime(today.Year, today.Month, 1).AddMonths(monthOffset);
            var monthEnd = monthAnchor.AddMonths(1); // exclusive
            var monthExpenses = await _expenseService.GetByDateRangeAsync(monthAnchor, monthEnd);

            // ---- Year (calendar, Jan–Dec) ----
            var yearAnchor = new DateTime(today.Year, 1, 1).AddYears(yearOffset);
            var yearEnd = yearAnchor.AddYears(1); // exclusive
            var yearExpenses = await _expenseService.GetByDateRangeAsync(yearAnchor, yearEnd);

            ViewBag.WeekOffset = weekOffset;
            ViewBag.MonthOffset = monthOffset;
            ViewBag.YearOffset = yearOffset;
            ViewBag.WeekStart = weekStart;
            ViewBag.WeekEnd = weekEnd.AddDays(-1); // inclusive display end (Friday)
            ViewBag.MonthAnchor = monthAnchor;
            ViewBag.YearAnchor = yearAnchor;

            ViewBag.WeekTotal = weekExpenses.Sum(e => e.Amount);
            ViewBag.MonthTotal = monthExpenses.Sum(e => e.Amount);
            ViewBag.YearTotal = yearExpenses.Sum(e => e.Amount);

            ViewBag.WeekBreakdown = Breakdown(weekExpenses);
            ViewBag.MonthBreakdown = Breakdown(monthExpenses);
            ViewBag.YearBreakdown = Breakdown(yearExpenses);

            return View();
        }

        /// <summary>
        /// Returns the Saturday that starts the work-week containing the given date.
        /// .NET DayOfWeek: Sunday=0 .. Saturday=6. Days elapsed since the most recent
        /// Saturday: Sat->0, Sun->1, Mon->2, ..., Fri->6, computed as (dow + 1) % 7.
        /// </summary>
        private static DateTime StartOfWeek(DateTime date)
        {
            int daysSinceSaturday = ((int)date.DayOfWeek + 1) % 7;
            return date.Date.AddDays(-daysSinceSaturday);
        }

        private static List<(ExpenseCategory Category, decimal Amount, double Percent)> Breakdown(List<Expense> expenses)
        {
            var total = expenses.Sum(e => e.Amount);
            return expenses
                .GroupBy(e => e.Category)
                .Select(g => (
                    Category: g.Key,
                    Amount: g.Sum(e => e.Amount),
                    Percent: total > 0 ? (double)(g.Sum(e => e.Amount) / total) * 100 : 0
                ))
                .OrderByDescending(x => x.Amount)
                .ToList();
        }
    }
}
