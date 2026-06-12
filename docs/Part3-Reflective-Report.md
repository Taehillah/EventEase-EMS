# EventEase EMS Part 3 Reflective Report

## Overview

EventEase EMS was built as an ASP.NET Core MVC application for managing venues, events, and bookings. Part 3 extends the system with event classification and richer reporting-style filters so administrators can find events by category, venue, date range, and booking availability.

## Part 3 Enhancements

The application now includes an `EventType` lookup table with predefined categories:

- Conference
- Wedding
- Concert
- Gala
- Corporate
- Workshop
- Exhibition
- Private Function

Each event can be assigned an event type when it is created or edited. The event list also supports advanced filtering by search text, event status, event type, venue, date range, and availability. Availability is based on whether the event already has an active booking, while unassigned events can be filtered separately when they still need a venue.

## How the App Components Work Together

The MVC controllers coordinate user requests, validation, filtering, and data updates. Razor views render the dashboard, forms, filters, and tables. Entity Framework Core maps the C# models to database tables and applies migrations to keep the Azure SQL schema aligned with the code.

The `Venue`, `Event`, `EventType`, and `Booking` tables represent the main business data. Venues store capacity and location details. Events store client, timing, expected guest, status, and event type information. Bookings connect an event to a venue and enforce the scheduling workflow.

The booking conflict service checks active bookings so a venue cannot be double-booked on overlapping dates. This protects the quality of the booking data and supports operational decisions from the event and booking lists.

## Azure Services Used

Azure App Service hosts the ASP.NET Core MVC application and provides the public HTTPS endpoint. It runs the published .NET 8 application and serves the admin interface without requiring a dedicated virtual machine.

Azure SQL Database stores the relational data for venues, event types, events, and bookings. SQL Server foreign keys preserve relationships between records, while EF Core migrations manage schema changes such as the new `EventType` lookup table.

Azure App Service application settings provide environment-specific configuration. This is where production connection strings and admin credentials should be stored, keeping deployment configuration separate from source code.

Azure Blob Storage is supported through the venue image storage abstraction. The app can run locally with file-based image storage and switch to Blob Storage when the storage connection string and container name are configured.

GitHub stores the project source code and gives the deployment a version-controlled source of truth. The repository contains the MVC code, EF Core migrations, SQL setup script, and documentation needed for assessment review.

## Reflection

The main learning from building EventEase was how the application layers depend on each other. A user-facing feature such as filtering by event type required a model, database table, migration, seed data, controller logic, view model properties, Razor controls, and deployment verification. Missing any one of those parts would make the feature incomplete.

Using Azure services also showed the importance of configuration management. Local development can run with the in-memory database, but the deployed application needs Azure SQL and production app settings. Keeping these responsibilities separate made the app easier to test locally while still being ready for cloud deployment.

The booking workflow highlighted why validation belongs close to the business rules. The conflict service prevents accidental double-bookings and keeps venue availability reliable. This improves the usefulness of the advanced filters because the availability status is based on actual active booking data.

Overall, EventEase demonstrates how an MVC application, EF Core, Azure App Service, Azure SQL, and optional Blob Storage can work together to support a practical event management system.
