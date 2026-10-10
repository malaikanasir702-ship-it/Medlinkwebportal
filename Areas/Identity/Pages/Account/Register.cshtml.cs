// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace MedLinkPortal.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _dbContext;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _dbContext = dbContext;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "First name is required")]
            [Display(Name = "First Name")]
            [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Last name is required")]
            [Display(Name = "Last Name")]
            [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Phone number is required")]
            [Display(Name = "Phone Number")]
            [Phone(ErrorMessage = "Please enter a valid phone number")]
            [StringLength(15, ErrorMessage = "Phone number cannot be longer than 15 characters")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Role is required")]
            [Display(Name = "Role")]
            public string Role { get; set; }

            [Display(Name = "Specialty")]
            public string Specialty { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Dashboard/Doctors");
            if (!Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Content("~/Dashboard/Doctors");
            }

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                // Set additional properties
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.PhoneNumber = Input.PhoneNumber;
                user.Email = Input.Email;
                user.UserName = Input.Email;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Ensure roles exist
                    if (!await _roleManager.RoleExistsAsync("Patient"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Patient"));
                        _logger.LogInformation("Created 'Patient' role.");
                    }
                    if (!await _roleManager.RoleExistsAsync("Doctor"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Doctor"));
                        _logger.LogInformation("Created 'Doctor' role.");
                    }

                    // Assign role to user
                    if (!string.IsNullOrEmpty(Input.Role))
                    {
                        // Validate role
                        if (Input.Role.ToLower() == "patient" || Input.Role.ToLower() == "doctor")
                        {
                            var roleName = Input.Role.ToLower() == "patient" ? "Patient" : "Doctor";
                            var roleResult = await _userManager.AddToRoleAsync(user, roleName);

                            if (!roleResult.Succeeded)
                            {
                                _logger.LogError($"Failed to assign role '{roleName}' to user.");
                                foreach (var error in roleResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                                // Clean up: delete the user if role assignment fails
                                await _userManager.DeleteAsync(user);
                                return Page();
                            }

                            _logger.LogInformation($"Successfully assigned role '{roleName}' to user {user.Email}.");

                            // If Doctor, create doctor record
                            if (roleName == "Doctor")
                            {
                                user.ApprovalStatus = "Pending";
                                user.Specialist = Input.Specialty;
                                await _userManager.UpdateAsync(user);

                                var doctor = new MedLinkPortal.Models.Doctor
                                {
                                    Name = $"{Input.FirstName} {Input.LastName}",
                                    Specialty = Input.Specialty ?? "General Practice",
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

                                _dbContext.Doctors.Add(doctor);
                                await _dbContext.SaveChangesAsync();
                                _logger.LogInformation($"Created Doctor profile for {user.Email}.");
                            }
                            else
                            {
                                user.ApprovalStatus = "Approved"; // Patients are auto-approved
                                await _userManager.UpdateAsync(user);
                            }

                            // Verify role assignment
                            var assignedRoles = await _userManager.GetRolesAsync(user);
                            _logger.LogInformation($"User {user.Email} now has roles: {string.Join(", ", assignedRoles)}");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Invalid role selected.");
                            await _userManager.DeleteAsync(user);
                            return Page();
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Please select a role.");
                        await _userManager.DeleteAsync(user);
                        return Page();
                    }

                    // Send email confirmation
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    try
                    {
                        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        _logger.LogInformation("Confirmation email sent to " + Input.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send confirmation email.");
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        // await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToPage("Login", new { returnUrl = returnUrl });
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}