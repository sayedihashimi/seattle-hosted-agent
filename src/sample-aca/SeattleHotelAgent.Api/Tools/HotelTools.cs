using System.ComponentModel;
using SeattleHotelAgent.Api.Models;

namespace SeattleHotelAgent.Api.Tools;

public static class HotelTools
{
    [Description("Search for hotels in Seattle. You can filter by neighborhood, minimum star rating, maximum price per night, and number of guests. Returns a list of matching hotels with their details.")]
    public static string SearchHotels(
        [Description("Optional neighborhood to filter by (e.g., 'Capitol Hill', 'Ballard', 'Downtown')")] string? neighborhood = null,
        [Description("Minimum star rating (1-5)")] int? minStarRating = null,
        [Description("Maximum price per night in USD")] decimal? maxPricePerNight = null,
        [Description("Number of guests to accommodate")] int? guests = null)
    {
        var results = HotelData.Hotels.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(neighborhood))
        {
            results = results.Where(h =>
                h.Neighborhood.Contains(neighborhood, StringComparison.OrdinalIgnoreCase));
        }

        if (minStarRating.HasValue)
        {
            results = results.Where(h => h.StarRating >= minStarRating.Value);
        }

        if (maxPricePerNight.HasValue)
        {
            results = results.Where(h =>
                h.Rooms.Any(r => r.PricePerNight <= maxPricePerNight.Value));
        }

        if (guests.HasValue)
        {
            results = results.Where(h =>
                h.Rooms.Any(r => r.MaxGuests >= guests.Value && r.AvailableCount > 0));
        }

        var hotels = results.ToList();
        if (hotels.Count == 0)
            return "No hotels found matching your criteria.";

        var lines = hotels.Select(h =>
        {
            var cheapest = h.Rooms.Min(r => r.PricePerNight);
            return $"- [ID: {h.Id}] {h.Name} ({h.StarRating}★, {h.Rating}/5.0) in {h.Neighborhood} — from ${cheapest}/night. {h.Description}";
        });

        return $"Found {hotels.Count} hotel(s):\n{string.Join("\n", lines)}";
    }

    [Description("Get detailed information about a specific hotel including all room types, prices, and amenities.")]
    public static string GetHotelDetails(
        [Description("The hotel ID (e.g., 'emerald-inn', 'pike-place-suites')")] string hotelId)
    {
        var hotel = HotelData.Hotels.FirstOrDefault(h =>
            h.Id.Equals(hotelId, StringComparison.OrdinalIgnoreCase));

        if (hotel is null)
            return $"Hotel with ID '{hotelId}' not found. Use SearchHotels to find available hotels.";

        var rooms = string.Join("\n", hotel.Rooms.Select(r =>
            $"  - {r.Type}: ${r.PricePerNight}/night (up to {r.MaxGuests} guests, {r.AvailableCount} available) — {r.Description}"));

        return $"""
            Hotel: {hotel.Name} ({hotel.StarRating}★, {hotel.Rating}/5.0 rating)
            Location: {hotel.Address} ({hotel.Neighborhood})
            Description: {hotel.Description}
            
            Rooms:
            {rooms}
            
            Amenities: {string.Join(", ", hotel.Amenities)}
            """;
    }

    [Description("Check room availability at a specific hotel for given dates. Returns available rooms and total prices.")]
    public static string CheckAvailability(
        [Description("The hotel ID")] string hotelId,
        [Description("Check-in date (YYYY-MM-DD)")] string checkInDate,
        [Description("Check-out date (YYYY-MM-DD)")] string checkOutDate,
        [Description("Number of guests")] int guests = 1)
    {
        var hotel = HotelData.Hotels.FirstOrDefault(h =>
            h.Id.Equals(hotelId, StringComparison.OrdinalIgnoreCase));

        if (hotel is null)
            return $"Hotel with ID '{hotelId}' not found.";

        if (!DateOnly.TryParse(checkInDate, out var checkIn) || !DateOnly.TryParse(checkOutDate, out var checkOut))
            return "Invalid date format. Please use YYYY-MM-DD.";

        if (checkOut <= checkIn)
            return "Check-out date must be after check-in date.";

        var nights = checkOut.DayNumber - checkIn.DayNumber;

        var availableRooms = hotel.Rooms
            .Where(r => r.MaxGuests >= guests && r.AvailableCount > 0)
            .ToList();

        if (availableRooms.Count == 0)
            return $"No rooms available at {hotel.Name} for {guests} guest(s) on those dates.";

        var lines = availableRooms.Select(r =>
            $"  - {r.Type}: ${r.PricePerNight}/night × {nights} nights = ${r.PricePerNight * nights} total ({r.AvailableCount} rooms left)");

        return $"""
            Availability at {hotel.Name} ({checkIn:MMM d} → {checkOut:MMM d}, {nights} night(s), {guests} guest(s)):
            {string.Join("\n", lines)}
            """;
    }

    [Description("Book a hotel room. Returns a confirmation with booking details and total price.")]
    public static string BookRoom(
        [Description("The hotel ID")] string hotelId,
        [Description("Room type to book (e.g., 'Standard Queen', 'Deluxe King')")] string roomType,
        [Description("Full name of the guest")] string guestName,
        [Description("Check-in date (YYYY-MM-DD)")] string checkInDate,
        [Description("Check-out date (YYYY-MM-DD)")] string checkOutDate)
    {
        var hotel = HotelData.Hotels.FirstOrDefault(h =>
            h.Id.Equals(hotelId, StringComparison.OrdinalIgnoreCase));

        if (hotel is null)
            return $"Hotel with ID '{hotelId}' not found.";

        var room = hotel.Rooms.FirstOrDefault(r =>
            r.Type.Equals(roomType, StringComparison.OrdinalIgnoreCase));

        if (room is null)
            return $"Room type '{roomType}' not found at {hotel.Name}. Available types: {string.Join(", ", hotel.Rooms.Select(r => r.Type))}";

        if (room.AvailableCount <= 0)
            return $"Sorry, no {room.Type} rooms are currently available at {hotel.Name}.";

        if (!DateOnly.TryParse(checkInDate, out var checkIn) || !DateOnly.TryParse(checkOutDate, out var checkOut))
            return "Invalid date format. Please use YYYY-MM-DD.";

        if (checkOut <= checkIn)
            return "Check-out date must be after check-in date.";

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        var totalPrice = room.PricePerNight * nights;
        var confirmationNumber = $"SEA-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        return $"""
            ✅ Booking Confirmed!
            
            Confirmation #: {confirmationNumber}
            Hotel: {hotel.Name}
            Room: {room.Type}
            Guest: {guestName}
            Check-in: {checkIn:ddd, MMM d, yyyy}
            Check-out: {checkOut:ddd, MMM d, yyyy}
            Duration: {nights} night(s)
            Rate: ${room.PricePerNight}/night
            Total: ${totalPrice}
            
            Please present this confirmation number at check-in. Enjoy your stay in Seattle!
            """;
    }
}
