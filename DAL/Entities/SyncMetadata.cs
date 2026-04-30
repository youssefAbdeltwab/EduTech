using System.ComponentModel.DataAnnotations;

namespace DAL.Entities
{
    public class SyncMetadata
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "معرف الجدول")]
        public string SpreadsheetId { get; set; } = string.Empty;

        [Display(Name = "آخر صف معالج")]
        public int LastProcessedRow { get; set; }

        [Display(Name = "آخر مزامنة")]
        public DateTime LastSyncDate { get; set; }
    }
}
