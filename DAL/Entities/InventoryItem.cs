using System.ComponentModel.DataAnnotations;
using DAL.Enums;

namespace DAL.Entities
{
    public class InventoryItem
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الصنف مطلوب")]
        [MaxLength(100, ErrorMessage = "اسم الصنف لا يمكن أن يتجاوز 100 حرف")]
        [Display(Name = "اسم الصنف")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع الصنف مطلوب")]
        [Display(Name = "نوع الصنف")]
        public InventoryItemType ItemType { get; set; }

        [Required(ErrorMessage = "التصنيف مطلوب")]
        [Display(Name = "التصنيف")]
        public InventoryCategory Category { get; set; }

        [Required(ErrorMessage = "الكمية مطلوبة")]
        [Range(0, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون صفر أو أكثر")]
        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "الحد الأدنى للكمية مطلوب")]
        [Range(0, int.MaxValue, ErrorMessage = "الحد الأدنى يجب أن يكون صفر أو أكثر")]
        [Display(Name = "الحد الأدنى للكمية")]
        public int MinimumQuantity { get; set; } = 0;

        [Display(Name = "الحالة")]
        public AssetCondition? Condition { get; set; }

        [MaxLength(100, ErrorMessage = "الموقع لا يمكن أن يتجاوز 100 حرف")]
        [Display(Name = "الموقع")]
        public string? Location { get; set; }

        [Display(Name = "الدورة")]
        public int? CourseId { get; set; }

        public Course? Course { get; set; }

        [MaxLength(200, ErrorMessage = "الملاحظات لا يمكن أن تتجاوز 200 حرف")]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
