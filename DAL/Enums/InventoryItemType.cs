using System.ComponentModel.DataAnnotations;

namespace DAL.Enums
{
    public enum InventoryItemType
    {
        [Display(Name = "مستهلكات")]
        Consumable = 1,

        [Display(Name = "أصول")]
        Asset = 2
    }
}
