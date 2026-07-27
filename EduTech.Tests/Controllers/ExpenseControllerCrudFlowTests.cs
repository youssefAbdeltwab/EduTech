using BLL.Services;
using DAL.Entities;
using DAL.Enums;
using EduTech.Controllers;
using EduTech.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Xunit;

namespace EduTech.Tests.Controllers
{
    /// <summary>
    /// Covers the remaining controller contract: happy-path views, 404s for missing/adversarial
    /// ids (IDOR-style probing of arbitrary ids), and delete idempotency/side effects.
    /// Uses the real ExpenseService against an InMemory AppDbContext (no repository layer exists
    /// to fake, and this keeps the test project's fixtures to a single reusable set).
    /// </summary>
    public class ExpenseControllerCrudFlowTests
    {
        private static ExpenseController BuildController(out DAL.AppDbContext db)
        {
            db = DbContextFactory.Create();
            var service = new ExpenseService(db);
            var controller = new ExpenseController(service);
            controller.SetUser("admin-user-1", "Admin");
            return controller;
        }

        // ---------- Index ----------

        [Fact]
        public async Task Index_NoParameters_DefaultsToCurrentMonthAndYear()
        {
            var controller = BuildController(out var db);
            db.Expenses.Add(new Expense { Id = 1, Amount = 100m, Category = ExpenseCategory.Rent, Date = DateTime.Today });
            await db.SaveChangesAsync();

            var result = await controller.Index(null, null, null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Expense>>(view.Model);
            Assert.Single(model);
            Assert.Equal(DateTime.Now.Month, view.ViewData["SelectedMonth"]);
            Assert.Equal(DateTime.Now.Year, view.ViewData["SelectedYear"]);
        }

        [Fact]
        public async Task Index_ComputesTotal_AsSumOfReturnedExpenses()
        {
            var controller = BuildController(out var db);
            db.Expenses.AddRange(
                new Expense { Id = 1, Amount = 100m, Category = ExpenseCategory.Rent, Date = new DateTime(2026, 5, 1) },
                new Expense { Id = 2, Amount = 50.25m, Category = ExpenseCategory.Rent, Date = new DateTime(2026, 5, 15) }
            );
            await db.SaveChangesAsync();

            var result = await controller.Index(5, 2026, null);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(150.25m, view.ViewData["Total"]);
        }

        [Fact]
        public async Task Index_NoMatches_ReturnsEmptyModel_AndZeroTotal()
        {
            var controller = BuildController(out _);

            var result = await controller.Index(1, 2000, null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Expense>>(view.Model);
            Assert.Empty(model);
            Assert.Equal(0m, view.ViewData["Total"]);
        }

        // ---------- Create GET ----------

        [Fact]
        public void Create_Get_ReturnsView_WithCategorySelectList()
        {
            var controller = BuildController(out _);

            var result = controller.Create();

            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<SelectList>(view.ViewData["Categories"]);
        }

        // ---------- Edit GET ----------

        [Fact]
        public async Task Edit_Get_ReturnsNotFound_ForNonExistentId()
        {
            var controller = BuildController(out _);

            var result = await controller.Edit(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ReturnsView_WithExpense_WhenFound()
        {
            var controller = BuildController(out var db);
            db.Expenses.Add(new Expense { Id = 1, Amount = 10m, Category = ExpenseCategory.Rent, Date = DateTime.Today });
            await db.SaveChangesAsync();

            var result = await controller.Edit(1);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Expense>(view.Model);
            Assert.Equal(1, model.Id);
        }

        // ---------- Delete GET ----------

        [Fact]
        public async Task Delete_Get_ReturnsNotFound_ForNonExistentId()
        {
            var controller = BuildController(out _);

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_ReturnsView_WithExpense_WhenFound()
        {
            var controller = BuildController(out var db);
            db.Expenses.Add(new Expense { Id = 1, Amount = 10m, Category = ExpenseCategory.Rent, Date = DateTime.Today });
            await db.SaveChangesAsync();

            var result = await controller.Delete(1);

            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<Expense>(view.Model);
        }

        // ---------- DeleteConfirmed (POST) ----------

        [Fact]
        public async Task DeleteConfirmed_RemovesExpense_AndRedirectsToIndex()
        {
            var controller = BuildController(out var db);
            db.Expenses.Add(new Expense { Id = 1, Amount = 10m, Category = ExpenseCategory.Rent, Date = DateTime.Today });
            await db.SaveChangesAsync();

            var result = await controller.DeleteConfirmed(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ExpenseController.Index), redirect.ActionName);
            Assert.Empty(db.Expenses);
        }

        [Fact]
        public async Task DeleteConfirmed_NonExistentId_StillRedirects_NoErrorSurfaced()
        {
            // Documents current behavior: DeleteConfirmed ignores the service's bool result and
            // always redirects, even for an id that was never there (e.g. double-submit / already
            // deleted by someone else) or an arbitrary/adversarial id. No 404 or error is surfaced.
            var controller = BuildController(out _);

            var result = await controller.DeleteConfirmed(999);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ExpenseController.Index), redirect.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_CalledTwice_IsIdempotent_SecondCallStillRedirectsWithoutError()
        {
            var controller = BuildController(out var db);
            db.Expenses.Add(new Expense { Id = 1, Amount = 10m, Category = ExpenseCategory.Rent, Date = DateTime.Today });
            await db.SaveChangesAsync();

            var first = await controller.DeleteConfirmed(1);
            var second = await controller.DeleteConfirmed(1);

            Assert.IsType<RedirectToActionResult>(first);
            Assert.IsType<RedirectToActionResult>(second);
            Assert.Empty(db.Expenses);
        }

        // ---------- Adversarial / edge inputs ----------

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public async Task Edit_Get_HandlesOutOfRangeIds_Gracefully_ReturnsNotFound(int id)
        {
            var controller = BuildController(out _);

            var result = await controller.Edit(id);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
