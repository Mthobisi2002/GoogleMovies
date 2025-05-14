using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GoogleMovies.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using GoogleMovies.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MovieDbContext for both application-specific and Identity-related data
builder.Services.AddDbContext<MovieDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity to use MovieDbContext
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<MovieDbContext>() // Use MovieDbContext for Identity
    .AddDefaultTokenProviders();

// Configure Cookie Authentication to validate JWT tokens
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                // Extract JWT token from the HttpOnly cookie
                var token = context.Request.Cookies["AuthToken"];
                if (string.IsNullOrEmpty(token))
                {
                    context.RejectPrincipal();
                    return;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("8d37463d2f6c96a9e45cbb30dba46154")), // Use your secure key
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = "YourIssuerHere", // Update with your actual issuer
                    ValidAudience = "YourAudienceHere", // Update with your actual audience
                    RoleClaimType = ClaimTypes.Role // Role claim for role-based authorization
                };

                try
                {
                    // Validate the token and replace the principal
                    var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                    context.ReplacePrincipal(principal);
                    context.ShouldRenew = true; // Renew cookie if validation succeeds
                }
                catch
                {
                    context.RejectPrincipal();
                }
            }
        };
    });

builder.Services.AddSession();
builder.Services.AddControllersWithViews()
        .AddSessionStateTempDataProvider();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSender>();
builder.Services.AddHttpClient<MovieSyncService, MovieSyncService>();


// Add Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSession();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DataSeeder.SeedRolesAndAdminUser(services);
}

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Enable authentication middleware
app.UseAuthorization(); // Enable authorization middleware


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");


app.Run();
