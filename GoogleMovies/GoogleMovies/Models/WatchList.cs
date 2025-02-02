using System.Collections.Generic;


namespace GoogleMovies.Models
{
    public class WatchList
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        public Guid MovieId { get; set; }
        public Movie Movie { get; set; }

        public Boolean IsWatched { get; set; }

        // New property to be added
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; }
    }
}
