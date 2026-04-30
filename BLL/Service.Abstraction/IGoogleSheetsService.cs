using BLL.Models;

namespace BLL.Service.Abstraction
{
    public interface IGoogleSheetsService
    {
        Task<IEnumerable<GoogleSheetRow>> GetNewRowsAsync(string spreadsheetId, int startRow);

        /// <summary>
        /// Deletes all data rows (keeps the header). Returns number of rows deleted.
        /// </summary>
        Task<int> ClearProcessedRowsAsync(string spreadsheetId, int lastRowToDelete);
    }
}
