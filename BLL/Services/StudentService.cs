using BLL.Service.Abstraction;
using DAL;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class StudentService : IStudentService
    {
        private readonly AppDbContext _context;

        public StudentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Student>> GetAllAsync()
        {
            return await _context.Students.Include(s => s.Course).ToListAsync();
        }

        public async Task<Student?> SearchAsync(int StudentCode)
        {
            return await _context.Students.Include(s => s.Course)
              .FirstOrDefaultAsync(s => s.StudentCode == StudentCode);
        }

        public async Task<Student?> GetByIdAsync(int id)
        {
            return await _context.Students.Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task CreateAsync(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }
        }
    }

    /// <summary> create the globle exception handler 
    // make it as an html page to show the error message to the user in a friendly way instead of showing the default error page of asp.net core </summary>
    //
    // in the dashboard show the students whose last five exams are failed
    // make the payments are immutable and can't be updated or deleted once created

    // in the student details page show the payment history and the exam history of the student

    // now I want to add authentication and authorization to the application, using the built-in authentication and authorization features of ASP.NET Core, and create two roles: Admin and User.
    // - Admin can perform all operations (create, read, update, delete) on students, courses, payments, and exams.
    // - User can't access only the dashboard and can't perform delete operations on any entity, but can create and update students, courses, payments, and exams.



    // in the student index page add a feature to send a what'sapp message to the student with an alert message to pay his fees. 
    // depend on the phone number of the student and use the free version of the whatsApp API to send the message.
}
