using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Admin.Models;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, IDbContextFactory<ApplicationDbContext> contextFactory, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _contextFactory = contextFactory;
            _userManager = userManager;
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

            // Load bank details for each doctor (decrypted via UserManager)
            var doctorBankInfo = new Dictionary<string, (string BankName, string AccountTitle, string IBAN)>();
            foreach (var req in requests)
            {
                if (req.DoctorId != null && !doctorBankInfo.ContainsKey(req.DoctorId))
                {
                    var user = await _userManager.FindByIdAsync(req.DoctorId);
                    if (user != null)
                    {
                        doctorBankInfo[req.DoctorId] = (
                            user.BankName ?? "—",
                            user.BankAccountTitle ?? "—",
                            user.BankAccountNumber ?? "—"
                        );
                    }
                }
            }

            ViewBag.DebugTotalCount = allCount;
            ViewBag.DebugPendingCount = pendingWithdrawCount;
            ViewBag.DoctorBankInfo = doctorBankInfo;

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveWithdrawal(int id, string? paymentReference = null)
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

                    // Append payment reference if provided
                    if (!string.IsNullOrWhiteSpace(paymentReference))
                    {
                        walletTx.Description = (walletTx.Description ?? "") + $" | Ref: {paymentReference}";
                    }

                    // Update Doctor's financials — WalletBalance already deducted at withdrawal request time
                    // (DoctorController.RequestWithdrawal deducts balance on request)
                    // We only need to increment TotalWithdrawn here
                    if (walletTx.Doctor != null)
                    {
                        walletTx.Doctor.TotalWithdrawn += walletTx.NetAmount > 0 ? walletTx.NetAmount : walletTx.Amount;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Withdrawal approved and payment recorded successfully." });
                }
                catch (Exception ex)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
                }
            });
        }

        // ── ENTERPRISE AI INTELLIGENCE ──────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DataIntelligence()
        {
            var totalDocs = await _context.ClinicalDocuments.CountAsync();
            var pendingJobs = await _context.ExtractionJobs.CountAsync(j => j.Status == "Pending" || j.Status == "Processing");
            var completedJobs = await _context.ExtractionJobs.CountAsync(j => j.Status == "Completed");
            var failedJobs = await _context.ExtractionJobs.CountAsync(j => j.Status == "Failed");

            var classifiedRecords = await _context.ClassifiedRecords
                .Include(r => r.Doctor)
                .OrderByDescending(r => r.ProcessedAt)
                .Take(25)
                .ToListAsync();

            var topTags = await _context.MedicalClassificationTags
                .OrderByDescending(t => t.UsageCount)
                .Take(20)
                .ToListAsync();

            ViewBag.TotalDocs = totalDocs;
            ViewBag.PendingJobs = pendingJobs;
            ViewBag.CompletedJobs = completedJobs;
            ViewBag.FailedJobs = failedJobs;
            ViewBag.TopTags = topTags;
            ViewBag.ClassifiedCount = await _context.ClassifiedRecords.CountAsync();

            return View(classifiedRecords);
        }

        [HttpGet]
        public async Task<IActionResult> KnowledgeGraph([FromServices] MedLinkPortal.Services.IKnowledgeGraphService kgService)
        {
            var graphData = await kgService.GetGraphDataAsync();
            var insights = await kgService.GetInsightsAsync();

            ViewBag.NodeCount = graphData.Nodes.Count;
            ViewBag.LinkCount = graphData.Links.Count;
            ViewBag.Insights = insights;
            ViewBag.GraphJson = System.Text.Json.JsonSerializer.Serialize(graphData);

            return View(graphData);
        }

        [HttpGet]
        public async Task<IActionResult> DataGovernance([FromServices] MedLinkPortal.Services.IDataGovernanceService govService)
        {
            var accessLogs = await govService.GetAccessLogsAsync(50);
            var totalConsents = await _context.ConsentRecords.CountAsync();
            var activeConsents = await _context.ConsentRecords.CountAsync(c => c.IsGranted);
            var totalLogs = await _context.DataAccessLogs.CountAsync();

            ViewBag.TotalConsents = totalConsents;
            ViewBag.ActiveConsents = activeConsents;
            ViewBag.TotalAccessLogs = totalLogs;

            return View(accessLogs);
        }

        // ── Live Presence Dashboard ──────────────────────────────────────────

        [HttpGet]
        public IActionResult LivePresence()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetLivePresenceData()
        {
            var fifteenMinAgo = DateTime.UtcNow.AddMinutes(-15);
            var today = DateTime.UtcNow.Date;

            // ── Doctors ──────────────────────────────────────────────────────
            var doctors = await _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Where(d => !d.IsSuspended)
                .ToListAsync();

            var doctorData = doctors.Select(d =>
            {
                // Determine login source: if has active SignalR → portal, else check mobile via JWT session marker
                var isOnlinePortal = d.User != null && !string.IsNullOrEmpty(d.User.ActiveSignalRConnectionId);
                var isOnline = d.Online || isOnlinePortal;

                string statusLabel;
                string statusClass;
                if (d.IsInConsultation)
                {
                    statusLabel = "In Consultation";
                    statusClass = "consultation";
                }
                else if (isOnline)
                {
                    statusLabel = isOnlinePortal ? "Online (Portal)" : "Online (App)";
                    statusClass = "online";
                }
                else
                {
                    statusLabel = "Offline";
                    statusClass = "offline";
                }

                return new
                {
                    id = d.Id,
                    name = d.Name,
                    specialty = d.Specialty,
                    image = d.Image,
                    status = statusLabel,
                    statusClass,
                    isInConsultation = d.IsInConsultation,
                    isOnline,
                    isOnlinePortal,
                    todayAppointments = _context.Appointments
                        .Count(a => a.DoctorId == d.Id && a.AppointmentDate.Date == today),
                    completedToday = _context.Appointments
                        .Count(a => a.DoctorId == d.Id && a.AppointmentDate.Date == today && a.Status == "Completed"),
                };
            }).OrderByDescending(d => d.isInConsultation)
              .ThenByDescending(d => d.isOnline)
              .ToList();

            // ── Patients (active sessions) ─────────────────────────────────
            var activeSessions = await _context.UserSessions
                .AsNoTracking()
                .Where(s => !s.IsRevoked && s.LastSeen >= fifteenMinAgo)
                .ToListAsync();

            var activeUserIds = activeSessions.Select(s => s.UserId).Distinct().ToList();

            // Exclude doctors and admins — get patients only
            var doctorUserIds = doctors.Select(d => d.UserId).Where(id => id != null).ToList();
            var patientUserIds = activeUserIds.Except(doctorUserIds).ToList();

            var patientUsers = await _context.Users
                .AsNoTracking()
                .Where(u => patientUserIds.Contains(u.Id))
                .ToListAsync();

            var patientData = patientUsers.Select(u =>
            {
                var session = activeSessions
                    .Where(s => s.UserId == u.Id)
                    .OrderByDescending(s => s.LastSeen)
                    .FirstOrDefault();

                // Check if patient is in active consultation
                var inConsultation = _context.Appointments
                    .Any(a => a.UserId == u.Id
                        && a.AppointmentDate.Date == today
                        && a.Status == "Confirmed"
                        && a.AppointmentDate <= DateTime.UtcNow.AddMinutes(30)
                        && a.AppointmentDate >= DateTime.UtcNow.AddMinutes(-60));

                var lastSeenMinutes = session != null
                    ? (int)(DateTime.UtcNow - session.LastSeen).TotalMinutes
                    : 999;

                string statusLabel = inConsultation ? "In Consultation" :
                                     lastSeenMinutes <= 2 ? "Active Now" :
                                     lastSeenMinutes <= 15 ? $"Active {lastSeenMinutes}m ago" : "Recently Active";

                return new
                {
                    id = u.Id,
                    name = u.FullName ?? u.Email ?? "Patient",
                    email = u.Email,
                    image = u.ProfileImage,
                    deviceName = session?.DeviceName ?? "Unknown",
                    lastSeen = session?.LastSeen,
                    lastSeenMinutes,
                    inConsultation,
                    status = statusLabel,
                    statusClass = inConsultation ? "consultation" :
                                  lastSeenMinutes <= 5 ? "online" : "recent",
                };
            }).OrderByDescending(p => p.inConsultation)
              .ThenBy(p => p.lastSeenMinutes)
              .ToList();

            // ── Summary Stats ──────────────────────────────────────────────
            var stats = new
            {
                totalOnlineDoctors = doctorData.Count(d => d.isOnline),
                inConsultationDoctors = doctorData.Count(d => d.isInConsultation),
                freeDoctors = doctorData.Count(d => d.isOnline && !d.isInConsultation),
                offlineDoctors = doctorData.Count(d => !d.isOnline),
                activePatients = patientData.Count,
                patientsInConsultation = patientData.Count(p => p.inConsultation),
            };

            return Json(new { doctors = doctorData, patients = patientData, stats });
        }
    }
}
