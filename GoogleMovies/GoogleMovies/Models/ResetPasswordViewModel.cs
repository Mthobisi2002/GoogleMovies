using System.ComponentModel.DataAnnotations;

namespace GoogleMovies.Models
{
    public class ResetPasswordViewModel
    {
        public string? UserId { get; set; }

        public string? Token { get; set; }

        [Required(ErrorMessage = "The new password is required.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "The password must be at least 8 characters long, and include at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "The confirm password is required.")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
