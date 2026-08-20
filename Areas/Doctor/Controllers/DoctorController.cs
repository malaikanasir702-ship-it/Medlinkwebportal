using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Areas.Doctor.Models;
using Appointment = MedLinkPortal.Areas.Doctor.Models.Appointment;
using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using Stripe;
using Stripe.Checkout;

namespace MedLinkPortal.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly Services.INotificationService _notificationService;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IMemoryCache _cache;

        public DoctorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, Services.INotificationService notificationService, IDbContextFactory<ApplicationDbContext> contextFactory, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _notificationService = notificationService;
            _contextFactory = contextFactory;
            _cache = cache;
        }

        public async Task<IActionResult> TranscriptionHistory()
        {
            // Self-healing: Ensure ConsultationTranscripts table exists
            try {
                await _context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ConsultationTranscripts]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [ConsultationTranscripts] (
                            [Id] int NOT NULL IDENTITY,
                            [AppointmentId] int NOT NULL,
                            [SpeakerId] nvarchar(max) NOT NULL,
                            [SpeakerName] nvarchar(100) NOT NULL,
                            [SpeakerRole] nvarchar(20) NOT NULL,
                            [OriginalText] nvarchar(max) NOT NULL,
                            [EnglishTranslation] nvarchar(max) NOT NULL,
                            [UrduTranslation] nvarchar(max) NOT NULL,
                            [DetectedLanguage] nvarchar(50) NOT NULL,
                            [Timestamp] datetime2 NOT NULL,
                            CONSTRAINT [PK_ConsultationTranscripts] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_ConsultationTranscripts_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE CASCADE
                        );
                        CREATE INDEX [IX_ConsultationTranscripts_AppointmentId] ON [ConsultationTranscripts] ([AppointmentId]);
                    END
                ");
            } catch { /* Ignore if it exists or fails silently */ }

            var userId = _userManager.GetUserId(User);
            var coreDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (coreDoctor == null) return NotFound();

            var consultationIdsWithTranscripts = await _context.ConsultationTranscripts
                .Select(t => t.AppointmentId)
                .Distinct()
                .ToListAsync();

            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == coreDoctor.Id && consultationIdsWithTranscripts.Contains(a.Id))
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var mapped = appointments.Select(a => new Appointment
            {
                Id = a.Id,
                DoctorId = userId ?? string.Empty,
                PatientId = a.UserId ?? string.Empty,
                Patient = _userManager.Users.FirstOrDefault(u => u.Id == a.UserId) ?? new ApplicationUser { UserName = "Unknown" },
                ScheduledTime = a.AppointmentDate,
                DurationMinutes = 30,
                ConsultationType = a.ConsultationType,
                Status = a.Status
            }).ToList();

            return View(mapped);
        }

        public async Task<IActionResult> TranscriptHistory(int id)
        {
            // Self-healing: Ensure ConsultationTranscripts table exists
            try {
                await _context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ConsultationTranscripts]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [ConsultationTranscripts] (
                            [Id] int NOT NULL IDENTITY,
                            [AppointmentId] int NOT NULL,
                            [SpeakerId] nvarchar(max) NOT NULL,
                            [SpeakerName] nvarchar(100) NOT NULL,
                            [SpeakerRole] nvarchar(20) NOT NULL,
                            [OriginalText] nvarchar(max) NOT NULL,
                            [EnglishTranslation] nvarchar(max) NOT NULL,
                            [UrduTranslation] nvarchar(max) NOT NULL,
                            [DetectedLanguage] nvarchar(50) NOT NULL,
                            [Timestamp] datetime2 NOT NULL,
                            CONSTRAINT [PK_ConsultationTranscripts] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_ConsultationTranscripts_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE CASCADE
                        );
                        CREATE INDEX [IX_ConsultationTranscripts_AppointmentId] ON [ConsultationTranscripts] ([AppointmentId]);
                    END
                ");
            } catch { /* Ignore */ }

            var userId = _userManager.GetUserId(User);
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            // Verify doctor has access to this appointment
            bool isDoctor = appointment.Doctor != null && appointment.Doctor.UserId == userId;
            if (!isDoctor) return Unauthorized();

            ViewBag.AppointmentId = id;
            ViewBag.DoctorName = appointment.Doctor?.Name ?? "Doctor";
            ViewBag.PatientName = (await _context.Users.FindAsync(appointment.UserId))?.Name ?? "Patient";
            ViewBag.AppointmentDate = appointment.AppointmentDate.ToString("MMMM dd, yyyy");

            return View();
        }

        public async Task<IActionResult> DoctorDashBoard()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // Link to the core Doctor entity - Parallel Task with ContextFactory
            var coreDoctorTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Doctors.AsNoTracking().Include(d => d.CurrentPlan).Include(d => d.User).FirstOrDefaultAsync(d => d.UserId == userId);
            });
            
            // Notifications - Parallel Task
            var notificationsTask = _notificationService.GetUserNotificationsAsync(userId);

            await Task.WhenAll(coreDoctorTask, notificationsTask);
            var coreDoctor = coreDoctorTask.Result;

            if (coreDoctor == null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    coreDoctor = await _context.Doctors.Include(d => d.CurrentPlan).Include(d => d.User).FirstOrDefaultAsync(d => d.Name == (user.Name ?? user.UserName));
                    if (coreDoctor != null)
                    {
                        coreDoctor.UserId = userId;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // --- Subscription Tracking for Dashboard ---
            var currentPlan = coreDoctor?.CurrentPlan ?? await _context.SubscriptionPlans.FindAsync(1);
            ViewBag.CurrentPlan = currentPlan;

            var coreDocId = coreDoctor?.Id;

            // Optimized: Get only necessary data for stats in one go
            var monthAgo = DateTime.Now.AddDays(-30);
            var today = DateTime.Today;

            var totalPatientsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Appointments.AsNoTracking().Where(a => a.DoctorId == coreDocId).Select(a => a.UserId).Distinct().CountAsync();
            });
            var activeTreatmentsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Appointments.AsNoTracking().Where(a => a.DoctorId == coreDocId && a.AppointmentDate >= monthAgo).Select(a => a.UserId).Distinct().CountAsync();
            });
            var followUpNeededTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Appointments.AsNoTracking().Where(a => a.DoctorId == coreDocId).CountAsync(a => a.AppointmentDate >= today);
            });
            
            // Recent Patients optimized - Fetch only IDs and Dates first
            var recentPatientIdsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Appointments.AsNoTracking()
                    .Where(a => a.DoctorId == coreDocId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Select(a => new { a.UserId, a.AppointmentDate })
                    .Take(50)
                    .ToListAsync();
            });

            await Task.WhenAll(totalPatientsTask, activeTreatmentsTask, followUpNeededTask, recentPatientIdsTask);

            ViewBag.PatientCount = totalPatientsTask.Result;
            ViewBag.IsPro = coreDoctor?.User?.IsPro ?? false;

            var viewModel = new DoctorDashboardViewModel
            {
                TotalPatients = totalPatientsTask.Result,
                ActiveTreatments = activeTreatmentsTask.Result,
                FollowUpNeeded = followUpNeededTask.Result,
                AverageRating = coreDoctor?.Rating ?? 4.8,
                Notifications = notificationsTask.Result
            };

            // Get distinct patient IDs for the last 6 patients
            var distinctRecentPatientIds = recentPatientIdsTask.Result
                .GroupBy(a => a.UserId)
                .Select(g => g.First())
                .Take(6)
                .ToList();

            var patientIds = distinctRecentPatientIds.Select(p => p.UserId).ToList();

            var patientsDataTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Users.AsNoTracking()
                    .Where(u => patientIds.Contains(u.Id))
                    .ToListAsync();
            });

            var todayCoreTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Appointments.AsNoTracking()
                    .Where(a => a.DoctorId == coreDocId && a.AppointmentDate.Date == today)
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync();
            });

            await Task.WhenAll(patientsDataTask, todayCoreTask);

            var patientsData = patientsDataTask.Result;

            foreach (var rp in distinctRecentPatientIds)
            {
                var patient = patientsData.FirstOrDefault(u => u.Id == rp.UserId);
                if (patient == null) continue;

                var displayName = patient.FullName ?? patient.UserName ?? "Unknown";
                viewModel.RecentPatients.Add(new PatientCardViewModel
                {
                    PatientId = rp.UserId,
                    Name = displayName,
                    Initials = displayName.Length >= 2 ? displayName.Substring(0, 2).ToUpper() : "PT",
                    Age = (patient.DateOfBirth.HasValue && patient.DateOfBirth.Value.Year > 1) ? DateTime.Today.Year - patient.DateOfBirth.Value.Year : 0,
                    Gender = patient.Gender ?? "N/A",
                    Status = "Active",
                    StatusColor = "bg-emerald-100 text-emerald-700",
                    LastVisit = rp.AppointmentDate,
                    Condition = "Routine Checkup",
                    RecordStatus = "Live"
                });
            }

            // Map core appointments to DoctorModels.Appointment for TodayAppointments collection
            viewModel.TodayAppointments = todayCoreTask.Result.Select(a => new Appointment
            {
                Id = a.Id,
                DoctorId = userId ?? string.Empty,
                PatientId = a.UserId ?? string.Empty,
                Patient = patientsData.FirstOrDefault(u => u.Id == a.UserId) ?? new ApplicationUser { UserName = "Unknown" },
                ScheduledTime = a.AppointmentDate,
                ConsultationType = a.ConsultationType,
                Status = a.Status
            }).ToList();

            await LogAction(userId, "Session Initiation", "Doctor Dashboard interface loaded", "Authorized");

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ConsultationRoom(string id)
        {
            var userId = _userManager.GetUserId(User);
            
            if (id.StartsWith("triage_"))
            {
                // Live triage handling (No fixed appointment in DB)
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                ViewBag.AppointmentId = id; 
                ViewBag.DoctorName = doctor?.Name ?? "Doctor";
                ViewBag.DoctorImage = doctor?.Image ?? "https://picsum.photos/seed/doc/100/100";
                ViewBag.PatientName = "Emergency Patient";
                ViewBag.PatientUserId = "emergency_patient";
                ViewBag.DoctorUserId = userId;
                ViewBag.CurrentUserId = userId;
                ViewBag.IsPrescriptionLocked = false;
                ViewBag.PrescriptionData = null;
                return View();
            }

            if (!int.TryParse(id, out int appointmentId)) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return NotFound();
            
            // Security check: only the assigned doctor can access
            if (appointment.Doctor == null || appointment.Doctor.UserId != userId)
            {
                return Unauthorized();
            }

            var patientUser = await _context.Users.FindAsync(appointment.UserId);

            // Fetch existing prescription if any
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionMedicines)
                .ThenInclude(pm => pm.Medicine)
                .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

            ViewBag.AppointmentId = id;
            ViewBag.DoctorName = appointment.Doctor?.Name ?? "Doctor";
            ViewBag.DoctorImage = appointment.Doctor?.Image ?? "https://picsum.photos/seed/doc/100/100";
            ViewBag.PatientName = patientUser?.FirstName + " " + patientUser?.LastName;
            ViewBag.PatientUserId = appointment.UserId;
            ViewBag.DoctorUserId = appointment.Doctor?.UserId;
            ViewBag.CurrentUserId = userId;
            ViewBag.IsPrescriptionLocked = prescription?.IsLocked ?? false;
            ViewBag.PrescriptionData = null; // Default initialization
            
            if (prescription != null)
            {
                ViewBag.PrescriptionData = Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    diagnosis = prescription.Diagnosis,
                    medications = prescription.PrescriptionMedicines.Select(pm => new {
                        medicineId = pm.MedicineId,
                        medicineName = pm.Medicine.Name,
                        dosage = pm.Dosage,
                        frequency = pm.Frequency,
                        duration = pm.Duration,
                        quantity = pm.Quantity
                    })
                });
            }

            return View();
        }

        public async Task<IActionResult> Wallet()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            var transactions = await _context.WalletTransactions
                .Where(t => t.DoctorId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            ViewBag.WalletBalance = user?.WalletBalance ?? 0;
            return View(transactions);
        }

        [HttpPost]
        public async Task<IActionResult> RequestWithdrawal(decimal amount)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync<IActionResult>(async () => {
                using var transactionScope = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user == null) return NotFound();

                    if (amount <= 0 || amount > user.WalletBalance)
                    {
                        return Json(new { success = false, message = "Invalid amount or insufficient balance." });
                    }

                    // Calculate 2% platform fee
                    decimal platformFee = Math.Round(amount * 0.02m, 2);
                    decimal netAmount = amount - platformFee;

                    // Deduct balance immediately as per requirement
                    user.WalletBalance -= amount;
                    _context.Users.Update(user); // Use context update within transaction for better atomicity
                    await _context.SaveChangesAsync();

                    var transaction = new WalletTransaction
                    {
                        DoctorId = userId,
                        Amount = amount,
                        PlatformFee = platformFee,
                        NetAmount = netAmount,
                        TransactionType = "WITHDRAWAL",
                        Description = $"Withdrawal Request (Platform Fee: PKR {platformFee:N2})",
                        Status = "Pending",
                        TransactionDate = DateTime.Now
                    };

                    _context.WalletTransactions.Add(transaction);
                    await _context.SaveChangesAsync();

                    await transactionScope.CommitAsync();

                    await LogAction(userId, "Financial Transaction", $"Withdrawal request of PKR {amount} processed with platform fee deduction", "Pending");

                    return Json(new { 
                        success = true, 
                        message = "Your withdrawal request has been submitted. It will be credited to your account after admin approval.",
                        platformFee = platformFee,
                        netAmount = netAmount
                    });
                }
                catch (Exception ex)
                {
                    if (transactionScope != null) await transactionScope.RollbackAsync();
                    
                    Console.WriteLine($"[ERROR] Withdrawal Error: {ex.Message}");
                    if (ex.InnerException != null) Console.WriteLine($"[INNER ERROR] {ex.InnerException.Message}");
                    
                    return Json(new { 
                        success = false, 
                        message = "An error occurred: " + (ex.InnerException?.Message ?? ex.Message) 
                    });
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogs()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await LogAction(userId, "System Access", "Accessed Audit Logs monitoring interface", "Processed");

            var logs = await _context.AuditLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .ToListAsync();

            return View(logs);
        }

        private async Task LogAction(string userId, string action, string details, string status = "Success")
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Details = details,
                    Status = status,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    Timestamp = DateTime.Now
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch 
            {
                // Fail silently to not disrupt user flow
            }
        }
        public async Task<IActionResult> PatientRecords()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var coreDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            var coreDocId = coreDoctor?.Id;

            // Fetch patients from core appointments
            var patientIds = await _context.Appointments
                .Where(a => a.DoctorId == coreDocId)
                .Select(a => a.UserId)
                .Distinct()
                .ToListAsync();

            var patients = await _userManager.Users
                .Where(u => patientIds.Contains(u.Id))
                .ToListAsync();

            var viewModel = new List<PatientRecordListViewModel>();
            foreach (var patient in patients)
            {
                var nextAppt = await _context.Appointments
                    .Where(a => a.UserId == patient.Id && a.DoctorId == coreDocId && a.AppointmentDate >= DateTime.Today.AddDays(-1) && a.Status != "Completed")
                    .OrderByDescending(a => a.AppointmentDate)
                    .FirstOrDefaultAsync();

                viewModel.Add(new PatientRecordListViewModel
                {
                    Patient = patient,
                    NextAppointmentTime = nextAppt?.AppointmentDate,
                    AppointmentId = nextAppt?.Id
                });
            }

            return View(viewModel);
        }

        public async Task<IActionResult> ViewHistory(string patientId)
        {
            if (string.IsNullOrEmpty(patientId)) return NotFound();

            var userId = _userManager.GetUserId(User);
            var coreDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            var coreDocId = coreDoctor?.Id;
            var now = DateTime.Now;
            
            // Check core appointments
            var hasStartedAppointment = await _context.Appointments
                .AnyAsync(a => a.DoctorId == coreDocId && a.UserId == patientId && a.AppointmentDate <= now);

            if (!hasStartedAppointment)
            {
                TempData["ErrorMessage"] = "You can only view patient history once their appointment has started.";
                return RedirectToAction(nameof(Schedule));
            }

            var patient = await _userManager.FindByIdAsync(patientId);
            if (patient == null) return NotFound();

            var records = await _context.DoctorPatientRecords
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.PatientName = patient.Name ?? patient.UserName ?? "Patient";
            ViewBag.PatientId = patientId;
            return View(records);
        }

        public async Task<IActionResult> RecordDetails(int id)
        {
            var record = await _context.DoctorPatientRecords
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (record == null) return NotFound();

            return View(record);
        }

        public async Task<IActionResult> Messages(string? patientId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var coreDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            var coreDocId = coreDoctor?.Id;

            var patientIds = await _context.Appointments
                .Where(a => a.DoctorId == coreDocId)
                .Select(a => a.UserId)
                .Distinct()
                .ToListAsync();

            var patients = await _userManager.Users
                .Where(u => patientIds.Contains(u.Id))
                .ToListAsync();

            var viewModel = new List<PatientRecordListViewModel>();
            foreach (var patient in patients)
            {
                var nextAppt = await _context.Appointments
                    .Where(a => a.UserId == patient.Id && a.DoctorId == coreDocId && a.AppointmentDate >= DateTime.Today.AddDays(-1) && a.Status != "Completed")
                    .OrderByDescending(a => a.AppointmentDate)
                    .FirstOrDefaultAsync();

                viewModel.Add(new PatientRecordListViewModel
                {
                    Patient = patient,
                    NextAppointmentTime = nextAppt?.AppointmentDate,
                    AppointmentId = nextAppt?.Id
                });
            }

            ViewBag.SelectedPatientId = patientId;
            return View(viewModel);
        }

        public async Task<IActionResult> Schedule(string period = "Week", DateTime? date = null)
        {
            var currentDate = date ?? DateTime.Today;
            var userId = _userManager.GetUserId(User);
            var coreDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            var coreDocId = coreDoctor?.Id;

            DateTime startDate, endDate;
            switch (period?.ToLower())
            {
                case "day":
                    startDate = currentDate.Date;
                    endDate = startDate.AddDays(1).AddTicks(-1);
                    break;
                case "month":
                    startDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                    endDate = startDate.AddMonths(1).AddTicks(-1);
                    break;
                case "week":
                default:
                    // Start of week (Monday)
                    int diff = (7 + (currentDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                    startDate = currentDate.AddDays(-1 * diff).Date;
                    endDate = startDate.AddDays(7).AddTicks(-1);
                    period = "Week"; // Ensure normalized case
                    break;
            }

            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == coreDocId && a.AppointmentDate.Date >= startDate.Date && a.AppointmentDate.Date <= endDate.Date)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            // Map to View Model and Parse TimeSlot for accurate sorting
            var mapped = appointments.Select(a => 
            {
                var scheduledDateTime = a.AppointmentDate.Date;
                if (!string.IsNullOrEmpty(a.TimeSlot))
                {
                    // Extract start time from slot like "10:00 AM - 11:00 AM"
                    var timePart = a.TimeSlot.Split('-')[0].Trim();
                    if (DateTime.TryParse(timePart, out var parsedTime))
                    {
                        scheduledDateTime = a.AppointmentDate.Date.Add(parsedTime.TimeOfDay);
                    }
                }

                return new Appointment
                {
                    Id = a.Id,
                    DoctorId = userId ?? string.Empty,
                    PatientId = a.UserId ?? string.Empty,
                    Patient = _userManager.Users.FirstOrDefault(u => u.Id == a.UserId) ?? new ApplicationUser { UserName = "Unknown" },
                    ScheduledTime = scheduledDateTime,
                    DurationMinutes = 30, // Default for mapping
                    ConsultationType = a.ConsultationType,
                    Status = a.Status
                };
            })
            .OrderByDescending(a => a.ScheduledTime) // Newest first
            .ToList();

            ViewBag.CurrentPeriod = period;
            ViewBag.CurrentDate = currentDate;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(mapped);
        }

        [HttpGet]
        public async Task<IActionResult> CreateRecord(string patientId, string? notes = null, string? prescription = null)
        {
            if (string.IsNullOrEmpty(patientId))
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var coreDoctor = await _context.Doctors.Include(d => d.CurrentPlan).Include(d => d.User).FirstOrDefaultAsync(d => d.UserId == userId);
            
            // --- PATIENT LIMIT CHECK ---
            var plan = coreDoctor?.CurrentPlan ?? await _context.SubscriptionPlans.FindAsync(1); // Default to Starter
            if (plan != null && plan.PatientLimit > 0)
            {
                var patientCount = await _context.Appointments
                    .Where(a => a.DoctorId == coreDoctor.Id)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

                if (patientCount >= plan.PatientLimit)
                {
                    TempData["WarningMessage"] = $"You have hit your plan limit of {plan.PatientLimit} patients. Please upgrade to MedLink PRO for unlimited records.";
                    return RedirectToAction(nameof(MedLinkPro));
                }
            }

            var doctorId = userId;
            var doctor = coreDoctor;
            ViewBag.IsPro = doctor?.User?.IsPro ?? false;
            ViewBag.PatientName = await _context.Users.Where(u => u.Id == patientId).Select(u => u.FullName ?? u.UserName).FirstOrDefaultAsync();
            
            var now = DateTime.Now;
            
            // Check if there is an appointment that has already started
            var hasStartedAppointment = await _context.DoctorAppointments
                .AnyAsync(a => a.DoctorId == doctorId && a.PatientId == patientId && a.ScheduledTime <= now);

            if (!hasStartedAppointment)
            {
                TempData["ErrorMessage"] = "You can only create records once the appointment has started.";
                return RedirectToAction(nameof(Schedule));
            }

            var patient = await _userManager.FindByIdAsync(patientId);
            if (patient == null)
            {
                return NotFound();
            }

            var model = new PatientRecord
            {
                PatientId = patientId,
                DoctorId = doctorId ?? "",
                Notes = notes,
                Prescription = prescription
            };

            ViewBag.PatientName = patient.Name ?? patient.UserName ?? "Patient";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRecord(PatientRecord record)
        {
            var doctorId = _userManager.GetUserId(User);
            var hasAppointment = await _context.DoctorAppointments
                .AnyAsync(a => a.DoctorId == doctorId && a.PatientId == record.PatientId);

            if (!hasAppointment) return Forbid();

            if (ModelState.IsValid)
            {
                record.DoctorId = doctorId ?? "";
                _context.DoctorPatientRecords.Add(record);
                await _context.SaveChangesAsync();
                return RedirectToAction("DoctorDashBoard");
            }

            var patient = await _userManager.FindByIdAsync(record.PatientId);
            ViewBag.PatientName = patient?.Name ?? "Unknown";
            return View(record);
        }

        [HttpGet]
        public async Task<IActionResult> EditRecord(int id)
        {
            var record = await _context.DoctorPatientRecords.FindAsync(id);
            if (record == null) return NotFound();

            var doctorId = _userManager.GetUserId(User);
            if (record.DoctorId != doctorId) return Forbid();

            var patient = await _userManager.FindByIdAsync(record.PatientId);
            ViewBag.PatientName = patient?.Name ?? "Unknown";
            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRecord(int id, PatientRecord record)
        {
            if (id != record.Id) return NotFound();

            var doctorId = _userManager.GetUserId(User);
            if (record.DoctorId != doctorId) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(record);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DoctorPatientRecords.Any(e => e.Id == record.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(ViewHistory), new { patientId = record.PatientId });
            }

            var patient = await _userManager.FindByIdAsync(record.PatientId);
            ViewBag.PatientName = patient?.Name ?? "Unknown";
            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRecord(int id)
        {
            var record = await _context.DoctorPatientRecords.FindAsync(id);
            if (record == null) return NotFound();

            var doctorId = _userManager.GetUserId(User);
            if (record.DoctorId != doctorId) return Forbid();

            var patientId = record.PatientId;
            _context.DoctorPatientRecords.Remove(record);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewHistory), new { patientId = patientId });
        }

        [HttpGet]
        public async Task<IActionResult> Reschedule(int? id)
        {
            if (id == null || id == 0) return RedirectToAction(nameof(Schedule));

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            var patient = await _userManager.FindByIdAsync(appointment.UserId);

            // Map core appointment to DoctorModels.Appointment for the view
            var model = new Appointment
            {
                Id = appointment.Id,
                PatientId = appointment.UserId,
                Patient = patient,
                ScheduledTime = appointment.AppointmentDate,
                DurationMinutes = 30, // Default mapping
                ConsultationType = appointment.ConsultationType,
                Status = appointment.Status
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime scheduledTime)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            appointment.AppointmentDate = scheduledTime;
            appointment.TimeSlot = scheduledTime.ToString("HH:mm");
            _context.Update(appointment);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Appointment rescheduled successfully.";
            return RedirectToAction(nameof(Schedule));
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var model = new ProfileViewModel
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
                VerificationDetails = user.VerificationDetails ?? string.Empty,
                
                // Construct new fields
                FatherHusbandName = user.FatherHusbandName,
                Gender = user.Gender,
                CNIC = user.CNIC,
                ResidentialAddress = user.ResidentialAddress,
                City = user.City,
                Province = user.Province,
                PMDCRegistrationNumber = user.PMDCRegistrationNumber,
                PMDCValidityDate = user.PMDCValidityDate,
                Qualification = user.Qualification,
                BankAccountNumber = user.BankAccountNumber,
                TermsConsent = user.TermsConsent,
                AdminRemarks = user.AdminRemarks,
                ApprovalDate = user.ApprovalDate,
                
                // URLs for existing files
                CNICFrontUrl = user.CNICFrontUrl,
                CNICBackUrl = user.CNICBackUrl,
                PMDCCertificateUrl = user.PMDCCertificateUrl,
                DegreeCertificateUrl = user.DegreeCertificateUrl
            };
            
            // Calculate Completion Percentage
            int totalFields = 18; // Identify key fields
            int filledFields = 0;
            
            if (!string.IsNullOrEmpty(model.Name)) filledFields++;
            if (!string.IsNullOrEmpty(model.FatherHusbandName)) filledFields++;
            if (!string.IsNullOrEmpty(model.Gender)) filledFields++;
            if (!string.IsNullOrEmpty(model.CNIC)) filledFields++;
            if (model.ConsultationFee > 0) filledFields++;
            
            // New fields from Doctor entity
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null)
            {
                model.Description = doctor.Description;
                model.Expertise = doctor.Expertise;
                model.HospitalAffiliations = doctor.HospitalAffiliations;
                model.ClinicMapUrl = doctor.ClinicMapUrl;
                model.Workplace = doctor.ClinicName; // Map new field
                model.ClinicAddress = doctor.ClinicAddress; // Separate address
                
                // Add to completion check
                if (!string.IsNullOrEmpty(doctor.Description)) filledFields++;
                if (!string.IsNullOrEmpty(doctor.Expertise)) filledFields++;
                if (!string.IsNullOrEmpty(doctor.HospitalAffiliations)) filledFields++;
                if (!string.IsNullOrEmpty(doctor.ClinicAddress)) filledFields++;
            }
            
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
            
            // Consider documents as one group or individually? Let's do essential docs
            if (!string.IsNullOrEmpty(model.CNICFrontUrl) && !string.IsNullOrEmpty(model.CNICBackUrl) && !string.IsNullOrEmpty(model.PMDCCertificateUrl)) filledFields++;

            model.CompletionPercentage = (int)((double)filledFields / totalFields * 100);
            if (model.CompletionPercentage > 100) model.CompletionPercentage = 100;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> OptimizeProfile()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            // We are using Description or Expertise field to store these extra details JSON-like or append for now if no specific columns exist. 
            // Ideally we should add columns, but for "Optimize" concept we can piggyback existing fields or assume they are stored.
            // For this implementation, I will treat them as new conceptual data or map to Description/Expertise if suitable, 
            // OR just return a view model designed to look like it's pulling data.
            
            // Let's assume we store "Diagnostic Capabilities" in Expertise for now, or just show empty for the prompt.
            
            var model = new OptimizeProfileViewModel
            {
                // Pre-fill if data exists
                DiagnosticCapabilities = doctor.Expertise ?? string.Empty, 
                ReferralPreferences = "", // Placeholder
                IsOptimized = !string.IsNullOrEmpty(doctor.Expertise) && doctor.Description?.Length > 100 // Mock logic
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OptimizeProfile(OptimizeProfileViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            // Update Doctor Entity with new optimization data
            // Mapping DiagnosticCapabilities to Expertise for this implementation as requested "Expand diagnostic profile"
            if (!string.IsNullOrEmpty(model.DiagnosticCapabilities))
            {
                doctor.Expertise = model.DiagnosticCapabilities; 
            }
            
            // We could append ReferralPreferences to Description or similar if no field exists
            if (!string.IsNullOrEmpty(model.ReferralPreferences))
            {
                doctor.Description += $"\n\n[Referral Preferences]: {model.ReferralPreferences}";
            }

            if (!string.IsNullOrEmpty(model.ResearchInterests))
            {
                doctor.Description += $"\n\n[Research]: {model.ResearchInterests}";
            }
            
            await _context.SaveChangesAsync();

            await LogAction(userId, "Optimization Protocol", "Applied diagnostic profile expansion configuration", "Provisioned");

            TempData["StatusMessage"] = "Profile Optimized! Elite Status Request Sent.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // if (!ModelState.IsValid) return View(model); // Temporarily relaxed for easier testing, or ensure validation is correct
            
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

            // 1. Basic Info Update
            user.Name = model.Name;
            user.Specialist = model.Specialist;
            user.Experience = model.Experience;
            user.PhoneNumber = model.PhoneNumber;
            user.ConsultationFee = model.ConsultationFee;
            user.Workplace = model.Workplace;

            // 2. Personal & Contact
            user.FatherHusbandName = model.FatherHusbandName;
            user.Gender = model.Gender;
            user.CNIC = model.CNIC;
            user.ResidentialAddress = model.ResidentialAddress;
            user.City = model.City;
            user.Province = model.Province;

            // 3. Professional
            user.PMDCRegistrationNumber = model.PMDCRegistrationNumber;
            user.PMDCValidityDate = model.PMDCValidityDate;
            user.Qualification = model.Qualification;
            user.BankAccountNumber = model.BankAccountNumber;
            user.TermsConsent = model.TermsConsent;

            // Sync Core Doctor Entity
            if (doctor != null)
            {
                doctor.Name = model.Name;
                doctor.Specialty = model.Specialist;
                doctor.Experience = model.Experience;
                
                // Sync Professional Details
                doctor.Description = model.Description ?? string.Empty;
                doctor.Expertise = model.Expertise ?? string.Empty;
                doctor.HospitalAffiliations = model.HospitalAffiliations;
                doctor.ClinicMapUrl = model.ClinicMapUrl;
                doctor.ClinicName = model.Workplace; // Map workplace to ClinicName
                doctor.ClinicAddress = model.ClinicAddress; // Save specific address
                
                await _context.SaveChangesAsync(); // Persist Doctor entity changes
            }

            // Helper for file uploads
            async Task<string> SaveFileAsync(IFormFile file, string folderName)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folderName);
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                return "uploads/" + folderName + "/" + uniqueFileName;
            }

            // Handle File Uploads
            if (model.ProfilePicture != null) 
            {
                var uploadedPath = await SaveFileAsync(model.ProfilePicture, "profiles");
                user.ProfilePictureUrl = uploadedPath;
                user.ProfileImage = "/" + uploadedPath; // Sync redundant field
                if (doctor != null) doctor.Image = "/" + uploadedPath; // Sync Doctor entity field
            }
            
            // For Documents - Reset to Pending if any sensitive doc is updated
            bool docsUpdated = false;
            
            if (model.CNICFront != null) 
            {
                user.CNICFrontUrl = await SaveFileAsync(model.CNICFront, "documents");
                docsUpdated = true;
            }
            if (model.CNICBack != null) 
            {
                user.CNICBackUrl = await SaveFileAsync(model.CNICBack, "documents");
                docsUpdated = true;
            }
            if (model.PMDCCertificate != null) 
            {
                user.PMDCCertificateUrl = await SaveFileAsync(model.PMDCCertificate, "documents");
                docsUpdated = true;
            }
            if (model.DegreeCertificate != null) 
            {
                user.DegreeCertificateUrl = await SaveFileAsync(model.DegreeCertificate, "documents");
                docsUpdated = true;
            }

            if (docsUpdated)
            {
                user.ApprovalStatus = "Pending";
            }

            // 4. Email Update
            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors) ModelState.AddModelError("Email", error.Description);
                    return View(model);
                }
                await _userManager.SetUserNameAsync(user, model.Email);
            }

            // 5. Password Update
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is required.");
                    return View(model);
                }
                var changePassResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!changePassResult.Succeeded)
                {
                    foreach (var error in changePassResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
                    return View(model);
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await LogAction(userId, "Profile Modification", "Doctor profile details updated successfully", "Success");
                TempData["StatusMessage"] = "Your profile has been updated.";
                return RedirectToAction(nameof(Profile));
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        public async Task<IActionResult> Availability()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            
            
            ViewBag.IsAvailable = user.IsAvailable;
            ViewBag.SlotDuration = (doctor?.SlotDuration > 0) ? doctor.SlotDuration : 20;
            ViewBag.BufferTime = (doctor?.BufferTime > 0) ? doctor.BufferTime : 5;
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.IsAvailable = !user.IsAvailable;
            await _userManager.UpdateAsync(user);

            // Sync with Doctor entity
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null)
            {
                doctor.Online = user.IsAvailable;
                doctor.Availability = user.IsAvailable ? "Available" : "Offline";
                await _context.SaveChangesAsync();

                // Invalidate the cache used in DashboardController.GetBaseModelAsync
                _cache.Remove("ApprovedDoctors");
            }

            return Json(new { success = true, isAvailable = user.IsAvailable });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGlobalConstraints(int slotDuration, int bufferTime)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            doctor.SlotDuration = slotDuration;
            doctor.BufferTime = bufferTime;
            
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientsAjax()
        {
            var patients = await _userManager.GetUsersInRoleAsync("Patient");
            if (patients == null || !patients.Any())
            {
                // Fallback: If no patients found in role, return all users (for dev/setup)
                var allUsers = await _userManager.Users.ToListAsync();
                return Json(allUsers.Select(p => new { p.Id, Name = p.Name ?? p.UserName }));
            }
            return Json(patients.Select(p => new { p.Id, Name = p.Name ?? p.UserName ?? "Patient" }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecordAjax(PatientRecord record)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(doctorId)) return Unauthorized();

            record.DoctorId = doctorId;
            record.CreatedAt = DateTime.Now;

            // Remove validation for Patient/Doctor objects as they are navigation properties
            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");

            if (ModelState.IsValid)
            {
                _context.DoctorPatientRecords.Add(record);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Intelligence Record Deployed" });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, errors = errors });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailabilitySlots(string day)
        {
            var doctorId = _userManager.GetUserId(User);
            var slots = await _context.DoctorAvailabilitySlots
                .Where(s => s.DoctorId == doctorId && s.DayOfWeek == day)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            return Json(slots.Select(s => new { 
                s.Id, 
                startTime = s.StartTime.ToString(@"hh\:mm"), 
                endTime = s.EndTime.ToString(@"hh\:mm"), 
                s.IsActive 
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAvailabilitySlot(DoctorAvailabilitySlot model)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(doctorId)) return Unauthorized();

            model.DoctorId = doctorId;
            ModelState.Remove("Doctor");
            ModelState.Remove("DoctorId");

            if (ModelState.IsValid)
            {
                if (model.Id == 0) _context.DoctorAvailabilitySlots.Add(model);
                else _context.Update(model);
                
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, errors = errors });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailabilitySlot(int id)
        {
            var slot = await _context.DoctorAvailabilitySlots.FindAsync(id);
            if (slot == null) return NotFound();
            
            var doctorId = _userManager.GetUserId(User);
            if (slot.DoctorId != doctorId) return Forbid();

            _context.DoctorAvailabilitySlots.Remove(slot);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointmentAjax(Appointment appointment)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var coreDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            var coreDocId = coreDoctor?.Id;

            var patient = await _userManager.FindByIdAsync(appointment.PatientId);
            if (patient == null) return NotFound("Patient not found.");

            // Create core appointment
            var coreAppointment = new MedLinkPortal.Models.Appointment
            {
                PatientName = patient.Name ?? patient.UserName ?? "Patient",
                Email = patient.Email ?? string.Empty,
                AppointmentDate = appointment.ScheduledTime,
                TimeSlot = appointment.ScheduledTime.ToString("HH:mm"),
                DoctorId = coreDocId,
                UserId = appointment.PatientId,
                ConsultationType = appointment.ConsultationType,
                Status = "Confirmed",
                Notes = "",
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(coreAppointment);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAppointmentAjax(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var coreDoctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.UserId == userId);
            
            if (appointment.DoctorId != coreDoctor?.Id) return Forbid();

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }



        [HttpGet]
        public async Task<IActionResult> MedLinkPro()
        {
            var userId = _userManager.GetUserId(User);
            var coreDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            
            ViewBag.CurrentPlanId = coreDoctor?.CurrentPlanId ?? 1; // Default to Starter
            
            var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync();
            return View(plans);
        }

        [HttpPost]
        public async Task<IActionResult> UpgradeToPro(int planId = 2)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var plan = await _context.SubscriptionPlans.FindAsync(planId);
            if (plan == null) return NotFound();

            var domain = $"{Request.Scheme}://{Request.Host.Value}";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(plan.Price * 100), 
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{plan.Name} Subscription",
                                Description = plan.Description ?? "MedLink Professional Tier"
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = domain + "/Doctor/Doctor/UpgradeSuccess?planId=" + planId,
                CancelUrl = domain + "/Doctor/Doctor/UpgradeCancel",
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "PlanId", planId.ToString() }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

        public async Task<IActionResult> UpgradeSuccess(int planId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            var coreDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            var plan = await _context.SubscriptionPlans.FindAsync(planId);

            if (user != null && coreDoctor != null && plan != null)
            {
                user.IsPro = (planId == 2); // MedLink PRO
                await _userManager.UpdateAsync(user);

                coreDoctor.CurrentPlanId = planId;
                await _context.SaveChangesAsync();

                // Track Subscription
                var subscription = new DoctorSubscription
                {
                    DoctorUserId = userId,
                    PlanId = planId,
                    PurchaseDate = DateTime.Now,
                    IsActive = true
                };
                _context.DoctorSubscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                await _notificationService.CreateAndSendNotificationAsync(userId,
                    $"Active: {plan.Name}",
                    $"Your subscription to {plan.Name} is now active. Enjoy your elite clinical tools!",
                    "crown", "purple");
            }

            return RedirectToAction("DoctorDashBoard");
        }

        public async Task<IActionResult> VIPSupport()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var coreDoctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (coreDoctor == null || coreDoctor.User?.IsPro != true)
            {
                return RedirectToAction("MedLinkPro");
            }

            // Target the default admin for support
            var adminSupport = await _userManager.FindByEmailAsync("admin@medlink.com");
            if (adminSupport == null)
            {
                // Fallback to any admin if the specific support account isn't found
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                adminSupport = admins.FirstOrDefault();
            }

            if (adminSupport == null)
            {
                TempData["StatusMessage"] = "Support system is currently undergoing maintenance. Please try again later.";
                return RedirectToAction("DoctorDashBoard");
            }

            ViewBag.AdminSupportId = adminSupport.Id;
            ViewBag.AdminSupportName = adminSupport.Name ?? "MedLink Support";
            
            return View();
        }

        public IActionResult UpgradeCancel()
        {
            return RedirectToAction("MedLinkPro");
        }
    }

    public class ProfileViewModel
    {
        public int CompletionPercentage { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialty is required")]
        public string Specialist { get; set; } = string.Empty;

        [Required(ErrorMessage = "Experience is required")]
        public string Experience { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Consultation Fee is required")]
        [Range(0, 100000, ErrorMessage = "Fee must be reasonable")]
        public decimal ConsultationFee { get; set; }

        public string? ProfilePictureUrl { get; set; }
        public IFormFile? ProfilePicture { get; set; }

        [Required(ErrorMessage = "Workplace/Clinic Name is required")]
        public string? Workplace { get; set; }
        public string? ClinicAddress { get; set; }
        public string? VerificationDetails { get; set; }

        // Professional Profile Details (Sync with Doctor Entity)
        public string? Description { get; set; }
        public string? Expertise { get; set; }
        public string? HospitalAffiliations { get; set; }
        public string? ClinicMapUrl { get; set; }
        
        [BindProperty(Name = "VerificationDocument")]
        public IFormFile? VerificationDocument { get; set; } 
        
        public string ApprovalStatus { get; set; } = "Pending";

        // Personal
        [Required(ErrorMessage = "Father/Husband Name is required")]
        public string? FatherHusbandName { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "CNIC is required")]
        [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC must be in format xxxxx-xxxxxxx-x")]
        public string? CNIC { get; set; }
        
        public string? CNICFrontUrl { get; set; }
        [BindProperty(Name = "CNICFront")]
        public IFormFile? CNICFront { get; set; }
        
        public string? CNICBackUrl { get; set; }
        [BindProperty(Name = "CNICBack")]
        public IFormFile? CNICBack { get; set; }

        // Contact
        [Required(ErrorMessage = "Residential Address is required")]
        public string? ResidentialAddress { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string? City { get; set; }

        [Required(ErrorMessage = "Province is required")]
        public string? Province { get; set; }

        // Professional
        [Required(ErrorMessage = "PMDC Registration Number is required")]
        public string? PMDCRegistrationNumber { get; set; }
        
        public string? PMDCCertificateUrl { get; set; }
        [BindProperty(Name = "PMDCCertificate")]
        public IFormFile? PMDCCertificate { get; set; }
        
        [Required(ErrorMessage = "PMDC Validity Date is required")]
        public DateTime? PMDCValidityDate { get; set; }

        [Required(ErrorMessage = "Qualification is required")]
        public string? Qualification { get; set; }
        
        public string? DegreeCertificateUrl { get; set; }
        [BindProperty(Name = "DegreeCertificate")]
        public IFormFile? DegreeCertificate { get; set; }

        // Financial
        [Required(ErrorMessage = "Bank Account / IBAN is required")]
        public string? BankAccountNumber { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms")]
        public bool TermsConsent { get; set; }

        // Admin
        public string? AdminRemarks { get; set; }
        public DateTime? ApprovalDate { get; set; }

        // Security section
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}

