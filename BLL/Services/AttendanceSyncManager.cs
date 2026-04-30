using BLL.Service.Abstraction;
using DAL;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class AttendanceSyncManager : IAttendanceSyncManager
    {
        private readonly AppDbContext _context;
        private readonly IGoogleSheetsService _sheetsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AttendanceSyncManager> _logger;

        public AttendanceSyncManager(
            AppDbContext context,
            IGoogleSheetsService sheetsService,
            IConfiguration configuration,
            ILogger<AttendanceSyncManager> logger)
        {
            _context = context;
            _sheetsService = sheetsService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SyncResult> ProcessPendingAttendanceAsync()
        {
            var result = new SyncResult();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"] ?? "";

            if (string.IsNullOrEmpty(spreadsheetId))
            {
                result.Success = false;
                result.Errors.Add("SpreadsheetId is not configured in appsettings.");
                return result;
            }

            try
            {
                // Step A: Get last processed row
                var syncMeta = await _context.SyncMetadata
                    .FirstOrDefaultAsync(s => s.SpreadsheetId == spreadsheetId);

                int startRow = (syncMeta?.LastProcessedRow ?? 1) + 1; // +1 to skip header or last processed

                // Step B: Fetch new rows from Google Sheet
                var newRows = await _sheetsService.GetNewRowsAsync(spreadsheetId, startRow);
                var rowList = newRows.ToList();

                if (rowList.Count == 0)
                {
                    _logger.LogInformation("No new rows to process.");
                    return result;
                }

                result.ProcessedRows = rowList.Count;

                // Pre-load lookups for validation
                var studentCodes = rowList.Select(r => r.StudentCode).Distinct().ToList();
                var students = await _context.Students
                    .Where(s => studentCodes.Contains(s.StudentCode))
                    .ToDictionaryAsync(s => s.StudentCode);

                var activeSessions = await _context.AttendanceSessions
                    .ToListAsync();

                var existingExternalIds = (await _context.Attendances
                    .Where(a => a.ExternalRowId != null)
                    .Select(a => a.ExternalRowId!)
                    .ToListAsync())
                    .ToHashSet();

                var validAttendances = new List<Attendance>();

                foreach (var row in rowList)
                {
                    var externalRowId = $"{spreadsheetId}_R{row.RowNumber}";

                    // Skip duplicates
                    if (existingExternalIds.Contains(externalRowId))
                    {
                        result.SkippedRecords++;
                        continue;
                    }

                    // Step C: Validation

                    // C1: Student exists
                    if (!students.TryGetValue(row.StudentCode, out var student))
                    {
                        result.SkippedRecords++;
                        result.Errors.Add($"Row {row.RowNumber}: Student code {row.StudentCode} not found.");
                        continue;
                    }

                    // C2: SessionPIN matches any session (derive CourseId from the session)
                    var matchingSession = activeSessions.FirstOrDefault(s =>
                        s.SessionPIN == row.SessionPIN);

                    if (matchingSession == null)
                    {
                        result.SkippedRecords++;
                        result.Errors.Add($"Row {row.RowNumber}: Invalid PIN '{row.SessionPIN}'.");
                        continue;
                    }

                    var courseId = matchingSession.CourseId;

                    // C3: Timestamp within 45 minutes of session creation (prevents sharing link after class)
                    var timeDiff = (row.Timestamp - matchingSession.CreatedAt).Duration();
                    if (timeDiff > TimeSpan.FromMinutes(45))
                    {
                        result.SkippedRecords++;
                        result.Errors.Add($"Row {row.RowNumber}: Timestamp outside session window.");
                        continue;
                    }

                    // C4: Verify student belongs to this course
                    if (student.CourseId != courseId)
                    {
                        result.SkippedRecords++;
                        result.Errors.Add($"Row {row.RowNumber}: Student {row.StudentCode} not in course {courseId}.");
                        continue;
                    }

                    validAttendances.Add(new Attendance
                    {
                        StudentId = student.Id,
                        CourseId = courseId,
                        Month = row.Timestamp.Month,
                        Year = row.Timestamp.Year,
                        SyncTimestamp = DateTime.UtcNow,
                        ExternalRowId = externalRowId
                    });

                    existingExternalIds.Add(externalRowId);
                }

                // Step D & E: Save in a single transaction (upsert logic)
                if (validAttendances.Count > 0)
                {
                    var strategy = _context.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            foreach (var att in validAttendances)
                            {
                                // Find existing record for this student/course/month/year
                                var existing = await _context.Attendances.FirstOrDefaultAsync(a =>
                                    a.StudentId == att.StudentId &&
                                    a.CourseId == att.CourseId &&
                                    a.Month == att.Month &&
                                    a.Year == att.Year);

                                if (existing != null)
                                {
                                    // Mark next available session slot
                                    MarkNextSession(existing);
                                    existing.SyncTimestamp = att.SyncTimestamp;
                                    existing.ExternalRowId = att.ExternalRowId;
                                }
                                else
                                {
                                    // Create new record with Session1 = true
                                    att.Session1 = true;
                                    _context.Attendances.Add(att);
                                }
                            }

                            // Update sync metadata
                            var maxRow = rowList.Max(r => r.RowNumber);
                            if (syncMeta == null)
                            {
                                syncMeta = new SyncMetadata
                                {
                                    SpreadsheetId = spreadsheetId,
                                    LastProcessedRow = maxRow,
                                    LastSyncDate = DateTime.UtcNow
                                };
                                _context.SyncMetadata.Add(syncMeta);
                            }
                            else
                            {
                                syncMeta.LastProcessedRow = maxRow;
                                syncMeta.LastSyncDate = DateTime.UtcNow;
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            result.ValidRecords = validAttendances.Count;
                            _logger.LogInformation("Sync complete: {Valid} valid, {Skipped} skipped out of {Total} rows.",
                                validAttendances.Count, result.SkippedRecords, result.ProcessedRows);
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "Transaction failed during attendance sync.");
                            result.Success = false;
                            var innerMsg = ex.InnerException?.Message ?? ex.Message;
                            result.Errors.Add($"Database error: {innerMsg}");
                        }
                    });
                }
                else
                {
                    // Still update the last processed row even if all rows were invalid
                    var maxRow = rowList.Max(r => r.RowNumber);
                    if (syncMeta == null)
                    {
                        syncMeta = new SyncMetadata
                        {
                            SpreadsheetId = spreadsheetId,
                            LastProcessedRow = maxRow,
                            LastSyncDate = DateTime.UtcNow
                        };
                        _context.SyncMetadata.Add(syncMeta);
                    }
                    else
                    {
                        syncMeta.LastProcessedRow = maxRow;
                        syncMeta.LastSyncDate = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }

                // Step F: Clear processed rows from the sheet so old data isn't re-fetched.
                // Reset LastProcessedRow to 1 since rows are physically removed.
                if (result.Success && rowList.Count > 0)
                {
                    try
                    {
                        var maxRowToDelete = rowList.Max(r => r.RowNumber);
                        await _sheetsService.ClearProcessedRowsAsync(spreadsheetId, maxRowToDelete);

                        var meta = await _context.SyncMetadata
                            .FirstOrDefaultAsync(s => s.SpreadsheetId == spreadsheetId);
                        if (meta != null)
                        {
                            meta.LastProcessedRow = 1;
                            meta.LastSyncDate = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Sync succeeded but failed to clear sheet rows.");
                        result.Errors.Add($"Warning: rows saved but sheet not cleared - {ex.Message}");
                    }
                }
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Google API error during sync.");
                result.Success = false;
                result.Errors.Add($"Google API error: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during sync.");
                result.Success = false;
                result.Errors.Add($"Network error: {ex.Message}");
            }

            return result;
        }

        private void MarkNextSession(Attendance attendance)
        {
            if (!attendance.Session1) { attendance.Session1 = true; return; }
            if (!attendance.Session2) { attendance.Session2 = true; return; }
            if (!attendance.Session3) { attendance.Session3 = true; return; }
            if (!attendance.Session4) { attendance.Session4 = true; return; }
            if (!attendance.Session5) { attendance.Session5 = true; return; }
            if (!attendance.Session6) { attendance.Session6 = true; return; }
            if (!attendance.Session7) { attendance.Session7 = true; return; }
            if (!attendance.Session8) { attendance.Session8 = true; return; }
            // All 8 sessions already marked — no-op
        }
    }
}
