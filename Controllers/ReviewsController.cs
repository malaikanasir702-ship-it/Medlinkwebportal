using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Identity.Pages.Account;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MedLinkPortal.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> _userManager)
        {
            _context = context;
            this._userManager = _userManager;
        }

        [HttpPost]
        public async Task<IActionResult> AddReview(int doctorId, int rating, string comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var review = new Review
            {
                DoctorId = doctorId,
                PatientId = user.Id,
                PatientName = $"{user.FirstName} {user.LastName}",
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Update Doctor Stats
            var doctor = await _context.Doctors.Include(d => d.PatientReviews).FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor != null)
            {
                doctor.Reviews = doctor.PatientReviews.Count;
                doctor.Rating = doctor.PatientReviews.Average(r => r.Rating);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, newRating = doctor?.Rating, newCount = doctor?.Reviews });
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorReviews(int doctorId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.DoctorId == doctorId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.PatientName,
                    r.Rating,
                    r.Comment,
                    Date = r.CreatedAt.ToString("MMM dd, yyyy")
                })
                .ToListAsync();

            return Json(new { success = true, reviews });
        }
    }
}
