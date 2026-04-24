using DAL.Entities;

namespace BLL.Service.Abstraction
{
    public interface IExamService
    {
        Task<List<Exam>> GetAllAsync();
        Task<List<Exam>> GetByCourseAsync(int courseId);
        Task<Exam?> GetByIdAsync(int id);
        Task<Exam?> GetByIdWithStudentsAsync(int id);
        Task CreateAsync(Exam exam);
        Task UpdateAsync(Exam exam);
        Task<bool> DeleteAsync(int id);
        Task UpdateScoreAsync(int studentExamId, decimal? score);
        Task SaveScoresAsync(List<StudentExam> studentExams);
        Task<List<StudentExam>> GetStudentExamsAsync(int studentId);

        /// <summary>
        /// Get students whose last 5 graded exams are all failed (score < 50% of MaxScore)
        /// </summary>
        Task<List<(Student Student, List<StudentExam> LastExams)>> GetFailingStudentsAsync();
    }
}
