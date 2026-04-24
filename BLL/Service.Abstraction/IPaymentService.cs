using DAL.Entities;

namespace BLL.Service.Abstraction
{
    public interface IPaymentService
    {
        /// <summary>
        /// Get all payments for a specific month/year
        /// </summary>
        Task<List<Payment>> GetByMonthAsync(int month, int year);

        /// <summary>
        /// Create a payment record (immutable, cannot be removed)
        /// </summary>
        Task<bool> CreatePaymentAsync(int studentId, int month, int year);

        /// <summary>
        /// Get all payments for a specific student (history)
        /// </summary>
        Task<List<Payment>> GetStudentPaymentsAsync(int studentId);
    }
}
