using BLL.Services;
using DAL.Entities;
using DAL.Enums;
using EduTech.Controllers;
using EduTech.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EduTech.Tests.Controllers
{
    /// <summary>
    /// Dashboard anchors week/month/year windows off DateTime.Today, which the controller does
    /// not take as an injectable dependency, so we can't pin exact calendar dates the way
    /// ExpenseControllerWeekMathTests does for StartOfWeek in isolation. These tests instead
    /// verify the *relative* composition rules (offset math, inclusive/exclusive boundaries,
    /// totals, breakdown) which must hold regardless of what day the suite runs on.
    /// </summary>
    public class ExpenseControllerDashboardTests
    {
        private static ExpenseController BuildController(out DAL.AppDbContext db)
        {
            db = DbContextFactory.Create();
            var service = new ExpenseService(db);
            var controller = new ExpenseController(service);
            controller.SetUser("admin-user-1", "Admin");
            return controller;
        }

        [Fact]
        public async Task Dashboard_DefaultOffsets_WeekEndIsSixDaysAfterWeekStart_DisplayInclusive()
        {
            var controller = BuildController(out _);

            var result = await controller.Dashboard();

            var view = Assert.IsType<ViewResult>(result);
            var weekStart = (DateTime)view.ViewData["WeekStart"]!;
            var weekEndInclusive = (DateTime)view.ViewData["WeekEnd"]!;

            Assert.Equal(6, (weekEndInclusive - weekStart).Days); // Sat..Fri inclusive span = 6 days
            Assert.Equal(DayOfWeek.Saturday, weekStart.DayOfWeek);
            Assert.Equal(DayOfWeek.Friday, weekEndInclusive.DayOfWeek);
        }

        [Fact]
        public async Task Dashboard_WeekOffset_ShiftsWeekStartBySevenDaysPerUnit()
        {
            // Two separate controller instances: a Controller's ViewData is a single dictionary
            // reused by every View() call on that instance, so reusing one controller for both
            // calls would make both ViewResults alias the same (final) dictionary.
            var baselineController = BuildController(out _);
            var shiftedController = BuildController(out _);

            var baseline = await baselineController.Dashboard(weekOffset: 0);
            var shifted = await shiftedController.Dashboard(weekOffset: -1);

            var baselineStart = (DateTime)((ViewResult)baseline).ViewData["WeekStart"]!;
            var shiftedStart = (DateTime)((ViewResult)shifted).ViewData["WeekStart"]!;

            Assert.Equal(7, (baselineStart - shiftedStart).Days);
        }

        [Fact]
        public async Task Dashboard_MonthOffset_AnchorsToFirstOfMonth_AndShiftsByWholeMonths()
        {
            var controller = BuildController(out _);

            var result = await controller.Dashboard(monthOffset: -2);

            var view = Assert.IsType<ViewResult>(result);
            var monthAnchor = (DateTime)view.ViewData["MonthAnchor"]!;
            var expectedAnchor = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-2);

            Assert.Equal(1, monthAnchor.Day);
            Assert.Equal(expectedAnchor, monthAnchor);
        }

        [Fact]
        public async Task Dashboard_YearOffset_AnchorsToJanuaryFirst()
        {
            var controller = BuildController(out _);

            var result = await controller.Dashboard(yearOffset: -1);

            var view = Assert.IsType<ViewResult>(result);
            var yearAnchor = (DateTime)view.ViewData["YearAnchor"]!;

            Assert.Equal(new DateTime(DateTime.Today.Year - 1, 1, 1), yearAnchor);
        }

        [Fact]
        public async Task Dashboard_WeekTotal_OnlySumsExpensesInsideTheComputedWeek()
        {
            var controller = BuildController(out var db);
            var today = DateTime.Today;
            var daysSinceSaturday = ((int)today.DayOfWeek + 1) % 7;
            var weekStart = today.AddDays(-daysSinceSaturday);

            db.Expenses.AddRange(
                new Expense { Id = 1, Amount = 100m, Category = ExpenseCategory.Rent, Date = weekStart },               // inside (first day)
                new Expense { Id = 2, Amount = 50m, Category = ExpenseCategory.Rent, Date = weekStart.AddDays(6) },     // inside (last day, Friday)
                new Expense { Id = 3, Amount = 999m, Category = ExpenseCategory.Rent, Date = weekStart.AddDays(-1) },   // outside (previous Friday)
                new Expense { Id = 4, Amount = 999m, Category = ExpenseCategory.Rent, Date = weekStart.AddDays(7) }     // outside (next Saturday)
            );
            await db.SaveChangesAsync();

            var result = await controller.Dashboard();

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(150m, view.ViewData["WeekTotal"]);
        }

        [Fact]
        public async Task Dashboard_CategoryBreakdown_ComputesPercentagesThatSumToOneHundred()
        {
            var controller = BuildController(out var db);
            db.Expenses.AddRange(
                new Expense { Id = 1, Amount = 75m, Category = ExpenseCategory.Rent, Date = DateTime.Today },
                new Expense { Id = 2, Amount = 25m, Category = ExpenseCategory.Utilities, Date = DateTime.Today }
            );
            await db.SaveChangesAsync();

            var result = await controller.Dashboard();

            var view = Assert.IsType<ViewResult>(result);
            var breakdown = Assert.IsAssignableFrom<List<(ExpenseCategory Category, decimal Amount, double Percent)>>(view.ViewData["WeekBreakdown"]);

            Assert.Equal(2, breakdown.Count);
            var rent = breakdown.Single(b => b.Category == ExpenseCategory.Rent);
            var utilities = breakdown.Single(b => b.Category == ExpenseCategory.Utilities);
            Assert.Equal(75.0, rent.Percent, precision: 5);
            Assert.Equal(25.0, utilities.Percent, precision: 5);
        }

        [Fact]
        public async Task Dashboard_NoExpenses_BreakdownPercentIsZero_NoDivideByZeroException()
        {
            var controller = BuildController(out _);

            var result = await controller.Dashboard();

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(0m, view.ViewData["WeekTotal"]);
            var breakdown = Assert.IsAssignableFrom<List<(ExpenseCategory Category, decimal Amount, double Percent)>>(view.ViewData["WeekBreakdown"]);
            Assert.Empty(breakdown);
        }
    }
}
