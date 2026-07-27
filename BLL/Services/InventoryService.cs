using BLL.Helpers;
using BLL.Service.Abstraction;
using DAL;
using DAL.Entities;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _context;

        public InventoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryItem>> GetAllAsync(InventoryItemType? type = null, InventoryCategory? category = null, int? courseId = null)
        {
            var query = _context.InventoryItems.AsQueryable();

            if (type.HasValue)
                query = query.Where(i => i.ItemType == type.Value);

            if (category.HasValue)
                query = query.Where(i => i.Category == category.Value);

            if (courseId.HasValue)
                query = query.Where(i => i.CourseId == courseId.Value);

            return await query
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<InventoryItem?> GetByIdAsync(int id)
        {
            return await _context.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<InventoryItem> CreateAsync(InventoryItem item, string? userId)
        {
            item.CreatedByUserId = userId;
            item.CreatedAt = EgyptTime.Now;

            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<InventoryItem> UpdateAsync(InventoryItem item)
        {
            var existing = await _context.InventoryItems.FirstOrDefaultAsync(i => i.Id == item.Id);
            if (existing == null)
                throw new InvalidOperationException($"Inventory item with id {item.Id} was not found.");

            existing.Name = item.Name;
            existing.ItemType = item.ItemType;
            existing.Category = item.Category;
            existing.Quantity = item.Quantity;
            existing.MinimumQuantity = item.MinimumQuantity;
            existing.Condition = item.Condition;
            existing.Location = item.Location;
            existing.CourseId = item.CourseId;
            existing.Notes = item.Notes;
            // CreatedByUserId / CreatedAt are never touched on edit — preserves original accountability

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _context.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);
            if (existing == null)
                return;

            _context.InventoryItems.Remove(existing);
            await _context.SaveChangesAsync();
        }

        public async Task<List<InventoryItem>> GetLowStockAsync()
        {
            return await _context.InventoryItems
                .Where(i => i.Quantity <= i.MinimumQuantity)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }
    }
}
