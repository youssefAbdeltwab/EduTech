using DAL.Entities;

namespace BLL.Service.Abstraction
{
    public interface IAttendanceService
    {
        /// <summary>
        /// Get attendance records for a course in a specific month/year.
        /// Returns one record per student (auto-creates if missing).
        /// </summary>
        Task<List<Attendance>> GetByCourseMonthAsync(int courseId, int month, int year);

        /// <summary>
        /// Save attendance records (bulk update).
        /// </summary>
        Task SaveAsync(List<Attendance> attendances);
    }
}
