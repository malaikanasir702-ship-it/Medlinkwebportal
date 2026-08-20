using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Identity.Pages.Account;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LaboratoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public LaboratoriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var labs = await _context.Laboratories
                .Include(l => l.City)
                .Select(l => new LabCompanyViewModel
                {
                    Id = l.Id,
                    Name = l.Name,
                    Address = l.Address,
                    PhoneNumber = l.PhoneNumber,
                    CityName = l.City.Name,
                    TestCount = _context.MedicalTests.Count(t => t.LaboratoryId == l.Id),
                    AdminEmail = _context.Users.Where(u => u.LaboratoryId == l.Id).Select(u => u.Email).FirstOrDefault() ?? "No Admin Assigned"
                })
                .ToListAsync();

            ViewBag.Cities = await _context.Cities.ToListAsync();
            return View(labs);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LabCompanyViewModel model)
        {
            if (model == null) return BadRequest(new { message = "Invalid data" });

            // 1. Create Laboratory
            var lab = new Laboratory
            {
                Name = model.Name,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                CityId = model.CityId,
                OpenTime = "09:00 AM",
                CloseTime = "09:00 PM",
                Rating = 5.0,
                HomeCollectionAvailable = true
            };

            _context.Laboratories.Add(lab);
            await _context.SaveChangesAsync();

            // 2. Create LabAdmin User if Email/Password provided
            if (!string.IsNullOrEmpty(model.AdminEmail) && !string.IsNullOrEmpty(model.AdminPassword))
            {
                var user = new ApplicationUser
                {
                    UserName = model.AdminEmail,
                    Email = model.AdminEmail,
                    FirstName = model.Name,
                    LastName = "Admin",
                    EmailConfirmed = true,
                    LaboratoryId = lab.Id,
                    ApprovalStatus = "Approved"
                };

                var userResult = await _userManager.CreateAsync(user, model.AdminPassword);
                if (userResult.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("LabAdmin"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("LabAdmin"));
                    }
                    await _userManager.AddToRoleAsync(user, "LabAdmin");
                }
                else
                {
                    // If user creation fails, we still have the lab. Maybe return warning.
                    return BadRequest(new { message = "Laboratory created, but admin user creation failed.", errors = userResult.Errors.Select(e => e.Description) });
                }
            }

            return Ok(new { success = true });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var lab = await _context.Laboratories.FindAsync(id);
            if (lab == null) return NotFound();

            // Note: In a real app, you'd handle cascading deletes or prevent delete if dependencies exist.
            _context.Laboratories.Remove(lab);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class LabCompanyViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public int TestCount { get; set; }
        public string AdminEmail { get; set; }
        public string AdminPassword { get; set; }
    }
}
