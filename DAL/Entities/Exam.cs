using System.ComponentModel.DataAnnotations;

namespace DAL.Entities
{
    public class Exam
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الامتحان مطلوب")]
        [MaxLength(100, ErrorMessage = "اسم الامتحان لا يمكن أن يتجاوز 100 حرف")]
        [Display(Name = "اسم الامتحان")]
        public string ExamName { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الامتحان مطلوب")]
        [Display(Name = "تاريخ الامتحان")]
        [DataType(DataType.Date)]
        public DateTime ExamDate { get; set; }

        [Required(ErrorMessage = "الدرجة العظمى مطلوبة")]
        [Range(1, 1000, ErrorMessage = "الدرجة العظمى يجب أن تكون بين 1 و 1000")]
        [Display(Name = "الدرجة العظمى")]
        public int MaxScore { get; set; }

        // FK to Course
        [Required(ErrorMessage = "الدورة مطلوبة")]
        [Display(Name = "الدورة")]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        // Navigation
        public ICollection<StudentExam> StudentExams { get; set; } = new List<StudentExam>();
    }
}
