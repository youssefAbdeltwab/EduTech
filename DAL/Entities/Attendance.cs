using System.ComponentModel.DataAnnotations;

namespace DAL.Entities
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Required]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        [Required]
        [Display(Name = "الشهر")]
        public int Month { get; set; }

        [Required]
        [Display(Name = "السنة")]
        public int Year { get; set; }

        [Display(Name = "الحصة 1")]
        public bool Session1 { get; set; }

        [Display(Name = "الحصة 2")]
        public bool Session2 { get; set; }

        [Display(Name = "الحصة 3")]
        public bool Session3 { get; set; }

        [Display(Name = "الحصة 4")]
        public bool Session4 { get; set; }

        [Display(Name = "الحصة 5")]
        public bool Session5 { get; set; }

        [Display(Name = "الحصة 6")]
        public bool Session6 { get; set; }

        [Display(Name = "الحصة 7")]
        public bool Session7 { get; set; }

        [Display(Name = "الحصة 8")]
        public bool Session8 { get; set; }
    }
}
