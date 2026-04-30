using BLL.Models;
using BLL.Service.Abstraction;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace BLL.Services
{
    public class GoogleSheetsService : IGoogleSheetsService
    {
        private readonly string _credentialPath;
        private readonly ILogger<GoogleSheetsService> _logger;
        private SheetsService? _sheetsService;
        private readonly object _lock = new();
        private string? _cachedSheetName;
        private int? _cachedSheetId;

        public GoogleSheetsService(string credentialPath, ILogger<GoogleSheetsService> logger)
        {
            _credentialPath = credentialPath;
            _logger = logger;
        }

        private SheetsService GetSheetsService()
        {
            if (_sheetsService != null) return _sheetsService;

            lock (_lock)
            {
                if (_sheetsService != null) return _sheetsService;

                if (!File.Exists(_credentialPath))
                    throw new FileNotFoundException(
                        $"Google Sheets credential file not found at '{_credentialPath}'. " +
                        "Place the service account JSON file there or update GoogleSheets:CredentialPath in appsettings.json.",
                        _credentialPath);

                var credential = GoogleCredential.FromFile(_credentialPath)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);

                _sheetsService = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "EduTech Attendance Sync"
                });
            }

            return _sheetsService;
        }

        public async Task<IEnumerable<GoogleSheetRow>> GetNewRowsAsync(string spreadsheetId, int startRow)
        {
            var rows = new List<GoogleSheetRow>();

            try
            {
                // Resolve actual sheet name (Google Forms names the sheet after the form title)
                var sheetName = await GetFirstSheetNameAsync(spreadsheetId);
                var range = $"'{sheetName}'!A{startRow}:C";
                var request = GetSheetsService().Spreadsheets.Values.Get(spreadsheetId, range);
                var response = await request.ExecuteAsync();

                if (response.Values == null || response.Values.Count == 0)
                {
                    _logger.LogInformation("No new rows found starting from row {StartRow}", startRow);
                    return rows;
                }

                for (int i = 0; i < response.Values.Count; i++)
                {
                    var row = response.Values[i];
                    if (row.Count < 3) continue;

                    try
                    {
                        var rawTimestamp = row[0]?.ToString() ?? "";
                        var timestamp = ParseArabicTimestamp(rawTimestamp);

                        var sheetRow = new GoogleSheetRow
                        {
                            RowNumber = startRow + i,
                            Timestamp = timestamp,
                            StudentCode = int.Parse(row[1]?.ToString() ?? "0"),
                            SessionPIN = row[2]?.ToString()?.Trim() ?? ""
                        };
                        rows.Add(sheetRow);
                    }
                    catch (FormatException ex)
                    {
                        _logger.LogWarning("Skipping row {Row}: data parsing error - {Message}",
                            startRow + i, ex.Message);
                    }
                }
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Google Sheets API error while fetching rows from spreadsheet {SpreadsheetId}",
                    spreadsheetId);
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error connecting to Google Sheets API");
                throw;
            }

            _logger.LogInformation("Fetched {Count} valid rows from Google Sheet", rows.Count);
            return rows;
        }

        private DateTime ParseArabicTimestamp(string raw)
        {
            // Google Forms Arabic timestamps: "12:42:41 ص 2026/04/30" (ص=AM, م=PM)
            var normalized = raw.Replace("ص", "AM").Replace("م", "PM");

            // Try Arabic culture first, then invariant
            if (DateTime.TryParse(normalized, new CultureInfo("ar-EG"), DateTimeStyles.None, out var dt))
                return dt;

            if (DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt;

            // Fallback: manual parse "h:mm:ss AM yyyy/MM/dd"
            if (DateTime.TryParseExact(normalized, 
                new[] { "h:mm:ss tt yyyy/MM/dd", "hh:mm:ss tt yyyy/MM/dd", "H:mm:ss yyyy/MM/dd" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt;

            throw new FormatException($"Cannot parse timestamp: '{raw}'");
        }

        private async Task<string> GetFirstSheetNameAsync(string spreadsheetId)
        {
            if (_cachedSheetName != null) return _cachedSheetName;
            await LoadSheetMetadataAsync(spreadsheetId);
            return _cachedSheetName!;
        }

        private async Task<int> GetFirstSheetIdAsync(string spreadsheetId)
        {
            if (_cachedSheetId.HasValue) return _cachedSheetId.Value;
            await LoadSheetMetadataAsync(spreadsheetId);
            return _cachedSheetId!.Value;
        }

        private async Task LoadSheetMetadataAsync(string spreadsheetId)
        {
            var metaRequest = GetSheetsService().Spreadsheets.Get(spreadsheetId);
            metaRequest.Fields = "sheets.properties";
            var spreadsheet = await metaRequest.ExecuteAsync();

            var firstSheet = spreadsheet.Sheets?.FirstOrDefault()?.Properties;
            _cachedSheetName = firstSheet?.Title ?? "Sheet1";
            _cachedSheetId = firstSheet?.SheetId ?? 0;
            _logger.LogInformation("Using sheet '{SheetName}' (gid={SheetId})", _cachedSheetName, _cachedSheetId);
        }

        public async Task<int> ClearProcessedRowsAsync(string spreadsheetId, int lastRowToDelete)
        {
            // Keep header (row 1). Delete rows 2..lastRowToDelete.
            if (lastRowToDelete < 2) return 0;

            var sheetId = await GetFirstSheetIdAsync(spreadsheetId);

            var request = new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Google.Apis.Sheets.v4.Data.Request>
                {
                    new Google.Apis.Sheets.v4.Data.Request
                    {
                        DeleteDimension = new Google.Apis.Sheets.v4.Data.DeleteDimensionRequest
                        {
                            Range = new Google.Apis.Sheets.v4.Data.DimensionRange
                            {
                                SheetId = sheetId,
                                Dimension = "ROWS",
                                StartIndex = 1,            // 0-based: skip header (row 1)
                                EndIndex = lastRowToDelete // exclusive upper bound = lastRowToDelete (1-based)
                            }
                        }
                    }
                }
            };

            try
            {
                await GetSheetsService().Spreadsheets.BatchUpdate(request, spreadsheetId).ExecuteAsync();
                var deleted = lastRowToDelete - 1;
                _logger.LogInformation("Deleted {Count} processed rows from sheet.", deleted);
                return deleted;
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Failed to delete processed rows from Google Sheet.");
                throw;
            }
        }

        /// <summary>
        /// Returns raw sheet data (all columns) for debugging column structure.
        /// </summary>
        public async Task<IList<IList<object>>?> GetRawDataAsync(string spreadsheetId, int maxRows = 10)
        {
            var sheetName = await GetFirstSheetNameAsync(spreadsheetId);
            var range = $"'{sheetName}'!A1:Z{maxRows}";
            var request = GetSheetsService().Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();
            return response.Values;
        }
    }
}
