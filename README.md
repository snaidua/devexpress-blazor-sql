# Stockroom

A responsive Blazor Server inventory starter built with DevExpress Blazor, EF Core, and SQL Server LocalDB.

## Requirements

- .NET 10 SDK
- SQL Server Express LocalDB on Windows
- A DevExpress Blazor license or trial for development

## Run locally

1. Restore and build the project:

   ```powershell
   dotnet restore
   dotnet build
   ```

2. Start the app:

   ```powershell
   dotnet run
   ```

The Development launch profile serves the app at `http://localhost:5294`. On first load, the app creates `StockroomDb` in `(localdb)\MSSQLLocalDB` and adds sample products when the database is empty. Change the `Inventory` connection string in `appsettings.json` to use a different SQL Server instance.

DevExpress packages are restored from NuGet. Configure your DevExpress license or trial using the official DevExpress instructions; do not commit a license key to this repository.

## Included

- Inventory dashboard with search, category filtering, stock indicators, and summary metrics
- Item and vendor master pages with DevExpress EditCell grids
- Reusable multiline input grid with row selection, per-column filtering, pagination, and CSV import/export
- Receipt and issue posting with stock validation, audit history, and EditCell-editable reference notes (posted quantities and items stay locked)
- Stock adjustment documents with counted quantities, calculated variance, and a master-detail history view
- Product create, edit, and delete flows with validation
- SQL Server LocalDB persistence through EF Core
- Responsive sidebar and topbar, global progress indicator, alerts, toast notifications, and reusable dialogs

The app uses `EnsureCreated` for a new local database and idempotently adds vendor, transaction, and stock adjustment tables to an existing Stockroom database. Use EF Core migrations for future schema changes.
