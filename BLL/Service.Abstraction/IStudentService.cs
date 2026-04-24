using DAL.Entities;

namespace BLL.Service.Abstraction
{
    public interface IStudentService
    {
        Task<List<Student>> GetAllAsync();
        Task<Student?> SearchAsync(int StudentCode);
        Task<Student?> GetByIdAsync(int id);
        Task CreateAsync(Student student);
        Task UpdateAsync(Student student);
        Task DeleteAsync(int id);
    }
}