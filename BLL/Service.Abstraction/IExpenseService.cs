using DAL.Entities;
using DAL.Enums;

namespace BLL.Service.Abstraction
{
    public interface IExpenseService
    {
        /// <summary>
        /// Get expenses for a specific month/year, optionally filtered by category
        /// </summary>
        Task<List<Expense>> GetByMonthAsync(int month, int year, ExpenseCategory? category);

        /// <summary>
        /// Get a single expense by id
        /// </summary>
        Task<Expense?> GetByIdAsync(int id);

        /// <summary>
        /// Create a new expense record
        /// </summary>
        Task<bool> CreateAsync(Expense expense, string? userId);

        /// <summary>
        /// Update an existing expense record
        /// </summary>
        Task<bool> UpdateAsync(Expense expense);

        /// <summary>
        /// Permanently delete an expense record
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Get expenses within a date range [startInclusive, endExclusive) — backs the Dashboard's week/month/year views
        /// </summary>
        Task<List<Expense>> GetByDateRangeAsync(DateTime startInclusive, DateTime endExclusive);
    }
}
