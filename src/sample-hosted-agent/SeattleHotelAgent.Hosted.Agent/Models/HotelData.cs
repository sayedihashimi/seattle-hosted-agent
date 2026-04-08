namespace SeattleHotelAgent.Hosted.Agent.Models;

public static class HotelData
{
    public static readonly List<Hotel> Hotels =
    [
        new()
        {
            Id = "emerald-inn",
            Name = "The Emerald Inn",
            Description = "A cozy boutique hotel nestled in the heart of Capitol Hill, offering a warm Pacific Northwest ambiance with locally sourced breakfast and stunning city views.",
            Address = "1425 Broadway E, Seattle, WA 98102",
            Neighborhood = "Capitol Hill",
            Rating = 4.6,
            StarRating = 4,
            Rooms =
            [
                new() { Type = "Standard Queen", Description = "Comfortable room with queen bed and city view", PricePerNight = 159m, MaxGuests = 2, AvailableCount = 8 },
                new() { Type = "Deluxe King", Description = "Spacious room with king bed, sitting area, and panoramic city views", PricePerNight = 229m, MaxGuests = 2, AvailableCount = 4 },
                new() { Type = "Suite", Description = "One-bedroom suite with separate living area and kitchenette", PricePerNight = 349m, MaxGuests = 4, AvailableCount = 2 }
            ],
            Amenities = ["Free WiFi", "Complimentary Breakfast", "Rooftop Terrace", "Bike Rentals", "EV Charging"]
        },
        new()
        {
            Id = "pike-place-suites",
            Name = "Pike Place Suites",
            Description = "Steps from the iconic Pike Place Market, this all-suite hotel offers spacious accommodations with full kitchens and floor-to-ceiling windows overlooking Elliott Bay.",
            Address = "86 Pine St, Seattle, WA 98101",
            Neighborhood = "Downtown / Pike Place",
            Rating = 4.8,
            StarRating = 5,
            Rooms =
            [
                new() { Type = "Studio Suite", Description = "Open-concept suite with kitchenette and market views", PricePerNight = 289m, MaxGuests = 2, AvailableCount = 6 },
                new() { Type = "One-Bedroom Suite", Description = "Separate bedroom with full kitchen and Elliott Bay views", PricePerNight = 419m, MaxGuests = 3, AvailableCount = 4 },
                new() { Type = "Penthouse Suite", Description = "Luxury two-bedroom penthouse with wraparound terrace and 360° views", PricePerNight = 799m, MaxGuests = 4, AvailableCount = 1 }
            ],
            Amenities = ["Free WiFi", "Full Kitchen", "Concierge Service", "Spa", "Fitness Center", "Valet Parking"]
        },
        new()
        {
            Id = "ballard-lodge",
            Name = "Ballard Nordic Lodge",
            Description = "Inspired by the neighborhood's Scandinavian heritage, this charming lodge features hygge-inspired rooms, a sauna, and is walking distance to Ballard's breweries.",
            Address = "5300 Ballard Ave NW, Seattle, WA 98107",
            Neighborhood = "Ballard",
            Rating = 4.5,
            StarRating = 3,
            Rooms =
            [
                new() { Type = "Standard Double", Description = "Nordic-themed room with two double beds", PricePerNight = 129m, MaxGuests = 4, AvailableCount = 10 },
                new() { Type = "Deluxe King", Description = "Spacious room with king bed, fireplace, and garden view", PricePerNight = 189m, MaxGuests = 2, AvailableCount = 5 },
                new() { Type = "Family Suite", Description = "Two-room suite ideal for families, with bunk beds and play area", PricePerNight = 269m, MaxGuests = 6, AvailableCount = 3 }
            ],
            Amenities = ["Free WiFi", "Sauna", "Free Parking", "Pet Friendly", "Brewery Tours"]
        },
        new()
        {
            Id = "waterfront-grand",
            Name = "The Waterfront Grand",
            Description = "An upscale waterfront hotel on Alaskan Way with direct access to the Seattle Great Wheel, featuring elegant rooms and a renowned seafood restaurant.",
            Address = "1001 Alaskan Way, Seattle, WA 98101",
            Neighborhood = "Waterfront",
            Rating = 4.7,
            StarRating = 5,
            Rooms =
            [
                new() { Type = "Harbor View Queen", Description = "Elegant room with queen bed overlooking the harbor", PricePerNight = 319m, MaxGuests = 2, AvailableCount = 7 },
                new() { Type = "Premium King", Description = "Premium room with king bed, balcony, and sunset views", PricePerNight = 449m, MaxGuests = 2, AvailableCount = 4 },
                new() { Type = "Presidential Suite", Description = "Expansive two-bedroom suite with private dining room and butler service", PricePerNight = 1199m, MaxGuests = 4, AvailableCount = 1 }
            ],
            Amenities = ["Free WiFi", "Waterfront Restaurant", "Spa & Wellness Center", "Indoor Pool", "Valet Parking", "Room Service"]
        },
        new()
        {
            Id = "fremont-artisan",
            Name = "Fremont Artisan Hotel",
            Description = "A quirky, art-filled hotel in the 'Center of the Universe' neighborhood, featuring rotating gallery exhibitions and rooms designed by local artists.",
            Address = "3601 Fremont Ave N, Seattle, WA 98103",
            Neighborhood = "Fremont",
            Rating = 4.4,
            StarRating = 3,
            Rooms =
            [
                new() { Type = "Artist Loft", Description = "Unique loft-style room with original art and skylight", PricePerNight = 149m, MaxGuests = 2, AvailableCount = 6 },
                new() { Type = "Gallery Suite", Description = "Spacious suite featuring rotating art installations", PricePerNight = 219m, MaxGuests = 3, AvailableCount = 3 },
                new() { Type = "Sculptor's Penthouse", Description = "Top-floor penthouse with rooftop sculpture garden access", PricePerNight = 379m, MaxGuests = 4, AvailableCount = 1 }
            ],
            Amenities = ["Free WiFi", "Art Gallery", "Coffee Bar", "Bike Rentals", "Garden Courtyard"]
        },
        new()
        {
            Id = "slu-tech-hotel",
            Name = "South Lake Union Tech Hotel",
            Description = "A modern, tech-forward hotel in Seattle's innovation district. Every room features smart home controls, and the hotel is steps from Amazon's campus and MOHAI.",
            Address = "401 Terry Ave N, Seattle, WA 98109",
            Neighborhood = "South Lake Union",
            Rating = 4.3,
            StarRating = 4,
            Rooms =
            [
                new() { Type = "Smart Standard", Description = "Tech-equipped room with voice controls and fast WiFi", PricePerNight = 179m, MaxGuests = 2, AvailableCount = 12 },
                new() { Type = "Innovation Suite", Description = "Suite with standing desk, dual monitors, and ergonomic workspace", PricePerNight = 299m, MaxGuests = 2, AvailableCount = 4 },
                new() { Type = "Executive Suite", Description = "Corner suite with meeting space for up to 6 and lake views", PricePerNight = 459m, MaxGuests = 3, AvailableCount = 2 }
            ],
            Amenities = ["Ultra-Fast WiFi", "Co-working Space", "Fitness Center", "Electric Shuttle", "Smart Room Controls"]
        },
        new()
        {
            Id = "pioneer-square-historic",
            Name = "Pioneer Square Heritage Hotel",
            Description = "A beautifully restored 1901 building in Seattle's oldest neighborhood, blending original brick and timber architecture with modern comforts.",
            Address = "95 S Jackson St, Seattle, WA 98104",
            Neighborhood = "Pioneer Square",
            Rating = 4.2,
            StarRating = 3,
            Rooms =
            [
                new() { Type = "Heritage Room", Description = "Charming room with exposed brick walls and period fixtures", PricePerNight = 139m, MaxGuests = 2, AvailableCount = 8 },
                new() { Type = "Loft Room", Description = "High-ceilinged loft with original timber beams", PricePerNight = 199m, MaxGuests = 2, AvailableCount = 4 },
                new() { Type = "Grand Heritage Suite", Description = "Corner suite with restored fireplace and antique furnishings", PricePerNight = 329m, MaxGuests = 3, AvailableCount = 2 }
            ],
            Amenities = ["Free WiFi", "Historical Tours", "Wine Bar", "Library Lounge", "Underground Tour Access"]
        },
        new()
        {
            Id = "green-lake-retreat",
            Name = "Green Lake Nature Retreat",
            Description = "A tranquil retreat overlooking Green Lake, perfect for nature lovers. Features a lakeside trail, kayak rentals, and farm-to-table dining.",
            Address = "7201 E Green Lake Dr N, Seattle, WA 98115",
            Neighborhood = "Green Lake",
            Rating = 4.6,
            StarRating = 4,
            Rooms =
            [
                new() { Type = "Garden Room", Description = "Ground-floor room with private garden patio", PricePerNight = 169m, MaxGuests = 2, AvailableCount = 6 },
                new() { Type = "Lake View King", Description = "Upper-floor room with king bed and lake panorama", PricePerNight = 249m, MaxGuests = 2, AvailableCount = 4 },
                new() { Type = "Nature Suite", Description = "Two-room suite with binoculars, nature library, and balcony", PricePerNight = 359m, MaxGuests = 4, AvailableCount = 2 }
            ],
            Amenities = ["Free WiFi", "Kayak Rentals", "Nature Trails", "Farm-to-Table Restaurant", "Yoga Classes", "Free Parking"]
        }
    ];
}
