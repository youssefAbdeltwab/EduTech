using BLL.Service.Abstraction;
using DAL;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Payment>> GetByMonthAsync(int month, int year)
        {
            return await _context.Payments
                .Include(p => p.Student)
                .Where(p => p.ForMonth == month && p.ForYear == year)
                .ToListAsync();
        }

        public async Task<bool> CreatePaymentAsync(int studentId, int month, int year)
        {
            var existing = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.StudentId == studentId &&
                    p.ForMonth == month &&
                    p.ForYear == year);

            if (existing != null)
            {
                // Already paid — do nothing
                return false;
            }

            _context.Payments.Add(new Payment
            {
                StudentId = studentId,
                ForMonth = month,
                ForYear = year,
                PaymentDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Payment>> GetStudentPaymentsAsync(int studentId)
        {
            return await _context.Payments
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.ForYear)
                .ThenByDescending(p => p.ForMonth)
                .ToListAsync();
        }
    }
}
