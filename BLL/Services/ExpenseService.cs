using BLL.Helpers;
using BLL.Service.Abstraction;
using DAL;
using DAL.Entities;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly AppDbContext _context;

        public ExpenseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Expense>> GetByMonthAsync(int month, int year, ExpenseCategory? category)
        {
            var query = _context.Expenses
                .Where(e => e.Date.Month == month && e.Date.Year == year);

            if (category.HasValue)
                query = query.Where(e => e.Category == category.Value);

            return await query
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .ToListAsync();
        }

        public async Task<Expense?> GetByIdAsync(int id)
        {
            return await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<bool> CreateAsync(Expense expense, string? userId)
        {
            expense.CreatedByUserId = userId;
            expense.CreatedAt = EgyptTime.Now;

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(Expense expense)
        {
            var existing = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == expense.Id);
            if (existing == null)
                return false;

            existing.Amount = expense.Amount;
            existing.Category = expense.Category;
            existing.Date = expense.Date;
            existing.Description = expense.Description;
            // CreatedByUserId / CreatedAt are never touched on edit — preserves original accountability

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id);
            if (existing == null)
                return false;

            _context.Expenses.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Expense>> GetByDateRangeAsync(DateTime startInclusive, DateTime endExclusive)
        {
            return await _context.Expenses
                .Where(e => e.Date >= startInclusive && e.Date < endExclusive)
                .ToListAsync();
        }
    }
}
