using GoogleMovies.Data;
using GoogleMovies.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;



namespace GoogleMovies.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly MovieDbContext _context;


        public AdminController(MovieDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")] // Restrict access to admins only
        [HttpGet]
        public IActionResult Add()
        {
            var genres = _context.Genres.ToList(); // Get genres from the database
            var viewModel = new AddMovieViewModel
            {
                GenreList = genres.Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Name }).ToList(),
                CastNames = new List<string>()
            };

            return View(viewModel);
        }


        //first change in git repo
        [HttpPost]
        public IActionResult Add(AddMovieViewModel model)
        {
            var errors = new List<string>();

            try
            {
                // Validate individual fields
                if (string.IsNullOrWhiteSpace(model.Title))
                    errors.Add("Title cannot be empty.");

                if (model.Year <= 0)
                    errors.Add("Year is required and must be a positive number.");

                if (model.DurationMinutes <= 0)
                    errors.Add("Duration is required and must be a positive number.");

                if (string.IsNullOrWhiteSpace(model.AgeRating))
                    errors.Add("Age Rating cannot be empty.");

                if (model.RottenTomatoesRating < 0 || model.RottenTomatoesRating > 100)
                    errors.Add("Rotten Tomatoes Rating must be between 0 and 100.");

                if (model.PriceRent < 0)
                    errors.Add("Price for Rent is required and must be a positive number.");

                if (model.PriceBuy < 0)
                    errors.Add("Price for Buy is required and must be a positive number.");

                if (string.IsNullOrWhiteSpace(model.TrailerUrl))
                    errors.Add("Movie Trailer URL cannot be empty.");

                if (string.IsNullOrWhiteSpace(model.ImageUrl))
                    errors.Add("Movie Image URL cannot be empty.");

                if (string.IsNullOrWhiteSpace(model.Description))  // Add validation for Description
                    errors.Add("Description cannot be empty.");

                // Validate CastNames
                if (model.CastNames == null || model.CastNames.Count < 1)
                {
                    errors.Add("At least one cast member must be added.");
                }
                else
                {
                    // Trim whitespace and remove empty entries
                    model.CastNames = model.CastNames
                        .Select(c => c?.Trim())
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToList();

                    if (model.CastNames.Count < 1)
                    {
                        errors.Add("At least one valid cast member must be added.");
                    }
                    else if (model.CastNames.Count > 3)
                    {
                        errors.Add("You can only add up to 3 cast members.");
                    }
                }

                // Check if there are validation errors
                if (errors.Any())
                {
                    TempData["ValidationErrors"] = JsonConvert.SerializeObject(errors); // Store validation errors
                    return ReloadAddMovieView(model); // Return the view with the same model to allow correction
                }

                // Create and save the Movie
                var userId = _userManager.GetUserId(User);
                var movie = new Movie
                {
                    Title = model.Title,
                    Year = model.Year,
                    DurationMinutes = model.DurationMinutes,
                    AgeRating = model.AgeRating,
                    RottenTomatoesRating = model.RottenTomatoesRating,
                    PriceRent = model.PriceRent,
                    PriceBuy = model.PriceBuy,
                    Description = model.Description ?? string.Empty, // Ensure Description is not null
                    TrailerUrl = model.TrailerUrl,
                    ImageUrl = model.ImageUrl,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    CreatedBy = userId

                };

                _context.Movies.Add(movie);
                _context.SaveChanges();

                // Save selected genres to MovieGenres table
                if (model.GenreIds != null)
                {
                    foreach (var genreId in model.GenreIds)
                    {
                        _context.MovieGenres.Add(new MovieGenre
                        {
                            MovieId = movie.Id,
                            GenreId = new Guid(genreId.ToString())
                        });
                    }
                }

                // Save cast members to MovieCasts table
                if (model.CastNames != null)
                {
                    for (int i = 0; i < model.CastNames.Count; i++)
                    {
                        var castName = model.CastNames[i].Trim(); // Ensure no leading/trailing spaces
                        var castImageUrl = model.CastImages != null && i < model.CastImages.Count && !string.IsNullOrWhiteSpace(model.CastImages[i])
                            ? model.CastImages[i].Trim()
                            : "https://cdn.pixabay.com/photo/2015/10/05/22/37/blank-profile-picture-973460_1280.png"; // Default image if none provided

                        // Check if cast already exists in the database
                        var cast = _context.Cast.FirstOrDefault(c => c.Name == castName);

                        if (cast == null)
                        {
                            // Cast does not exist, create new
                            cast = new Cast
                            {
                                Name = castName,
                                ImageUrl = castImageUrl,
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = userId
                            };
                            _context.Cast.Add(cast);
                            _context.SaveChanges(); // Save to get the ID
                        }
                        else if (string.IsNullOrWhiteSpace(cast.ImageUrl))
                        {
                            // If cast exists but ImageUrl is null, update it
                            cast.ImageUrl = castImageUrl;
                            cast.ModifiedDate = DateTime.Now;
                            cast.CreatedBy = userId;
                            _context.Cast.Update(cast);
                            _context.SaveChanges();
                        }

                        // Now add to MovieCasts table
                        _context.MovieCasts.Add(new MovieCast
                        {
                            MovieId = movie.Id,
                            CastId = cast.Id
                        });
                    }
                }


                _context.SaveChanges();

                TempData["Success"] = "Movie added successfully.";
                return RedirectToAction("ListMovies", "Admin"); // Redirect to movie list page
            }
            catch (Exception ex)
            {
                var errorList = new List<string> { $"An error occurred while adding the movie: {ex.Message}" };
                TempData["ExceptionErrors"] = JsonConvert.SerializeObject(errorList); // Store exception errors
                return ReloadAddMovieView(model);
            }
        }


        // Reload the Add Movie View
        private IActionResult ReloadAddMovieView(AddMovieViewModel model)
        {
            model.GenreList = _context.Genres.Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            }).ToList();

            return View("Add", model);
        }

        [Authorize(Roles = "Admin")] // Restrict access to admins only
        [HttpGet]
        public IActionResult ListMovies()
        {
            var movies = _context.Movies
                .Include(m => m.MovieGenres) // Assuming a many-to-many relationship
                .ThenInclude(mg => mg.Genre)
                .ToList();
            return View(movies);
        }

        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            var movie = _context.Movies.Find(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                _context.SaveChanges();
            }
            return RedirectToAction("ListMovies");
        }

        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            // Fetch the movie along with its casts
            var movie = _context.Movies
                .Include(m => m.MovieCasts) // Include MovieCasts
                .ThenInclude(mc => mc.Cast) // Include Cast details
                .FirstOrDefault(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            // Create the EditMovieViewModel
            var model = new EditMovieViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Year = movie.Year,
                DurationMinutes = movie.DurationMinutes,
                AgeRating = movie.AgeRating,
                RottenTomatoesRating = movie.RottenTomatoesRating,
                PriceRent = movie.PriceRent,
                PriceBuy = movie.PriceBuy,
                Description = movie.Description,
                TrailerUrl = movie.TrailerUrl,
                ImageUrl = movie.ImageUrl,

                CastNames = movie.MovieCasts?.Select(mc => mc.Cast?.Name).ToList() ?? new List<string>(),
                CastImages = movie.MovieCasts?.Select(mc => mc.Cast?.ImageUrl).ToList() ?? new List<string>(),

                // No pre-selection of genres, just populate the genre dropdown
                Genres = _context.Genres.Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(), // Populate with genre name instead of ID
                    Text = g.Name   // Display genre name in the dropdown
                }).ToList()
            };


            // Ensure there are exactly 3 slots for CastNames and CastImages
            while (model.CastNames.Count < 3)
            {
                model.CastNames.Add(string.Empty);
            }

            while (model.CastImages.Count < 3)
            {
                model.CastImages.Add(string.Empty);
            }

            return View(model);
        }



        [HttpPost]
        public IActionResult Edit(EditMovieViewModel model)
        {
            var errors = new List<string>();

            try
            {
                // Validate individual fields
                if (string.IsNullOrWhiteSpace(model.Title))
                    errors.Add("Title cannot be empty.");

                if (model.Year <= 0)
                    errors.Add("Year is required and must be a positive number.");

                if (model.DurationMinutes <= 0)
                    errors.Add("Duration is required and must be a positive number.");

                if (string.IsNullOrWhiteSpace(model.AgeRating))
                    errors.Add("Age Rating cannot be empty.");

                if (model.RottenTomatoesRating < 0 || model.RottenTomatoesRating > 100)
                    errors.Add("Rotten Tomatoes Rating must be between 0 and 100.");

                if (model.PriceRent < 0)
                    errors.Add("Price for Rent is required and must be a positive number.");

                if (model.PriceBuy < 0)
                    errors.Add("Price for Buy is required and must be a positive number.");

                if (string.IsNullOrWhiteSpace(model.TrailerUrl))
                    errors.Add("Movie Trailer URL cannot be empty.");

                if (string.IsNullOrWhiteSpace(model.ImageUrl))
                    errors.Add("Movie Image URL cannot be empty.");

                if (string.IsNullOrWhiteSpace(model.Description)) // Add validation for Description
                    errors.Add("Description cannot be empty.");

                // Validate CastNames
                if (model.CastNames == null || model.CastNames.Count < 1)
                {
                    errors.Add("At least one cast member must be added.");
                }
                else
                {
                    // Trim whitespace and remove empty entries
                    model.CastNames = model.CastNames
                        .Select(c => c?.Trim())
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToList();


                    if (model.CastNames.Count != model.CastImages.Count)
                    {
                        errors.Add("The number of Cast Names must match the number of Cast Images.");
                    }

                    if (model.CastNames.Count < 1)
                    {
                        errors.Add("At least one valid cast member must be added.");
                    }
                    else if (model.CastNames.Count > 3)
                    {
                        errors.Add("You can only add up to 3 cast members.");
                    }
                }


                // Check if there are validation errors
                if (errors.Any())
                {
                    TempData["ValidationErrors"] = JsonConvert.SerializeObject(errors); // Store validation errors
                    return ReloadEditMovieView(model); // Return the view with the same model to allow correction
                }

                // Fetch the movie from the database
                var movie = _context.Movies
                    .Include(m => m.MovieGenres)
                    .Include(m => m.MovieCasts)
                    .FirstOrDefault(m => m.Id == model.Id);

                if (movie == null)
                {
                    errors.Add("Movie not found.");
                    TempData["ValidationErrors"] = JsonConvert.SerializeObject(errors);
                    return RedirectToAction("ListMovies"); // Redirect if movie doesn't exist
                }

                // Update movie details
                var userId = _userManager.GetUserId(User);
                movie.Title = model.Title;
                movie.Year = model.Year;
                movie.DurationMinutes = model.DurationMinutes;
                movie.AgeRating = model.AgeRating;
                movie.RottenTomatoesRating = model.RottenTomatoesRating;
                movie.PriceRent = model.PriceRent;
                movie.PriceBuy = model.PriceBuy;
                movie.Description = model.Description ?? string.Empty;
                movie.TrailerUrl = model.TrailerUrl;
                movie.ImageUrl = model.ImageUrl;
                movie.ModifiedDate = DateTime.Now;


                movie.MovieGenres.Clear();
                // Add selected genres
                if (model.GenreIds != null && model.GenreIds.Any())
                {
                    foreach (var genreId in model.GenreIds)
                    {
                        movie.MovieGenres.Add(new MovieGenre
                        {
                            MovieId = movie.Id,
                            GenreId = genreId
                        });
                    }
                }

                // Update cast members
                movie.MovieCasts.Clear();
                if (model.CastNames != null)
                {
                    for (int i = 0; i < model.CastNames.Count; i++)
                    {
                        var castName = model.CastNames[i].Trim();
                        var castImageUrl = model.CastImages != null && i < model.CastImages.Count && !string.IsNullOrWhiteSpace(model.CastImages[i])
                            ? model.CastImages[i].Trim()
                            : "https://cdn.pixabay.com/photo/2015/10/05/22/37/blank-profile-picture-973460_1280.png"; // Default image if none provided

                        // Check if cast already exists in the database
                        var cast = _context.Cast.FirstOrDefault(c => c.Name == castName);

                        if (cast == null)
                        {
                            // Cast does not exist, create new
                            cast = new Cast
                            {
                                Name = castName,
                                ImageUrl = castImageUrl,
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = userId
                            };
                            _context.Cast.Add(cast);
                            _context.SaveChanges(); // Save to get the ID
                        }
                        else if (string.IsNullOrWhiteSpace(cast.ImageUrl))
                        {
                            // If cast exists but ImageUrl is null, update it
                            cast.ImageUrl = castImageUrl;
                            cast.ModifiedDate = DateTime.Now;
                            cast.CreatedBy = userId;
                            _context.Cast.Update(cast);
                            _context.SaveChanges();
                        }

                        // Now add to MovieCasts table
                        movie.MovieCasts.Add(new MovieCast
                        {
                            MovieId = movie.Id,
                            CastId = cast.Id
                        });
                    }
                }

                _context.SaveChanges();

                TempData["Success"] = "Movie updated successfully.";
                return RedirectToAction("ListMovies", "Admin"); // Redirect to movie list page
            }
            catch (Exception ex)
            {
                var errorList = new List<string> { $"An error occurred while editing the movie: {ex.Message}" };
                TempData["ExceptionErrors"] = JsonConvert.SerializeObject(errorList); // Store exception errors
                return ReloadEditMovieView(model);
            }
        }





        private IActionResult ReloadEditMovieView(EditMovieViewModel model)
        {
            // Populate the Genres dropdown
            model.Genres = _context.Genres.Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            }).ToList();

            return View("Edit", model);
        }



    }

}