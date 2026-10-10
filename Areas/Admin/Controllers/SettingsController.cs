using Microsoft.AspNetCore.Mvc;
using MedLinkPortal.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var admin = await _context.AdminProfiles.FirstOrDefaultAsync();
            return View(admin);
        }
    }
}
