# EventEase EMS

ASP.NET Core MVC admin platform for venue, event, and booking management. The app is structured for Azure SQL integration and already includes:

- Venue, event, and booking entities with CRUD screens
- Conflict checks to block double-bookings on overlapping venue times
- Event-first workflow so events can exist before a venue is assigned
- Delete protection for venues and events tied to bookings
- Secure venue image upload validation
- Cookie-based admin sign-in for authorised staff
- In-memory fallback for local demo mode when no SQL connection string is set
- Azure SQL schema script in `Database/CreateSchema.sql`

## Current deployment

- Live site: `https://eventease-ems-10534346-f0f0gudcddcehedd.canadacentral-01.azurewebsites.net`
- Hosting: Azure App Service on the Free (`F1`) tier
- Web app name: `eventease-ems-10534346`
- Resource group: `DefaultResourceGroup-EUS`
- Runtime: .NET 8
- Source repository: `https://github.com/Taehillah/EventEase-EMS.git`

The root URL redirects to the admin sign-in page:

```text
/Account/Login?ReturnUrl=%2F
```

## Sign-in

- Email: `lecture@eventease.com`
- Password: `Admin@123`

## Run prerequisites

Install the .NET 8 SDK, then restore and run:

```bash
dotnet restore
dotnet run
```

When no SQL connection string is configured, the app runs with the built-in in-memory demo database.

## Azure SQL setup

1. Set the connection string in `appsettings.json` or with an environment variable:

```bash
ConnectionStrings__EventEaseDb="Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

2. Create the initial migration and apply it:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

3. For Azure App Service, store the connection string and admin credentials in application settings instead of committing secrets.

If you prefer direct SQL before using EF migrations, run `Database/CreateSchema.sql` against your Azure SQL database.

## Notes

- Azure SQL provides automated backups; this app is prepared to use that by targeting SQL Server through EF Core.
- Venue images are stored locally for now behind an abstraction. The app can switch to Azure Blob Storage when `VenueImages:StorageConnectionString` and `VenueImages:ContainerName` are configured.
