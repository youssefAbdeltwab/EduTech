using System.ComponentModel.DataAnnotations;

namespace DAL.Entities
{
    public class AttendanceSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "الدورة")]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        [Required]
        [MaxLength(10)]
        [Display(Name = "رمز الحضور")]
        public string SessionPIN { get; set; } = string.Empty;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;
    }
}
