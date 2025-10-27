namespace MSFD_EventEaseApp.Models
{
    public class AttendanceRecord
    {
        public string AttendanceId { get; set; } = Guid.NewGuid().ToString();
        public int EventId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Registered;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? CheckedInAt { get; set; }
        public DateTime? CheckedOutAt { get; set; }
        public string? Notes { get; set; }
        
        // Calculated properties
        public TimeSpan? AttendanceDuration => CheckedOutAt.HasValue && CheckedInAt.HasValue 
            ? CheckedOutAt.Value - CheckedInAt.Value 
            : null;
        
        public bool IsPresent => Status == AttendanceStatus.Present || Status == AttendanceStatus.CheckedOut;
    }

    public enum AttendanceStatus
    {
        Registered,    // Signed up but not checked in
        Present,       // Checked in and attending
        CheckedOut,    // Attended and checked out
        Absent,        // Registered but didn't show up
        Cancelled      // Registration cancelled
    }

    public class EventAttendanceSummary
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = "";
        public int TotalRegistered { get; set; }
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalCheckedOut { get; set; }
        public double AttendanceRate => TotalRegistered > 0 
            ? (double)(TotalPresent + TotalCheckedOut) / TotalRegistered * 100 
            : 0;
        
        public List<AttendanceRecord> Attendees { get; set; } = new();
    }

    public class UserAttendanceHistory
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
        public int TotalEventsAttended => AttendanceRecords.Count(r => r.IsPresent);
        public int TotalEventsRegistered => AttendanceRecords.Count;
        public double AttendanceRate => TotalEventsRegistered > 0 
            ? (double)TotalEventsAttended / TotalEventsRegistered * 100 
            : 0;
    }

    public class AttendanceEventArgs : EventArgs
    {
        public AttendanceRecord Record { get; }
        public string Action { get; }

        public AttendanceEventArgs(AttendanceRecord record, string action)
        {
            Record = record;
            Action = action;
        }
    }
}