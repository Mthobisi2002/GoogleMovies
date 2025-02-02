using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;


namespace GoogleMovies.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        // FK to IdentityUser
        public string IdentityUserId { get; set; }
        public IdentityUser IdentityUser { get; set; }

        public ICollection<WatchList> WatchLists { get; set; }

        // New property to be added
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; }
    }

}
