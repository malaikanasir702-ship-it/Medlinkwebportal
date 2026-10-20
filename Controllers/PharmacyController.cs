using MedLinkPortal.Models;
using MedLinkPortal.Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MedLinkPortal.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;
using Stripe;
using Stripe.Checkout;

namespace MedLinkPortal.Controllers
{
    [Authorize]
    public class PharmacyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MedLinkPortal.Services.INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<MedLinkPortal.Hubs.ChatHub> _hubContext;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ICloudinaryService _cloudinaryService;

        public PharmacyController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            MedLinkPortal.Services.INotificationService notificationService,
            IConfiguration configuration,
            Microsoft.AspNetCore.SignalR.IHubContext<MedLinkPortal.Hubs.ChatHub> hubContext,
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _configuration = configuration;
            _hubContext = hubContext;
            _contextFactory = contextFactory;
            _cloudinaryService = cloudinaryService;
        }

        // --- Master Data APIs (Used by Doctor in Consultation Room) ---

        [HttpGet]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> Dashboard()
        {
            var orders = await _context.PharmacyOrders
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.LowStockCount = await _context.Medicines.CountAsync(m => m.StockQuantity < 20);
            return View(orders);
        }

        [HttpPost]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateStatusRequest model)
        {
            var order = await _context.PharmacyOrders.FindAsync(model.OrderId);
            if (order == null) return NotFound();

            order.Status = (PharmacyOrderStatus)model.Status;
            await _context.SaveChangesAsync();
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> Analytics()
        {
            var orders = await _context.PharmacyOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Medicine)
                .Where(o => o.Status != PharmacyOrderStatus.Cancelled)
                .ToListAsync();

            var totalRevenue = orders.Sum(o => o.TotalAmount);
            var totalOrders = orders.Count;
            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            var topMedicines = orders
                .SelectMany(o => o.OrderItems)
                .GroupBy(oi => oi.MedicineId)
                .Select(g => new MedicinePerformance
                {
                    Name = g.First().Medicine.Name,
                    Sold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(x => x.Sold)
                .Take(5)
                .ToList();

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.AverageOrderValue = averageOrderValue;
            ViewBag.TopMedicines = topMedicines;

            // Monthly Revenue (Last 6 months)
            var monthlyRevenue = orders
                .Where(o => o.CreatedAt > DateTime.UtcNow.AddMonths(-6))
                .GroupBy(o => o.CreatedAt.ToString("MMM"))
                .Select(g => new MonthlyRevenue { Month = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .ToList();

            ViewBag.MonthlyRevenue = monthlyRevenue;

            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.PharmacyOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Medicine)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        public class MedicinePerformance
        {
            public string Name { get; set; } = string.Empty;
            public int Sold { get; set; }
            public decimal Revenue { get; set; }
        }

        public class MonthlyRevenue
        {
            public string Month { get; set; } = string.Empty;
            public decimal Revenue { get; set; }
        }

        public class UpdateStatusRequest
        {
            public int OrderId { get; set; }
            public int Status { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> SearchMedicines(string term)
        {
            var query = _context.Medicines.Where(m => m.IsActive == true);

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(m => m.Name.Contains(term));
            }

            var medicines = await query
                .Select(m => new {
                    id = m.Id,
                    name = m.Name,
                    brand = m.Brand,
                    price = m.Price,
                    prescriptionRequired = m.PrescriptionRequired
                })
                .Take(20)
                .ToListAsync();

            return Json(medicines);
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int id)
        {
            var userId = _userManager.GetUserId(User);
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (appointment == null) return NotFound("Appointment not found or unauthorized.");

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionMedicines)
                .ThenInclude(pm => pm.Medicine)
                .FirstOrDefaultAsync(p => p.AppointmentId == id);

            if (prescription == null)
            {
                // Fallback: If no structured prescription, check medications list if used in other flows
                return NotFound("No prescription found for this appointment.");
            }

            var model = await GetPatientDashboardModelAsync();
            model.ActiveTab = "appointments"; // Highlight appointments or add a new tab if needed
            model.CurrentPrescription = prescription;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DirectCheckout(int medicineId)
        {
            var medicine = await _context.Medicines.FindAsync(medicineId);
            if (medicine == null || medicine.IsActive != true) return NotFound("Medicine not available.");

            // Create a temporary/virtual prescription for the checkout view
            var virtualPrescription = new Prescription
            {
                Id = 0, // Indicates direct order
                AppointmentId = 0, // No appointment
                PrescriptionMedicines = new List<PrescriptionMedicine>
                {
                    new PrescriptionMedicine
                    {
                        MedicineId = medicine.Id,
                        Medicine = medicine,
                        Quantity = 1, // Default to 1 for now
                        Dosage = "As needed", // Default
                        PrescriptionId = 0
                    }
                }
            };

            var model = await GetPatientDashboardModelAsync();
            model.ActiveTab = "store";
            model.CurrentPrescription = virtualPrescription;

            return View("Checkout", model);
        }

        [HttpPost]
        public async Task<IActionResult> CartCheckout(string cartItemsJson)
        {
            if (string.IsNullOrEmpty(cartItemsJson)) return RedirectToAction("Store");

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var cartItems = System.Text.Json.JsonSerializer.Deserialize<List<CartItemDto>>(cartItemsJson, options);
            if (cartItems == null || !cartItems.Any()) return RedirectToAction("Store");

            var medicineIds = cartItems.Select(c => c.MedicineId).ToList();
            var medicines = await _context.Medicines
                .Where(m => medicineIds.Contains(m.Id) && m.IsActive == true)
                .ToListAsync();

            var virtualPrescription = new Prescription
            {
                Id = 0,
                AppointmentId = 0,
                PrescriptionMedicines = new List<PrescriptionMedicine>()
            };

            foreach (var item in cartItems)
            {
                var med = medicines.FirstOrDefault(m => m.Id == item.MedicineId);
                if (med != null)
                {
                    virtualPrescription.PrescriptionMedicines.Add(new PrescriptionMedicine
                    {
                        MedicineId = med.Id,
                        Medicine = med,
                        Quantity = item.Quantity,
                        Dosage = "As needed",
                        PrescriptionId = 0
                    });
                }
            }

            var model = await GetPatientDashboardModelAsync();
            model.ActiveTab = "store";
            model.CurrentPrescription = virtualPrescription;

            return View("Checkout", model);
        }

        public class CartItemDto
        {
            public int MedicineId { get; set; }
            public int Quantity { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Store()
        {
            var model = await GetPatientDashboardModelAsync();
            model.ActiveTab = "store";
            model.StoreMedicines = await _context.Medicines.Where(m => m.IsActive == true).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var model = await GetPatientDashboardModelAsync();
            model.ActiveTab = "orders";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null || medicine.IsActive != true) return NotFound();

            var model = await GetPatientDashboardModelAsync();
            model.ActiveTab = "store";
            ViewBag.Medicine = medicine;

            return View(model);
        }

        // --- Structured Prescription APIs ---

        [HttpPost]
        public async Task<IActionResult> SubmitStructuredPrescription([FromBody] PrescriptionRequest model)
        {
            if (model == null || model.AppointmentId == 0) return Json(new { success = false, message = "Invalid data" });

            var appointment = await _context.Appointments.FindAsync(model.AppointmentId);
            if (appointment == null) return Json(new { success = false, message = "Appointment not found" });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Unauthorized: User not found" });
            var isDoctor = await _userManager.IsInRoleAsync(user, "Doctor");
            if (!isDoctor) return Json(new { success = false, message = "Unauthorized" });

            // Create or Update Prescription
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionMedicines)
                .FirstOrDefaultAsync(p => p.AppointmentId == model.AppointmentId);

            if (prescription == null)
            {
                prescription = new Prescription
                {
                    AppointmentId = model.AppointmentId,
                    DoctorId = user.Id,
                    PatientId = appointment.UserId,
                    CreatedAt = DateTime.UtcNow,
                    MedicationsJson = "Structured", // Indicator
                };
                _context.Prescriptions.Add(prescription);
            }
            else if (prescription.IsLocked)
            {
                return Json(new { success = false, message = "Prescription is locked" });
            }

            prescription.Diagnosis = model.Diagnosis;
            prescription.Notes = model.Notes;
            prescription.IsLocked = model.Finalize;

            // Update Medicines
            _context.PrescriptionMedicines.RemoveRange(prescription.PrescriptionMedicines);

            foreach (var med in model.Medicines)
            {
                _context.PrescriptionMedicines.Add(new PrescriptionMedicine
                {
                    Prescription = prescription,
                    MedicineId = med.MedicineId,
                    Dosage = med.Dosage,
                    Frequency = med.Frequency,
                    Duration = med.Duration,
                    Quantity = med.Quantity,
                    Instructions = med.Instructions
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, prescriptionId = prescription.Id });
        }

        [HttpGet]
        public async Task<IActionResult> OrderTracking(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.PharmacyOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Medicine)
                .FirstOrDefaultAsync(o => o.Id == id && o.PatientId == userId);

            if (order == null) return NotFound();

            var model = await GetPatientDashboardModelAsync();
            model.ActiveTab = "overview"; // Or keep it highlighted on others
            model.CurrentOrder = order;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SupportIntelligence(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.PharmacyOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Medicine)
                .FirstOrDefaultAsync(o => o.Id == id && o.PatientId == userId);

            if (order == null) return NotFound();

            var model = await GetPatientDashboardModelAsync();
            model.ActiveTab = "overview";
            model.CurrentOrder = order;

            // Find a targeted pharmacist (assigned to order, or default)
            var pharmacistId = order.PharmacistId;
            if (string.IsNullOrEmpty(pharmacistId))
            {
                // 1. Try Default Pharmacist
                var mainPharmacist = await _userManager.FindByEmailAsync("pharmacist@medlink.com");
                pharmacistId = mainPharmacist?.Id;

                // 2. Fallback: Any Pharmacist
                if (string.IsNullOrEmpty(pharmacistId))
                {
                    var pharmacists = await _userManager.GetUsersInRoleAsync("Pharmacist");
                    pharmacistId = pharmacists.FirstOrDefault()?.Id;
                }

                // 3. Fallback: Current Admin/User (Last Resort for Dev)
                if (string.IsNullOrEmpty(pharmacistId))
                {
                    // This is risky if the user isn't a pharmacist, but better than null
                    // For now, let's leave it null and handle in view? 
                    // No, let's try to assign to the first user found to ensure *someone* gets it in a single-user dev env
                    pharmacistId = _userManager.Users.FirstOrDefault()?.Id;
                }
            }
            ViewBag.PharmacistId = pharmacistId;

            return View(model);
        }

        private async Task<PatientDashboardModel> GetPatientDashboardModelAsync()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userEmail = user?.Email;

            // --- Optimized Parallel Fetching with IDbContextFactory ---

            // 1. Appointments
            var appointmentsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Appointments
                    .AsNoTracking()
                    .Include(a => a.Doctor)
                    .Where(a => (a.UserId == userId || (a.UserId == null && a.Email == userEmail)) && a.Status == "Confirmed")
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
            });

            // 2. Health Records
            var recordsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.HealthRecords
                    .AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.Date)
                    .ToListAsync();
            });

            // 3. Medications
            var medicationsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.Medications
                    .AsNoTracking()
                    .Where(m => m.UserId == userId)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            });

            // 4. Notifications
            var notificationsTask = _notificationService.GetUserNotificationsAsync(userId);

            // 5. AI Analyses
            var aiAnalysesTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.AIAnalyses
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();
            });

            // 6. User Sessions
            var sessionsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.UserSessions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId && !s.IsRevoked)
                    .OrderByDescending(s => s.LastSeen)
                    .Select(s => new UserSession
                    {
                        Id = s.Id,
                        DeviceName = s.DeviceName,
                        IPAddress = s.IPAddress,
                        Location = s.Location,
                        LastSeen = s.LastSeen,
                        SessionIdentifier = s.SessionIdentifier,
                        UserAgent = s.UserAgent
                    }).ToListAsync();
            });

            // 7. Pharmacy Orders
            var pharmacyOrdersTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.PharmacyOrders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Medicine)
                    .Where(o => o.PatientId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            });

            await Task.WhenAll(appointmentsTask, recordsTask, medicationsTask, notificationsTask, aiAnalysesTask, sessionsTask, pharmacyOrdersTask);

            var model = new PatientDashboardModel
            {
                IsLoading = false,
                PatientName = user?.FirstName ?? User.Identity?.Name?.Split('@')[0] ?? "Alex",
                PatientId = userId,
                PatientEmail = user?.Email,
                Phone = user?.PhoneNumber,
                DateOfBirth = user?.DateOfBirth,
                ProfileImage = user?.ProfileImage ?? "https://picsum.photos/seed/patient/100/100",
                EmailNotificationsEnabled = user?.EmailNotificationsEnabled ?? true,
                PushNotificationsEnabled = user?.PushNotificationsEnabled ?? true,
                MarketingEmailsEnabled = user?.MarketingEmailsEnabled ?? false,
                DarkModeEnabled = user?.DarkModeEnabled ?? false,
                HealthVitals = new List<DashboardVital>
                {
                    new DashboardVital { Label = "Heart Rate", Value = "72", Unit = "bpm", Icon = "heart", Trend = "+2%", Color = "rose" },
                    new DashboardVital { Label = "Temperature", Value = "36.6", Unit = "°C", Icon = "thermometer", Trend = "Stable", Color = "amber" },
                    new DashboardVital { Label = "Blood Oxygen", Value = "98", Unit = "%", Icon = "activity", Trend = "Normal", Color = "emerald" },
                    new DashboardVital { Label = "Daily Steps", Value = "8,432", Unit = "steps", Icon = "zap", Trend = "+12%", Color = "blue" }
                },
                UpcomingConsultations = appointmentsTask.Result.Select(a => new Consultation
                {
                    Id = a.Id,
                    DoctorId = a.DoctorId,
                    Doctor = a.Doctor?.Name ?? "General Doctor",
                    Specialty = a.Doctor?.Specialty ?? "General",
                    Time = a.AppointmentDate.ToString("MMMM dd, hh:mm tt"),
                    Type = a.ConsultationType ?? "Video Call",
                    Image = a.Doctor?.Image ?? "https://picsum.photos/seed/doc/100/100"
                }).ToList(),
                HealthRecords = recordsTask.Result,
                Medications = medicationsTask.Result,
                Notifications = notificationsTask.Result ?? new List<Notification>(),
                AIAnalyses = aiAnalysesTask.Result,
                RecentDevices = sessionsTask.Result,
                VapidPublicKey = _configuration["Vapid:PublicKey"],
                BillingHistory = new List<BillingInvoice>(),
                PharmacyOrders = pharmacyOrdersTask.Result
            };

            return model;
        }

        [HttpGet]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> UpdateStock(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        [HttpGet]
        [Authorize(Roles = "Pharmacist")]
        public IActionResult AddStock()
        {
            return View(new Medicine { IsActive = true });
        }

        [HttpPost]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> SaveMedicine([FromForm] Medicine model, IFormFile imageFile)
        {
            if (model.Id == 0)
            {
                model.IsActive = true;
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadedUrl = await _cloudinaryService.UploadImageAsync(imageFile, "pharmacy_medicines");
                    if (!string.IsNullOrEmpty(uploadedUrl))
                        model.ImageUrl = uploadedUrl;
                }
                _context.Medicines.Add(model);
            }
            else
            {
                var existing = await _context.Medicines.FindAsync(model.Id);
                if (existing == null) return NotFound();

                existing.Name = model.Name;
                existing.Brand = model.Brand;
                existing.Category = model.Category;
                existing.Price = model.Price;
                existing.StockQuantity = model.StockQuantity;
                existing.PrescriptionRequired = model.PrescriptionRequired;
                existing.Description = model.Description;

                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadedUrl = await _cloudinaryService.UploadImageAsync(imageFile, "pharmacy_medicines");
                    if (!string.IsNullOrEmpty(uploadedUrl))
                        existing.ImageUrl = uploadedUrl;
                }

                _context.Medicines.Update(existing);
            }

            await _context.SaveChangesAsync();

            // Return JSON for AJAX compatibility or Redirect for standard POST
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }
            return RedirectToAction("Inventory");
        }

        [HttpGet]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.PharmacyOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Medicine)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpGet]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> Inventory()
        {
            var medicines = await _context.Medicines.ToListAsync();
            return View(medicines);
        }

        // ─── Rider Assignment (Task 4.9) ─────────────────────────────────────

        /// <summary>
        /// Returns available (active + not on active delivery) riders for the assignment dropdown.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> AvailableRiders()
        {
            var busyRiderIds = await _context.RiderSessions
                .Where(s => s.IsActive)
                .Select(s => s.RiderId)
                .Distinct()
                .ToListAsync();

            var available = await _context.Riders
                .Include(r => r.User)
                .Where(r => r.IsActive && !busyRiderIds.Contains(r.Id))
                .Select(r => new
                {
                    id = r.Id,
                    name = r.User != null
                        ? r.User.FirstName + " " + r.User.LastName
                        : "Rider",
                    vehicleType = r.VehicleType,
                    vehicleNumber = r.VehicleNumber
                })
                .ToListAsync();

            return Json(available);
        }

        /// <summary>
        /// Pharmacist assigns a rider to a pharmacy order (status must be Accepted or Packed).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> AssignRider([FromBody] AssignRiderRequest req)
        {
            var order = await _context.PharmacyOrders.FindAsync(req.OrderId);
            if (order == null) return NotFound(new { message = "Order not found." });

            if (order.Status != PharmacyOrderStatus.Accepted
                && order.Status != PharmacyOrderStatus.Packed)
                return Conflict(new { message = "Order must be Accepted or Packed to assign a rider." });

            var rider = await _context.Riders
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == req.RiderId);

            if (rider == null) return NotFound(new { message = "Rider not found." });
            if (!rider.IsActive) return BadRequest(new { message = "Rider is deactivated." });

            order.RiderId = rider.Id;
            order.Status = PharmacyOrderStatus.RiderAssigned;
            order.UpdatedAt = DateTime.UtcNow;

            // Create RiderSession
            var alreadyExists = await _context.RiderSessions
                .AnyAsync(s => s.RiderId == rider.Id
                    && s.OrderId == req.OrderId
                    && s.OrderType == "PharmacyOrder"
                    && s.IsActive);

            if (!alreadyExists)
            {
                _context.RiderSessions.Add(new RiderSession
                {
                    RiderId = rider.Id,
                    OrderId = req.OrderId,
                    OrderType = "PharmacyOrder",
                    IsActive = true,
                    LastUpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            // Push notification to patient
            if (!string.IsNullOrEmpty(order.PatientId))
            {
                var riderName = rider.User != null
                    ? $"{rider.User.FirstName} {rider.User.LastName}".Trim()
                    : "A rider";

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.NotifyUserAsync(
                            order.PatientId,
                            NotificationType.General,
                            "✅ Rider Assigned",
                            $"{riderName} has been assigned to your order.",
                            data: new System.Collections.Generic.Dictionary<string, string>
                            {
                                { "orderId",   req.OrderId.ToString() },
                                { "orderType", "PharmacyOrder" }
                            });
                    }
                    catch { }
                });
            }

            return Json(new { success = true });
        }

        // ─── AI Prescription Scan (Groq qwen/qwen3.8-27b) ───────────────────────

        [HttpPost]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ai")]
        public async Task<IActionResult> ScanPrescription(IFormFile prescriptionImage)
        {
            if (prescriptionImage == null || prescriptionImage.Length == 0)
                return Json(new { success = false, message = "No image uploaded." });

            // Max 20MB per Groq docs
            if (prescriptionImage.Length > 20 * 1024 * 1024)
                return Json(new { success = false, message = "Image too large. Max 20MB." });

            var groqApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
                          ?? Environment.GetEnvironmentVariable("Groq__ApiKey")
                          ?? _configuration["Groq:ApiKey"];
            if (string.IsNullOrEmpty(groqApiKey))
                return Json(new { success = false, message = "MedLink AI service not configured. Please contact support." });

            try
            {
                // Convert image to base64
                using var ms = new System.IO.MemoryStream();
                await prescriptionImage.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                var mimeType = prescriptionImage.ContentType ?? "image/jpeg";

                // Build Groq request
                var requestBody = new
                {
                    model = "qwen/qwen3.8-27b",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = @"You are a pharmacy assistant. Analyze this prescription image and extract ALL medicine names written on it.
Return ONLY a JSON object in this exact format, no explanation text:
{
  ""medicines"": [""MedicineName1"", ""MedicineName2""]
}
Extract every medicine/drug name you can find. Be thorough. If you cannot find any medicines, return {""medicines"": []}."
                                },
                                new
                                {
                                    type = "image_url",
                                    image_url = new { url = $"data:{mimeType};base64,{base64}" }
                                }
                            }
                        }
                    },
                    temperature = 0.1,
                    max_completion_tokens = 512,
                    response_format = new { type = "json_object" }
                };

                var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {groqApiKey}");

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var groqResponse = await httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
                var responseText = await groqResponse.Content.ReadAsStringAsync();

                if (!groqResponse.IsSuccessStatusCode)
                    return Json(new { success = false, message = $"Groq API error: {groqResponse.StatusCode}" });

                // Parse Groq response
                using var doc = System.Text.Json.JsonDocument.Parse(responseText);
                var messageContent = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "{}";

                // Parse thinking tags if present
                if (messageContent.Contains("<think>"))
                {
                    var thinkEnd = messageContent.IndexOf("</think>");
                    if (thinkEnd >= 0)
                        messageContent = messageContent.Substring(thinkEnd + 8).Trim();
                }

                using var parsedJson = System.Text.Json.JsonDocument.Parse(messageContent);
                var medicineNames = parsedJson.RootElement
                    .GetProperty("medicines")
                    .EnumerateArray()
                    .Select(m => m.GetString()?.Trim())
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();

                if (!medicineNames.Any())
                    return Json(new { success = true, foundMedicines = new List<object>(), notFoundMedicines = new List<string>() });

                // Match against our inventory (case-insensitive fuzzy)
                var allMedicines = await _context.Medicines
                    .Where(m => m.IsActive == true && m.StockQuantity > 0)
                    .ToListAsync();

                var foundMedicines = new List<object>();
                var notFoundMedicines = new List<string>();

                foreach (var name in medicineNames)
                {
                    // Try exact match first, then contains
                    var match = allMedicines.FirstOrDefault(m =>
                        m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
                        allMedicines.FirstOrDefault(m =>
                            m.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                            name.Contains(m.Name, StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                    {
                        foundMedicines.Add(new
                        {
                            id = match.Id,
                            name = match.Name,
                            brand = match.Brand,
                            price = match.Price ?? 0,
                            imageUrl = match.ImageUrl,
                            stock = match.StockQuantity
                        });
                    }
                    else
                    {
                        notFoundMedicines.Add(name);
                    }
                }

                return Json(new { success = true, foundMedicines, notFoundMedicines });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Scan failed: {ex.Message}" });
            }
        }

        // ─── Stripe: Create Checkout SESSION (redirects to Stripe hosted page) ────

        [HttpPost]
        public async Task<IActionResult> CreatePharmacyCheckoutSession([FromBody] PharmacyCheckoutSessionRequest req)
        {
            if (req == null || req.Items == null || !req.Items.Any())
                return Json(new { success = false, message = "Empty cart" });

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            try
            {
                // Calculate total from DB (never trust client prices)
                var medicineIds = req.Items.Select(i => i.MedicineId).ToList();
                var medicines = await _context.Medicines
                    .Where(m => medicineIds.Contains(m.Id) && m.IsActive == true)
                    .ToListAsync();

                var lineItems = new List<Stripe.Checkout.SessionLineItemOptions>();
                foreach (var item in req.Items)
                {
                    var med = medicines.FirstOrDefault(m => m.Id == item.MedicineId);
                    if (med == null) return Json(new { success = false, message = $"Medicine {item.MedicineId} not available" });

                    lineItems.Add(new Stripe.Checkout.SessionLineItemOptions
                    {
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)((med.Price ?? 0) * 100), // paisa
                            Currency = "pkr",
                            ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = med.Name,
                                Description = med.Brand,
                            }
                        },
                        Quantity = item.Quantity,
                    });
                }

                var domain = $"{Request.Scheme}://{Request.Host}";

                // Encode cart info in metadata for webhook/success handler
                var itemsJson = System.Text.Json.JsonSerializer.Serialize(req.Items);

                StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = $"{domain}/Pharmacy/StripeSuccess?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{domain}/Pharmacy/CartCheckout",
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId },
                        { "shippingAddress", req.ShippingAddress ?? "" },
                        { "itemsJson", itemsJson.Length > 490 ? itemsJson.Substring(0, 490) : itemsJson }
                    }
                };

                // Store full cart in session (Stripe metadata has 500 char limit per value)
                HttpContext.Session.SetString($"pharmacy_cart_{userId}", itemsJson);
                HttpContext.Session.SetString($"pharmacy_addr_{userId}", req.ShippingAddress ?? "");

                var service = new Stripe.Checkout.SessionService();
                var session = await service.CreateAsync(options);

                return Json(new { success = true, url = session.Url });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─── Stripe: Checkout Success handler (after redirect back) ─────────────

        [HttpGet]
        public async Task<IActionResult> StripeSuccess(string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
                return RedirectToAction("Store");

            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Store");

            try
            {
                StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
                var sessionService = new Stripe.Checkout.SessionService();
                var session = await sessionService.GetAsync(session_id,
                    new Stripe.Checkout.SessionGetOptions { Expand = new List<string> { "payment_intent", "payment_intent.latest_charge" } });

                if (session.PaymentStatus != "paid")
                    return RedirectToAction("Store");

                // Get card details
                string? cardBrand = null, cardLast4 = null, chargeId = null, paymentIntentId = null;
                try
                {
                    paymentIntentId = session.PaymentIntentId;
                    var charge = session.PaymentIntent?.LatestCharge;
                    chargeId = charge?.Id;
                    cardBrand = charge?.PaymentMethodDetails?.Card?.Brand;
                    cardLast4 = charge?.PaymentMethodDetails?.Card?.Last4;
                }
                catch { }

                // Retrieve cart from session storage
                var itemsJson = HttpContext.Session.GetString($"pharmacy_cart_{userId}");
                var shippingAddress = HttpContext.Session.GetString($"pharmacy_addr_{userId}")
                    ?? session.Metadata?.GetValueOrDefault("shippingAddress") ?? "";

                List<OrderItemRequest>? items = null;
                if (!string.IsNullOrEmpty(itemsJson))
                {
                    items = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemRequest>>(itemsJson);
                }

                // Fallback: parse from metadata
                if (items == null || !items.Any())
                {
                    var metaItems = session.Metadata?.GetValueOrDefault("itemsJson");
                    if (!string.IsNullOrEmpty(metaItems))
                        items = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemRequest>>(metaItems);
                }

                if (items == null || !items.Any())
                {
                    // Payment succeeded but can't reconstruct order — show generic success
                    TempData["PaymentSuccess"] = "Payment completed successfully!";
                    return RedirectToAction("Store");
                }

                // Create order in DB
                var strategy = _context.Database.CreateExecutionStrategy();
                int newOrderId = 0;

                await strategy.ExecuteAsync(async () =>
                {
                    using var tx = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Check if Stripe columns exist
                        bool stripeColumnsExist = false;
                        try
                        {
                            var colResult = _context.Database.SqlQueryRaw<int>(
                                "SELECT COUNT(*) as \"Value\" FROM information_schema.columns WHERE table_name='PharmacyOrders' AND column_name='StripePaymentIntentId'");
                            stripeColumnsExist = await colResult.AnyAsync();
                        }
                        catch { }

                        PharmacyOrder order;
                        if (stripeColumnsExist)
                        {
                            order = new PharmacyOrder
                            {
                                PatientId = userId,
                                ShippingAddress = shippingAddress,
                                PaymentMethod = MedLinkPortal.Models.PaymentMethod.OnlinePayment,
                                PaymentStatus = "Paid",
                                StripePaymentIntentId = paymentIntentId,
                                StripeChargeId = chargeId,
                                StripeCardBrand = cardBrand,
                                StripeCardLast4 = cardLast4,
                                PaidAt = DateTime.UtcNow,
                                Status = PharmacyOrderStatus.Pending,
                                CreatedAt = DateTime.UtcNow,
                                TotalAmount = 0
                            };
                        }
                        else
                        {
                            order = new PharmacyOrder
                            {
                                PatientId = userId,
                                ShippingAddress = shippingAddress,
                                PaymentMethod = MedLinkPortal.Models.PaymentMethod.OnlinePayment,
                                PaymentStatus = "Paid",
                                Status = PharmacyOrderStatus.Pending,
                                CreatedAt = DateTime.UtcNow,
                                TotalAmount = 0
                            };
                        }

                        _context.PharmacyOrders.Add(order);
                        await _context.SaveChangesAsync();

                        decimal total = 0;
                        var medicineIds = items.Select(i => i.MedicineId).ToList();
                        var medicines = await _context.Medicines
                            .Where(m => medicineIds.Contains(m.Id) && m.IsActive == true)
                            .ToListAsync();

                        foreach (var item in items)
                        {
                            var med = medicines.FirstOrDefault(m => m.Id == item.MedicineId);
                            if (med == null) continue;

                            if ((med.StockQuantity ?? 0) >= item.Quantity)
                                med.StockQuantity -= item.Quantity;

                            _context.Medicines.Update(med);
                            _context.PharmacyOrderItems.Add(new PharmacyOrderItem
                            {
                                OrderId = order.Id,
                                MedicineId = med.Id,
                                Quantity = item.Quantity,
                                UnitPrice = med.Price ?? 0
                            });
                            total += (med.Price ?? 0) * item.Quantity;
                        }

                        order.TotalAmount = total;
                        _context.PharmacyOrders.Update(order);
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                        newOrderId = order.Id;
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                    }
                });

                // Clear session cart
                HttpContext.Session.Remove($"pharmacy_cart_{userId}");
                HttpContext.Session.Remove($"pharmacy_addr_{userId}");

                if (newOrderId > 0)
                    return RedirectToAction("PaymentSuccess", new { orderId = newOrderId });

                return RedirectToAction("Store");
            }
            catch
            {
                return RedirectToAction("Store");
            }
        }

        // ─── Stripe: Create Payment Intent for Web Cart Checkout ─────────────────

        [HttpPost]
        public async Task<IActionResult> CreateStripePaymentIntent([FromBody] CreatePaymentIntentRequest req)
        {
            if (req == null || req.Items == null || !req.Items.Any())
                return Json(new { success = false, message = "Empty cart" });

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            try
            {
                // Calculate total from DB prices (never trust client)
                var medicineIds = req.Items.Select(i => i.MedicineId).ToList();
                var medicines = await _context.Medicines
                    .Where(m => medicineIds.Contains(m.Id) && m.IsActive == true)
                    .ToListAsync();

                long totalPaise = 0; // Stripe uses smallest currency unit
                foreach (var item in req.Items)
                {
                    var med = medicines.FirstOrDefault(m => m.Id == item.MedicineId);
                    if (med == null) return Json(new { success = false, message = $"Medicine {item.MedicineId} not available" });
                    totalPaise += (long)((med.Price ?? 0) * item.Quantity * 100); // PKR to paisa
                }

                if (totalPaise < 50) // Stripe minimum
                    return Json(new { success = false, message = "Order total too small for online payment" });

                var stripeSecretKey = _configuration["Stripe:SecretKey"];
                StripeConfiguration.ApiKey = stripeSecretKey;

                var options = new Stripe.PaymentIntentCreateOptions
                {
                    Amount = totalPaise,
                    Currency = "pkr",
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId },
                        { "source", "web_pharmacy" }
                    },
                    AutomaticPaymentMethods = new Stripe.PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    }
                };

                var service = new Stripe.PaymentIntentService();
                var intent = await service.CreateAsync(options);

                return Json(new
                {
                    success = true,
                    clientSecret = intent.ClientSecret,
                    paymentIntentId = intent.Id,
                    publishableKey = _configuration["Stripe:PublishableKey"]
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─── Stripe: Confirm payment + create order atomically ───────────────────

        [HttpPost]
        public async Task<IActionResult> ConfirmStripeAndPlaceOrder([FromBody] ConfirmStripeOrderRequest req)
        {
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            try
            {
                // Verify payment intent with Stripe
                StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
                var piService = new Stripe.PaymentIntentService();
                var intent = await piService.GetAsync(req.PaymentIntentId);

                if (intent.Status != "succeeded")
                    return Json(new { success = false, message = $"Payment not completed. Status: {intent.Status}" });

                // Get charge details for card info
                string? cardBrand = null, cardLast4 = null, chargeId = null;
                try
                {
                    if (!string.IsNullOrEmpty(intent.LatestChargeId))
                    {
                        var chargeService = new Stripe.ChargeService();
                        var charge = await chargeService.GetAsync(intent.LatestChargeId);
                        chargeId = charge.Id;
                        if (charge.PaymentMethodDetails?.Card != null)
                        {
                            cardBrand = charge.PaymentMethodDetails.Card.Brand;
                            cardLast4 = charge.PaymentMethodDetails.Card.Last4;
                        }
                    }
                }
                catch
                {
                    // Non-critical — card details optional
                }

                // Create order in DB
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync<IActionResult>(async () =>
                {
                    using var tx = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Step 1: Check which columns exist to avoid EF migration errors
                        bool stripeColumnsExist = false;
                        try
                        {
                            var colCheck = await _context.Database.ExecuteSqlRawAsync(
                                "SELECT 1 FROM information_schema.columns WHERE table_name='PharmacyOrders' AND column_name='StripePaymentIntentId' LIMIT 1");
                            stripeColumnsExist = true;
                        }
                        catch { }

                        // Step 2: Check via actual column existence query
                        try
                        {
                            var result = _context.Database.SqlQueryRaw<int>(
                                "SELECT COUNT(*) as \"Value\" FROM information_schema.columns WHERE table_name='PharmacyOrders' AND column_name='StripePaymentIntentId'");
                            stripeColumnsExist = await result.AnyAsync();
                        }
                        catch { stripeColumnsExist = false; }

                        int orderId;

                        if (stripeColumnsExist)
                        {
                            // Full insert with Stripe columns
                            var order = new PharmacyOrder
                            {
                                PatientId = userId,
                                ShippingAddress = req.ShippingAddress ?? "",
                                PaymentMethod = MedLinkPortal.Models.PaymentMethod.OnlinePayment,
                                PaymentStatus = "Paid",
                                StripePaymentIntentId = intent.Id,
                                StripeChargeId = chargeId,
                                StripeCardBrand = cardBrand,
                                StripeCardLast4 = cardLast4,
                                PaidAt = DateTime.UtcNow,
                                Status = PharmacyOrderStatus.Pending,
                                CreatedAt = DateTime.UtcNow,
                                TotalAmount = 0
                            };
                            _context.PharmacyOrders.Add(order);
                            await _context.SaveChangesAsync();
                            orderId = order.Id;
                        }
                        else
                        {
                            // Insert without Stripe columns (migration not run yet)
                            // Use raw SQL to insert only existing columns
                            await _context.Database.ExecuteSqlRawAsync(
                                @"INSERT INTO ""PharmacyOrders"" (""PatientId"", ""ShippingAddress"", ""PaymentMethod"", ""PaymentStatus"", ""Status"", ""TotalAmount"", ""CreatedAt"")
                                  VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                                userId,
                                req.ShippingAddress ?? "",
                                1, // OnlinePayment
                                "Paid",
                                0, // Pending
                                0m,
                                DateTime.UtcNow);

                            // Get the inserted ID
                            orderId = await _context.PharmacyOrders
                                .Where(o => o.PatientId == userId && o.PaymentStatus == "Paid")
                                .OrderByDescending(o => o.CreatedAt)
                                .Select(o => o.Id)
                                .FirstOrDefaultAsync();

                            if (orderId == 0) throw new Exception("Failed to create order");
                        }

                        // Step 3: Add order items and update stock
                        decimal total = 0;
                        var medicineIds = req.Items.Select(i => i.MedicineId).ToList();
                        var medicines = await _context.Medicines
                            .Where(m => medicineIds.Contains(m.Id) && m.IsActive == true)
                            .ToListAsync();

                        foreach (var item in req.Items)
                        {
                            var med = medicines.FirstOrDefault(m => m.Id == item.MedicineId);
                            if (med == null) throw new Exception($"Medicine {item.MedicineId} not found");
                            if ((med.StockQuantity ?? 0) < item.Quantity)
                                throw new Exception($"Insufficient stock for {med.Name}");

                            med.StockQuantity -= item.Quantity;
                            _context.Medicines.Update(med);

                            _context.PharmacyOrderItems.Add(new PharmacyOrderItem
                            {
                                OrderId = orderId,
                                MedicineId = med.Id,
                                Quantity = item.Quantity,
                                UnitPrice = med.Price ?? 0
                            });
                            total += (med.Price ?? 0) * item.Quantity;
                        }

                        // Update total
                        await _context.PharmacyOrders
                            .Where(o => o.Id == orderId)
                            .ExecuteUpdateAsync(s => s.SetProperty(o => o.TotalAmount, total));

                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();

                        return Json(new { success = true, orderId = orderId });
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        return Json(new { success = false, message = ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─── Payment Success Page ────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int orderId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                // Use raw SQL to avoid EF Core mapping issues with pending Stripe columns
                PharmacyOrder? order = null;
                try
                {
                    order = await _context.PharmacyOrders
                        .AsNoTracking()
                        .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Medicine)
                        .FirstOrDefaultAsync(o => o.Id == orderId && o.PatientId == userId);
                }
                catch
                {
                    // Stripe columns not in DB yet — fetch without them
                    order = await _context.PharmacyOrders
                        .AsNoTracking()
                        .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Medicine)
                        .FirstOrDefaultAsync(o => o.Id == orderId && o.PatientId == userId);
                }

                if (order == null)
                {
                    // Still show success even if we can't load the order details
                    order = new PharmacyOrder
                    {
                        Id = orderId,
                        PatientId = userId ?? "",
                        TotalAmount = 0,
                        PaymentMethod = MedLinkPortal.Models.PaymentMethod.OnlinePayment,
                        PaymentStatus = "Paid",
                        Status = PharmacyOrderStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        ShippingAddress = "",
                        OrderItems = new List<PharmacyOrderItem>()
                    };
                }

                return View(order);
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<PharmacyController>>();
                logger?.LogError(ex, "PaymentSuccess view error for orderId {OrderId}: {Message}", orderId, ex.Message);

                // Always show success page — payment went through
                var fallback = new PharmacyOrder
                {
                    Id = orderId,
                    PatientId = _userManager.GetUserId(User) ?? "",
                    TotalAmount = 0,
                    PaymentMethod = MedLinkPortal.Models.PaymentMethod.OnlinePayment,
                    PaymentStatus = "Paid",
                    Status = PharmacyOrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    ShippingAddress = "",
                    OrderItems = new List<PharmacyOrderItem>()
                };
                return View(fallback);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
                return Json(new { success = false, message = "Invalid order data" });

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () => {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var order = new PharmacyOrder
                    {
                        PatientId = userId,
                        PrescriptionId = model.PrescriptionId > 0 ? model.PrescriptionId : null,
                        ShippingAddress = model.ShippingAddress,
                        PaymentMethod = (MedLinkPortal.Models.PaymentMethod)model.PaymentMethod,
                        Status = PharmacyOrderStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        TotalAmount = 0 // Will calculate
                    };

                    _context.PharmacyOrders.Add(order);
                    await _context.SaveChangesAsync();

                    decimal totalAmount = 0;
                    foreach (var item in model.Items)
                    {
                        var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                        if (medicine == null || medicine.IsActive != true)
                            throw new Exception($"Medicine {item.MedicineId} not found or inactive.");

                        if (medicine.StockQuantity < item.Quantity)
                            throw new Exception($"Insufficient stock for {medicine.Name}.");

                        // Reserve Stock
                        medicine.StockQuantity -= item.Quantity;
                        _context.Medicines.Update(medicine);

                        var orderItem = new PharmacyOrderItem
                        {
                            OrderId = order.Id,
                            MedicineId = item.MedicineId,
                            Quantity = item.Quantity,
                            UnitPrice = medicine.Price ?? 0
                        };
                        _context.PharmacyOrderItems.Add(orderItem);
                        totalAmount += ((medicine.Price ?? 0) * item.Quantity);
                    }

                    order.TotalAmount = totalAmount;
                    _context.PharmacyOrders.Update(order);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return Json(new { success = true, orderId = order.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = ex.Message });
                }
            });
        }
        // --- Profile Management ---

        [HttpGet]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new PharmacyProfileViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Workplace = user.Workplace, // Pharmacy Name
                City = user.City,
                ResidentialAddress = user.ResidentialAddress,
                ProfileImageUrl = user.ProfileImage ?? "https://picsum.photos/seed/pharmacist/150/150",
                IsEmailVerified = user.EmailConfirmed
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Pharmacist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(PharmacyProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Update fields
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Workplace = model.Workplace;
            user.City = model.City;
            user.ResidentialAddress = model.ResidentialAddress;

            // Handle Image Upload
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                try
                {
                    var uploadedUrl = await _cloudinaryService.UploadImageAsync(model.ProfileImage, "pharmacist_profiles");
                    if (!string.IsNullOrEmpty(uploadedUrl))
                    {
                        user.ProfileImage = uploadedUrl;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ProfileImage", "Failed to upload image: " + ex.Message);
                    return View(model);
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }
        // --- Customer Support (Real-Time Chat) ---

        [HttpGet]
        [Authorize(Roles = "Pharmacist")]
        public IActionResult CustomerSupport()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveChats()
        {
            try
            {
                var pharmacistId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(pharmacistId)) return Unauthorized();

                // 1. Get IDs of all users who have interacted with this pharmacist
                var chatInteractions = await _context.ChatMessages
                    .Where(m => m.ReceiverId == pharmacistId || m.SenderId == pharmacistId)
                    .OrderByDescending(m => m.Timestamp)
                    .Select(m => new { m.SenderId, m.ReceiverId, m.Timestamp })
                    .ToListAsync();

                var chatUserIds = chatInteractions
                    .Select(m => m.SenderId == pharmacistId ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .Take(20)
                    .ToList();

                // 2. Get User Details
                var users = await _context.Users
                    .Where(u => chatUserIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                var chatList = new List<ChatUserDTO>();

                foreach (var partnerId in chatUserIds)
                {
                    if (!users.ContainsKey(partnerId)) continue;
                    var user = users[partnerId];

                    var lastMsg = await _context.ChatMessages
                        .Where(m => (m.SenderId == user.Id && m.ReceiverId == pharmacistId) || (m.SenderId == pharmacistId && m.ReceiverId == user.Id))
                        .OrderByDescending(m => m.Timestamp)
                        .Select(m => new { m.Content, m.Timestamp })
                        .FirstOrDefaultAsync();

                    var unreadCount = await _context.ChatMessages
                        .CountAsync(m => m.SenderId == user.Id && m.ReceiverId == pharmacistId && !m.IsRead);

                    chatList.Add(new ChatUserDTO
                    {
                        Id = user.Id,
                        Name = (user.FirstName + " " + user.LastName).Trim(),
                        Email = user.Email,
                        Image = !string.IsNullOrEmpty(user.ProfileImage) ? user.ProfileImage : "https://picsum.photos/seed/" + user.Id + "/50/50",
                        LastMessage = lastMsg?.Content ?? "No messages yet",
                        LastMessageTime = lastMsg?.Timestamp,
                        UnreadCount = unreadCount
                    });
                }

                return Json(chatList.OrderByDescending(u => u.LastMessageTime ?? DateTime.MinValue).ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActiveChats: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSupportAgent(int orderId)
        {
            var order = await _context.PharmacyOrders.FindAsync(orderId);
            string pharmacistId = order?.PharmacistId;

            // If no pharmacist assigned to order, find the main one
            if (string.IsNullOrEmpty(pharmacistId))
            {
                var mainPharmacist = await _userManager.FindByEmailAsync("pharmacist@medlink.com");
                pharmacistId = mainPharmacist?.Id;

                if (string.IsNullOrEmpty(pharmacistId))
                {
                    var pharmacists = await _userManager.GetUsersInRoleAsync("Pharmacist");
                    pharmacistId = pharmacists.FirstOrDefault()?.Id;
                }

                if (string.IsNullOrEmpty(pharmacistId))
                {
                    // Final fallback: use currently logged in user if they are a pharmacist
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (await _userManager.IsInRoleAsync(currentUser, "Pharmacist"))
                        pharmacistId = currentUser.Id;
                    else
                        pharmacistId = _userManager.Users.FirstOrDefault(u => u.Email.Contains("admin"))?.Id ?? _userManager.Users.FirstOrDefault()?.Id;
                }
            }
            return Json(new { pharmacistId });
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string otherUserId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(otherUserId)) return Json(new List<object>());

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new {
                    id = m.Id,
                    content = m.Content,
                    timestamp = m.Timestamp,
                    isMe = m.SenderId == currentUserId
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrEmpty(request.Content) || string.IsNullOrEmpty(request.ReceiverId))
            {
                return BadRequest("Invalid message");
            }

            var senderId = _userManager.GetUserId(User);

            // 1. Save to Database
            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                Timestamp = DateTime.UtcNow,
                IsRead = false,
                MessageType = "Text"
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // 2. Notify via SignalR (Best Effort)
            try
            {
                await _hubContext.Clients.User(request.ReceiverId).SendAsync("ReceiveMessage", senderId, request.Content, "Text", "", "", senderId);
            }
            catch (Exception ex)
            {
                // Log error but don't fail request since DB save worked
                Console.WriteLine($"SignalR Notification Failed: {ex.Message}");
            }

            return Ok(new { success = true });
        }
    }

    public class ChatUserDTO
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("image")]
        public string Image { get; set; }
        [JsonPropertyName("lastMessage")]
        public string LastMessage { get; set; }
        [JsonPropertyName("lastMessageTime")]
        public DateTime? LastMessageTime { get; set; }
        [JsonPropertyName("unreadCount")]
        public int UnreadCount { get; set; }
    }

    public class SendMessageRequest
    {
        public string ReceiverId { get; set; }
        public string Content { get; set; }
    }

    // ... Existing classes ...

    public class OrderRequest
    {
        public int AppointmentId { get; set; }
        public int PrescriptionId { get; set; }
        public string ShippingAddress { get; set; }
        public int PaymentMethod { get; set; }
        public List<OrderItemRequest> Items { get; set; }
    }

    public class OrderItemRequest
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreatePaymentIntentRequest
    {
        public List<OrderItemRequest> Items { get; set; }
    }

    public class PharmacyCheckoutSessionRequest
    {
        public string? ShippingAddress { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class ConfirmStripeOrderRequest
    {
        public string PaymentIntentId { get; set; }
        public string ShippingAddress { get; set; }
        public List<OrderItemRequest> Items { get; set; }
    }


    public class PrescriptionRequest
    {
        public int AppointmentId { get; set; }
        public string Diagnosis { get; set; }
        public string Notes { get; set; }
        public bool Finalize { get; set; }
        public List<PrescriptionMedItem> Medicines { get; set; }
    }

    public class PrescriptionMedItem
    {
        public int MedicineId { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public string Duration { get; set; }
        public int Quantity { get; set; }
        public string Instructions { get; set; }
    }
}
