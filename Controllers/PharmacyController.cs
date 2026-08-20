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

        public PharmacyController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            MedLinkPortal.Services.INotificationService notificationService,
            IConfiguration configuration,
            Microsoft.AspNetCore.SignalR.IHubContext<MedLinkPortal.Hubs.ChatHub> hubContext,
            IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _configuration = configuration;
            _hubContext = hubContext;
            _contextFactory = contextFactory;
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
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/medicines", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    model.ImageUrl = "/images/medicines/" + fileName;
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
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/medicines", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    existing.ImageUrl = "/images/medicines/" + fileName;
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
                        PaymentMethod = (PaymentMethod)model.PaymentMethod,
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
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImage.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles", fileName);

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImage.CopyToAsync(stream);
                    }
                    user.ProfileImage = "/images/profiles/" + fileName;
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
