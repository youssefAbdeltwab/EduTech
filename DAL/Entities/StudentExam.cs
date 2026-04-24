using System.ComponentModel.DataAnnotations;

namespace DAL.Entities
{
    public class StudentExam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "الطالب")]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Required]
        [Display(Name = "الامتحان")]
        public int ExamId { get; set; }
        public Exam? Exam { get; set; }

        [Display(Name = "الدرجة")]
        [Range(0, 1000, ErrorMessage = "الدرجة يجب أن تكون بين 0 و 1000")]
        public decimal? Score { get; set; }
    }
}
