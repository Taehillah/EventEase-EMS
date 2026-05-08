using EventEase.EMS.Data;
using EventEase.EMS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("EventEaseDb");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlServer(connectionString);
        return;
    }

    options.UseInMemoryDatabase("EventEaseDemo");
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdminCredentialValidator, ConfigAdminCredentialValidator>();
builder.Services.AddScoped<IBookingConflictService, BookingConflictService>();

var blobConnectionString = builder.Configuration["VenueImages:StorageConnectionString"];
var blobContainerName = builder.Configuration["VenueImages:ContainerName"];

if (!string.IsNullOrWhiteSpace(blobConnectionString) && !string.IsNullOrWhiteSpace(blobContainerName))
{
    builder.Services.AddScoped<IVenueImageStorage, BlobVenueImageStorage>();
}
else
{
    builder.Services.AddScoped<IVenueImageStorage, LocalVenueImageStorage>();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (db.Database.IsRelational())
    {
        var migrationsAssembly = db.Database.GetService<IMigrationsAssembly>();
        if (migrationsAssembly.Migrations.Any())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    await SeedData.InitialiseAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
