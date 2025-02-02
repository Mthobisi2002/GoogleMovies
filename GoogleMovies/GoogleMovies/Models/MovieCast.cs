using System.Collections.Generic;


namespace GoogleMovies.Models
{
    public class MovieCast
    {
        public Guid MovieId { get; set; }
        public Movie Movie { get; set; }

        public Guid CastId { get; set; }
        public Cast Cast { get; set; }

    }
}
