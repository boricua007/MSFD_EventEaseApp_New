using Microsoft.JSInterop;
using System.Text.Json;
using MSFD_EventEaseApp.Models;

namespace MSFD_EventEaseApp.Services
{
    public class AttendanceTrackerService : IDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly UserSessionTrackerService _sessionTracker;
        private const string AttendanceStorageKey = "eventease_attendance_records";
        
        private List<AttendanceRecord> _attendanceRecords = new();
        private bool _disposed = false;

        // Events for attendance changes
        public event EventHandler<AttendanceEventArgs>? AttendanceChanged;
        public event EventHandler<AttendanceRecord>? UserRegistered;
        public event EventHandler<AttendanceRecord>? UserCheckedIn;
        public event EventHandler<AttendanceRecord>? UserCheckedOut;

        public AttendanceTrackerService(IJSRuntime jsRuntime, UserSessionTrackerService sessionTracker)
        {
            _jsRuntime = jsRuntime;
            _sessionTracker = sessionTracker;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var attendanceData = await GetFromLocalStorageAsync(AttendanceStorageKey);
                if (!string.IsNullOrEmpty(attendanceData))
                {
                    var records = JsonSerializer.Deserialize<List<AttendanceRecord>>(attendanceData);
                    if (records != null)
                    {
                        _attendanceRecords = records;
                    }
                }
            }
            catch (Exception)
            {
                // If there's any error loading data, start fresh
                _attendanceRecords = new List<AttendanceRecord>();
            }
        }

        #region Registration Management

        public async Task<bool> RegisterForEventAsync(int eventId, string? notes = null)
        {
            var user = _sessionTracker.CurrentSession.User;
            
            // Check if already registered
            if (IsRegisteredForEvent(eventId, user.UserId))
            {
                return false; // Already registered
            }

            var record = new AttendanceRecord
            {
                EventId = eventId,
                UserId = user.UserId,
                UserName = user.Username,
                UserEmail = user.Email,
                Status = AttendanceStatus.Registered,
                RegisteredAt = DateTime.UtcNow,
                Notes = notes
            };

            _attendanceRecords.Add(record);
            await SaveToLocalStorageAsync();
            
            UserRegistered?.Invoke(this, record);
            OnAttendanceChanged("UserRegistered", record);
            
            return true;
        }

        public async Task<bool> CancelRegistrationAsync(int eventId)
        {
            var user = _sessionTracker.CurrentSession.User;
            var record = GetAttendanceRecord(eventId, user.UserId);
            
            if (record == null || record.Status == AttendanceStatus.Present)
            {
                return false; // Can't cancel if not registered or already checked in
            }

            record.Status = AttendanceStatus.Cancelled;
            await SaveToLocalStorageAsync();
            
            OnAttendanceChanged("RegistrationCancelled", record);
            return true;
        }

        #endregion

        #region Check-in/Check-out

        public async Task<bool> CheckInAsync(int eventId, string? notes = null)
        {
            var user = _sessionTracker.CurrentSession.User;
            var record = GetAttendanceRecord(eventId, user.UserId);
            
            if (record == null)
            {
                // Auto-register if not already registered
                await RegisterForEventAsync(eventId, notes);
                record = GetAttendanceRecord(eventId, user.UserId);
            }

            if (record != null && record.Status != AttendanceStatus.Present)
            {
                record.Status = AttendanceStatus.Present;
                record.CheckedInAt = DateTime.UtcNow;
                record.Notes = notes ?? record.Notes;
                
                await SaveToLocalStorageAsync();
                
                UserCheckedIn?.Invoke(this, record);
                OnAttendanceChanged("UserCheckedIn", record);
                
                return true;
            }

            return false;
        }

        public async Task<bool> CheckOutAsync(int eventId, string? notes = null)
        {
            var user = _sessionTracker.CurrentSession.User;
            var record = GetAttendanceRecord(eventId, user.UserId);
            
            if (record != null && record.Status == AttendanceStatus.Present)
            {
                record.Status = AttendanceStatus.CheckedOut;
                record.CheckedOutAt = DateTime.UtcNow;
                record.Notes = notes ?? record.Notes;
                
                await SaveToLocalStorageAsync();
                
                UserCheckedOut?.Invoke(this, record);
                OnAttendanceChanged("UserCheckedOut", record);
                
                return true;
            }

            return false;
        }

        #endregion

        #region Query Methods

        public bool IsRegisteredForEvent(int eventId, string? userId = null)
        {
            userId ??= _sessionTracker.CurrentSession.User.UserId;
            return _attendanceRecords.Any(r => r.EventId == eventId && 
                                              r.UserId == userId && 
                                              r.Status != AttendanceStatus.Cancelled);
        }

        public AttendanceRecord? GetAttendanceRecord(int eventId, string? userId = null)
        {
            userId ??= _sessionTracker.CurrentSession.User.UserId;
            return _attendanceRecords.FirstOrDefault(r => r.EventId == eventId && r.UserId == userId);
        }

        public AttendanceStatus GetAttendanceStatus(int eventId, string? userId = null)
        {
            var record = GetAttendanceRecord(eventId, userId);
            return record?.Status ?? AttendanceStatus.Registered;
        }

        public List<AttendanceRecord> GetUserAttendanceHistory(string? userId = null)
        {
            userId ??= _sessionTracker.CurrentSession.User.UserId;
            return _attendanceRecords
                .Where(r => r.UserId == userId && r.Status != AttendanceStatus.Cancelled)
                .OrderByDescending(r => r.RegisteredAt)
                .ToList();
        }

        public EventAttendanceSummary GetEventAttendanceSummary(int eventId, string eventName = "")
        {
            var eventRecords = _attendanceRecords.Where(r => r.EventId == eventId).ToList();
            
            return new EventAttendanceSummary
            {
                EventId = eventId,
                EventName = eventName,
                TotalRegistered = eventRecords.Count(r => r.Status != AttendanceStatus.Cancelled),
                TotalPresent = eventRecords.Count(r => r.Status == AttendanceStatus.Present),
                TotalAbsent = eventRecords.Count(r => r.Status == AttendanceStatus.Absent),
                TotalCheckedOut = eventRecords.Count(r => r.Status == AttendanceStatus.CheckedOut),
                Attendees = eventRecords
            };
        }

        public List<EventAttendanceSummary> GetAllEventsSummary()
        {
            return _attendanceRecords
                .GroupBy(r => r.EventId)
                .Select(g => GetEventAttendanceSummary(g.Key, $"Event {g.Key}"))
                .OrderByDescending(s => s.TotalRegistered)
                .ToList();
        }

        #endregion

        #region Administrative Methods

        public async Task MarkAsAbsentAsync(int eventId, string userId)
        {
            var record = GetAttendanceRecord(eventId, userId);
            if (record != null && record.Status == AttendanceStatus.Registered)
            {
                record.Status = AttendanceStatus.Absent;
                await SaveToLocalStorageAsync();
                
                OnAttendanceChanged("MarkedAbsent", record);
            }
        }

        public async Task ClearAttendanceDataAsync()
        {
            _attendanceRecords.Clear();
            await RemoveFromLocalStorageAsync(AttendanceStorageKey);
        }

        #endregion

        #region Helper Methods

        private void OnAttendanceChanged(string action, AttendanceRecord record)
        {
            AttendanceChanged?.Invoke(this, new AttendanceEventArgs(record, action));
        }

        private async Task SaveToLocalStorageAsync()
        {
            try
            {
                var attendanceJson = JsonSerializer.Serialize(_attendanceRecords);
                await SetInLocalStorageAsync(AttendanceStorageKey, attendanceJson);
            }
            catch (Exception)
            {
                // Handle storage errors gracefully
            }
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
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}