using DAL.Entities;

namespace BLL.Service.Abstraction
{
    public interface ICourseService
    {
        Task<List<Course>> GetAllAsync();
        Task<Course?> GetByIdAsync(int id);
        Task<Course?> GetByIdWithStudentsAsync(int id);
        Task CreateAsync(Course course);
        Task UpdateAsync(Course course);
        Task<bool> DeleteAsync(int id);
    }
}
