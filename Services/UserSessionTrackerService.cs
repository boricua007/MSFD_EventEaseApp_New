using Microsoft.JSInterop;
using System.Text.Json;
using MSFD_EventEaseApp.Models;

namespace MSFD_EventEaseApp.Services
{
    public class UserSessionTrackerService : IDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private const string SessionStorageKey = "eventease_user_session";
        private const string SessionTimeoutMinutes = "30";
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(int.Parse(SessionTimeoutMinutes));
        
        private UserSession _currentSession;
        private readonly Timer _activityTimer;
        private bool _disposed = false;

        // Events for session changes
        public event EventHandler<SessionEventArgs>? SessionChanged;
        public event EventHandler<UserSession>? SessionExpired;
        public event EventHandler<UserSession>? SessionStarted;
        public event EventHandler<UserSession>? UserAuthenticated;

        public UserSessionTrackerService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _currentSession = new UserSession();
            
            // Set up activity timer to check for session expiration every minute
            _activityTimer = new Timer(CheckSessionExpiration, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public UserSession CurrentSession => _currentSession;
        public bool IsAuthenticated => _currentSession.User.IsAuthenticated;
        public string SessionId => _currentSession.SessionId;

        #region Initialization and Persistence

        public async Task InitializeAsync()
        {
            try
            {
                var sessionData = await GetFromLocalStorageAsync(SessionStorageKey);
                if (!string.IsNullOrEmpty(sessionData))
                {
                    var session = JsonSerializer.Deserialize<UserSession>(sessionData);
                    if (session != null && !session.IsSessionExpired(_sessionTimeout))
                    {
                        _currentSession = session;
                        _currentSession.UpdateActivity();
                        await SaveToLocalStorageAsync();
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // If there's any error loading session, start fresh
            }

            // Start new session
            await StartNewSessionAsync();
        }

        public async Task StartNewSessionAsync()
        {
            var oldSession = _currentSession;
            _currentSession = new UserSession();
            await SaveToLocalStorageAsync();
            
            SessionStarted?.Invoke(this, _currentSession);
            OnSessionChanged("SessionStarted", oldSession, _currentSession);
        }

        public async Task SaveToLocalStorageAsync()
        {
            try
            {
                var sessionJson = JsonSerializer.Serialize(_currentSession);
                await SetInLocalStorageAsync(SessionStorageKey, sessionJson);
            }
            catch (Exception)
            {
                // Handle storage errors gracefully
            }
        }

        public async Task ClearSessionAsync()
        {
            var oldSession = _currentSession;
            await RemoveFromLocalStorageAsync(SessionStorageKey);
            await StartNewSessionAsync();
            OnSessionChanged("SessionCleared", oldSession, _currentSession);
        }

        #endregion

        #region User Management

        public async Task LoginUserAsync(string userId, string username, string? email = null, List<string>? roles = null)
        {
            var oldUser = _currentSession.User;
            
            _currentSession.User = new UserInfo
            {
                UserId = userId,
                Username = username,
                Email = email,
                IsAuthenticated = true,
                Roles = roles ?? new List<string>(),
                LastLogin = DateTime.UtcNow
            };
            
            _currentSession.UpdateActivity();
            await SaveToLocalStorageAsync();
            
            UserAuthenticated?.Invoke(this, _currentSession);
            OnSessionChanged("UserLogin", oldUser, _currentSession.User);
        }

        public async Task LogoutUserAsync()
        {
            var oldUser = _currentSession.User;
            
            _currentSession.User = new UserInfo();
            _currentSession.UpdateActivity();
            await SaveToLocalStorageAsync();
            
            OnSessionChanged("UserLogout", oldUser, _currentSession.User);
        }

        #endregion

        #region Preferences Management

        public async Task UpdatePreferencesAsync(UserPreferences preferences)
        {
            var oldPreferences = _currentSession.Preferences;
            _currentSession.Preferences = preferences;
            _currentSession.UpdateActivity();
            await SaveToLocalStorageAsync();
            
            OnSessionChanged("PreferencesUpdated", oldPreferences, preferences);
        }

        public async Task UpdatePreferenceAsync<T>(string key, T value) where T : notnull
        {
            var oldValue = _currentSession.Preferences.CustomSettings.TryGetValue(key, out var existing) ? existing : null;
            
            switch (key.ToLowerInvariant())
            {
                case "theme":
                    _currentSession.Preferences.Theme = value.ToString() ?? "light";
                    break;
                case "language":
                    _currentSession.Preferences.Language = value.ToString() ?? "en";
                    break;
                case "enablenotifications":
                    _currentSession.Preferences.EnableNotifications = Convert.ToBoolean(value);
                    break;
                case "pagesize":
                    _currentSession.Preferences.PageSize = Convert.ToInt32(value);
                    break;
                default:
                    _currentSession.Preferences.CustomSettings[key] = value;
                    break;
            }

            _currentSession.UpdateActivity();
            await SaveToLocalStorageAsync();
            OnSessionChanged($"Preference_{key}", oldValue, value);
        }

        #endregion

        #region State Management

        public async Task UpdateStateAsync(string key, object value)
        {
            var oldValue = _currentSession.State.ComponentStates.TryGetValue(key, out var existing) ? existing : null;
            
            _currentSession.State.ComponentStates[key] = value;
            _currentSession.UpdateActivity();
            await SaveToLocalStorageAsync();
            
            OnSessionChanged($"State_{key}", oldValue, value);
        }

        public T? GetState<T>(string key, T? defaultValue = default)
        {
            if (_currentSession.State.ComponentStates.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is JsonElement jsonElement)
                    {
                        return jsonElement.Deserialize<T>();
                    }
                    return (T)value;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public async Task TrackPageVisitAsync(string pageName)
        {
            var oldPage = _currentSession.State.CurrentPage;
            _currentSession.State.CurrentPage = pageName;
            _currentSession.AddToNavigationHistory(pageName);
            _currentSession.UpdateActivity();
            await SaveToLocalStorageAsync();
            
            OnSessionChanged("PageVisit", oldPage, pageName);
        }

        public async Task TrackEventViewAsync(int eventId)
        {
            if (!_currentSession.State.ViewedEventIds.Contains(eventId))
            {
                _currentSession.State.ViewedEventIds.Add(eventId);
                _currentSession.UpdateActivity();
                await SaveToLocalStorageAsync();
                
                OnSessionChanged("EventViewed", null, eventId);
            }
        }

        public async Task ToggleBookmarkAsync(int eventId)
        {
            var wasBookmarked = _currentSession.State.BookmarkedEventIds.Contains(eventId);
            
            if (wasBookmarked)
            {
                _currentSession.State.BookmarkedEventIds.Remove(eventId);
            }
            else
            {
                _currentSession.State.BookmarkedEventIds.Add(eventId);
            }
            
            _currentSession.UpdateActivity();
            await SaveToLocalStorageAsync();
            
            OnSessionChanged("BookmarkToggled", wasBookmarked, !wasBookmarked);
        }

        public async Task SetLastSearchAsync(SearchCriteria searchCriteria)
        {
            var oldSearch = _currentSession.State.LastSearch;
            _currentSession.State.LastSearch = searchCriteria;
            _currentSession.UpdateActivity();
            await SaveToLocalStorageAsync();
            
            OnSessionChanged("LastSearchUpdated", oldSearch, searchCriteria);
        }

        #endregion

        #region Activity Tracking

        public async Task UpdateActivityAsync()
        {
            _currentSession.UpdateActivity();
            await SaveToLocalStorageAsync();
        }

        private async void CheckSessionExpiration(object? state)
        {
            if (_currentSession.IsSessionExpired(_sessionTimeout))
            {
                SessionExpired?.Invoke(this, _currentSession);
                await StartNewSessionAsync();
            }
        }

        #endregion

        #region Helper Methods

        private void OnSessionChanged(string changeType, object? oldValue, object? newValue)
        {
            SessionChanged?.Invoke(this, new SessionEventArgs(_currentSession, changeType, oldValue, newValue));
        }

        private async Task<string?> GetFromLocalStorageAsync(string key)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            }
            catch
            {
                return null;
            }
        }

        private async Task SetInLocalStorageAsync(string key, string value)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
            }
            catch
            {
                // Handle storage errors gracefully
            }
        }

        private async Task RemoveFromLocalStorageAsync(string key)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch
            {
                // Handle storage errors gracefully
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _activityTimer?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}