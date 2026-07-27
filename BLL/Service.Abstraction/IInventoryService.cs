using DAL.Entities;
using DAL.Enums;

namespace BLL.Service.Abstraction
{
    public interface IInventoryService
    {
        /// <summary>
        /// Get all inventory items, optionally filtered by type, category, and/or linked course.
        /// </summary>
        Task<List<InventoryItem>> GetAllAsync(InventoryItemType? type = null, InventoryCategory? category = null, int? courseId = null);

        /// <summary>
        /// Get a single inventory item by id.
        /// </summary>
        Task<InventoryItem?> GetByIdAsync(int id);

        /// <summary>
        /// Create a new inventory item, stamping CreatedByUserId/CreatedAt.
        /// </summary>
        Task<InventoryItem> CreateAsync(InventoryItem item, string? userId);

        /// <summary>
        /// Update an existing inventory item's editable fields, preserving audit fields.
        /// Throws InvalidOperationException if no item with the given Id exists.
        /// </summary>
        Task<InventoryItem> UpdateAsync(InventoryItem item);

        /// <summary>
        /// Permanently delete an inventory item. No-ops if the id doesn't exist.
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Get all items where Quantity &lt;= MinimumQuantity.
        /// </summary>
        Task<List<InventoryItem>> GetLowStockAsync();
    }
}
