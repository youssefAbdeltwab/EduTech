using System.ComponentModel.DataAnnotations;

namespace DAL.Enums
{
    public enum InventoryCategory
    {
        [Display(Name = "أدوات مكتبية")]
        Stationery = 1,

        [Display(Name = "نظافة")]
        Cleaning = 2,

        [Display(Name = "إلكترونيات")]
        Electronics = 3,

        [Display(Name = "أثاث")]
        Furniture = 4,

        [Display(Name = "معدات معمل")]
        LabEquipment = 5,

        [Display(Name = "رياضة")]
        Sports = 6,

        [Display(Name = "كتب")]
        Books = 7,

        [Display(Name = "أخرى")]
        Other = 8
    }
}
