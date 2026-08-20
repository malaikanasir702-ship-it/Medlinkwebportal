using Microsoft.AspNetCore.Authorization;
using MedLinkPortal.Models;
using MedLinkPortal.Models.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MedLinkPortal.Controllers.Api
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
            if (await _userManager.FindByEmailAsync(request.Email) != null)
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Email already exists" });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                ApprovalStatus = request.Role == "Doctor" ? "Pending" : "Approved"
            };

            if (request.Role == "Doctor")
            {
                user.Specialist = request.Specialty;
            }

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, request.Role);

                if (request.Role == "Doctor")
                {
                    // Create doctor record
                    var doctor = new MedLinkPortal.Models.Doctor
                    {
                        Name = $"{request.FirstName} {request.LastName}",
                        Specialty = request.Specialty ?? "General Practice",
                        UserId = user.Id,
                        Rating = 0,
                        Reviews = 0,
                        Availability = "Next Available: TBD",
                        Online = false,
                        Experience = "New",
                        Image = "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&q=80&w=400",
                        Qualification = "TBD",
                        Description = "New doctor profile pending verification.",
                        Languages = "English"
                    };

                    using (var scope = HttpContext.RequestServices.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<MedLinkPortal.Models.ApplicationDbContext>();
                        context.Doctors.Add(doctor);
                        await context.SaveChangesAsync();
                    }
                }

                // Auto-login after registration or just return success
                return await Login(new LoginRequest { Email = request.Email, Password = request.Password });
            }

            return BadRequest(new AuthResponse 
            { 
                Success = false, 
                Message = string.Join(", ", result.Errors.Select(e => e.Description)) 
            });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse { Success = false, Message = "Registration failed. Please try again." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? ""));

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    expires: DateTime.Now.AddMinutes(double.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "1440")),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                // Record Session
                try
                {
                    var userAgent = Request.Headers["User-Agent"].ToString();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    
                    var session = new UserSession
                    {
                        UserId = user.Id,
                        UserAgent = userAgent,
                        IPAddress = ipAddress,
                        DeviceName = userAgent.Contains("Postman") ? "API Client" : (userAgent.Contains("iPhone") ? "iPhone" : (userAgent.Contains("Android") ? "Android Device" : "Web Browser")),
                        LoginTime = DateTime.UtcNow,
                        LastSeen = DateTime.UtcNow,
                        SessionIdentifier = Guid.NewGuid().ToString()
                    };
                    
                    _context.UserSessions.Add(session);
                    await _context.SaveChangesAsync();
                }
                catch { /* Log error or ignore to not block login */ }

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expiration = token.ValidTo,
                    Email = user.Email,
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    UserName = user.UserName,
                    Role = userRoles.FirstOrDefault(),
                    Message = "Authentication successful"
                });
            }

            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid credentials"
            });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse { Success = false, Message = "Login failed. Please try again." });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("update-fcm-token")]
        public async Task<IActionResult> UpdateFcmToken([FromBody] FcmTokenUpdateModel model)
        {
            try
            {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.FcmToken = model.Token;
            await _userManager.UpdateAsync(user);

            return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Failed to update FCM token." });
            }
        }

        public class FcmTokenUpdateModel { public string Token { get; set; } = string.Empty; }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("current-user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                UserId = user.Id,
                Email = user.Email,
                Role = roles.FirstOrDefault(),
                UserName = user.UserName,
                FirstName = user.FirstName
            });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Failed to retrieve user." });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Ok(new AuthResponse { Success = true, Message = "If your email is registered, you will receive an OTP." });
            }

            var otp = new Random().Next(100000, 999999).ToString();
            user.VerificationCode = otp;
            user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
            await _userManager.UpdateAsync(user);

            var emailBody = $@"
                <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                    <h2 style='color: #2563eb;'>MedLink Password Reset</h2>
                    <p>You requested a password reset. Use the following OTP to verify your identity:</p>
                    <div style='background: #f3f4f6; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; border-radius: 5px; color: #1e40af;'>
                        {otp}
                    </div>
                    <p>This code will expire in 15 minutes.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='font-size: 12px; color: #888;'>If you didn't request this, please ignore this email.</p>
                </div>";

            await _emailSender.SendEmailAsync(user.Email, "MedLink: Password Reset OTP", emailBody);

            return Ok(new AuthResponse { Success = true, Message = "OTP sent to your email." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse { Success = false, Message = "Failed to process password reset. Please try again." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || user.VerificationCode != request.OTP || user.VerificationCodeExpiry < DateTime.UtcNow)
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Invalid or expired OTP." });
            }

            user.VerificationCode = null;
            user.VerificationCodeExpiry = null;
            await _userManager.UpdateAsync(user);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new AuthResponse { Success = true, Message = "Password reset successfully. You can now login." });
            }

            return BadRequest(new AuthResponse 
            { 
                Success = false, 
                Message = string.Join(", ", result.Errors.Select(e => e.Description)) 
            });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse { Success = false, Message = "Password reset failed. Please try again." });
            }
        }
    }
}
