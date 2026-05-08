namespace EventEase.EMS.Services;

public interface IBookingConflictService
{
    Task<bool> HasVenueConflictAsync(int venueId, DateTime startUtc, DateTime endUtc, int? currentBookingId = null);
    Task<bool> HasActiveBookingForVenueAsync(int venueId);
    Task<bool> HasActiveBookingForEventAsync(int eventId, int? currentBookingId = null);
}
