// Google / external-provider login callback page
#nullable disable
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MedLinkPortal.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser>  _userManager;
        private readonly IEmailSender                  _emailSender;
        private readonly ILogger<ExternalLoginModel>   _logger;
        private readonly ApplicationDbContext          _dbContext;
        private readonly RoleManager<IdentityRole>     _roleManager;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser>  userManager,
            IEmailSender                  emailSender,
            ILogger<ExternalLoginModel>   logger,
            ApplicationDbContext          dbContext,
            RoleManager<IdentityRole>     roleManager)
        {
            _signInManager = signInManager;
            _userManager   = userManager;
            _emailSender   = emailSender;
            _logger        = logger;
            _dbContext     = dbContext;
            _roleManager   = roleManager;
        }

        [BindProperty] public InputModel Input            { get; set; }
        public string                    ProviderDisplayName { get; set; }
        public string                    ReturnUrl           { get; set; }
        [TempData] public string         ErrorMessage        { get; set; }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; }

            [Phone]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }

            [Required]
            public string Role { get; set; } = "Patient";
        }

        // ── Step 1: redirect browser to Google ───────────────────────────
        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback",
                values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        // ── Step 2: Google redirects back here ───────────────────────────
        public async Task<IActionResult> OnGetCallbackAsync(
            string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Try sign-in with existing external login
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider}.",
                    info.Principal.Identity.Name, info.LoginProvider);
                return RedirectToLoginDestination(returnUrl);
            }

            if (result.IsLockedOut)
                return RedirectToPage("./Lockout");

            // First time — ask user for extra details
            ReturnUrl           = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;

            var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            Input = new InputModel { Email = email };
            return Page();
        }

        // ── Step 3: user submits the completion form ──────────────────────
        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (!ModelState.IsValid)
            {
                ReturnUrl           = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                return Page();
            }

            // Check if email already registered
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                // Link the external login to the existing account
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded || addLoginResult.Errors.All(e => e.Code == "LoginAlreadyAssociated"))
                {
                    await _signInManager.SignInAsync(existingUser, isPersistent: false, info.LoginProvider);
                    return RedirectToLoginDestination(returnUrl);
                }
                ModelState.AddModelError(string.Empty, "This Google account is already linked to a different email.");
                ReturnUrl           = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                return Page();
            }

            // Create new user
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName)
                         ?? Input.Email.Split('@')[0];
            var lastName  = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;

            var user = new ApplicationUser
            {
                UserName       = Input.Email,
                Email          = Input.Email,
                EmailConfirmed = true,            // Google already verified the email
                FirstName      = firstName,
                LastName       = lastName,
                PhoneNumber    = Input.PhoneNumber,
                ApprovalStatus = Input.Role == "Doctor" ? "Pending" : "Approved"
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                ReturnUrl           = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                return Page();
            }

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(Input.Role))
                await _roleManager.CreateAsync(new IdentityRole(Input.Role));

            await _userManager.AddToRoleAsync(user, Input.Role);

            // Doctor profile
            if (Input.Role == "Doctor")
            {
                _dbContext.Doctors.Add(new MedLinkPortal.Models.Doctor
                {
                    Name         = $"{firstName} {lastName}".Trim(),
                    Specialty    = "General Practice",
                    UserId       = user.Id,
                    Rating       = 0, Reviews = 0,
                    Availability = "Next Available: TBD",
                    Online       = false,
                    Experience   = "New",
                    Image        = "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&q=80&w=400",
                    Qualification = "TBD",
                    Description  = "New doctor profile pending verification.",
                    Languages    = "English"
                });
                await _dbContext.SaveChangesAsync();
            }

            // Link Google login
            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
            return RedirectToLoginDestination(returnUrl);
        }

        // ── Helper ────────────────────────────────────────────────────────
        private IActionResult RedirectToLoginDestination(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl) && returnUrl != "/")
                return LocalRedirect(returnUrl);
            return LocalRedirect("~/");
        }
    }
}
