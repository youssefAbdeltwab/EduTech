using BLL.Service.Abstraction;
using DAL;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class ExamService : IExamService
    {
        private readonly AppDbContext _context;

        public ExamService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Exam>> GetAllAsync()
        {
            return await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.StudentExams)
                .OrderByDescending(e => e.ExamDate)
                .ToListAsync();
        }

        public async Task<List<Exam>> GetByCourseAsync(int courseId)
        {
            return await _context.Exams
                .Include(e => e.StudentExams)
                .Where(e => e.CourseId == courseId)
                .OrderByDescending(e => e.ExamDate)
                .ToListAsync();
        }

        public async Task<Exam?> GetByIdAsync(int id)
        {
            return await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Exam?> GetByIdWithStudentsAsync(int id)
        {
            return await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.StudentExams)
                    .ThenInclude(se => se.Student)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task CreateAsync(Exam exam)
        {
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();

            // Auto-create StudentExam rows for all students in the course
            var courseStudents = await _context.Students
                .Where(s => s.CourseId == exam.CourseId)
                .ToListAsync();

            foreach (var student in courseStudents)
            {
                _context.StudentExams.Add(new StudentExam
                {
                    StudentId = student.Id,
                    ExamId = exam.Id,
                    Score = null
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Exam exam)
        {
            _context.Exams.Update(exam);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exam = await _context.Exams
                .Include(e => e.StudentExams)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null) return false;

            // Remove all student exam records first
            _context.StudentExams.RemoveRange(exam.StudentExams);
            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateScoreAsync(int studentExamId, decimal? score)
        {
            var se = await _context.StudentExams.FindAsync(studentExamId);
            if (se != null)
            {
                se.Score = score;
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveScoresAsync(List<StudentExam> studentExams)
        {
            foreach (var item in studentExams)
            {
                var se = await _context.StudentExams.FindAsync(item.Id);
                if (se != null)
                {
                    se.Score = item.Score;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<StudentExam>> GetStudentExamsAsync(int studentId)
        {
            return await _context.StudentExams
                .Include(se => se.Exam)
                .Where(se => se.StudentId == studentId)
                .OrderByDescending(se => se.Exam!.ExamDate)
                .ToListAsync();
        }

        public async Task<List<(Student Student, List<StudentExam> LastExams)>> GetFailingStudentsAsync()
        {
            var allGraded = await _context.StudentExams
                .Include(se => se.Student)
                    .ThenInclude(s => s.Course)
                .Include(se => se.Exam)
                .Where(se => se.Score != null)
                .ToListAsync();

            var result = new List<(Student, List<StudentExam>)>();

            var grouped = allGraded.GroupBy(se => se.StudentId);
            foreach (var group in grouped)
            {
                var last5 = group
                    .OrderByDescending(se => se.Exam!.ExamDate)
                    .ThenByDescending(se => se.Id)
                    .Take(5)
                    .ToList();

                if (last5.Count == 5 && last5.All(se => se.Score < (se.Exam!.MaxScore * 0.5m)))
                {
                    result.Add((last5.First().Student!, last5));
                }
            }

            return result;
        }
    }
}
