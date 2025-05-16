namespace GoogleMovies.Models
{
    public class HomeViewModel
    {
        public List<Movie> Trending { get; set; } // Trending
        public List<Movie> Blockbusters { get; set; } // BoxOffice >= 350 million
        public List<Movie> LatestMovies { get; set; } // Year = 2025
    }

}
