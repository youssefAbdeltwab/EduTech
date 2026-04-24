using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<StudentExam> StudentExams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique: one payment per student per month/year
            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.StudentId, p.ForMonth, p.ForYear })
                .IsUnique();

            // Unique: one record per student per exam
            modelBuilder.Entity<StudentExam>()
                .HasIndex(se => new { se.StudentId, se.ExamId })
                .IsUnique();
        }
    }
}
