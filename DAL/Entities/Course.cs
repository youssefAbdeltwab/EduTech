using System.ComponentModel.DataAnnotations;

namespace DAL.Entities
{
    public class Course
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الدورة مطلوب")]
        [MaxLength(100, ErrorMessage = "اسم الدورة لا يمكن أن يتجاوز 100 حرف")]
        [Display(Name = "اسم الدورة")]
        public string CourseName { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "الوصف لا يمكن أن يتجاوز 500 حرف")]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        // Navigation
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}
