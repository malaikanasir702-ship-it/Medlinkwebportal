using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Identity.Pages.Account;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PharmacyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PharmacyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Pharmacy
        public async Task<IActionResult> Index()
        {
            // Get all users in Pharmacist role
            var pharmacists = await _userManager.GetUsersInRoleAsync("Pharmacist");

            var viewModels = new List<PharmacyAdminViewModel>();

            // Summary stats for ViewBag
            var allOrders = await _context.PharmacyOrders.ToListAsync();
            ViewBag.TotalOrders    = allOrders.Count;
            ViewBag.PendingOrders  = allOrders.Count(o => o.Status == PharmacyOrderStatus.Pending || o.Status == PharmacyOrderStatus.Accepted);
            ViewBag.TotalRevenue   = allOrders.Where(o => o.Status == PharmacyOrderStatus.Delivered).Sum(o => o.TotalAmount);
            ViewBag.TotalPharmacies = pharmacists.Count;

            foreach (var p in pharmacists)
            {
                var orders = allOrders.Where(o => o.PharmacistId == p.Id).ToList();
                viewModels.Add(new PharmacyAdminViewModel
                {
                    PharmacistId    = p.Id,
                    PharmacistName  = p.FullName,
                    PharmacistEmail = p.Email ?? "",
                    TotalOrders     = orders.Count,
                    PendingOrders   = orders.Count(o => o.Status == PharmacyOrderStatus.Pending || o.Status == PharmacyOrderStatus.Accepted),
                    DeliveredOrders = orders.Count(o => o.Status == PharmacyOrderStatus.Delivered),
                    TotalRevenue    = orders.Where(o => o.Status == PharmacyOrderStatus.Delivered).Sum(o => o.TotalAmount),
                    LastOrderDate   = orders.Any() ? orders.Max(o => o.CreatedAt) : (DateTime?)null
                });
            }

            return View(viewModels);
        }

        // GET: /Admin/Pharmacy/Orders?pharmacistId=xxx
        public async Task<IActionResult> Orders(string? pharmacistId)
        {
            var query = _context.PharmacyOrders
                .Include(o => o.Patient)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Medicine)
                .AsQueryable();

            if (!string.IsNullOrEmpty(pharmacistId))
            {
                query = query.Where(o => o.PharmacistId == pharmacistId);
                var pharmacist = await _userManager.FindByIdAsync(pharmacistId);
                ViewBag.PharmacistName = pharmacist?.FullName;
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            // Get all riders for name lookup
            var riderIds = orders.Where(o => o.RiderId.HasValue).Select(o => o.RiderId!.Value).Distinct().ToList();
            var riders = await _context.Riders
                .Include(r => r.User)
                .Where(r => riderIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.User!.FullName);

            // Get pharmacist names
            var pharmacistIds = orders.Where(o => !string.IsNullOrEmpty(o.PharmacistId))
                                      .Select(o => o.PharmacistId!).Distinct().ToList();
            var pharmacistMap = new Dictionary<string, string>();
            foreach (var pid in pharmacistIds)
            {
                var u = await _userManager.FindByIdAsync(pid);
                if (u != null) pharmacistMap[pid] = u.FullName;
            }

            var viewModels = orders.Select(o => new PharmacyOrderAdminViewModel
            {
                OrderId          = o.Id,
                PatientName      = o.Patient?.FullName ?? "Unknown",
                PatientEmail     = o.Patient?.Email ?? "",
                PharmacistName   = o.PharmacistId != null && pharmacistMap.ContainsKey(o.PharmacistId)
                                       ? pharmacistMap[o.PharmacistId] : "Unassigned",
                MedicinesSummary = o.OrderItems.Any()
                                       ? string.Join(", ", o.OrderItems.Select(i => $"{i.Medicine?.Name ?? "?"} x{i.Quantity}"))
                                       : "—",
                TotalAmount      = o.TotalAmount,
                Status           = o.Status.ToString(),
                StatusCode       = (int)o.Status,
                PaymentMethod    = o.PaymentMethod.ToString(),
                PaymentStatus    = o.PaymentStatus,
                CreatedAt        = o.CreatedAt,
                RiderName        = o.RiderId.HasValue && riders.ContainsKey(o.RiderId.Value)
                                       ? riders[o.RiderId.Value] : null
            }).ToList();

            ViewBag.TotalCount    = viewModels.Count;
            ViewBag.PendingCount  = viewModels.Count(v => v.StatusCode <= 1);
            ViewBag.DeliveredCount = viewModels.Count(v => v.StatusCode == 6);
            ViewBag.TotalRevenue  = viewModels.Where(v => v.StatusCode == 6).Sum(v => v.TotalAmount);

            return View(viewModels);
        }

        // POST: /Admin/Pharmacy/UpdateStatus
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, int status)
        {
            var order = await _context.PharmacyOrders.FindAsync(orderId);
            if (order == null) return NotFound(new { message = "Order not found" });

            order.Status    = (PharmacyOrderStatus)status;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, newStatus = ((PharmacyOrderStatus)status).ToString() });
        }
    }

    // ── ViewModels ─────────────────────────────────────────────────────────────

    public class PharmacyAdminViewModel
    {
        public string PharmacistId    { get; set; } = "";
        public string PharmacistName  { get; set; } = "";
        public string PharmacistEmail { get; set; } = "";
        public int    TotalOrders     { get; set; }
        public int    PendingOrders   { get; set; }
        public int    DeliveredOrders { get; set; }
        public decimal TotalRevenue   { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }

    public class PharmacyOrderAdminViewModel
    {
        public int     OrderId          { get; set; }
        public string  PatientName      { get; set; } = "";
        public string  PatientEmail     { get; set; } = "";
        public string  PharmacistName   { get; set; } = "";
        public string  MedicinesSummary { get; set; } = "";
        public decimal TotalAmount      { get; set; }
        public string  Status           { get; set; } = "";
        public int     StatusCode       { get; set; }
        public string  PaymentMethod    { get; set; } = "";
        public string  PaymentStatus    { get; set; } = "";
        public DateTime CreatedAt       { get; set; }
        public string? RiderName        { get; set; }
    }
}
