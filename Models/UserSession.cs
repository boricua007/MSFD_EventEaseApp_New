using System.Text.Json.Serialization;

namespace MSFD_EventEaseApp.Models
{
    public class UserSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime SessionStartTime { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public UserInfo User { get; set; } = new();
        public UserPreferences Preferences { get; set; } = new();
        public SessionState State { get; set; } = new();
        public List<string> NavigationHistory { get; set; } = new();
        public Dictionary<string, object> CustomData { get; set; } = new();

        public TimeSpan SessionDuration => DateTime.UtcNow - SessionStartTime;
        public bool IsSessionExpired(TimeSpan timeout) => DateTime.UtcNow - LastActivity > timeout;

        public void UpdateActivity()
        {
            LastActivity = DateTime.UtcNow;
        }

        public void AddToNavigationHistory(string page)
        {
            NavigationHistory.Insert(0, page);
            if (NavigationHistory.Count > 10) // Keep only last 10 pages
            {
                NavigationHistory = NavigationHistory.Take(10).ToList();
            }
        }
    }

    public class UserInfo
    {
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool IsAuthenticated { get; set; } = false;
        public List<string> Roles { get; set; } = new();
        public DateTime? LastLogin { get; set; }
    }

    public class UserPreferences
    {
        public string Theme { get; set; } = "light";
        public string Language { get; set; } = "en";
        public string TimeZone { get; set; } = "UTC";
        public bool EnableNotifications { get; set; } = true;
        public string DefaultEventView { get; set; } = "grid";
        public int PageSize { get; set; } = 10;
        public List<string> FavoriteCategories { get; set; } = new();
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    public class SessionState
    {
        public string? CurrentPage { get; set; }
        public SearchCriteria? LastSearch { get; set; }
        public List<int> ViewedEventIds { get; set; } = new();
        public List<int> BookmarkedEventIds { get; set; } = new();
        public Dictionary<string, object> ComponentStates { get; set; } = new();
        public string? LastSelectedCategory { get; set; }
        public int? CurrentEventId { get; set; }
        public bool HasUnsavedChanges { get; set; } = false;
    }

    public class SessionEventArgs : EventArgs
    {
        public UserSession Session { get; }
        public string ChangeType { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }

        public SessionEventArgs(UserSession session, string changeType, object? oldValue = null, object? newValue = null)
        {
            Session = session;
            ChangeType = changeType;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}