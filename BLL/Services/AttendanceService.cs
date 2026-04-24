using BLL.Service.Abstraction;
using DAL;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;

        public AttendanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Attendance>> GetByCourseMonthAsync(int courseId, int month, int year)
        {
            // Get all students in this course
            var students = await _context.Students
                .Where(s => s.CourseId == courseId)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            // Get existing attendance records
            var existing = await _context.Attendances
                .Where(a => a.CourseId == courseId && a.Month == month && a.Year == year)
                .ToListAsync();

            var existingStudentIds = existing.Select(a => a.StudentId).ToHashSet();

            // Auto-create missing records
            foreach (var student in students)
            {
                if (!existingStudentIds.Contains(student.Id))
                {
                    var attendance = new Attendance
                    {
                        StudentId = student.Id,
                        CourseId = courseId,
                        Month = month,
                        Year = year
                    };
                    _context.Attendances.Add(attendance);
                    existing.Add(attendance);
                }
            }

            await _context.SaveChangesAsync();

            // Reload with student data
            return await _context.Attendances
                .Include(a => a.Student)
                .Where(a => a.CourseId == courseId && a.Month == month && a.Year == year)
                .OrderBy(a => a.Student!.FullName)
                .ToListAsync();
        }

        public async Task SaveAsync(List<Attendance> attendances)
        {
            foreach (var item in attendances)
            {
                var existing = await _context.Attendances.FindAsync(item.Id);
                if (existing != null)
                {
                    existing.Session1 = item.Session1;
                    existing.Session2 = item.Session2;
                    existing.Session3 = item.Session3;
                    existing.Session4 = item.Session4;
                    existing.Session5 = item.Session5;
                    existing.Session6 = item.Session6;
                    existing.Session7 = item.Session7;
                    existing.Session8 = item.Session8;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
