//using GoogleMovies.Data;
//using GoogleMovies.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using System.Linq;

//namespace GoogleMovies.Controllers
//{
//    public class MoviesController : Controller
//    {
//        private readonly MovieDbContext _context;

//        public MoviesController(MovieDbContext context)
//        {
//            _context = context;
//        }

//        [HttpGet]
//        public IActionResult Add()
//        {
//            var genres = _context.Genres.ToList(); // Get genres from the database
//            var viewModel = new AddMovieViewModel
//            {
//                GenreList = genres.Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Name }).ToList(),
//                CastNames = new List<string>() // Initialize CastNames to avoid null reference
//            };

//            return View(viewModel); // Make sure to pass the viewModel to the view
//        }


//        [HttpPost]
//        [Authorize(Roles = "Admin")]
//        public IActionResult Add(AddMovieViewModel model)
//        {
//            if (!ModelState.IsValid)
//            {
//                // Reload GenreList if model validation fails
//                model.GenreList = _context.Genres.Select(g => new SelectListItem
//                {
//                    Value = g.Id.ToString(),
//                    Text = g.Name
//                }).ToList();
//                return View(model);
//            }

//            // Create and save the Movie
//            var movie = new Movie
//            {
//                Title = model.Title,
//                Year = model.Year,
//                DurationMinutes = model.DurationMinutes,
//                AgeRating = model.AgeRating,
//                RottenTomatoesRating = model.RottenTomatoesRating,
//                PriceRent = model.PriceRent,
//                PriceBuy = model.PriceBuy,
//                Description = model.Description,
//                TrailerUrl = model.TrailerUrl,
//                ImageUrl = model.ImageUrl
//            };

//            _context.Movies.Add(movie);
//            _context.SaveChanges();

//            // Save selected genres to MovieGenres table
//            if (model.GenreIds != null)
//            {
//                foreach (var genreId in model.GenreIds)
//                {
//                    _context.MovieGenres.Add(new MovieGenre
//                    {
//                        MovieId = movie.Id, // Ensure movie.Id is a Guid
//                        GenreId = new Guid(genreId.ToString()) // Convert int to Guid if needed
//                    });
//                }
//            }

//            // Save cast members to MovieCasts table
//            if (model.CastNames != null)
//            {
//                foreach (var castName in model.CastNames.Take(3)) // Limit to 3 cast members
//                {
//                    // Check if cast already exists; add if it doesn't
//                    var cast = _context.Cast.FirstOrDefault(c => c.Name == castName) ?? new Cast
//                    {
//                        Name = castName,
//                        ImageUrl = model.CastImages.FirstOrDefault() // Optional, for image URLs
//                    };

//                    if (cast.Id == Guid.Empty)
//                    {
//                        _context.Cast.Add(cast);
//                        _context.SaveChanges(); // Ensure cast.Id gets a value
//                    }

//                    _context.MovieCasts.Add(new MovieCast
//                    {
//                        MovieId = movie.Id, // Ensure movie.Id is a Guid
//                        CastId = cast.Id // Ensure cast.Id is a Guid
//                    });
//                }

//            }

//            _context.SaveChanges();

//            return RedirectToAction("Add", "Admin");
//        }
//    }
//}
