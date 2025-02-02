using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GoogleMovies.Models
{
    public class AddMovieViewModel
    {
        // Use Guid instead of int for GenreIds
        public List<Guid> GenreIds { get; set; }
        public List<SelectListItem> GenreList { get; set; }

        // Cast Names and Images
        public List<string> CastNames { get; set; }
        public List<string> CastImages { get; set; }

        // Other movie details remain the same
        public string Title { get; set; }
        public int Year { get; set; }
        public int DurationMinutes { get; set; }
        public string AgeRating { get; set; }
        public decimal RottenTomatoesRating { get; set; }
        public decimal PriceRent { get; set; }
        public decimal PriceBuy { get; set; }
        public string Description { get; set; }
        public string TrailerUrl { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; }

    }



}
