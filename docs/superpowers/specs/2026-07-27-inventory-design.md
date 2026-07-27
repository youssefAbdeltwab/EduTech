# Inventory Feature — Design Spec

**Date:** 2026-07-27
**Status:** Approved for planning

## Purpose

Track the school's physical inventory — both consumable supplies (e.g. paper, markers, cleaning
supplies) and durable assets (e.g. projectors, furniture) — in a single feature. This is a
single-institution app (no multi-school/branch concept), so inventory belongs to the school as a
whole, with an optional link to a specific `Course` when relevant.

This feature is independent of the existing `Expense` feature for this first version — no
automatic linking between an inventory purchase and an expense record.

## Architecture

Follows the same layered pattern as the recently-added `Expense` feature:

```
DAL   -> Entity + Enums + EF Migration
BLL   -> Service.Abstraction interface + Services implementation
EduTech -> Controller + Razor Views
EduTech.Tests -> Controller + Service tests, following existing Expense test structure
```

No new external dependencies or infrastructure are introduced.

## Data Model

### Enums

`DAL/Enums/InventoryItemType.cs`
```csharp
public enum InventoryItemType { Consumable, Asset }
```

`DAL/Enums/InventoryCategory.cs` (with `[Display(Name = "...")]` Arabic labels, same pattern as
`ExpenseCategory`, via `EnumDisplayExtensions`)
```csharp
public enum InventoryCategory
{
    Stationery, Cleaning, Electronics, Furniture, LabEquipment, Sports, Books, Other
}
```

`DAL/Enums/AssetCondition.cs`
```csharp
public enum AssetCondition { Good, NeedsRepair, Damaged, Retired }
```

### Entity — `DAL/Entities/InventoryItem.cs`

| Field | Type | Notes |
|---|---|---|
| `Id` | `int` | PK |
| `Name` | `string`, required, max 100 | e.g. "Whiteboard Markers" |
| `ItemType` | `InventoryItemType`, required | Consumable or Asset |
| `Category` | `InventoryCategory`, required | |
| `Quantity` | `int`, required, ≥ 0 | Current count. Meaningful for both types (e.g. 5 chairs is a valid asset quantity) |
| `MinimumQuantity` | `int`, required, ≥ 0, default 0 | Low-stock threshold. Most meaningful for Consumables but available to both |
| `Condition` | `AssetCondition?`, nullable | Only meaningful/shown when `ItemType == Asset` |
| `Location` | `string?`, max 100 | e.g. "Room 3", "Storage Closet" — optional |
| `CourseId` | `int?`, nullable FK → `Course` | Optional link. `OnDelete: SetNull` — deleting a course must not be blocked by inventory records |
| `Notes` | `string?`, max 200 | Optional free text |
| `CreatedByUserId` | `string?`, max 450 | Audit field, same convention as `Expense` |
| `CreatedAt` | `DateTime` | Audit field, stamped via `EgyptTime.Now` |

All validation messages in Arabic, matching existing convention.

## Service Layer

`BLL/Service.Abstraction/IInventoryService.cs`
```csharp
Task<List<InventoryItem>> GetAllAsync(InventoryItemType? type = null, InventoryCategory? category = null, int? courseId = null);
Task<InventoryItem?> GetByIdAsync(int id);
Task<InventoryItem> CreateAsync(InventoryItem item, string? userId);
Task<InventoryItem> UpdateAsync(InventoryItem item);
Task DeleteAsync(int id);
Task<List<InventoryItem>> GetLowStockAsync(); // items where Quantity <= MinimumQuantity
```

`BLL/Services/InventoryService.cs` mirrors `ExpenseService`: direct `_context.InventoryItems`
queries (no repository layer), `CreateAsync` stamps `CreatedByUserId`/`CreatedAt`, `UpdateAsync`
copies mutable fields while preserving audit fields, `DeleteAsync` performs a hard delete.

## Controller & Authorization

`EduTech/Controllers/InventoryController.cs` — class-level `[Authorize]` only. Unlike `Expense`
(Admin-only), **any authenticated user (Admin or User role) has full CRUD access** — this was an
explicit choice for this feature.

Actions:
- `Index` — filters by type/category/course; shows a low-stock badge per row
- `Create` (GET/POST)
- `Edit` (GET/POST)
- `Delete` (GET view) / `DeleteConfirmed` (POST, `[ActionName("Delete")]`)

All mutating POST actions carry `[ValidateAntiForgeryToken]`.

No separate Dashboard view in this first version — Index with filters covers v1 scope. A
Dashboard can be added later once real usage data exists.

## Views (`EduTech/Views/Inventory/`)

`Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml` — Bootstrap 5 RTL cards/tables,
matching the existing `Expense` views' styling and conventions (`card`/`card-body`, `table
table-hover`, Bootstrap Icons, Arabic labels).

`Create`/`Edit` forms conditionally show the `Condition` field only when `ItemType == Asset`
(client-side JS toggle based on the type select).

### Low-stock indicator + WhatsApp alert button

On `Index.cshtml`, any row where `Quantity <= MinimumQuantity` gets:
- A red "متبقي منخفض" (low stock) badge
- A manual WhatsApp alert button, following the **existing, real pattern** already used in
  `Student/Index.cshtml` and `Payment/Index.cshtml` — a `wa.me` link that opens WhatsApp
  Web/app with a prefilled message (e.g. *"تنبيه: كمية [اسم الصنف] منخفضة (المتبقي: X)"*),
  targeting a fixed admin/procurement phone number stored in configuration
  (`appsettings.json`, e.g. `"InventoryAlertPhone": "20xxxxxxxxxx"`)

This is a **manual, click-to-send** button — clicking it opens WhatsApp for a human to review
and send. There is no automatic/server-triggered sending: no WhatsApp Business/Cloud API
integration exists anywhere in this codebase today (the only prior reference is an unimplemented
comment in `StudentService.cs`), and building that was explicitly deferred out of scope for this
feature to avoid requiring external account setup (Meta Business/Developer app, access tokens)
before shipping v1.

## Migration & Tests

- One EF Core migration, `AddInventoryItem`, creating the `InventoryItems` table
- `AppDbContext.OnModelCreating` config for the `CourseId` FK (`DeleteBehavior.SetNull`)
- Tests follow the `Expense` test structure using existing `DbContextFactory` /
  `ControllerTestHelpers`:
  - `InventoryControllerAuthorizationTests` — verifies `[Authorize]` (no role restriction),
    `[ValidateAntiForgeryToken]` on mutating POSTs
  - Controller CRUD flow tests
  - `InventoryServiceTests` — covers `GetAllAsync` filters, `CreateAsync`/`UpdateAsync` audit
    field handling, `GetLowStockAsync` threshold logic

## Out of scope for v1

- Automatic/API-based WhatsApp sending (deferred — requires external provider account setup)
- Dashboard/reporting view
- Stock movement transaction log (quantity is edited directly, no history)
- Per-unit serial number tracking for assets
- Automatic linking to Expense records
