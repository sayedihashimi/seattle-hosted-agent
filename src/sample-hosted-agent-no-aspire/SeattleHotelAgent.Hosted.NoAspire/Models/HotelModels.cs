namespace SeattleHotelAgent.Hosted.NoAspire.Models;

public record Hotel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Address { get; init; }
    public required string Neighborhood { get; init; }
    public required double Rating { get; init; }
    public required int StarRating { get; init; }
    public required List<Room> Rooms { get; init; }
    public required List<string> Amenities { get; init; }
}

public record Room
{
    public required string Type { get; init; }
    public required string Description { get; init; }
    public required decimal PricePerNight { get; init; }
    public required int MaxGuests { get; init; }
    public required int AvailableCount { get; init; }
}

public record BookingRequest
{
    public required string HotelId { get; init; }
    public required string RoomType { get; init; }
    public required string GuestName { get; init; }
    public required DateOnly CheckIn { get; init; }
    public required DateOnly CheckOut { get; init; }
    public int Guests { get; init; } = 1;
}

public record Booking
{
    public required string ConfirmationNumber { get; init; }
    public required string HotelName { get; init; }
    public required string RoomType { get; init; }
    public required string GuestName { get; init; }
    public required DateOnly CheckIn { get; init; }
    public required DateOnly CheckOut { get; init; }
    public required decimal TotalPrice { get; init; }
    public required int Nights { get; init; }
}
