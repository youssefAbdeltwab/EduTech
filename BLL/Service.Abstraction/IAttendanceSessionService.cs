using DAL.Entities;

namespace BLL.Service.Abstraction
{
    public interface IAttendanceSessionService
    {
        Task<AttendanceSession> GenerateSessionAsync(int courseId);
        Task<AttendanceSession?> GetActiveSessionAsync(int courseId);
        Task DeactivateSessionAsync(int sessionId);
    }
}
