using System.ComponentModel.DataAnnotations;

namespace DAL.Enums
{
    public enum Gender
    {
        [Display(Name = "ذكر")]
        Male = 1,

        [Display(Name = "أنثى")]
        Female = 2
    }
}
