using MSFD_EventEaseApp.Models;

namespace MSFD_EventEaseApp.Services
{
    public class RegistrationService
    {
        private readonly List<Registration> _registrations;
        private readonly EventService _eventService;
        private int _nextRegistrationId = 1;

        public RegistrationService(EventService eventService)
        {
            _eventService = eventService;
            _registrations = new List<Registration>();
        }

        /// <summary>
        /// Submit a new registration for an event
        /// </summary>
        public async Task<RegistrationResult> SubmitRegistrationAsync(Registration registration)
        {
            try
            {
                // Validate the registration
                var validationResult = await ValidateRegistrationAsync(registration);
                if (!validationResult.IsValid)
                {
                    return validationResult;
                }

                // Get the event details
                var eventItem = await _eventService.GetEventByIdAsync(registration.EventId);
                if (eventItem == null)
                {
                    return RegistrationResult.Failed("Event not found.");
                }

                // Check availability
                var currentRegistrations = GetRegistrationsByEventId(registration.EventId);
                var totalAttendeesRegistered = currentRegistrations.Sum(r => r.NumberOfAttendees);
                var availableSeats = eventItem.AvailableSeats - totalAttendeesRegistered;

                if (registration.NumberOfAttendees > availableSeats)
                {
                    if (availableSeats > 0)
                    {
                        return RegistrationResult.Failed($"Only {availableSeats} seats available. Please reduce the number of attendees.");
                    }
                    else
                    {
                        // Add to waitlist
                        registration.Status = RegistrationStatus.WaitList;
                        return await ProcessRegistrationAsync(registration, eventItem, isWaitList: true);
                    }
                }

                // Process the registration
                return await ProcessRegistrationAsync(registration, eventItem);
            }
            catch (Exception ex)
            {
                return RegistrationResult.Failed($"An error occurred while processing your registration: {ex.Message}");
            }
        }

        /// <summary>
        /// Process and save the registration
        /// </summary>
        private async Task<RegistrationResult> ProcessRegistrationAsync(Registration registration, Event eventItem, bool isWaitList = false)
        {
            // Assign registration ID
            registration.RegistrationId = _nextRegistrationId++;
            registration.RegistrationDate = DateTime.Now;
            registration.Event = eventItem;

            // Set status
            registration.Status = isWaitList ? RegistrationStatus.WaitList : RegistrationStatus.Confirmed;

            // Save the registration
            _registrations.Add(registration);

            // Simulate async processing (email sending, etc.)
            await Task.Delay(100);

            var message = isWaitList 
                ? $"You have been added to the waitlist for {eventItem.Name}. We'll notify you if spots become available."
                : $"Registration confirmed for {eventItem.Name}! Confirmation details have been sent to {registration.Email}.";

            return RegistrationResult.Success(message, registration.RegistrationId);
        }

        /// <summary>
        /// Validate registration data
        /// </summary>
        private async Task<RegistrationResult> ValidateRegistrationAsync(Registration registration)
        {
            var errors = new List<string>();

            // Basic validation
            if (string.IsNullOrWhiteSpace(registration.FirstName))
                errors.Add("First name is required.");

            if (string.IsNullOrWhiteSpace(registration.LastName))
                errors.Add("Last name is required.");

            if (string.IsNullOrWhiteSpace(registration.Email))
                errors.Add("Email address is required.");
            else if (!IsValidEmail(registration.Email))
                errors.Add("Please enter a valid email address.");

            if (string.IsNullOrWhiteSpace(registration.PhoneNumber))
                errors.Add("Phone number is required.");

            if (registration.NumberOfAttendees < 1 || registration.NumberOfAttendees > 10)
                errors.Add("Number of attendees must be between 1 and 10.");

            if (!registration.AgreeToTerms)
                errors.Add("You must agree to the terms and conditions.");

            // Check for duplicate email for the same event
            if (await IsDuplicateRegistrationAsync(registration.EventId, registration.Email))
                errors.Add("A registration with this email address already exists for this event.");

            if (errors.Any())
            {
                return RegistrationResult.Failed(string.Join(" ", errors));
            }

            return RegistrationResult.Success("Validation passed");
        }

        /// <summary>
        /// Check if email is already registered for an event
        /// </summary>
        private async Task<bool> IsDuplicateRegistrationAsync(int eventId, string email)
        {
            await Task.CompletedTask; // Simulate async operation
            return _registrations.Any(r => r.EventId == eventId && 
                                         r.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all registrations for a specific event
        /// </summary>
        public List<Registration> GetRegistrationsByEventId(int eventId)
        {
            return _registrations.Where(r => r.EventId == eventId).ToList();
        }

        /// <summary>
        /// Get registration by ID
        /// </summary>
        public async Task<Registration?> GetRegistrationByIdAsync(int registrationId)
        {
            await Task.CompletedTask; // Simulate async operation
            return _registrations.FirstOrDefault(r => r.RegistrationId == registrationId);
        }

        /// <summary>
        /// Get all registrations for a specific email
        /// </summary>
        public async Task<List<Registration>> GetRegistrationsByEmailAsync(string email)
        {
            await Task.CompletedTask; // Simulate async operation
            return _registrations.Where(r => r.Email.Equals(email, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Cancel a registration
        /// </summary>
        public async Task<RegistrationResult> CancelRegistrationAsync(int registrationId)
        {
            await Task.CompletedTask; // Simulate async operation
            
            var registration = _registrations.FirstOrDefault(r => r.RegistrationId == registrationId);
            if (registration == null)
            {
                return RegistrationResult.Failed("Registration not found.");
            }

            if (registration.Status == RegistrationStatus.Cancelled)
            {
                return RegistrationResult.Failed("Registration is already cancelled.");
            }

            registration.Status = RegistrationStatus.Cancelled;
            return RegistrationResult.Success("Registration cancelled successfully.");
        }

        /// <summary>
        /// Get registration statistics for an event
        /// </summary>
        public async Task<RegistrationStatistics> GetEventRegistrationStatisticsAsync(int eventId)
        {
            await Task.CompletedTask; // Simulate async operation
            
            var eventRegistrations = GetRegistrationsByEventId(eventId);
            
            return new RegistrationStatistics
            {
                EventId = eventId,
                TotalRegistrations = eventRegistrations.Count,
                ConfirmedRegistrations = eventRegistrations.Count(r => r.Status == RegistrationStatus.Confirmed),
                WaitlistRegistrations = eventRegistrations.Count(r => r.Status == RegistrationStatus.WaitList),
                CancelledRegistrations = eventRegistrations.Count(r => r.Status == RegistrationStatus.Cancelled),
                TotalAttendees = eventRegistrations.Where(r => r.Status == RegistrationStatus.Confirmed)
                                                .Sum(r => r.NumberOfAttendees)
            };
        }

        /// <summary>
        /// Basic email validation
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Result object for registration operations
    /// </summary>
    public class RegistrationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? RegistrationId { get; set; }

        public static RegistrationResult Success(string message, int? registrationId = null)
        {
            return new RegistrationResult
            {
                IsValid = true,
                Message = message,
                RegistrationId = registrationId
            };
        }

        public static RegistrationResult Failed(string message)
        {
            return new RegistrationResult
            {
                IsValid = false,
                Message = message
            };
        }
    }

    /// <summary>
    /// Registration statistics for an event
    /// </summary>
    public class RegistrationStatistics
    {
        public int EventId { get; set; }
        public int TotalRegistrations { get; set; }
        public int ConfirmedRegistrations { get; set; }
        public int WaitlistRegistrations { get; set; }
        public int CancelledRegistrations { get; set; }
        public int TotalAttendees { get; set; }
    }
}