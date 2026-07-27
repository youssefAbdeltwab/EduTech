using BLL.Services;
using DAL.Entities;
using DAL.Enums;
using EduTech.Tests.TestHelpers;
using Xunit;

namespace EduTech.Tests.Services
{
    public class InventoryServiceTests
    {
        // ---------- CreateAsync ----------

        [Fact]
        public async Task CreateAsync_PersistsItem_AndStampsCreatedByAndCreatedAt()
        {
            using var db = DbContextFactory.Create();
            var sut = new InventoryService(db);

            var item = new InventoryItem
            {
                Name = "Whiteboard Markers",
                ItemType = InventoryItemType.Consumable,
                Category = InventoryCategory.Stationery,
                Quantity = 20,
                MinimumQuantity = 5
            };

            var before = DateTime.UtcNow;
            var result = await sut.CreateAsync(item, "user-123");
            var after = DateTime.UtcNow;

            var saved = Assert.Single(db.InventoryItems);
            Assert.Equal(result.Id, saved.Id);
            Assert.Equal("user-123", saved.CreatedByUserId);
            Assert.True(saved.CreatedAt != default, "CreatedAt should be stamped, not left at default(DateTime)");
            Assert.InRange(saved.CreatedAt, before.AddHours(-4), after.AddHours(4));
        }

        [Fact]
        public async Task CreateAsync_AllowsNullUserId()
        {
            using var db = DbContextFactory.Create();
            var sut = new InventoryService(db);

            var item = new InventoryItem
            {
                Name = "Projector",
                ItemType = InventoryItemType.Asset,
                Category = InventoryCategory.Electronics,
                Quantity = 1,
                MinimumQuantity = 0
            };

            await sut.CreateAsync(item, null);

            var saved = Assert.Single(db.InventoryItems);
            Assert.Null(saved.CreatedByUserId);
        }

        // ---------- GetByIdAsync ----------

        [Fact]
        public async Task GetByIdAsync_ReturnsItem_WhenExists()
        {
            using var db = DbContextFactory.Create();
            db.InventoryItems.Add(new InventoryItem { Id = 1, Name = "Chairs", ItemType = InventoryItemType.Asset, Category = InventoryCategory.Furniture, Quantity = 30, MinimumQuantity = 0 });
            await db.SaveChangesAsync();
            var sut = new InventoryService(db);

            var result = await sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            using var db = DbContextFactory.Create();
            var sut = new InventoryService(db);

            var result = await sut.GetByIdAsync(999);

            Assert.Null(result);
        }

        // ---------- UpdateAsync ----------

        [Fact]
        public async Task UpdateAsync_Throws_WhenItemDoesNotExist()
        {
            using var db = DbContextFactory.Create();
            var sut = new InventoryService(db);

            var payload = new InventoryItem { Id = 12345, Name = "Ghost", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Other, Quantity = 1, MinimumQuantity = 0 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateAsync(payload));
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEditableFields_ButPreservesCreatedByUserIdAndCreatedAt()
        {
            using var db = DbContextFactory.Create();
            var originalCreatedAt = new DateTime(2026, 1, 1, 8, 0, 0);
            db.InventoryItems.Add(new InventoryItem
            {
                Id = 1,
                Name = "Original Name",
                ItemType = InventoryItemType.Consumable,
                Category = InventoryCategory.Stationery,
                Quantity = 10,
                MinimumQuantity = 2,
                Condition = null,
                Location = "Room 1",
                CourseId = null,
                Notes = "Original notes",
                CreatedByUserId = "original-user",
                CreatedAt = originalCreatedAt
            });
            await db.SaveChangesAsync();
            var sut = new InventoryService(db);

            var updatePayload = new InventoryItem
            {
                Id = 1,
                Name = "Updated Name",
                ItemType = InventoryItemType.Asset,
                Category = InventoryCategory.Electronics,
                Quantity = 3,
                MinimumQuantity = 1,
                Condition = AssetCondition.Good,
                Location = "Room 2",
                CourseId = null,
                Notes = "Updated notes",
                // Simulates a caller trying to overwrite audit fields via the payload —
                // UpdateAsync must ignore these.
                CreatedByUserId = "attacker-controlled",
                CreatedAt = DateTime.UtcNow
            };

            var result = await sut.UpdateAsync(updatePayload);

            Assert.Equal("Updated Name", result.Name);
            var updated = await db.InventoryItems.FindAsync(1);
            Assert.NotNull(updated);
            Assert.Equal("Updated Name", updated!.Name);
            Assert.Equal(InventoryItemType.Asset, updated.ItemType);
            Assert.Equal(InventoryCategory.Electronics, updated.Category);
            Assert.Equal(3, updated.Quantity);
            Assert.Equal(1, updated.MinimumQuantity);
            Assert.Equal(AssetCondition.Good, updated.Condition);
            Assert.Equal("Room 2", updated.Location);
            Assert.Equal("Updated notes", updated.Notes);
            Assert.Equal("original-user", updated.CreatedByUserId);
            Assert.Equal(originalCreatedAt, updated.CreatedAt);
        }

        // ---------- DeleteAsync ----------

        [Fact]
        public async Task DeleteAsync_HardDeletes_WhenExists()
        {
            using var db = DbContextFactory.Create();
            db.InventoryItems.Add(new InventoryItem { Id = 1, Name = "Markers", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 5, MinimumQuantity = 1 });
            await db.SaveChangesAsync();
            var sut = new InventoryService(db);

            await sut.DeleteAsync(1);

            Assert.Empty(db.InventoryItems);
        }

        [Fact]
        public async Task DeleteAsync_NoOps_WhenNotFound()
        {
            using var db = DbContextFactory.Create();
            var sut = new InventoryService(db);

            var exception = await Record.ExceptionAsync(() => sut.DeleteAsync(999));

            Assert.Null(exception);
        }

        [Fact]
        public async Task DeleteAsync_IsIdempotent_SecondDeleteDoesNotThrow()
        {
            using var db = DbContextFactory.Create();
            db.InventoryItems.Add(new InventoryItem { Id = 1, Name = "Markers", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 5, MinimumQuantity = 1 });
            await db.SaveChangesAsync();
            var sut = new InventoryService(db);

            await sut.DeleteAsync(1);
            var exception = await Record.ExceptionAsync(() => sut.DeleteAsync(1));

            Assert.Null(exception);
            Assert.Empty(db.InventoryItems);
        }

        // ---------- GetAllAsync ----------

        [Fact]
        public async Task GetAllAsync_NoFilters_ReturnsEverything()
        {
            using var db = DbContextFactory.Create();
            db.InventoryItems.AddRange(
                new InventoryItem { Id = 1, Name = "A", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 1, MinimumQuantity = 0 },
                new InventoryItem { Id = 2, Name = "B", ItemType = InventoryItemType.Asset, Category = InventoryCategory.Furniture, Quantity = 1, MinimumQuantity = 0 }
            );
            await db.SaveChangesAsync();
            var sut = new InventoryService(db);

            var result = await sut.GetAllAsync();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_FiltersByType()
        {
            using var db = DbContextFactory.Create();
            db.InventoryItems.AddRange(
                new InventoryItem { Id = 1, Name = "A", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 1, MinimumQuantity = 0 },
                new InventoryItem { Id = 2, Name = "B", ItemType = InventoryItemType.Asset, Category = InventoryCategory.Furniture, Quantity = 1, MinimumQuantity = 0 }
            );
            await db.SaveChangesAsync();
            var sut = new InventoryService(db);

            var result = await sut.GetAllAsync(type: InventoryItemType.Asset);

            var single = Assert.Single(result);
            Assert.Equal(2, single.Id);
        }

        [Fact]
        public async Task GetAllAsync_FiltersByCategory()
        {
            using var db = DbContextFactory.Create();
            db.InventoryItems.AddRange(
                new InventoryItem { Id = 1, Name = "A", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 1, MinimumQuantity = 0 },
                new InventoryItem { Id = 2, Name = "B", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Cleaning, Quantity = 1, MinimumQuantity = 0 }
            );
            await db.SaveChangesAsync();
            var sut = new InventoryService(db);

            var result = await sut.GetAllAsync(category: InventoryCategory.Cleaning);

            var single = Assert.Single(result);
            Assert.Equal(2, single.Id);
        }

        [Fact]
        public async Task GetAllAsync_FiltersByCourseId()
        {
            using var db = DbContextFactory.Create();
            db.InventoryItems.AddRange(
                new InventoryItem { Id = 1, Name = "A", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.LabEquipment, Quantity = 1, MinimumQuantity = 0, CourseId = 10 },
                new InventoryItem { Id = 2, Name = "B", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.LabEquipment, Quantity = 1, MinimumQuantity = 0, CourseId = 20 }
            );
            await db.SaveChangesAsync();
            var sut = new InventoryService(db);

            var result = await sut.GetAllAsync(courseId: 10);

            var single = Assert.Single(result);
            Assert.Equal(1, single.Id);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyList_WhenNoMatches()
        {
            using var db = DbContextFactory.Create();
            var sut = new InventoryService(db);

            var result = await sut.GetAllAsync(type: InventoryItemType.Asset);

            Assert.Empty(result);
        }

        // ---------- GetLowStockAsync ----------

        [Fact]
        public async Task GetLowStockAsync_ReturnsItems_WhereQuantityLessThanOrEqualMinimum()
        {
            using var db = DbContextFactory.Create();
            db.InventoryItems.AddRange(
                new InventoryItem { Id = 1, Name = "Low", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 2, MinimumQuantity = 5 },
                new InventoryItem { Id = 2, Name = "Exact", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 5, MinimumQuantity = 5 },
                new InventoryItem { Id = 3, Name = "Fine", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 50, MinimumQuantity = 5 }
            );
            await db.SaveChangesAsync();
            var sut = new InventoryService(db);

            var result = await sut.GetLowStockAsync();

            Assert.Equal(new[] { 1, 2 }, result.Select(i => i.Id).OrderBy(id => id));
        }

        [Fact]
        public async Task GetLowStockAsync_ReturnsEmpty_WhenNothingIsLow()
        {
            using var db = DbContextFactory.Create();
            db.InventoryItems.Add(new InventoryItem { Id = 1, Name = "Fine", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 50, MinimumQuantity = 5 });
            await db.SaveChangesAsync();
            var sut = new InventoryService(db);

            var result = await sut.GetLowStockAsync();

            Assert.Empty(result);
        }
    }
}
