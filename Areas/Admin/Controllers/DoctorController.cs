using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Admin.Models;
using MedLinkPortal.Areas.Identity.Pages.Account;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DoctorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<IActionResult> Index()
        {
            // Get all users in the "Doctor" role
            var doctorsInRole = await _userManager.GetUsersInRoleAsync("Doctor");
            var doctorEntities = _context.Doctors.ToList();

            var doctors = doctorsInRole.Select(user =>
            {
                var entity = doctorEntities.FirstOrDefault(d => d.UserId == user.Id);
                return new Physician
                {
                    Id = user.Id, // Use Identity ID as the robust unique handle
                    Name = entity?.Name ?? $"{user.FirstName} {user.LastName}",
                    Specialty = entity?.Specialty ?? user.Specialist ?? "General Practice",
                    Experience = (entity != null && int.TryParse(entity.Experience, out int exp)) ? exp : 0,
                    Email = user.Email,
                    Phone = user.PhoneNumber ?? "N/A",
                    Office = entity?.ClinicAddress ?? user.Workplace ?? "Main Clinic",
                    Bio = entity?.Description ?? "New doctor profile pending verification.",
                    ProfileImage = entity?.Image ?? "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&q=80&w=400",
                    UserId = user.Id,
                    ApprovalStatus = user.ApprovalStatus ?? "Pending",
                    PmdcRegistrationNumber = entity?.PmdcRegistrationNumber ?? user.PMDCRegistrationNumber
                };
            }).ToList();

            return View(doctors);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Physician physician)
        {
            if (physician == null) return BadRequest(new { message = "Invalid physician data" });
            
            if (string.IsNullOrEmpty(physician.Name) || string.IsNullOrEmpty(physician.Specialty) || string.IsNullOrEmpty(physician.Email))
            {
                 return BadRequest(new { message = "Name, Specialty, and Email are required." });
            }

            if (string.IsNullOrEmpty(physician.Password))
            {
                return BadRequest(new { message = "Password is required for registration." });
            }

            // 1. Create Identity User
            var user = new ApplicationUser
            {
                UserName = physician.Email,
                Email = physician.Email,
                FirstName = physician.Name.Split(' ').FirstOrDefault() ?? "Doctor",
                LastName = physician.Name.Contains(' ') ? physician.Name.Split(' ').Last() : "User",
                EmailConfirmed = true,
                PhoneNumber = physician.Phone,
                Specialist = physician.Specialty,
                Workplace = physician.Office,
                ApprovalStatus = "Approved", // Admin-created doctors are auto-approved
                PMDCRegistrationNumber = physician.PmdcRegistrationNumber
            };

            var userResult = await _userManager.CreateAsync(user, physician.Password);
            if (!userResult.Succeeded)
            {
                var errors = userResult.Errors.Select(e => e.Description);
                return BadRequest(new { message = "Identity creation failed", errors = errors });
            }

            // 2. Assign Doctor Role
            if (!await _roleManager.RoleExistsAsync("Doctor"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Doctor"));
            }
            await _userManager.AddToRoleAsync(user, "Doctor");

            // 3. Create core Doctor entity
            var newDoctor = new MedLinkPortal.Models.Doctor
            {
                Name = physician.Name,
                Specialty = physician.Specialty,
                Experience = physician.Experience.ToString(),
                Description = physician.Bio ?? "Medical Professional",
                Image = physician.ProfileImage,
                ClinicAddress = physician.Office,
                PmdcRegistrationNumber = physician.PmdcRegistrationNumber,
                UserId = user.Id, // Link to Identity User
                
                Rating = 5.0,
                Reviews = 0,
                Availability = "Available",
                Online = true,
                Languages = "English",
                Qualification = "MBBS",
                HospitalAffiliations = "MedLink Hospital"
            };

            _context.Doctors.Add(newDoctor);
            await _context.SaveChangesAsync();
            
            physician.Id = user.Id; // Consistent handle
            physician.UserId = user.Id;
            
            return Ok(physician);
        }

        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] Physician physician)
        {
            try 
            {
                if (physician == null || string.IsNullOrEmpty(physician.Id)) 
                    return BadRequest(new { message = "Invalid physician data" });

                // 1. Update Identity User (The primary handle)
                var user = await _userManager.FindByIdAsync(physician.Id);
                if (user == null) return NotFound(new { message = "Doctor account not found." });

                user.Email = physician.Email;
                user.UserName = physician.Email;
                user.PhoneNumber = physician.Phone;
                user.Specialist = physician.Specialty;
                user.Workplace = physician.Office;
                user.FirstName = physician.Name.Split(' ').FirstOrDefault() ?? "Doctor";
                user.LastName = physician.Name.Contains(' ') ? physician.Name.Split(' ').Last() : "User";
                user.PMDCRegistrationNumber = physician.PmdcRegistrationNumber;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return BadRequest(new { message = "Failed to update security credentials", errors = updateResult.Errors.Select(e => e.Description) });
                }

                // Optional Password Update
                if (!string.IsNullOrEmpty(physician.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetResult = await _userManager.ResetPasswordAsync(user, token, physician.Password);
                    if (!resetResult.Succeeded)
                    {
                        return BadRequest(new { message = "Failed to update password", errors = resetResult.Errors.Select(e => e.Description) });
                    }
                }

                // 2. Update/Create Core Entity
                var existingDoc = _context.Doctors.FirstOrDefault(p => p.UserId == user.Id);
                if (existingDoc == null)
                {
                    // Create if missing
                    existingDoc = new MedLinkPortal.Models.Doctor { UserId = user.Id, Rating = 5.0, Availability = "Available" };
                    _context.Doctors.Add(existingDoc);
                }

                existingDoc.Name = physician.Name;
                existingDoc.Specialty = physician.Specialty;
                existingDoc.Experience = physician.Experience.ToString();
                existingDoc.ClinicAddress = physician.Office;
                existingDoc.Description = physician.Bio;
                existingDoc.PmdcRegistrationNumber = physician.PmdcRegistrationNumber;
                if (!string.IsNullOrEmpty(physician.ProfileImage))
                {
                    existingDoc.Image = physician.ProfileImage;
                }
                
                await _context.SaveChangesAsync();
                return Ok(physician);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            try 
            {
                if (string.IsNullOrEmpty(id)) return BadRequest(new { message = "Doctor ID is required" });

                // 1. Find and Remove Identity User
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound(new { message = "Doctor account not found." });

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Failed to remove security credentials.", errors = result.Errors.Select(e => e.Description) });
                }

                // 2. Remove Doctor Entity if exists
                var doctor = _context.Doctors.FirstOrDefault(p => p.UserId == id);
                if (doctor != null)
                {
                     _context.Doctors.Remove(doctor);
                     await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Could not delete doctor: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new MedLinkPortal.Areas.Doctor.Controllers.ProfileViewModel
            {
                Name = user.Name ?? user.UserName ?? "Doctor",
                Email = user.Email ?? "",
                Specialist = user.Specialist ?? "",
                Experience = user.Experience ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                ConsultationFee = user.ConsultationFee,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Workplace = user.Workplace,
                ApprovalStatus = user.ApprovalStatus ?? "Pending",
                
                // Personal
                FatherHusbandName = user.FatherHusbandName,
                Gender = user.Gender,
                CNIC = user.CNIC,
                ResidentialAddress = user.ResidentialAddress,
                City = user.City,
                Province = user.Province,

                // Professional
                PMDCRegistrationNumber = user.PMDCRegistrationNumber,
                PMDCValidityDate = user.PMDCValidityDate,
                Qualification = user.Qualification,
                BankAccountNumber = user.BankAccountNumber,
                TermsConsent = user.TermsConsent,

                // Documents
                CNICFrontUrl = user.CNICFrontUrl,
                CNICBackUrl = user.CNICBackUrl,
                PMDCCertificateUrl = user.PMDCCertificateUrl,
                DegreeCertificateUrl = user.DegreeCertificateUrl,
                
                // Security linkage
                NewPassword = user.Id // Hiring Id here for mapping in view
            };

            // Calculate Percentage for Admin View
            int totalFields = 18;
            int filledFields = 0;
            if (!string.IsNullOrEmpty(model.Name)) filledFields++;
            if (!string.IsNullOrEmpty(model.FatherHusbandName)) filledFields++;
            if (!string.IsNullOrEmpty(model.Gender)) filledFields++;
            if (!string.IsNullOrEmpty(model.CNIC)) filledFields++;
            if (model.ConsultationFee > 0) filledFields++;
            if (!string.IsNullOrEmpty(model.PhoneNumber)) filledFields++;
            if (!string.IsNullOrEmpty(model.Email)) filledFields++;
            if (!string.IsNullOrEmpty(model.ResidentialAddress)) filledFields++;
            if (!string.IsNullOrEmpty(model.City)) filledFields++;
            if (!string.IsNullOrEmpty(model.Province)) filledFields++;
            if (!string.IsNullOrEmpty(model.PMDCRegistrationNumber)) filledFields++;
            if (model.PMDCValidityDate.HasValue) filledFields++;
            if (!string.IsNullOrEmpty(model.Specialist)) filledFields++;
            if (!string.IsNullOrEmpty(model.Qualification)) filledFields++;
            if (!string.IsNullOrEmpty(model.Experience)) filledFields++;
            if (!string.IsNullOrEmpty(model.Workplace)) filledFields++;
            if (!string.IsNullOrEmpty(model.BankAccountNumber)) filledFields++;
            if (!string.IsNullOrEmpty(model.CNICFrontUrl) && !string.IsNullOrEmpty(model.CNICBackUrl) && !string.IsNullOrEmpty(model.PMDCCertificateUrl)) filledFields++;

            model.CompletionPercentage = (int)((double)filledFields / totalFields * 100);
            if (model.CompletionPercentage > 100) model.CompletionPercentage = 100;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest(new { message = "User ID is required" });
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "Doctor account not found." });

            // ENFORCE COMPLETION CHECK
             int totalFields = 18;
            int filledFields = 0;
            if (!string.IsNullOrEmpty(user.Name)) filledFields++;
            if (!string.IsNullOrEmpty(user.FatherHusbandName)) filledFields++;
            if (!string.IsNullOrEmpty(user.Gender)) filledFields++;
            if (!string.IsNullOrEmpty(user.CNIC)) filledFields++;
            if (user.ConsultationFee > 0) filledFields++;
            if (!string.IsNullOrEmpty(user.PhoneNumber)) filledFields++;
            if (!string.IsNullOrEmpty(user.Email)) filledFields++;
            if (!string.IsNullOrEmpty(user.ResidentialAddress)) filledFields++;
            if (!string.IsNullOrEmpty(user.City)) filledFields++;
            if (!string.IsNullOrEmpty(user.Province)) filledFields++;
            if (!string.IsNullOrEmpty(user.PMDCRegistrationNumber)) filledFields++;
            if (user.PMDCValidityDate.HasValue) filledFields++;
            if (!string.IsNullOrEmpty(user.Specialist)) filledFields++;
            if (!string.IsNullOrEmpty(user.Qualification)) filledFields++;
            if (!string.IsNullOrEmpty(user.Experience)) filledFields++;
            if (!string.IsNullOrEmpty(user.Workplace)) filledFields++;
            if (!string.IsNullOrEmpty(user.BankAccountNumber)) filledFields++;
            if (!string.IsNullOrEmpty(user.CNICFrontUrl) && !string.IsNullOrEmpty(user.CNICBackUrl) && !string.IsNullOrEmpty(user.PMDCCertificateUrl)) filledFields++;

            int completion = (int)((double)filledFields / totalFields * 100);
            
            if (completion < 100)
            {
                 return BadRequest(new { message = $"Doctor profile is only {completion}% complete. Cannot approve until 100%." });
            }

            user.ApprovalStatus = "Approved";
            user.ApprovalDate = DateTime.Now;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded) 
            {
                // Ensure core Doctor record exists for visibility
                var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == userId);
                if (doctor == null)
                {
                    doctor = new MedLinkPortal.Models.Doctor
                    {
                        UserId = userId,
                        Name = $"{user.FirstName} {user.LastName}",
                        Specialty = user.Specialist ?? "General Practice",
                        ClinicAddress = user.Workplace ?? "Main Hospital",
                        Experience = "1",
                        Rating = 5.0,
                        Online = true,
                        Availability = "Available Today"
                    };
                    _context.Doctors.Add(doctor);
                }
                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest(new { message = "Failed to approve doctor", errors = result.Errors.Select(e => e.Description) });
        }

        [HttpPost]
        public async Task<IActionResult> Reject(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest(new { message = "User ID is required" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "Doctor account not found." });

            user.ApprovalStatus = "Rejected";
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded) return Ok();
            return BadRequest(new { message = "Failed to reject doctor", errors = result.Errors.Select(e => e.Description) });
        }
    }
}
