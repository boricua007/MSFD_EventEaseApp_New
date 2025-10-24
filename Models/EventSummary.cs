namespace MSFD_EventEaseApp.Models
{
    public class EventSummary
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
        public string ShortDescription { get; set; } = string.Empty;

        // Pre-computed display values for performance
        public string DisplayDate { get; set; } = string.Empty;
        public string DisplayTime { get; set; } = string.Empty;
        public string DisplayPrice { get; set; } = string.Empty;
        public bool HasMissingData { get; set; }
        public string HeaderColorClass { get; set; } = "bg-primary";

        public static EventSummary FromEvent(Event evt)
        {
            var summary = new EventSummary
            {
                EventId = evt.EventId,
                Name = string.IsNullOrWhiteSpace(evt.Name) ? "Unnamed Event" : evt.Name.Trim(),
                Category = string.IsNullOrWhiteSpace(evt.Category) ? "Uncategorized" : evt.Category.Trim(),
                Date = evt.Date,
                Location = string.IsNullOrWhiteSpace(evt.Location) ? "Location TBD" : evt.Location.Trim(),
                Price = evt.Price,
                AvailableSeats = evt.AvailableSeats
            };

            // Pre-compute display values
            summary.DisplayDate = evt.Date == default(DateTime) ? "Date TBD" : evt.Date.ToString("MMM dd, yyyy");
            summary.DisplayTime = evt.Date == default(DateTime) ? "Time TBD" : evt.Date.ToString("h:mm tt");
            summary.DisplayPrice = evt.Price < 0 ? "Price TBD" : $"${evt.Price:F2}";

            // Pre-compute description with truncation
            if (string.IsNullOrWhiteSpace(evt.Description))
            {
                summary.ShortDescription = "No description available.";
            }
            else
            {
                var description = evt.Description.Trim();
                summary.ShortDescription = description.Length > 120 
                    ? $"{description.Substring(0, 120).Trim()}..." 
                    : description;
            }

            // Pre-compute missing data status
            summary.HasMissingData = string.IsNullOrWhiteSpace(evt.Name) ||
                                   string.IsNullOrWhiteSpace(evt.Category) ||
                                   string.IsNullOrWhiteSpace(evt.Location) ||
                                   string.IsNullOrWhiteSpace(evt.Description) ||
                                   evt.Date == default(DateTime) ||
                                   evt.Price < 0 ||
                                   evt.AvailableSeats < 0;

            // Pre-compute header color class
            if (!summary.HasMissingData)
            {
                summary.HeaderColorClass = "bg-primary";
            }
            else
            {
                var missingCount = 0;
                if (string.IsNullOrWhiteSpace(evt.Name)) missingCount++;
                if (string.IsNullOrWhiteSpace(evt.Category)) missingCount++;
                if (evt.Date == default(DateTime)) missingCount++;
                if (string.IsNullOrWhiteSpace(evt.Location)) missingCount++;
                
                summary.HeaderColorClass = missingCount >= 2 ? "bg-danger" : "bg-warning";
            }

            return summary;
        }
    }
}