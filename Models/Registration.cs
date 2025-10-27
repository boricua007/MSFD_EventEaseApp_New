using System.ComponentModel.DataAnnotations;

namespace MSFD_EventEaseApp.Models
{
    public class Registration
    {
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100, ErrorMessage = "Email address cannot exceed 100 characters")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters")]
        [Display(Name = "Company/Organization")]
        public string Company { get; set; } = string.Empty;
        
        [StringLength(50, ErrorMessage = "Job title cannot exceed 50 characters")]
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; } = string.Empty;
        
        [Range(1, 10, ErrorMessage = "Number of attendees must be between 1 and 10")]
        [Display(Name = "Number of Attendees")]
        public int NumberOfAttendees { get; set; } = 1;
        
        [StringLength(500, ErrorMessage = "Special requirements cannot exceed 500 characters")]
        [Display(Name = "Special Requirements or Dietary Restrictions")]
        public string SpecialRequirements { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "Additional comments cannot exceed 1000 characters")]
        [Display(Name = "Additional Comments")]
        public string Comments { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "You must agree to the terms and conditions")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions")]
        [Display(Name = "I agree to the terms and conditions")]
        public bool AgreeToTerms { get; set; }
        
        [Display(Name = "Subscribe to event notifications")]
        public bool SubscribeToNewsletter { get; set; }
        
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;
        
        // Computed properties for display
        public string FullName => $"{FirstName} {LastName}".Trim();
        public decimal TotalCost => NumberOfAttendees * (Event?.Price ?? 0);
        
        // Navigation property
        public Event? Event { get; set; }
    }
    
    public enum RegistrationStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        WaitList
    }
}