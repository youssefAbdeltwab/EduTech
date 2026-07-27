using BLL.Services;
using DAL.Entities;
using DAL.Enums;
using EduTech.Controllers;
using EduTech.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EduTech.Tests.Controllers
{
    public class InventoryControllerCrudFlowTests
    {
        private static InventoryController BuildController(out DAL.AppDbContext db)
        {
            db = DbContextFactory.Create();
            var service = new InventoryService(db);
            var courseService = new CourseService(db);
            var controller = new InventoryController(service, courseService);
            controller.SetUser("user-1", "User");
            return controller;
        }

        // ---------- Index ----------

        [Fact]
        public async Task Index_NoFilters_ReturnsAllItems()
        {
            var controller = BuildController(out var db);
            db.InventoryItems.Add(new InventoryItem { Id = 1, Name = "Markers", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 10, MinimumQuantity = 2 });
            await db.SaveChangesAsync();

            var result = await controller.Index(null, null, null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<InventoryItem>>(view.Model);
            Assert.Single(model);
            Assert.IsType<HashSet<int>>(view.ViewData["LowStockIds"]);
            Assert.IsType<int>(view.ViewData["LowStockCount"]);
        }

        [Fact]
        public async Task Index_NoMatches_ReturnsEmptyModel()
        {
            var controller = BuildController(out _);

            var result = await controller.Index(InventoryItemType.Asset, null, null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<InventoryItem>>(view.Model);
            Assert.Empty(model);
        }

        // ---------- Create GET ----------

        [Fact]
        public async Task Create_Get_ReturnsView()
        {
            var controller = BuildController(out _);

            var result = await controller.Create();

            Assert.IsType<ViewResult>(result);
        }

        // ---------- Create POST ----------

        [Fact]
        public async Task Create_Post_ValidItem_RedirectsToIndex_AndPersists()
        {
            var controller = BuildController(out var db);
            var item = new InventoryItem { Name = "Chairs", ItemType = InventoryItemType.Asset, Category = InventoryCategory.Furniture, Quantity = 20, MinimumQuantity = 5 };

            var result = await controller.Create(item);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(InventoryController.Index), redirect.ActionName);
            Assert.Single(db.InventoryItems);
        }

        [Fact]
        public async Task Create_Post_ConsumableWithCondition_ClearsConditionBeforeSaving()
        {
            var controller = BuildController(out var db);
            var item = new InventoryItem { Name = "Notebooks", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 5, MinimumQuantity = 1, Condition = AssetCondition.Good };

            var result = await controller.Create(item);

            Assert.IsType<RedirectToActionResult>(result);
            var saved = db.InventoryItems.Single();
            Assert.Null(saved.Condition);
        }

        // ---------- Edit GET ----------

        [Fact]
        public async Task Edit_Get_ReturnsNotFound_ForNonExistentId()
        {
            var controller = BuildController(out _);

            var result = await controller.Edit(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ReturnsView_WithItem_WhenFound()
        {
            var controller = BuildController(out var db);
            db.InventoryItems.Add(new InventoryItem { Id = 1, Name = "Markers", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 10, MinimumQuantity = 2 });
            await db.SaveChangesAsync();

            var result = await controller.Edit(1);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<InventoryItem>(view.Model);
            Assert.Equal(1, model.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public async Task Edit_Get_HandlesOutOfRangeIds_Gracefully_ReturnsNotFound(int id)
        {
            var controller = BuildController(out _);

            var result = await controller.Edit(id);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------- Edit POST ----------

        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsNotFound()
        {
            var controller = BuildController(out _);
            var item = new InventoryItem { Id = 2, Name = "X", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Other, Quantity = 1, MinimumQuantity = 0 };

            var result = await controller.Edit(1, item);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_NonExistentId_ReturnsNotFound()
        {
            var controller = BuildController(out _);
            var item = new InventoryItem { Id = 999, Name = "X", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Other, Quantity = 1, MinimumQuantity = 0 };

            var result = await controller.Edit(999, item);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidItem_RedirectsToIndex_AndPersistsChanges()
        {
            var controller = BuildController(out var db);
            db.InventoryItems.Add(new InventoryItem { Id = 1, Name = "Markers", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 10, MinimumQuantity = 2 });
            await db.SaveChangesAsync();
            var updated = new InventoryItem { Id = 1, Name = "Whiteboard Markers", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 3, MinimumQuantity = 2 };

            var result = await controller.Edit(1, updated);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(InventoryController.Index), redirect.ActionName);
            var saved = await db.InventoryItems.FindAsync(1);
            Assert.Equal("Whiteboard Markers", saved!.Name);
            Assert.Equal(3, saved.Quantity);
        }

        [Fact]
        public async Task Edit_Post_ChangingAssetToConsumable_ClearsCondition()
        {
            var controller = BuildController(out var db);
            db.InventoryItems.Add(new InventoryItem { Id = 1, Name = "Projector", ItemType = InventoryItemType.Asset, Category = InventoryCategory.Electronics, Quantity = 1, MinimumQuantity = 0, Condition = AssetCondition.Good });
            await db.SaveChangesAsync();
            var updated = new InventoryItem { Id = 1, Name = "Projector", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Electronics, Quantity = 1, MinimumQuantity = 0, Condition = AssetCondition.Good };

            var result = await controller.Edit(1, updated);

            Assert.IsType<RedirectToActionResult>(result);
            var saved = await db.InventoryItems.FindAsync(1);
            Assert.Null(saved!.Condition);
        }

        // ---------- Delete GET ----------

        [Fact]
        public async Task Delete_Get_ReturnsNotFound_ForNonExistentId()
        {
            var controller = BuildController(out _);

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_ReturnsView_WithItem_WhenFound()
        {
            var controller = BuildController(out var db);
            db.InventoryItems.Add(new InventoryItem { Id = 1, Name = "Markers", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 10, MinimumQuantity = 2 });
            await db.SaveChangesAsync();

            var result = await controller.Delete(1);

            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<InventoryItem>(view.Model);
        }

        // ---------- DeleteConfirmed (POST) ----------

        [Fact]
        public async Task DeleteConfirmed_RemovesItem_AndRedirectsToIndex()
        {
            var controller = BuildController(out var db);
            db.InventoryItems.Add(new InventoryItem { Id = 1, Name = "Markers", ItemType = InventoryItemType.Consumable, Category = InventoryCategory.Stationery, Quantity = 10, MinimumQuantity = 2 });
            await db.SaveChangesAsync();

            var result = await controller.DeleteConfirmed(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(InventoryController.Index), redirect.ActionName);
            Assert.Empty(db.InventoryItems);
        }

        [Fact]
        public async Task DeleteConfirmed_NonExistentId_StillRedirects_NoErrorSurfaced()
        {
            var controller = BuildController(out _);

            var result = await controller.DeleteConfirmed(999);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(InventoryController.Index), redirect.ActionName);
        }
    }
}
