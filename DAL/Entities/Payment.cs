using System.ComponentModel.DataAnnotations;

namespace DAL.Entities
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "الطالب")]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Required]
        [Range(1, 12, ErrorMessage = "الشهر يجب أن يكون بين 1 و 12")]
        [Display(Name = "الشهر")]
        public int ForMonth { get; set; }

        [Required]
        [Display(Name = "السنة")]
        public int ForYear { get; set; }

        [Display(Name = "تاريخ الدفع")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [MaxLength(200, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 200 حرف")]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
    }
}
