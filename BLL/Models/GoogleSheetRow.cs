namespace BLL.Models
{
    public class GoogleSheetRow
    {
        public int RowNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public int StudentCode { get; set; }
        public string SessionPIN { get; set; } = string.Empty;
    }
}
