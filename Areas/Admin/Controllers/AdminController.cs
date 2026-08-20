using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Admin.Models;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public AdminController(ApplicationDbContext context, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _context = context;
            _contextFactory = contextFactory;
        }
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var sevenDaysAgo = today.AddDays(-6);
            var thirtyDaysAgo = today.AddDays(-29);

            // Parallelizing core counts with ContextFactory for thread safety
            var patientCountTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Users.AsNoTracking().CountAsync();
            });
            var physicianCountTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Doctors.AsNoTracking().CountAsync();
            });
            var unitCountTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Doctors.AsNoTracking().Select(p => p.Specialty).Distinct().CountAsync();
            });
            var criticalCountTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.AdminPatients.AsNoTracking().CountAsync(p => p.Status == "CRITICAL");
            });

            // Optimized Chart Data - Single Query for 7 days
            var appointmentStats7Task = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Appointments
                    .AsNoTracking()
                    .Where(a => a.AppointmentDate >= sevenDaysAgo)
                    .GroupBy(a => a.AppointmentDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();
            });

            var recordStats7Task = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.HealthRecords
                    .AsNoTracking()
                    .Where(r => r.Date >= sevenDaysAgo)
                    .GroupBy(r => r.Date.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();
            });

            // Optimized Chart Data - Single Query for 30 days
            var appointmentStats30Task = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Appointments
                    .AsNoTracking()
                    .Where(a => a.AppointmentDate >= thirtyDaysAgo)
                    .GroupBy(a => a.AppointmentDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();
            });

            var recordStats30Task = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.HealthRecords
                    .AsNoTracking()
                    .Where(r => r.Date >= thirtyDaysAgo)
                    .GroupBy(r => r.Date.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();
            });

            var triageQueueTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Appointments
                    .AsNoTracking()
                    .Include(a => a.Doctor)
                    .Where(a => a.AppointmentDate >= today && a.Status != "Cancelled")
                    .OrderBy(a => a.AppointmentDate)
                    .Take(10)
                    .Select(a => new {
                        name = a.PatientName,
                        unit = a.Doctor.Specialty ?? "General",
                        time = a.TimeSlot ?? a.AppointmentDate.ToString("HH:mm"),
                        color = a.Status == "Confirmed" ? "blue" : 
                                a.Status == "Completed" ? "emerald" : 
                                a.Status == "Pending" ? "amber" : "slate"
                    })
                    .ToListAsync();
            });

            var recentDoctorsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Doctors
                    .AsNoTracking()
                    .OrderByDescending(p => p.Id)
                    .Take(5)
                    .Select(d => new {
                        ProfileImage = d.Image,
                        Name = d.Name,
                        Specialty = d.Specialty,
                        Email = "doctor@medlink.com", 
                        Office = d.ClinicAddress ?? "Main Clinic"
                    })
                    .ToListAsync();
            });

            await Task.WhenAll(
                patientCountTask, physicianCountTask, unitCountTask, criticalCountTask,
                appointmentStats7Task, recordStats7Task, 
                appointmentStats30Task, recordStats30Task,
                triageQueueTask, recentDoctorsTask
            );

            ViewBag.PatientCount = patientCountTask.Result;
            ViewBag.PhysicianCount = physicianCountTask.Result;
            ViewBag.UnitCount = unitCountTask.Result;
            ViewBag.CriticalCount = criticalCountTask.Result;

            // Process 7-day stats
            var dates7 = Enumerable.Range(0, 7).Select(i => sevenDaysAgo.AddDays(i).Date).ToList();
            var appts7Dict = appointmentStats7Task.Result.ToDictionary(x => x.Date, x => x.Count);
            var records7Dict = recordStats7Task.Result.ToDictionary(x => x.Date, x => x.Count);
            
            ViewBag.ChartLabels = dates7.Select(d => d.ToString("MMM dd")).ToArray();
            ViewBag.ChartAppointments = dates7.Select(d => appts7Dict.ContainsKey(d) ? appts7Dict[d] : 0).ToArray();
            ViewBag.ChartBillings = new int[7];
            ViewBag.ChartPatients = new int[7];
            ViewBag.ChartRecords = dates7.Select(d => records7Dict.ContainsKey(d) ? records7Dict[d] : 0).ToArray();

            // Process 30-day stats
            var dates30 = Enumerable.Range(0, 30).Select(i => thirtyDaysAgo.AddDays(i).Date).ToList();
            var appts30Dict = appointmentStats30Task.Result.ToDictionary(x => x.Date, x => x.Count);
            var records30Dict = recordStats30Task.Result.ToDictionary(x => x.Date, x => x.Count);

            ViewBag.ChartLabels30 = dates30.Select(d => d.ToString("MMM dd")).ToArray();
            ViewBag.ChartAppointments30 = dates30.Select(d => appts30Dict.ContainsKey(d) ? appts30Dict[d] : 0).ToArray();
            ViewBag.ChartBillings30 = new int[30];
            ViewBag.ChartPatients30 = new int[30];
            ViewBag.ChartRecords30 = dates30.Select(d => records30Dict.ContainsKey(d) ? records30Dict[d] : 0).ToArray();

            ViewBag.TriageQueue = triageQueueTask.Result;
            ViewBag.RecentDoctors = recentDoctorsTask.Result;

            return View();
        }
        public IActionResult Sitemap()
        {
            return View();
        }
        public IActionResult Profile()
        {
            var profile = _context.AdminProfiles.FirstOrDefault(p => p.Id == 1);
            return View(profile);
        }
        [HttpPost]
        public IActionResult UpdateProfile([FromBody] AdminProfile updatedProfile)
        {
            if (updatedProfile == null) return BadRequest(new { message = "Invalid profile data" });
            // For update, we don't validate Password as it's optional during update
            ModelState.Remove("Password");
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { message = "Validation failed", errors = errors });
            }
            var profile = _context.AdminProfiles.FirstOrDefault(p => p.Id == updatedProfile.Id);
            if (profile == null) return NotFound();
            profile.Name = updatedProfile.Name;
            profile.Email = updatedProfile.Email;
            profile.Phone = updatedProfile.Phone;
            profile.Office = updatedProfile.Office;
            profile.Bio = updatedProfile.Bio;
            profile.Specialty = updatedProfile.Specialty;
            profile.ProfileImage = updatedProfile.ProfileImage;
            _context.SaveChanges();
            return Ok();
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Specialty,Email,Phone,Office,Bio,Password")] AdminProfile adminProfile)
        {
            if (ModelState.IsValid)
            {
                _context.Add(adminProfile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(adminProfile);
        }
        public async Task<IActionResult> PaymentRequests()
        {
            var allCount = await _context.WalletTransactions.CountAsync();
            var pendingWithdrawCount = await _context.WalletTransactions
                .CountAsync(t => t.TransactionType == "WITHDRAWAL" && (t.Status == "Pending" || t.Status == "pending" || t.Status == "Approved"));
            
            Console.WriteLine($"[DEBUG] Total Transactions: {allCount}");
            Console.WriteLine($"[DEBUG] Pending/Approved Withdrawals: {pendingWithdrawCount}");

            var requests = await _context.WalletTransactions
                .Where(t => t.TransactionType == "WITHDRAWAL" && (t.Status == "Pending" || t.Status == "pending" || t.Status == "Approved"))
                .Include(t => t.Doctor)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            ViewBag.DebugTotalCount = allCount;
            ViewBag.DebugPendingCount = pendingWithdrawCount;

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveWithdrawal(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () => {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var walletTx = await _context.WalletTransactions
                        .Include(t => t.Doctor)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (walletTx == null) return NotFound();
                    if (walletTx.Status != "Pending" && walletTx.Status != "pending") 
                        return BadRequest(new { message = "Request already processed." });

                    // Update Transaction Status
                    walletTx.Status = "Approved";
                    walletTx.ProcessedDate = DateTime.Now;
                    walletTx.ProcessedBy = User.Identity?.Name ?? "Admin";

                    // Update Doctor's Total Withdrawn
                    if (walletTx.Doctor != null)
                    {
                        walletTx.Doctor.TotalWithdrawn += walletTx.Amount;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Withdrawal request approved successfully." });
                }
                catch (Exception ex)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
                }
            });
        }
    }
}
