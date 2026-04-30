namespace BLL.Service.Abstraction
{
    public interface IAttendanceSyncManager
    {
        Task<SyncResult> ProcessPendingAttendanceAsync();
    }

    public class SyncResult
    {
        public int ProcessedRows { get; set; }
        public int ValidRecords { get; set; }
        public int SkippedRecords { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool Success { get; set; } = true;
    }
}
