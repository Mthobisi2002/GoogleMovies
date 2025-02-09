using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GoogleMovies.Models;
using GoogleMovies.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;



namespace GoogleMovies.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly MovieDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            MovieDbContext context, IEmailSender emailSender, ILogger<AccountController> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
            _configuration = configuration;
        }

        private string GenerateJwtToken(IdentityUser user)
        {
            var secretKey = _configuration["JwtSettings:SecretKey"];  // Retrieve from appsettings.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Get the roles for the user
            var roles = _userManager.GetRolesAsync(user).Result; // Fetch roles asynchronously

            // Add claims, including UserId and roles
            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Role, string.Join(",", roles)) // Add multiple roles if necessary
        };

            var token = new JwtSecurityToken(
                issuer: "YourIssuerHere",
                audience: "YourAudienceHere",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // Token expiration
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }






        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    // Check if passwords match
                    if (model.Password != model.ConfirmPassword)
                    {
                        ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
                        return View(model);
                    }

                    // Check if the email already exists in the AspNetUsers table
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "An account with this email already exists.");
                        return View(model);
                    }


                    var identityUser = new IdentityUser { UserName = model.Email, Email = model.Email };
                    var result = await _userManager.CreateAsync(identityUser, model.Password);

                    if (result.Succeeded)
                    {

                        // Save the user to the custom table
                        var customUser = new User
                        {
                            FullName = model.FullName, // Assuming FullName is part of the RegisterViewModel
                            Email = model.Email,
                            IdentityUserId = identityUser.Id,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now,
                            CreatedBy = identityUser.Id
                        };

                        _context.Users.Add(customUser); // Add the custom user to the database
                        await _context.SaveChangesAsync(); // Save changes to persist

                        // Assign the "Customer" role to the user
                        var roleResult = await _userManager.AddToRoleAsync(identityUser, "Customer");
                        if (!roleResult.Succeeded)
                        {
                            // Handle role assignment errors
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, $"Role assignment error: {error.Description}");
                            }

                            // Optional: You can decide to remove the user and custom data if role assignment fails
                        }

                        string token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);

                        string subject = "Welcome to Google Movies - Verify Your Email";
                        string body = $@"
                <h1>Welcome to Google Movies!</h1>
                <p>Your verification code is: <strong>{token}</strong></p>
                <p>Please enter this code on the verification page to confirm your email.</p>";

                        await _emailSender.SendEmailAsync(identityUser.Email, subject, body);

                        TempData["VerificationType"] = "Registration"; // Store verification type in TempData
                                                                       //TempData["Email"] = identityUser.Email; // Store email in TempData
                                                                       //TempData["Token"] = token; // Optional: You may want to store the token for debugging or testing purposes

                        return RedirectToAction("VerifyEmail", "Account");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception (optional: use a logging library)
                    TempData["Error"] = $"An error occurred while processing your request: {ex.Message}";
                    return View(model); // Keep the user on the same page
                }

            }

            return View(model);
        }



        // GET: /Account/VerifyEmail
        [HttpGet]
        public IActionResult VerifyEmail()
        {
            var model = new VerifyEmailViewModel
            {
                VerificationType = TempData["VerificationType"]?.ToString(), // Retrieve verification type from TempData
                Email = TempData["Email"]?.ToString(), // Pre-fill email if available
            };

            return View(model);
        }


        // POST: /Account/VerifyEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email address.");
                    return View(model);
                }

                IdentityResult result = null; // Initialize the variable

                if (model.VerificationType == "Registration")
                {
                    result = await _userManager.ConfirmEmailAsync(user, model.Token);
                    if (result.Succeeded)
                    {
                        TempData["Message"] = "Your email has been successfully verified!";
                        return RedirectToAction("Login", "Account");
                    }
                }
                else if (model.VerificationType == "ResetPassword")
                {
                    // Token verification only
                    var isValidToken = await _userManager.VerifyUserTokenAsync(
                        user,
                        _userManager.Options.Tokens.PasswordResetTokenProvider,
                        "ResetPassword",
                        model.Token);

                    if (isValidToken)
                    {
                        TempData["UserId"] = user.Id; // Store UserId in TempData
                        TempData["Token"] = model.Token;
                        return RedirectToAction("ResetPassword", "Account");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid or expired token.");
                    }
                }

                if (result != null)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (optional: use a logging library)
                TempData["Error"] = $"An error occurred while processing your request: {ex.Message}";
                return View(model); // Keep the user on the same page
            }

            return View(model);
        }





        // GET: /Account/Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                    if (result.Succeeded)
                    {
                        var user = await _userManager.FindByEmailAsync(model.Email);
                        if (user != null)
                        {
                            // Generate JWT token
                            var token = GenerateJwtToken(user);

                            // Set the token in an HttpOnly cookie
                            Response.Cookies.Append("AuthToken", token, new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true, // Ensures the cookie is sent over HTTPS
                                SameSite = SameSiteMode.Strict, // Prevents cross-site cookie usage
                                Expires = DateTime.UtcNow.AddHours(1) // Token expiration
                            });

                            // Redirect based on user role (admin or customer)
                            if (_userManager.IsInRoleAsync(user, "Admin").Result)
                            {
                                return RedirectToAction("ListMovies", "Admin");
                            }
                            else
                            {
                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }

                    // Add validation error for invalid credentials
                    ModelState.AddModelError(string.Empty, "Email or Password Invalid");
                }
                catch (Exception ex)
                {
                    // Log the exception (optional: use a logging library)
                    TempData["Error"] = $"An error occurred while processing your request: {ex.Message}";
                    return View(model); // Keep the user on the same page
                }

            }
            return View(model);
        }




        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken] // Prevent CSRF attacks
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            // Clear the HttpOnly AuthToken cookie
            Response.Cookies.Delete("AuthToken");

            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            // Retrieve from TempData if null
            userId ??= TempData["UserId"]?.ToString();
            token ??= TempData["Token"]?.ToString();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest("Invalid password reset token.");
            }

            var model = new ResetPasswordViewModel { UserId = userId, Token = token };
            return View(model);
        }


        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if the new password matches the confirm password
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("", "The new password and confirm password do not match.");
                    return View(model);
                }


                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return BadRequest("Invalid user.");
                }

                var resetResult = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                if (resetResult.Succeeded)
                {
                    TempData["Message"] = "Password reset successful! You can now log in.";
                    return RedirectToAction("Login", "Account");
                }

                //foreach (var error in resetResult.Errors)
                //{
                //    ModelState.AddModelError(string.Empty, error.Description);
                //}

            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                TempData["Error"] = $"An error occurred while processing your request: {ex.Message}";
            }
            return View(model);
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Security measure: don't reveal if the user or confirmation status
                    TempData["Message"] = "Invalid Email";
                    return View(model);
                }

                // Generate the password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Send the token via email
                string subject = "Password Reset Request";
                string body = $@"
        <h1>Password Reset</h1>
        <p>Your password reset verification code is: <strong>{token}</strong></p>
        <p>Please use this code to reset your password.</p>";

                await _emailSender.SendEmailAsync(user.Email, subject, body);

                TempData["VerificationType"] = "ResetPassword";
                TempData["Email"] = user.Email;

                return RedirectToAction("VerifyEmail", "Account");
            }
            catch (Exception ex)
            {
                // Log the exception (optional: log the error using a logging library)
                TempData["Error"] = $"An error occurred while processing your request: {ex.Message}";
                return View(model); // Keep the user on the same page
            }

        }




    }
}
