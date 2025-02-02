using System.ComponentModel.DataAnnotations;

namespace GoogleMovies.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

}
