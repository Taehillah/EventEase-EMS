using EventEase.EMS.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEase.EMS.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<EventRecord> Events => Set<EventRecord>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.ToTable("Venue");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).HasColumnName("VenueId");
            entity.Property(v => v.Name).HasColumnName("VenueName");
            entity.Property(v => v.ImagePath).HasColumnName("ImageUrl");
            entity.HasIndex(v => v.Name).IsUnique();
            entity.Property(v => v.Capacity).HasDefaultValue(0);
        });

        modelBuilder.Entity<EventRecord>(entity =>
        {
            entity.ToTable("Event");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("EventId");
            entity.Property(e => e.Name).HasColumnName("EventName");
            entity.Property(e => e.RequestedStartUtc).HasColumnName("EventDate");
            entity.Property(e => e.VenueId).HasColumnName("VenueId");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Booking");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Id).HasColumnName("BookingID");
            entity.Property(b => b.EventRecordId).HasColumnName("EventID");
            entity.Property(b => b.VenueId).HasColumnName("VenueID");
            entity.Property(b => b.CreatedUtc).HasColumnName("BookingDate");
            entity.Property(b => b.Status).HasConversion<string>();
            entity.HasIndex(b => b.BookingReference).IsUnique();
            entity.HasOne(b => b.Venue)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VenueId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.EventRecord)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventRecordId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
