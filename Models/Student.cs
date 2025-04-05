using System.ComponentModel.DataAnnotations;

namespace FutureTech_Academy.Models
{
    public class Student
    {
        [Key]
        public string id { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required")]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enrolment status is required")]
        [Display(Name = "Enrolment Status")]
        public string EnrolmentStatus { get; set; } = string.Empty;

        [Display(Name = "Profile Image")]
        public string ProfileImageUrl { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;
    }
} 