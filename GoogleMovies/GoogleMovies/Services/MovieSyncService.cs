using GoogleMovies.Data;
using Microsoft.EntityFrameworkCore;


namespace GoogleMovies.Services
{

    public class TmdbTrendingResponse
    {
        public List<TmdbMovieResult> results { get; set; }
    }

    public class TmdbSearchResponse
    {
        public List<TmdbMovieResult> results { get; set; }
    }

    public class TmdbMovieResult
    {
        public int id { get; set; }
        public string title { get; set; }
        public string release_date { get; set; }
    }

    public class TmdbMovieDetails
    {
        public long? revenue { get; set; }
    }


    public class MovieSyncService
    {
        private readonly MovieDbContext _context;
        private readonly HttpClient _http;
        private const string ApiKey = "1a614e0bc6ebf1df8116d2113855fd29";
        private const string BaseUrl = "https://api.themoviedb.org/3";

        public MovieSyncService(MovieDbContext context, HttpClient http)
        {
            _context = context;
            _http = http;
        }

        private int ParseYear(string releaseDate)
        {
            return DateTime.TryParse(releaseDate, out var dt) ? dt.Year : 0;
        }

        public async Task SyncTrendingAndBoxOfficeAsync()
        {
            var trendingResponse = await _http.GetFromJsonAsync<TmdbTrendingResponse>(
                $"{BaseUrl}/trending/movie/week?api_key={ApiKey}");

            if (trendingResponse?.results != null)
            {
                foreach (var trending in trendingResponse.results)
                {
                    var localMovie = await _context.Movies
                        .FirstOrDefaultAsync(m => m.Title == trending.title && m.Year == ParseYear(trending.release_date));

                    if (localMovie != null)
                    {
                        bool needsUpdate = false;

                        // Update IsTrending only if different
                        if (!localMovie.IsTrending)
                        {
                            localMovie.IsTrending = true;
                            needsUpdate = true;
                        }

                        // Fetch movie details (for BoxOffice)
                        var details = await _http.GetFromJsonAsync<TmdbMovieDetails>(
                            $"{BaseUrl}/movie/{trending.id}?api_key={ApiKey}");

                        if (details != null)
                        {
                            var tmdbRevenue = (decimal)(details.revenue ?? 0L); // 0L is a long literal


                            if (localMovie.BoxOffice != tmdbRevenue)
                            {
                                localMovie.BoxOffice = tmdbRevenue;
                                needsUpdate = true;
                            }
                        }

                        if (needsUpdate)
                        {
                            localMovie.ModifiedDate = DateTime.UtcNow;
                        }
                    }
                }
            }

            // ✅ Step 2: Update BoxOffice for ALL local movies (skip ones already handled or unchanged)
            var allLocalMovies = await _context.Movies.ToListAsync();

            foreach (var localMovie in allLocalMovies)
            {
                // Skip if already updated in trending section
                bool alreadyTrending = trendingResponse?.results?.Any(
                    t => t.title == localMovie.Title && ParseYear(t.release_date) == localMovie.Year) == true;

                if (alreadyTrending)
                    continue;

                var searchResponse = await _http.GetFromJsonAsync<TmdbSearchResponse>(
                    $"{BaseUrl}/search/movie?api_key={ApiKey}&query={Uri.EscapeDataString(localMovie.Title)}&year={localMovie.Year}");

                var match = searchResponse?.results?.FirstOrDefault();
                if (match != null)
                {
                    var details = await _http.GetFromJsonAsync<TmdbMovieDetails>(
                        $"{BaseUrl}/movie/{match.id}?api_key={ApiKey}");

                    if (details != null)
                    {
                        var tmdbRevenue = (decimal)(details.revenue ?? 0L);

                        if (localMovie.BoxOffice != tmdbRevenue)
                        {
                            localMovie.BoxOffice = tmdbRevenue;
                            localMovie.ModifiedDate = DateTime.UtcNow;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }


    }

}
