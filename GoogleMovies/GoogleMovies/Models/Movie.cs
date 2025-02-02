using System.Collections.Generic;


namespace GoogleMovies.Models
{
    public class Movie
    {
        public Guid Id { get; set; }
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

        public ICollection<MovieGenre> MovieGenres { get; set; }
        public ICollection<MovieCast> MovieCasts { get; set; }

        // New property to be added
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; }
    }
}
