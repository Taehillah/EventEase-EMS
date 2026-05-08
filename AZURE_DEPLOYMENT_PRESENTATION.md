# EventEase EMS Azure Deployment Notes

## Current Deployment Summary

EventEase EMS is currently deployed as an ASP.NET Core MVC application on Azure App Service.

- Live site: `https://eventease-ems-10534346-f0f0gudcddcehedd.canadacentral-01.azurewebsites.net`
- Azure Web App: `eventease-ems-10534346`
- Resource group: `DefaultResourceGroup-EUS`
- Hosting plan: Azure App Service Free tier (`F1`)
- Region: Canada Central
- Runtime: .NET 8
- Source repository: `https://github.com/Taehillah/EventEase-EMS.git`

The deployed application redirects unauthenticated users to:

```text
/Account/Login?ReturnUrl=%2F
```

## Current Sign-In Details

- Email: `lecture@eventease.com`
- Password: `Admin@123`

These are demo/admin credentials configured through the app configuration. For a production deployment, store admin credentials in Azure App Service application settings rather than committing real secrets to source control.

## Application Stack

- ASP.NET Core MVC
- Target framework: `.NET 8`
- Entity Framework Core
- SQL Server provider for Azure SQL support
- In-memory provider for local demo mode when no SQL connection string is supplied
- Razor views for dashboard, venues, events, bookings, and account login

## Main Features

- Admin login for authorised staff
- Dashboard overview
- Venue CRUD
- Event CRUD
- Booking CRUD
- Double-booking prevention for active bookings on the same venue/date
- Deletion protection for venues and events linked to active bookings
- Venue image handling through a storage abstraction
- Azure SQL schema script in `Database/CreateSchema.sql`

## Deployment Model

The current public hosting path is Azure App Service, not the older VM-hosted URL.

Current public URL:

```text
https://eventease-ems-10534346-f0f0gudcddcehedd.canadacentral-01.azurewebsites.net
```

The app is hosted on the Free (`F1`) tier to keep costs as low as possible.

## Local Run Commands

```bash
dotnet restore
dotnet run
```

If no SQL connection string is present, the app uses its in-memory demo database.

## Azure App Service Deployment Commands

The project can be published locally and deployed to the existing Azure Web App:

```bash
dotnet publish "EventEase.EMS.csproj" -c Release -o ./publish-appservice
```

Then deploy the published output through Azure App Service tooling or Visual Studio.

The existing app can be checked with:

```bash
az webapp show \
  --resource-group DefaultResourceGroup-EUS \
  --name eventease-ems-10534346 \
  --query "{name:name,state:state,defaultHostName:defaultHostName}" \
  --output table
```

If it is stopped, start it with:

```bash
az webapp start \
  --resource-group DefaultResourceGroup-EUS \
  --name eventease-ems-10534346
```

## Azure SQL Configuration

The application is ready to use Azure SQL when this setting is provided:

```text
ConnectionStrings__EventEaseDb=Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Recommended Azure App Service application settings:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__EventEaseDb=<azure-sql-connection-string>
AdminUser__Email=<admin-email>
AdminUser__Password=<admin-password>
```

## Evidence to Include in Assessment

- Public web app URL
- GitHub repository URL
- Login page screenshot
- Dashboard screenshot
- Venue CRUD screenshots
- Event CRUD screenshots
- Booking CRUD screenshots
- Azure App Service overview showing the app is running
- Azure SQL database/table evidence if the deployed app is connected to Azure SQL
- ERD and `Database/CreateSchema.sql`

## Important Cost Note

The current App Service plan is the Free (`F1`) tier. This keeps hosting cost low, but Free tier apps have limitations such as reduced performance, no custom domain SSL features beyond the default Azure hostname, and possible quota limits.
