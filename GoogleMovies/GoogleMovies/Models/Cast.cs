using System.Collections.Generic;


namespace GoogleMovies.Models
{
    public class Cast
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }

        public ICollection<MovieCast> MovieCast { get; set; }

        // New property to be added
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string CreatedBy { get; set; }


        // New columns 13/05/2025
        public bool IsTrending { get; set; }
    }
}
