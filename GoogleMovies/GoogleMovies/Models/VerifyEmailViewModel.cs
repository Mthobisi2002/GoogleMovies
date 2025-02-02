using System.ComponentModel.DataAnnotations;

namespace GoogleMovies.Models
{
    public class VerifyEmailViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public string VerificationType { get; set; } // "Registration" or "ResetPassword"
    }

}
