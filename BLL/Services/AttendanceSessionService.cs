using BLL.Service.Abstraction;
using DAL;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class AttendanceSessionService : IAttendanceSessionService
    {
        private readonly AppDbContext _context;

        public AttendanceSessionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AttendanceSession> GenerateSessionAsync(int courseId)
        {
            // Deactivate any existing active session for this course first
            var existing = await _context.AttendanceSessions
                .Where(s => s.CourseId == courseId && s.IsActive)
                .ToListAsync();

            foreach (var s in existing)
                s.IsActive = false;

            // Generate a random 6-digit PIN
            var pin = Random.Shared.Next(100000, 999999).ToString();

            var session = new AttendanceSession
            {
                CourseId = courseId,
                SessionPIN = pin,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.AttendanceSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<AttendanceSession?> GetActiveSessionAsync(int courseId)
        {
            return await _context.AttendanceSessions
                .FirstOrDefaultAsync(s => s.CourseId == courseId && s.IsActive);
        }

        public async Task DeactivateSessionAsync(int sessionId)
        {
            var session = await _context.AttendanceSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }
    }
}
