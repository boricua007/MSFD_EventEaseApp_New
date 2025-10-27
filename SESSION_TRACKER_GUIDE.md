# User Session Tracker for Blazor WebAssembly

The User Session Tracker provides comprehensive state management capabilities for your Blazor WebAssembly application, including user authentication state, preferences, navigation history, and component-level state persistence.

## Features

### 🔐 User Management
- Authentication state tracking
- User profile management
- Role-based access control
- Session timeout handling

### 🎯 User Preferences
- Theme preferences (light/dark)
- Language settings
- Page size and display options
- Custom user settings
- Notification preferences

### 📊 Activity Tracking
- Page navigation history
- Event viewing history
- Bookmark management
- Search history
- Component state persistence

### 💾 Data Persistence
- Automatic localStorage synchronization
- Session recovery on app reload
- Cross-tab session sharing
- Configurable session timeout

## Quick Start

### 1. Service Registration
The service is already registered in `Program.cs` as a singleton:

```csharp
builder.Services.AddSingleton<UserSessionTrackerService>();
```

### 2. Component Integration
Inject the service into your components:

```razor
@inject UserSessionTrackerService SessionTracker
@implements IDisposable

@code {
    protected override async Task OnInitializedAsync()
    {
        // Initialize session
        await SessionTracker.InitializeAsync();
        
        // Track page visit
        await SessionTracker.TrackPageVisitAsync("MyPage");
        
        // Subscribe to changes
        SessionTracker.SessionChanged += OnSessionChanged;
    }

    public void Dispose()
    {
        SessionTracker.SessionChanged -= OnSessionChanged;
    }
}
```

## Usage Examples

### User Authentication

```csharp
// Login user
await SessionTracker.LoginUserAsync(
    userId: "user123",
    username: "john.doe",
    email: "john@example.com",
    roles: new List<string> { "User", "EventViewer" }
);

// Logout user
await SessionTracker.LogoutUserAsync();

// Check authentication
if (SessionTracker.IsAuthenticated)
{
    var username = SessionTracker.CurrentSession.User.Username;
}
```

### Managing Preferences

```csharp
// Update theme
await SessionTracker.UpdatePreferenceAsync("theme", "dark");

// Update page size
await SessionTracker.UpdatePreferenceAsync("pagesize", 25);

// Update notifications
await SessionTracker.UpdatePreferenceAsync("enablenotifications", false);

// Get preference
var theme = SessionTracker.CurrentSession.Preferences.Theme;
var pageSize = SessionTracker.CurrentSession.Preferences.PageSize;
```

### State Management

```csharp
// Store component state
await SessionTracker.UpdateStateAsync("filterSettings", new FilterSettings 
{ 
    Category = "Technology",
    DateRange = "ThisWeek" 
});

// Retrieve component state
var filters = SessionTracker.GetState<FilterSettings>("filterSettings");

// Track user actions
await SessionTracker.TrackEventViewAsync(eventId);
await SessionTracker.ToggleBookmarkAsync(eventId);
```

### Navigation and History

```csharp
// Track page visits (done automatically in examples)
await SessionTracker.TrackPageVisitAsync("EventDetails");

// Access navigation history
var recentPages = SessionTracker.CurrentSession.NavigationHistory.Take(5);

// Check if user has viewed an event
var hasViewed = SessionTracker.CurrentSession.State.ViewedEventIds.Contains(eventId);
```

### Search History

```csharp
// Save search criteria
var searchCriteria = new SearchCriteria 
{
    EventName = "Conference",
    Category = "Technology"
};
await SessionTracker.SetLastSearchAsync(searchCriteria);

// Restore last search
var lastSearch = SessionTracker.CurrentSession.State.LastSearch;
```

## Session Events

Subscribe to session events to react to changes:

```csharp
SessionTracker.SessionChanged += (sender, e) => 
{
    Console.WriteLine($"Session changed: {e.ChangeType}");
    // React to changes in UI
    StateHasChanged();
};

SessionTracker.UserAuthenticated += (sender, session) => 
{
    // User logged in
    NavigationManager.NavigateTo("/dashboard");
};

SessionTracker.SessionExpired += (sender, session) => 
{
    // Session expired
    // Show notification or redirect to login
};
```

## Demo Pages

### Session Demo (`/session-demo`)
Interactive demonstration of all session tracker features including:
- User login/logout
- Preference updates
- Activity tracking
- Real-time session monitoring

### Events with Session (`/events-with-session`)
Practical example showing how to integrate session tracking into an existing events page:
- View tracking
- Bookmark management
- Search history
- Personalized recommendations
- User preference integration

## Configuration

### Session Timeout
Default session timeout is 30 minutes. Modify in `UserSessionTrackerService.cs`:

```csharp
private const string SessionTimeoutMinutes = "30";
```

### Storage Key
Change the localStorage key if needed:

```csharp
private const string SessionStorageKey = "eventease_user_session";
```

## Best Practices

1. **Always Initialize**: Call `InitializeAsync()` in `OnInitializedAsync()`
2. **Track Navigation**: Call `TrackPageVisitAsync()` for important pages
3. **Subscribe to Events**: Listen for session changes to update UI
4. **Dispose Properly**: Unsubscribe from events in `Dispose()`
5. **Handle Errors**: Session operations are designed to fail gracefully
6. **Performance**: Session data is cached in memory and synced to localStorage
7. **Privacy**: Session data is stored locally and not transmitted

## Advanced Features

### Custom State Storage

```csharp
// Store complex objects
await SessionTracker.UpdateStateAsync("shoppingCart", cartItems);

// Store primitive values
await SessionTracker.UpdateStateAsync("currentStep", 3);

// Retrieve with defaults
var cart = SessionTracker.GetState<List<CartItem>>("shoppingCart", new List<CartItem>());
```

### Session Analytics

```csharp
// Access session metrics
var sessionDuration = SessionTracker.CurrentSession.SessionDuration;
var pageCount = SessionTracker.CurrentSession.NavigationHistory.Count;
var eventsViewed = SessionTracker.CurrentSession.State.ViewedEventIds.Count;
```

### Multi-Component Communication

Use session state to communicate between components:

```csharp
// Component A sets state
await SessionTracker.UpdateStateAsync("selectedFilters", filters);

// Component B reads state and subscribes to changes
var filters = SessionTracker.GetState<FilterModel>("selectedFilters");
SessionTracker.SessionChanged += (s, e) => 
{
    if (e.ChangeType == "State_selectedFilters")
    {
        // Update component when filters change
        LoadFilteredData();
    }
};
```

This session tracker provides a robust foundation for state management in your Blazor WebAssembly application, enabling personalized user experiences and seamless data persistence across browser sessions.