using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<StudentExam> StudentExams { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AttendanceSession> AttendanceSessions { get; set; }
        public DbSet<SyncMetadata> SyncMetadata { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }

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

            // Precision for decimal Score
            modelBuilder.Entity<StudentExam>()
                .Property(se => se.Score)
                .HasPrecision(18, 2);

            // Precision for decimal Amount
            modelBuilder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            // Unique: one attendance record per student per course per month/year
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.StudentId, a.CourseId, a.Month, a.Year })
                .IsUnique();

            // Disable cascade delete to avoid multiple cascade paths (SQL Server limitation)
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Course)
                .WithMany()
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentExam>()
                .HasOne(se => se.Student)
                .WithMany()
                .HasForeignKey(se => se.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentExam>()
                .HasOne(se => se.Exam)
                .WithMany(e => e.StudentExams)
                .HasForeignKey(se => se.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Exams)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Course)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique index on ExternalRowId to prevent duplicate sync entries
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => a.ExternalRowId)
                .IsUnique()
                .HasFilter("[ExternalRowId] IS NOT NULL");

            // AttendanceSession FK to Course with Restrict delete
            modelBuilder.Entity<AttendanceSession>()
                .HasOne(s => s.Course)
                .WithMany()
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional link to Course — deleting a course must not be blocked by inventory records,
            // so this is the first SetNull FK in the codebase (every other FK here is Restrict).
            modelBuilder.Entity<InventoryItem>()
                .HasOne(i => i.Course)
                .WithMany()
                .HasForeignKey(i => i.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
