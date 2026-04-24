using System.ComponentModel.DataAnnotations;
using DAL.Enums;

namespace DAL.Entities
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "كود الطالب مطلوب")]
        [Display(Name = "كود الطالب")]
        [Range(1000, 9999, ErrorMessage = "كود الطالب يجب أن يكون 4 أرقام ")]
        public int StudentCode { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [MaxLength(100, ErrorMessage = "الاسم لا يمكن أن يتجاوز 100 حرف")]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 20 رقم")]
        [Display(Name = "رقم الهاتف")]
        [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
        public string? Phone { get; set; }

        [MaxLength(100, ErrorMessage = "البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف")]
        [Display(Name = "البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
        public string? Email { get; set; }

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(200, ErrorMessage = "العنوان لا يمكن أن يتجاوز 200 حرف")]
        [Display(Name = "العنوان")]
        public string? Address { get; set; }

        [Display(Name = "الجنس")]
        [Required(ErrorMessage = "الجنس مطلوب")]
        public Gender Gender { get; set; }

        // FK to Course
        [Required(ErrorMessage = "الدورة مطلوبة")]
        [Display(Name = "الدورة")]
        public int CourseId { get; set; }
        public Course? Course { get; set; }
    }
}
