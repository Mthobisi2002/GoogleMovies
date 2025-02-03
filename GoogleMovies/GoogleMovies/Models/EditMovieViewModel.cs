using Microsoft.AspNetCore.Mvc.Rendering;

namespace GoogleMovies.Models
{
    public class EditMovieViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public int DurationMinutes { get; set; }
        public string AgeRating { get; set; }
        public decimal RottenTomatoesRating { get; set; }
        public decimal PriceRent { get; set; }
        public decimal PriceBuy { get; set; }
        public string TrailerUrl { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public List<Guid> GenreIds { get; set; } = new List<Guid>(); // ✅ Ensure it's always initialized
        public List<string> CastNames { get; set; } = new List<string>(); // ✅ No more null errors
        public List<string> CastImages { get; set; } = new List<string>(); // ✅ Avoid null errors
        public List<SelectListItem> GenreList { get; set; } = new List<SelectListItem>(); // ✅ Always initialized
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; }
    }

}
