using BLL.Services;
using DAL.Entities;
using DAL.Enums;
using EduTech.Tests.TestHelpers;
using Xunit;

namespace EduTech.Tests.Services
{
    public class ExpenseServiceTests
    {
        // ---------- CreateAsync ----------

        [Fact]
        public async Task CreateAsync_PersistsExpense_AndStampsCreatedByAndCreatedAt()
        {
            using var db = DbContextFactory.Create();
            var sut = new ExpenseService(db);

            var expense = new Expense
            {
                Amount = 150.50m,
                Category = ExpenseCategory.Rent,
                Date = DateTime.Today,
                Description = "July rent"
            };

            var before = DateTime.UtcNow;
            var result = await sut.CreateAsync(expense, "user-123");
            var after = DateTime.UtcNow;

            Assert.True(result);
            var saved = Assert.Single(db.Expenses);
            Assert.Equal("user-123", saved.CreatedByUserId);
            Assert.True(saved.CreatedAt != default, "CreatedAt should be stamped, not left at default(DateTime)");
            // CreatedAt uses EgyptTime.Now (UTC+2/+3) — assert it's within a generous window of "now"
            // rather than hardcoding an offset, since server/CI clocks and DST can vary.
            Assert.InRange(saved.CreatedAt, before.AddHours(-4), after.AddHours(4));
        }

        [Fact]
        public async Task CreateAsync_AllowsNullUserId()
        {
            using var db = DbContextFactory.Create();
            var sut = new ExpenseService(db);

            var expense = new Expense { Amount = 10m, Category = ExpenseCategory.Other, Date = DateTime.Today };

            var result = await sut.CreateAsync(expense, null);

            Assert.True(result);
            var saved = Assert.Single(db.Expenses);
            Assert.Null(saved.CreatedByUserId);
        }

        // ---------- GetByIdAsync ----------

        [Fact]
        public async Task GetByIdAsync_ReturnsExpense_WhenExists()
        {
            using var db = DbContextFactory.Create();
            db.Expenses.Add(new Expense { Id = 1, Amount = 5m, Category = ExpenseCategory.Supplies, Date = DateTime.Today });
            await db.SaveChangesAsync();
            var sut = new ExpenseService(db);

            var result = await sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            using var db = DbContextFactory.Create();
            var sut = new ExpenseService(db);

            var result = await sut.GetByIdAsync(999);

            Assert.Null(result);
        }

        // ---------- UpdateAsync ----------

        [Fact]
        public async Task UpdateAsync_ReturnsFalse_WhenExpenseDoesNotExist()
        {
            using var db = DbContextFactory.Create();
            var sut = new ExpenseService(db);

            var result = await sut.UpdateAsync(new Expense { Id = 12345, Amount = 1m, Category = ExpenseCategory.Other, Date = DateTime.Today });

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFields_ButPreservesCreatedByUserIdAndCreatedAt()
        {
            using var db = DbContextFactory.Create();
            var originalCreatedAt = new DateTime(2026, 1, 1, 8, 0, 0);
            db.Expenses.Add(new Expense
            {
                Id = 1,
                Amount = 100m,
                Category = ExpenseCategory.Rent,
                Date = new DateTime(2026, 1, 1),
                Description = "Original",
                CreatedByUserId = "original-user",
                CreatedAt = originalCreatedAt
            });
            await db.SaveChangesAsync();
            var sut = new ExpenseService(db);

            var updatePayload = new Expense
            {
                Id = 1,
                Amount = 250m,
                Category = ExpenseCategory.Utilities,
                Date = new DateTime(2026, 1, 15),
                Description = "Updated",
                // Simulates a malicious/careless caller trying to overwrite audit fields via the payload —
                // UpdateAsync must ignore these since it only copies the four editable fields.
                CreatedByUserId = "attacker-controlled",
                CreatedAt = DateTime.UtcNow
            };

            var result = await sut.UpdateAsync(updatePayload);

            Assert.True(result);
            var updated = await db.Expenses.FindAsync(1);
            Assert.NotNull(updated);
            Assert.Equal(250m, updated!.Amount);
            Assert.Equal(ExpenseCategory.Utilities, updated.Category);
            Assert.Equal(new DateTime(2026, 1, 15), updated.Date);
            Assert.Equal("Updated", updated.Description);
            // The two audit fields must be untouched by UpdateAsync.
            Assert.Equal("original-user", updated.CreatedByUserId);
            Assert.Equal(originalCreatedAt, updated.CreatedAt);
        }

        // ---------- DeleteAsync ----------

        [Fact]
        public async Task DeleteAsync_HardDeletes_AndReturnsTrue_WhenExists()
        {
            using var db = DbContextFactory.Create();
            db.Expenses.Add(new Expense { Id = 1, Amount = 5m, Category = ExpenseCategory.Other, Date = DateTime.Today });
            await db.SaveChangesAsync();
            var sut = new ExpenseService(db);

            var result = await sut.DeleteAsync(1);

            Assert.True(result);
            Assert.Empty(db.Expenses); // hard delete — no soft-delete/IsDeleted flag survives
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
        {
            using var db = DbContextFactory.Create();
            var sut = new ExpenseService(db);

            var result = await sut.DeleteAsync(999);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_IsIdempotent_SecondDeleteReturnsFalse()
        {
            using var db = DbContextFactory.Create();
            db.Expenses.Add(new Expense { Id = 1, Amount = 5m, Category = ExpenseCategory.Other, Date = DateTime.Today });
            await db.SaveChangesAsync();
            var sut = new ExpenseService(db);

            var first = await sut.DeleteAsync(1);
            var second = await sut.DeleteAsync(1);

            Assert.True(first);
            Assert.False(second);
        }

        // ---------- GetByMonthAsync ----------

        [Fact]
        public async Task GetByMonthAsync_FiltersByMonthAndYear_ExcludingOtherMonths()
        {
            using var db = DbContextFactory.Create();
            db.Expenses.AddRange(
                new Expense { Id = 1, Amount = 10m, Category = ExpenseCategory.Rent, Date = new DateTime(2026, 3, 5) },
                new Expense { Id = 2, Amount = 20m, Category = ExpenseCategory.Rent, Date = new DateTime(2026, 3, 20) },
                new Expense { Id = 3, Amount = 30m, Category = ExpenseCategory.Rent, Date = new DateTime(2026, 4, 1) },   // different month
                new Expense { Id = 4, Amount = 40m, Category = ExpenseCategory.Rent, Date = new DateTime(2025, 3, 5) }    // same month, different year
            );
            await db.SaveChangesAsync();
            var sut = new ExpenseService(db);

            var result = await sut.GetByMonthAsync(3, 2026, null);

            Assert.Equal(2, result.Count);
            Assert.All(result, e => Assert.Equal(3, e.Date.Month));
            Assert.All(result, e => Assert.Equal(2026, e.Date.Year));
        }

        [Fact]
        public async Task GetByMonthAsync_FiltersByCategory_WhenProvided()
        {
            using var db = DbContextFactory.Create();
            db.Expenses.AddRange(
                new Expense { Id = 1, Amount = 10m, Category = ExpenseCategory.Rent, Date = new DateTime(2026, 3, 5) },
                new Expense { Id = 2, Amount = 20m, Category = ExpenseCategory.Salaries, Date = new DateTime(2026, 3, 6) }
            );
            await db.SaveChangesAsync();
            var sut = new ExpenseService(db);

            var result = await sut.GetByMonthAsync(3, 2026, ExpenseCategory.Salaries);

            var single = Assert.Single(result);
            Assert.Equal(ExpenseCategory.Salaries, single.Category);
        }

        [Fact]
        public async Task GetByMonthAsync_ReturnsEmptyList_WhenNoMatches()
        {
            using var db = DbContextFactory.Create();
            var sut = new ExpenseService(db);

            var result = await sut.GetByMonthAsync(1, 2020, null);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByMonthAsync_OrdersByDateDescending_ThenIdDescending()
        {
            using var db = DbContextFactory.Create();
            db.Expenses.AddRange(
                new Expense { Id = 1, Amount = 1m, Category = ExpenseCategory.Other, Date = new DateTime(2026, 3, 1) },
                new Expense { Id = 2, Amount = 2m, Category = ExpenseCategory.Other, Date = new DateTime(2026, 3, 10) },
                new Expense { Id = 3, Amount = 3m, Category = ExpenseCategory.Other, Date = new DateTime(2026, 3, 10) } // same date as #2, higher id
            );
            await db.SaveChangesAsync();
            var sut = new ExpenseService(db);

            var result = await sut.GetByMonthAsync(3, 2026, null);

            Assert.Equal(new[] { 3, 2, 1 }, result.Select(e => e.Id));
        }

        // ---------- GetByDateRangeAsync ----------

        [Fact]
        public async Task GetByDateRangeAsync_StartIsInclusive_EndIsExclusive()
        {
            using var db = DbContextFactory.Create();
            var start = new DateTime(2026, 3, 1);
            var end = new DateTime(2026, 4, 1);
            db.Expenses.AddRange(
                new Expense { Id = 1, Amount = 1m, Category = ExpenseCategory.Other, Date = start },                  // == start -> included
                new Expense { Id = 2, Amount = 2m, Category = ExpenseCategory.Other, Date = start.AddDays(15) },      // inside range
                new Expense { Id = 3, Amount = 3m, Category = ExpenseCategory.Other, Date = end },                    // == end -> excluded
                new Expense { Id = 4, Amount = 4m, Category = ExpenseCategory.Other, Date = start.AddDays(-1) }       // just before start -> excluded
            );
            await db.SaveChangesAsync();
            var sut = new ExpenseService(db);

            var result = await sut.GetByDateRangeAsync(start, end);

            Assert.Equal(new[] { 1, 2 }, result.Select(e => e.Id).OrderBy(id => id));
        }

        [Fact]
        public async Task GetByDateRangeAsync_ReturnsEmpty_WhenNoExpensesInRange()
        {
            using var db = DbContextFactory.Create();
            db.Expenses.Add(new Expense { Id = 1, Amount = 1m, Category = ExpenseCategory.Other, Date = new DateTime(2020, 1, 1) });
            await db.SaveChangesAsync();
            var sut = new ExpenseService(db);

            var result = await sut.GetByDateRangeAsync(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

            Assert.Empty(result);
        }
    }
}
