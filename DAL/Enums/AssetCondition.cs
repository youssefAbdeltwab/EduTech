using System.ComponentModel.DataAnnotations;

namespace DAL.Enums
{
    public enum AssetCondition
    {
        [Display(Name = "جيد")]
        Good = 1,

        [Display(Name = "يحتاج صيانة")]
        NeedsRepair = 2,

        [Display(Name = "تالف")]
        Damaged = 3,

        [Display(Name = "خارج الخدمة")]
        Retired = 4
    }
}
