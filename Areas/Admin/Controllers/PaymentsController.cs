using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string status = "Pending")
        {
            var query = _context.WalletTransactions
                .Include(t => t.Doctor)
                .Where(t => t.TransactionType == "WITHDRAWAL");

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            // Calculate summary statistics
            ViewBag.TotalPending = await _context.WalletTransactions
                .Where(t => t.TransactionType == "WITHDRAWAL" && t.Status == "Pending")
                .SumAsync(t => t.NetAmount);

            ViewBag.TotalProcessed = await _context.WalletTransactions
                .Where(t => t.TransactionType == "WITHDRAWAL" && t.Status == "Completed")
                .SumAsync(t => t.NetAmount);

            ViewBag.TotalPlatformFees = await _context.WalletTransactions
                .Where(t => t.TransactionType == "WITHDRAWAL" && t.Status == "Completed")
                .SumAsync(t => t.PlatformFee);

            ViewBag.CurrentStatus = status;

            return View(transactions);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int id)
        {
            var transaction = await _context.WalletTransactions
                .Include(t => t.Doctor)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                return Json(new { success = false, message = "Transaction not found." });

            if (transaction.Status != "Pending")
                return Json(new { success = false, message = "Transaction has already been processed." });

            var adminUserId = _userManager.GetUserId(User);

            transaction.Status = "Completed";
            transaction.ProcessedBy = adminUserId;
            transaction.ProcessedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Payment of PKR {transaction.NetAmount:N2} has been approved and marked as completed." 
            });
        }

        [HttpPost]
        public async Task<IActionResult> RejectPayment(int id, string reason = "")
        {
            var transaction = await _context.WalletTransactions
                .Include(t => t.Doctor)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                return Json(new { success = false, message = "Transaction not found." });

            if (transaction.Status != "Pending")
                return Json(new { success = false, message = "Transaction has already been processed." });

            var adminUserId = _userManager.GetUserId(User);

            // Restore doctor's wallet balance
            var doctor = await _userManager.FindByIdAsync(transaction.DoctorId);
            if (doctor != null)
            {
                doctor.WalletBalance += transaction.Amount; // Restore full amount
                await _userManager.UpdateAsync(doctor);
            }

            transaction.Status = "Rejected";
            transaction.ProcessedBy = adminUserId;
            transaction.ProcessedDate = DateTime.Now;
            transaction.Description += $" | Rejected: {reason}";

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Payment request has been rejected and PKR {transaction.Amount:N2} has been restored to doctor's wallet." 
            });
        }
    }
}
