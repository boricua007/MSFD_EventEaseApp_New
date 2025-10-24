using MSFD_EventEaseApp.Models;

namespace MSFD_EventEaseApp.Services
{
    public class EventService
    {
        private readonly List<Event> _events;
        private List<string>? _cachedCategories;
        private readonly Dictionary<string, List<Event>> _categoryCache = new();
        private readonly Dictionary<int, Event?> _eventCache = new();

        public EventService()
        {
            _events = GenerateSampleEvents();
            PreloadCaches();
        }

        private void PreloadCaches()
        {
            // Preload event cache
            foreach (var evt in _events)
            {
                _eventCache[evt.EventId] = evt;
            }
        }

        public Task<List<Event>> GetAllEventsAsync()
        {
            return Task.FromResult(_events);
        }

        public Task<Event?> GetEventByIdAsync(int id)
        {
            // Use cached lookup for O(1) performance
            _eventCache.TryGetValue(id, out var eventItem);
            return Task.FromResult(eventItem);
        }

        public Task<List<Event>> GetEventsByCategoryAsync(string category)
        {
            if (string.IsNullOrEmpty(category))
                return Task.FromResult(_events);

            // Use cached category lookup
            if (!_categoryCache.TryGetValue(category, out var filteredEvents))
            {
                filteredEvents = _events.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
                _categoryCache[category] = filteredEvents;
            }
            
            return Task.FromResult(filteredEvents);
        }

        public Task<List<Event>> SearchEventsAsync(string? eventName = null, string? location = null, string? category = null, DateTime? date = null)
        {
            var query = _events.AsQueryable();

            if (!string.IsNullOrEmpty(eventName))
            {
                query = query.Where(e => e.Name.Contains(eventName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(e => e.Location.Contains(location, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (date.HasValue)
            {
                query = query.Where(e => e.Date.Date == date.Value.Date);
            }

            return Task.FromResult(query.ToList());
        }

        public List<string> GetAllCategories()
        {
            // Cache categories to avoid repeated LINQ operations
            if (_cachedCategories == null)
            {
                _cachedCategories = _events.Select(e => e.Category).Distinct().OrderBy(c => c).ToList();
            }
            return _cachedCategories;
        }

        // Performance-optimized methods for rendering
        public Task<List<EventSummary>> GetEventSummariesAsync()
        {
            var summaries = _events.Select(EventSummary.FromEvent).ToList();
            return Task.FromResult(summaries);
        }

        public Task<List<EventSummary>> SearchEventSummariesAsync(string? eventName = null, string? location = null, string? category = null, DateTime? date = null)
        {
            var query = _events.AsQueryable();

            if (!string.IsNullOrEmpty(eventName))
            {
                query = query.Where(e => e.Name.Contains(eventName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(e => e.Location.Contains(location, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (date.HasValue)
            {
                query = query.Where(e => e.Date.Date == date.Value.Date);
            }

            var summaries = query.Select(EventSummary.FromEvent).ToList();
            return Task.FromResult(summaries);
        }


        // Sample data generation
        private List<Event> GenerateSampleEvents()
        {
            return new List<Event>
            {
                new Event
                {
                    EventId = 1,
                    Name = "Annual Tech Conference 2025",
                    // Date = new DateTime(2025, 11, 15, 9, 0, 0),
                    Date = new DateTime(2025, 11, 15, 9, 0, 0),
                    Location = "Microsoft Conference Center, Redmond, WA",
                    Description = "Join industry leaders for insights into the latest technology trends, innovations, and future developments in software engineering and AI.",
                    Category = "Technology",
                    Price = 299.99m,
                    AvailableSeats = 500,
                    ImageUrl = "/images/tech-conference.jpg",
                    Organizer = "TechEvents Inc."
                },
                new Event
                {
                    EventId = 2,
                    Name = "Digital Marketing Summit",
                    Date = new DateTime(2025, 12, 5, 10, 0, 0),
                    Location = "Convention Center, Austin, TX",
                    Description = "Explore the latest strategies in digital marketing, social media engagement, and customer acquisition techniques.",
                    Category = "Marketing",
                    Price = 199.99m,
                    AvailableSeats = 300,
                    ImageUrl = "/images/marketing-summit.jpg",
                    Organizer = "Marketing Pro Events"
                },
                new Event
                {
                    EventId = 3,
                    Name = "Leadership Workshop",
                    Date = new DateTime(2025, 11, 22, 14, 0, 0),
                    Location = "Business Center, Chicago, IL",
                    Description = "Interactive workshop focusing on modern leadership skills, team management, and organizational effectiveness.",
                    Category = "Leadership",
                    Price = 149.99m,
                    AvailableSeats = 100,
                    ImageUrl = "/images/leadership-workshop.jpg",
                    Organizer = "Leadership Academy"
                },
                new Event
                {
                    EventId = 4,
                    Name = "Corporate Networking Gala",
                    Date = new DateTime(2025, 12, 10, 18, 30, 0),
                    Location = "Grand Ballroom, Chicago, IL",
                    Description = "An elegant evening of professional networking with industry executives, entrepreneurs, and thought leaders.",
                    Category = "Networking",
                    Price = 125.00m,
                    AvailableSeats = 250,
                    ImageUrl = "/images/networking-gala.jpg",
                    Organizer = "Business Connect"
                },
                new Event
                {
                    EventId = 5,
                    Name = "Innovation in Healthcare Symposium",
                    Date = new DateTime(2025, 11, 28, 8, 30, 0),
                    Location = "Medical Center Auditorium, Boston, MA",
                    Description = "Discover breakthrough innovations in healthcare technology, telemedicine, and patient care solutions.",
                    Category = "Healthcare",
                    Price = 275.00m,
                    AvailableSeats = 400,
                    ImageUrl = "/images/healthcare-symposium.jpg",
                    Organizer = "HealthTech Events"
                },
                new Event
                {
                    EventId = 6,
                    Name = "Startup Pitch Competition",
                    Date = new DateTime(2025, 12, 15, 13, 0, 0),
                    Location = "Innovation Hub, San Francisco, CA",
                    Description = "Watch emerging startups pitch their innovative ideas to a panel of venture capitalists and industry experts.",
                    Category = "Entrepreneurship",
                    Price = 75.00m,
                    AvailableSeats = 200,
                    ImageUrl = "/images/startup-pitch.jpg",
                    Organizer = "Startup Valley"
                },
                // Test Cases for Validation
                new Event
                {
                    EventId = 7,
                    Name = "", // Empty name - should show "Unnamed Event"
                    Date = new DateTime(2025, 12, 20, 10, 0, 0),
                    Location = "Test Location",
                    Description = "Test event with empty name - 1 critical field missing.",
                    Category = "Testing",
                    Price = 50.00m,
                    AvailableSeats = 50,
                    ImageUrl = "",
                    Organizer = "Test Organizer"
                },
                new Event
                {
                    EventId = 8,
                    Name = "Event Missing Data - 2 Critical Fields",
                    Date = default(DateTime), // Invalid date - should show "Date TBD"
                    Location = "", // Empty location - should show "Location TBD"
                    Description = "", // Empty description - should show fallback
                    Category = "", // Empty category - should show "Uncategorized"
                    Price = -10.00m, // Negative price - should show "Price TBD"
                    AvailableSeats = -5, // Negative seats - should show "Seats TBD"
                    ImageUrl = "",
                    Organizer = ""
                }
            };
        }
    }
}