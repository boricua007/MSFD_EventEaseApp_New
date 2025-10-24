namespace MSFD_EventEaseApp.Models
{
    public class SearchCriteria
    {
        public string EventName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
    }
}