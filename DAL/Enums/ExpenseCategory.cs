using System.ComponentModel.DataAnnotations;

namespace DAL.Enums
{
    public enum ExpenseCategory
    {
        [Display(Name = "إيجار")]
        Rent = 1,

        [Display(Name = "رواتب")]
        Salaries = 2,

        [Display(Name = "مرافق")]
        Utilities = 3,

        [Display(Name = "صيانة")]
        Maintenance = 4,

        [Display(Name = "مستلزمات")]
        Supplies = 5,

        [Display(Name = "تسويق")]
        Marketing = 6,

        [Display(Name = "أخرى")]
        Other = 7
    }
}
