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
    /// Covers the future-date rejection logic duplicated in both Create(POST) and Edit(POST):
    ///   if (expense.Date.Date > DateTime.Today) ModelState.AddModelError(...)
    /// This is hand-rolled business validation (not a DataAnnotation on the entity), so it only
    /// fires when the controller action actually runs -- worth testing directly per action.
    /// </summary>
    public class ExpenseControllerDateValidationTests
    {
        private static ExpenseController BuildController(out BLL.Services.ExpenseService service, out DAL.AppDbContext db)
        {
            db = DbContextFactory.Create();
            service = new ExpenseService(db);
            var controller = new ExpenseController(service);
            controller.SetUser("admin-user-1", "Admin");
            return controller;
        }

        // ---------- Create POST ----------

        [Fact]
        public async Task Create_Post_WithFutureDate_AddsModelStateError_AndReturnsViewWithModel_DoesNotPersist()
        {
            var controller = BuildController(out _, out var db);
            var expense = new Expense
            {
                Amount = 100m,
                Category = ExpenseCategory.Rent,
                Date = DateTime.Today.AddDays(1),
                Description = "Future expense"
            };

            var result = await controller.Create(expense);

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(Expense.Date)));
            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(expense, view.Model);
            Assert.Empty(db.Expenses); // rejected before reaching the service
        }

        [Fact]
        public async Task Create_Post_WithTodaysDate_Succeeds_AndRedirectsToIndex()
        {
            var controller = BuildController(out _, out var db);
            var expense = new Expense
            {
                Amount = 100m,
                Category = ExpenseCategory.Rent,
                Date = DateTime.Today,
                Description = "Today expense"
            };

            var result = await controller.Create(expense);

            Assert.True(controller.ModelState.IsValid);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ExpenseController.Index), redirect.ActionName);
            Assert.Single(db.Expenses);
        }

        [Fact]
        public async Task Create_Post_WithPastDate_Succeeds()
        {
            var controller = BuildController(out _, out var db);
            var expense = new Expense
            {
                Amount = 50m,
                Category = ExpenseCategory.Maintenance,
                Date = DateTime.Today.AddYears(-1)
            };

            var result = await controller.Create(expense);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Single(db.Expenses);
        }

        [Fact]
        public async Task Create_Post_StampsCreatedByUserId_FromAuthenticatedUser()
        {
            var controller = BuildController(out _, out var db);
            var expense = new Expense { Amount = 20m, Category = ExpenseCategory.Other, Date = DateTime.Today };

            await controller.Create(expense);

            var saved = Assert.Single(db.Expenses);
            Assert.Equal("admin-user-1", saved.CreatedByUserId);
        }

        // ---------- Edit POST ----------

        [Fact]
        public async Task Edit_Post_WithFutureDate_AddsModelStateError_AndReturnsViewWithModel()
        {
            var controller = BuildController(out var service, out var db);
            db.Expenses.Add(new Expense { Id = 1, Amount = 10m, Category = ExpenseCategory.Rent, Date = DateTime.Today, CreatedByUserId = "orig" });
            await db.SaveChangesAsync();

            var payload = new Expense { Id = 1, Amount = 999m, Category = ExpenseCategory.Rent, Date = DateTime.Today.AddDays(2) };

            var result = await controller.Edit(1, payload);

            Assert.False(controller.ModelState.IsValid);
            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(payload, view.Model);
            // Original record must be untouched since the update was rejected before hitting the service.
            var stillOriginal = await service.GetByIdAsync(1);
            Assert.Equal(10m, stillOriginal!.Amount);
        }

        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsNotFound_BeforeAnyDateOrModelCheck()
        {
            var controller = BuildController(out _, out _);
            var payload = new Expense { Id = 2, Amount = 10m, Category = ExpenseCategory.Rent, Date = DateTime.Today };

            var result = await controller.Edit(1, payload);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidDate_ForNonExistentExpense_ReturnsNotFound()
        {
            var controller = BuildController(out _, out _);
            var payload = new Expense { Id = 42, Amount = 10m, Category = ExpenseCategory.Rent, Date = DateTime.Today };

            var result = await controller.Edit(42, payload);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidDate_ExistingExpense_UpdatesAndRedirects_PreservingAuditFields()
        {
            var controller = BuildController(out var service, out var db);
            var createdAt = new DateTime(2026, 1, 1);
            db.Expenses.Add(new Expense
            {
                Id = 1,
                Amount = 10m,
                Category = ExpenseCategory.Rent,
                Date = new DateTime(2026, 1, 1),
                CreatedByUserId = "original-creator",
                CreatedAt = createdAt
            });
            await db.SaveChangesAsync();

            var payload = new Expense { Id = 1, Amount = 500m, Category = ExpenseCategory.Marketing, Date = DateTime.Today, Description = "changed" };

            var result = await controller.Edit(1, payload);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ExpenseController.Index), redirect.ActionName);

            var updated = await service.GetByIdAsync(1);
            Assert.Equal(500m, updated!.Amount);
            Assert.Equal(ExpenseCategory.Marketing, updated.Category);
            Assert.Equal("original-creator", updated.CreatedByUserId); // preserved, not overwritten by the acting admin
            Assert.Equal(createdAt, updated.CreatedAt);                // preserved
        }
    }
}
