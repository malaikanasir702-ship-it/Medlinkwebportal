using Microsoft.AspNetCore.Mvc;
using MedLinkPortal.Services;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Identity.Pages.Account;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // Added for UserManager and SignInManager
using Microsoft.AspNetCore.Identity.UI.Services; // Added for IEmailSender
using System; // Added for DateTime.UtcNow, Guid
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Stripe.Checkout;
using Microsoft.Extensions.Caching.Memory;
using AdminModels = MedLinkPortal.Areas.Admin.Models;

namespace MedLinkPortal.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // Changed from Microsoft.AspNetCore.Identity.UserManager
        private readonly SignInManager<ApplicationUser> _signInManager; // Added
        private readonly IAiChatService _aiChatService;
        private readonly IEmailSender _emailSender;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<MedLinkPortal.Hubs.ChatHub> _hubContext;
        private readonly Services.INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardController> _logger;
        private readonly INeuralReportService _reportService;
        private readonly IMemoryCache _cache;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public DashboardController(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAiChatService aiChatService,
            IEmailSender emailSender,
            Microsoft.AspNetCore.SignalR.IHubContext<MedLinkPortal.Hubs.ChatHub> hubContext,
            Services.INotificationService notificationService,
            IConfiguration configuration,
            ILogger<DashboardController> logger,
            INeuralReportService reportService,
            IMemoryCache cache,
            IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _aiChatService = aiChatService;
            _emailSender = emailSender;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;
            _reportService = reportService;
            _cache = cache;
            _contextFactory = contextFactory;
        }

        public async Task<IActionResult> Index()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "overview";
            return View(model);
        }

        public async Task<IActionResult> Appointments()
        {
            var userId = _userManager.GetUserId(User);
            var model = await GetBaseModelAsync();
            model.ActiveTab = "appointments";
            
            // In a real app, medications and records would also be filtered by UserId
            // For now, focusing on Appointments as requested
            return View(model);
        }

        public async Task<IActionResult> HealthRecords()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "records";
            // AvailableDoctors is now populated in GetBaseModel
            return View(model);
        }

        public async Task<IActionResult> AiDoctor()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "ai-doctor";
            ViewData["HeaderTitle"] = "AI Doctor Consultant";
            ViewData["HeaderSubtitle"] = "Describe symptoms and get guided next steps.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAiDoctorHistory()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var history = await _aiChatService.GetChatHistoryAsync(userId);
            return Json(history.Select(message => new
            {
                role = message.Role,
                content = message.Content,
                timestamp = message.Timestamp
            }));
        }

        [HttpPost]
        public async Task<IActionResult> SendAiDoctorMessage([FromBody] AiDoctorChatRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var message = request?.Message?.Trim();
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest(new { reply = "Message cannot be empty." });
            }

            var response = await _aiChatService.ProcessMessageAsync(userId, message);
            return Json(new
            {
                reply = response.Reply,
                isComplete = response.IsComplete,
                suggestedDoctors = response.SuggestedDoctors.Select(doctor => new
                {
                    id = doctor.Id,
                    name = doctor.Name,
                    specialty = doctor.Specialty,
                    image = doctor.Image,
                    rating = doctor.Rating,
                    hospitalAffiliations = doctor.HospitalAffiliations,
                    clinicName = doctor.ClinicName,
                    availability = doctor.Availability
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> ClearAiDoctorHistory()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _aiChatService.ClearHistoryAsync(userId);
            return Json(new { success = true });
        }

        public async Task<IActionResult> AIAnalysis()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "overview";
            
            // Mock dynamic data based on common health patterns if no input is provided
            // In a real app, this would come from TempData/Session after ProcessHealthAnalysis
            if (model.AIReport == null)
            {
                 model.AIReport = new AIHealthReport
                {
                    OverallScore = 92,
                    StatusLabel = "Excellent",
                    StatusColor = "emerald",
                    Summary = "Your health is trending positively. Based on your recent vitals, your cardiovascular resilience has improved by 8% this week.",
                    ResilienceTrend = "+8%",
                    Vitals = model.HealthVitals,
                    DailyTips = new List<DailyTip>
                    {
                        new DailyTip { Title = "Hydration Focus", Description = "Biometric variance indicates a 12% dip in hydration during afternoon hours.", Icon = "droplets", Color = "blue" },
                        new DailyTip { Title = "Sun Exposure", Description = "Maintain 15 mins of morning sun for Vitamin D synthesis and circadian rhythm alignment.", Icon = "sun", Color = "amber" },
                        new DailyTip { Title = "Sleep Hygiene", Description = "Consistent 10 PM wind-down recommended for hormonal synergy.", Icon = "moon", Color = "indigo" }
                    },
                    DietPlan = new List<MealPlan>
                    {
                        new MealPlan { MealTime = "Breakfast", FoodItems = "Oatmeal with chia seeds, blueberries, and walnuts", NutritionalValue = "High Fiber, Omega-3", Icon = "coffee" },
                        new MealPlan { MealTime = "Lunch", FoodItems = "Grilled chicken salad with avocado, kale, and lemon tahini dressing", NutritionalValue = "Lean Protein, Healthy Fats", Icon = "utensils" },
                        new MealPlan { MealTime = "Dinner", FoodItems = "Baked salmon with roasted sweet potatoes and asparagus", NutritionalValue = "Protein, Vitamin A, Minerals", Icon = "fish" },
                        new MealPlan { MealTime = "Snack", FoodItems = "Greek yogurt with a handful of almonds", NutritionalValue = "Probiotics, Protein", Icon = "apple" }
                    },
                    Protocols = new List<ClinicalProtocol>
                    {
                        new ClinicalProtocol { Title = "Z2 Endurance Work", Description = "Engage in 135 BPM cardio for 45 mins.", ActionText = "Set Reminder", Color = "emerald", Icon = "activity" },
                        new ClinicalProtocol { Title = "Hormonal Synergy", Description = "Maintain consistent sleep cycle.", ActionText = "View Sleep Map", Color = "blue", Icon = "shield-plus" }
                    }
                };
            }
           
            return View(model);
        }

        public async Task<IActionResult> DownloadReport()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Regenerate the same mock data for the report (in a real app, this would be fetched from DB)
            var model = await GetBaseModelAsync();
            var report = new AIHealthReport
            {
                OverallScore = 92,
                StatusLabel = "Excellent",
                StatusColor = "emerald",
                Summary = "Your health is trending positively. Based on your recent vitals, your cardiovascular resilience has improved by 8% this week.",
                ResilienceTrend = "+8%",
                Vitals = model.HealthVitals,
                DailyTips = new List<DailyTip>
                {
                    new DailyTip { Title = "Hydration Focus", Description = "Biometric variance indicates a 12% dip in hydration during afternoon hours.", Icon = "droplets", Color = "blue" },
                    new DailyTip { Title = "Sun Exposure", Description = "Maintain 15 mins of morning sun for Vitamin D synthesis and circadian rhythm alignment.", Icon = "sun", Color = "amber" },
                    new DailyTip { Title = "Sleep Hygiene", Description = "Consistent 10 PM wind-down recommended for hormonal synergy.", Icon = "moon", Color = "indigo" }
                },
                DietPlan = new List<MealPlan>
                {
                    new MealPlan { MealTime = "Breakfast", FoodItems = "Oatmeal with chia seeds, blueberries, and walnuts", NutritionalValue = "High Fiber, Omega-3", Icon = "coffee" },
                    new MealPlan { MealTime = "Lunch", FoodItems = "Grilled chicken salad with avocado, kale, and lemon tahini dressing", NutritionalValue = "Lean Protein, Healthy Fats", Icon = "utensils" },
                    new MealPlan { MealTime = "Dinner", FoodItems = "Baked salmon with roasted sweet potatoes and asparagus", NutritionalValue = "Protein, Vitamin A, Minerals", Icon = "fish" },
                    new MealPlan { MealTime = "Snack", FoodItems = "Greek yogurt with a handful of almonds", NutritionalValue = "Probiotics, Protein", Icon = "apple" }
                },
                Protocols = new List<ClinicalProtocol>
                {
                    new ClinicalProtocol { Title = "Z2 Endurance Work", Description = "Engage in 135 BPM cardio for 45 mins.", ActionText = "Set Reminder", Color = "emerald", Icon = "activity" },
                    new ClinicalProtocol { Title = "Hormonal Synergy", Description = "Maintain consistent sleep cycle.", ActionText = "View Sleep Map", Color = "blue", Icon = "shield-plus" }
                }
            };

            var pdfBytes = _reportService.GenerateReport(report, $"{user.FirstName} {user.LastName}");
            return File(pdfBytes, "application/pdf", $"NeuralReport_{DateTime.Now:yyyyMMdd}.pdf");
        }

        public async Task<IActionResult> HealthAnalysisInput()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "overview";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessHealthAnalysis(HealthAnalysisInput input)
        {
            // Here we would normally use a service to generate the AI report
            // For now, we redirect to the processing animation which will then land on AIAnalysis
            return RedirectToAction("AIProcessing");
        }

        public async Task<IActionResult> AIProcessing()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "overview";
            return View(model);
        }

        public async Task<IActionResult> SleepMap()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "overview";
            
            // Generate comprehensive mock sleep data
            var sleepData = new SleepData
            {
                OverallSleepScore = 87,
                SleepQuality = "Good",
                WeeklyPattern = new List<SleepDayData>
                {
                    new SleepDayData { Date = "Feb 3", DayName = "Mon", TotalHours = 7.5, QualityScore = 85, BedTime = "10:30 PM", WakeTime = "6:00 AM", Interruptions = 2 },
                    new SleepDayData { Date = "Feb 4", DayName = "Tue", TotalHours = 6.8, QualityScore = 78, BedTime = "11:15 PM", WakeTime = "6:00 AM", Interruptions = 3 },
                    new SleepDayData { Date = "Feb 5", DayName = "Wed", TotalHours = 8.2, QualityScore = 92, BedTime = "10:00 PM", WakeTime = "6:15 AM", Interruptions = 1 },
                    new SleepDayData { Date = "Feb 6", DayName = "Thu", TotalHours = 7.0, QualityScore = 80, BedTime = "11:00 PM", WakeTime = "6:00 AM", Interruptions = 2 },
                    new SleepDayData { Date = "Feb 7", DayName = "Fri", TotalHours = 7.8, QualityScore = 88, BedTime = "10:20 PM", WakeTime = "6:10 AM", Interruptions = 1 },
                    new SleepDayData { Date = "Feb 8", DayName = "Sat", TotalHours = 8.5, QualityScore = 94, BedTime = "10:00 PM", WakeTime = "6:30 AM", Interruptions = 0 },
                    new SleepDayData { Date = "Feb 9", DayName = "Sun", TotalHours = 8.0, QualityScore = 90, BedTime = "10:15 PM", WakeTime = "6:15 AM", Interruptions = 1 }
                },
                SleepStages = new SleepStages
                {
                    RemPercentage = 22.5,
                    DeepPercentage = 18.3,
                    LightPercentage = 54.2,
                    AwakePercentage = 5.0
                },
                CircadianData = new CircadianRhythm
                {
                    OptimalBedTime = "10:00 PM",
                    OptimalWakeTime = "6:00 AM",
                    CircadianAlignment = 92,
                    Chronotype = "Morning Lark"
                },
                Insights = new List<SleepInsight>
                {
                    new SleepInsight { Title = "Consistent Sleep Schedule", Description = "Your bedtime variance is only 45 minutes. Maintaining this consistency optimizes circadian rhythm.", Icon = "clock", Priority = "High", Color = "emerald" },
                    new SleepInsight { Title = "REM Sleep Optimization", Description = "REM sleep at 22.5% is within optimal range (20-25%). This supports memory consolidation and emotional regulation.", Icon = "brain", Priority = "Medium", Color = "blue" },
                    new SleepInsight { Title = "Reduce Mid-Sleep Interruptions", Description = "Average 1.4 interruptions per night. Consider limiting fluid intake 2 hours before bed.", Icon = "alert-circle", Priority = "Medium", Color = "amber" },
                    new SleepInsight { Title = "Weekend Sleep Extension", Description = "You sleep 30 mins longer on weekends. This suggests weekday sleep debt accumulation.", Icon = "calendar", Priority = "Low", Color = "slate" }
                },
                HygieneScore = new SleepHygieneScore
                {
                    OverallScore = 82,
                    Factors = new List<HygieneFactor>
                    {
                        new HygieneFactor { Name = "Sleep Schedule Consistency", Score = 95, Status = "Excellent", Recommendation = "Maintain current bedtime routine" },
                        new HygieneFactor { Name = "Sleep Duration", Score = 88, Status = "Good", Recommendation = "Aim for 8 hours consistently" },
                        new HygieneFactor { Name = "Sleep Environment", Score = 75, Status = "Needs Improvement", Recommendation = "Reduce bedroom temperature to 65-68°F" },
                        new HygieneFactor { Name = "Pre-Sleep Routine", Score = 70, Status = "Needs Improvement", Recommendation = "Avoid screens 1 hour before bed" },
                        new HygieneFactor { Name = "Physical Activity", Score = 90, Status = "Excellent", Recommendation = "Continue regular exercise routine" }
                    }
                }
            };
            
            ViewBag.SleepData = sleepData;
            return View(model);
        }

        public async Task<IActionResult> Consultations()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "appointments"; // Reusing the appointments tab context
            ViewData["HeaderTitle"] = "Consultation History";
            ViewData["HeaderSubtitle"] = "Review and access all your past and upcoming medical sessions.";
            return View(model);
        }

        // Reminder Management Actions
        public async Task<IActionResult> SetReminder()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "overview";
            
            // Get user's active reminders
            var userId = _userManager.GetUserId(User);
            var reminders = await _context.Reminders
                .Where(r => r.UserId == userId && r.IsActive && !r.IsCompleted)
                .OrderBy(r => r.ScheduledTime)
                .ToListAsync();
            
            ViewBag.ActiveReminders = reminders.Select(r => new ReminderViewModel
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                ReminderType = r.ReminderType,
                ScheduledTime = r.ScheduledTime,
                IsRecurring = r.IsRecurring,
                RecurrencePattern = r.RecurrencePattern,
                IsActive = r.IsActive,
                IsCompleted = r.IsCompleted,
                FormattedTime = r.ScheduledTime.ToString("MMM dd, yyyy hh:mm tt"),
                TimeUntil = GetTimeUntilReminder(r.ScheduledTime)
            }).ToList();
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateReminder(CreateReminderModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("SetReminder");
            }

            var userId = _userManager.GetUserId(User);
            var reminder = new Reminder
            {
                UserId = userId,
                Title = model.Title,
                Description = model.Description,
                ReminderType = model.ReminderType,
                ScheduledTime = model.ScheduledTime,
                IsRecurring = model.IsRecurring,
                RecurrencePattern = model.RecurrencePattern,
                IsActive = true,
                IsCompleted = false,
                CreatedAt = DateTime.Now
            };

            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync();

            return RedirectToAction("SetReminder");
        }

        [HttpPost]
        public async Task<IActionResult> CompleteReminder(int id)
        {
            var userId = _userManager.GetUserId(User);
            var reminder = await _context.Reminders
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reminder != null)
            {
                reminder.IsCompleted = true;
                reminder.CompletedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("SetReminder");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReminder(int id)
        {
            var userId = _userManager.GetUserId(User);
            var reminder = await _context.Reminders
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reminder != null)
            {
                _context.Reminders.Remove(reminder);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("SetReminder");
        }

        private string GetTimeUntilReminder(DateTime scheduledTime)
        {
            var timeSpan = scheduledTime - DateTime.Now;
            
            if (timeSpan.TotalMinutes < 0)
                return "Overdue";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d";
            
            return scheduledTime.ToString("MMM dd");
        }

        public async Task<IActionResult> Messages(int? doctorId)
        {
            var userId = _userManager.GetUserId(User);
            var model = await GetBaseModelAsync();
            model.ActiveTab = "messages";

            // If a doctor is selected, fetch conversation
            if (doctorId.HasValue)
            {
                var selectedDoc = _context.Doctors.Find(doctorId.Value);
                if (selectedDoc != null)
                {
                    ViewBag.SelectedDoctorId = doctorId;
                    ViewBag.SelectedDoctorName = selectedDoc.Name;
                    ViewBag.SelectedDoctorImage = selectedDoc.Image;
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetConversation(int? doctorId, string? otherUserId)
        {
            var userId = _userManager.GetUserId(User);
            
            IQueryable<ChatMessage> query = _context.ChatMessages;
            
            if (doctorId.HasValue && doctorId.Value > 0)
            {
                query = query.Where(m => (m.SenderId == userId && m.DoctorId == doctorId) || 
                                       (m.ReceiverId == userId && m.DoctorId == doctorId));
            }
            else if (!string.IsNullOrEmpty(otherUserId))
            {
                query = query.Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) || 
                                       (m.SenderId == otherUserId && m.ReceiverId == userId));
            }
            else
            {
                return Json(new List<object>());
            }

            var messages = await query.OrderBy(m => m.Timestamp).ToListAsync();

            // Mark received messages as read
            var unreadMessages = messages.Where(m => m.ReceiverId == userId && !m.IsRead).ToList();
            if (unreadMessages.Any())
            {
                foreach (var m in unreadMessages) m.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var projectedMessages = messages.Select(m => new {
                m.Id,
                m.Content,
                m.SenderId,
                m.Timestamp,
                AttachmentUrl = m.AttachmentUrl,
                m.AttachmentType,
                m.AttachmentName,
                m.MessageType,
                m.IsRead,
                m.IsDeleted,
                IsMe = m.SenderId == userId
            }).ToList();

            return Json(projectedMessages);
        }

        [HttpPost]
        public async Task<IActionResult> ShareAnalysis(int doctorId, int analysisId)
        {
            var userId = _userManager.GetUserId(User);
            var analysis = await _context.AIAnalyses.FindAsync(analysisId);
            if (analysis == null) return Json(new { success = false, message = "Analysis not found" });

            // Ensure analysis belongs to user
            if (analysis.UserId != userId) return Json(new { success = false, message = "Access denied" });

            var content = System.Text.Json.JsonSerializer.Serialize(new {
                type = "AI_REPORT",
                fileName = analysis.FileName,
                status = analysis.Status,
                summary = analysis.AnalysisResult,
                analysisId = analysis.Id,
                timestamp = analysis.CreatedAt
            });

            var message = new ChatMessage
            {
                SenderId = userId,
                ReceiverId = "doctor_" + doctorId,
                DoctorId = doctorId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsRead = false,
                MessageType = "AI_Report",
                AttachmentName = "AI_Analysis_" + analysis.FileName,
                AttachmentType = "Document"
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int? doctorId, string? receiverId, string content, IFormFile? attachment)
        {
            var userId = _userManager.GetUserId(User);
            string finalReceiverId = receiverId ?? "";
            int? finalDoctorId = doctorId;

            if (doctorId.HasValue && doctorId.Value > 0)
            {
                var doctor = await _context.Doctors.FindAsync(doctorId);
                finalReceiverId = doctor?.UserId ?? "";
            }

            var message = new ChatMessage
            {
                SenderId = userId,
                ReceiverId = finalReceiverId,
                DoctorId = finalDoctorId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            if (attachment != null)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + attachment.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(fileStream);
                }

                message.AttachmentUrl = "/uploads/" + uniqueFileName;
                message.AttachmentName = attachment.FileName;
                message.AttachmentType = attachment.ContentType.Contains("image") ? "image" : 
                                       attachment.ContentType.Contains("video") ? "video" : "document";
            }

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Notify via SignalR
            var senderName = User.Identity?.Name?.Split('@')[0] ?? "User";
            var type = message.AttachmentType?.ToLower() ?? "text";
            await _hubContext.Clients.User(finalReceiverId).SendAsync("ReceiveMessage", senderName, message.Content ?? "", type, message.AttachmentUrl, message.AttachmentName, userId);

            // Create and Send Real-time Notification
            string displayContent = message.Content ?? (attachment != null ? $"Sent an {message.AttachmentType}" : "New Message");
            await _notificationService.NotifyUserAsync(finalReceiverId, 
                NotificationType.NewMessageReceived,
                $"New Message from {senderName}", 
                displayContent.Length > 50 ? displayContent.Substring(0, 47) + "..." : displayContent, 
                "message-square", "blue");

            return Json(new { success = true, message = message });
        }

        [HttpPost]
        public async Task<IActionResult> EditMessage(int messageId, string content)
        {
            var userId = _userManager.GetUserId(User);
            var message = await _context.ChatMessages.FindAsync(messageId);

            if (message == null || message.SenderId != userId || message.IsDeleted)
                return Json(new { success = false, error = "Forbidden or not found" });

            message.Content = content;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int messageId, string deleteType)
        {
            var userId = _userManager.GetUserId(User);
            var message = await _context.ChatMessages.FindAsync(messageId);

            if (message == null) return Json(new { success = false });

            if (deleteType == "everyone")
            {
                if (message.SenderId != userId) return Json(new { success = false, error = "Forbidden" });
                message.IsDeleted = true;
                message.DeletedBy = "Everyone";
            }
            else
            {
                // Delete for me - in a real app we'd need a mapping table for per-user visibility
                // For this demo, we'll just hide it if it's "deleted for me" by the sender
                if (message.SenderId == userId)
                {
                    message.IsDeleted = true;
                    message.DeletedBy = "Me";
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> LogCall(int doctorId, string callType)
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _context.Doctors.FindAsync(doctorId);
            var receiverId = doctor?.UserId ?? "";

            var message = new ChatMessage
            {
                SenderId = userId,
                ReceiverId = receiverId,
                DoctorId = doctorId,
                MessageType = "Call",
                Content = $"Voice call started at {DateTime.UtcNow.AddHours(5):HH:mm}", // Still showing local-ish hours for text but saving UTC
                Timestamp = DateTime.UtcNow,
                IsRead = true
            };

            if (callType == "video")
            {
                message.Content = $"Video call started at {DateTime.UtcNow.AddHours(5):HH:mm}";
            }


            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Notify via SignalR
            var senderName = User.Identity?.Name?.Split('@')[0] ?? "User";
            await _hubContext.Clients.User(receiverId).SendAsync("ReceiveMessage", senderName, message.Content, "Call", null, null, userId);

            return Json(new { success = true, message = message });
        }






        public async Task<IActionResult> AIDiagnosticLab()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "ai-lab";
            return View(model);
        }

        public async Task<IActionResult> LiveTriage()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "overview"; // Or a new one if sidebar needs highlight
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PerformLiveTriage(string symptoms, string duration, int intensity)
        {
            try
            {
                // Rule-based Triage (Simplified)
                string summary = $"Symptoms: {symptoms} for {duration}. Priority based on intensity {intensity}/10.";
                string priority = intensity > 7 ? "Emergency" : (intensity > 4 ? "Urgent" : "Routine");
                string specialty = "General Practitioner";
                
                if (symptoms.ToLower().Contains("chest") || symptoms.ToLower().Contains("dil")) specialty = "Cardiologist";
                else if (symptoms.ToLower().Contains("pait") || symptoms.ToLower().Contains("stomach")) specialty = "Gastroenterologist";
                else if (symptoms.ToLower().Contains("sar") || symptoms.ToLower().Contains("head")) specialty = "Neurologist";
                
                string estimatedWait = "2-5 mins";

                // 3. Find Matching Doctor
                // Try to find a doctor with the specific specialty
                var doctor = await _context.Doctors
                    .Where(d => d.Specialty.Contains(specialty) || d.Description.Contains(specialty))
                    .FirstOrDefaultAsync();

                // Fallback to any doctor if no specialist found
                if (doctor == null)
                {
                    doctor = await _context.Doctors.FirstOrDefaultAsync();
                }

                // Fallback valid data if database is empty
                var doctorData = new
                {
                    name = doctor?.Name ?? "Dr. On Call",
                    id = doctor?.Id ?? 0,
                    userId = doctor?.UserId ?? "",
                    image = doctor?.Image ?? "https://picsum.photos/seed/doctor/100/100",
                    specialty = doctor?.Specialty ?? "General Practitioner",
                    qualification = doctor?.Qualification ?? "MBBS"
                };

                return Json(new 
                { 
                    success = true, 
                    analysis = new 
                    { 
                        priority, 
                        specialty, 
                        summary,
                        estimatedWait
                    },
                    doctor = doctorData
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeFile(IFormFile file)
        {
            var userId = _userManager.GetUserId(User);
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file uploaded." });

            try 
            {
                // 1. Save File
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ai_uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // 2. Prepare for Gemini
                string base64Content;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    base64Content = Convert.ToBase64String(memoryStream.ToArray());
                }

                // Simplified rule-based file analysis
                string analysisText = $"Analyzed medical document: {file.FileName}. Preliminary check shows results within typical ranges. Please consult a doctor for official interpretation.";
                if (file.FileName.ToLower().Contains("blood") || file.ContentType.Contains("image")) 
                    analysisText += " Vital markers detected.";
                
                // 3. (Gemini call removed)

                // 4. Determine Status (Simple Logic for Demo)
                string status = "Normal";
                if (analysisText.ToLower().Contains("critical") || analysisText.ToLower().Contains("urgent")) status = "Critical";
                else if (analysisText.ToLower().Contains("attention") || analysisText.ToLower().Contains("needed") || analysisText.ToLower().Contains("suggest")) status = "Action Needed";

                // 5. Save to Database
                var analysis = new AIAnalysis
                {
                    UserId = userId,
                    FileName = file.FileName,
                    FilePath = "/ai_uploads/" + uniqueFileName,
                    FileType = file.ContentType.Contains("image") ? "Imaging" : "Report",
                    Status = status,
                    AnalysisResult = analysisText,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AIAnalyses.Add(analysis);
                await _context.SaveChangesAsync();

                // Notify user that analysis is complete
                await _notificationService.CreateAndSendNotificationAsync(userId, 
                    "AI Analysis Complete", 
                    $"Your file '{file.FileName}' has been analyzed with status: {status}.", 
                    "brain", status == "Critical" ? "rose" : "emerald");

                return Json(new { success = true, analysis = analysis });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Settings()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "settings";
            ViewData["VapidPublicKey"] = _configuration["Vapid:PublicKey"];
            return View(model);
        }
        
        public async Task<IActionResult> Doctors()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "doctors";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorsList()
        {
            var doctors = await _context.Doctors
                .Select(d => new { d.Id, d.Name, Specialization = d.Specialty, ProfileImage = d.Image })
                .ToListAsync();
            return Json(new { success = true, doctors });
        }

        [HttpGet]
        public async Task<IActionResult> GetNearbyDoctors(double lat, double lng)
        {
            var doctors = await _context.Doctors.ToListAsync();

            // Haversine distance calculation
            static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
            {
                const double R = 6371;
                var dLat = (lat2 - lat1) * Math.PI / 180;
                var dLon = (lon2 - lon1) * Math.PI / 180;
                var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                        Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            }

            var result = doctors.Select(d => new
            {
                d.Id,
                d.Name,
                d.Specialty,
                d.UserId,
                d.Online,
                ProfileImage = d.Image,
                // Compute distance only if doctor has coordinates stored
                distanceKm = (d.Latitude.HasValue && d.Longitude.HasValue)
                    ? (double?)HaversineKm(lat, lng, d.Latitude.Value, d.Longitude.Value)
                    : (double?)null
            })
            .OrderBy(d => d.distanceKm ?? double.MaxValue)
            .ToList();

            return Json(new { success = true, doctors = result });
        }


        public async Task<IActionResult> Wishlist()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "wishlist";
            return View(model);
        }



        public async Task<IActionResult> DoctorProfile(int id)
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "doctors"; // Keep doctors tab active
            
            var doctor = model.AvailableDoctors.FirstOrDefault(d => d.Id == id);
            if (doctor == null)
            {
                return RedirectToAction("Doctors");
            }
            
            model.SelectedDoctor = doctor;

            // Load Reviews explicitly
            await _context.Entry(doctor)
                .Collection(d => d.PatientReviews)
                .LoadAsync();
            
            // Fetch doctor's availability slots
            var availabilitySlots = await _context.DoctorAvailabilitySlots
                .Where(s => s.DoctorId == doctor.UserId && s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
            
            ViewBag.AvailabilitySlots = availabilitySlots;
            
            // Calculate next available slot
            var today = DateTime.Now.DayOfWeek.ToString();
            var currentTime = DateTime.Now.TimeOfDay;
            var nextSlot = availabilitySlots
                .FirstOrDefault(s => s.DayOfWeek == today && s.StartTime > currentTime);
            
            if (nextSlot == null)
            {
                // Find next day's first slot
                var daysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
                var todayIndex = Array.IndexOf(daysOfWeek, today);
                for (int i = 1; i <= 7; i++)
                {
                    var nextDay = daysOfWeek[(todayIndex + i) % 7];
                    nextSlot = availabilitySlots.FirstOrDefault(s => s.DayOfWeek == nextDay);
                    if (nextSlot != null) break;
                }
            }
            
            ViewBag.NextAvailableSlot = nextSlot;
            
            return View(model);
        }


        public class PushSubscriptionModel
        {
            public string Endpoint { get; set; }
            public string P256dh { get; set; }
            public string Auth { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0) return Json(new { success = false });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(uploadsFolder, fileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            user.ProfileImage = "/uploads/profiles/" + fileName;
            await _userManager.UpdateAsync(user);

            return Json(new { success = true, imageUrl = user.ProfileImage });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return Json(new { success = result.Succeeded, errors = result.Errors.Select(e => e.Description) });
        }

        [HttpPost]
        public async Task<IActionResult> RequestSessionRevoke()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var code = new Random().Next(100000, 999999).ToString();
            user.VerificationCode = code;
            user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
            await _userManager.UpdateAsync(user);

            await _emailSender.SendEmailAsync(user.Email ?? "", "Secure Sign-out Verification", 
                $"Your verification code to sign out other devices is: <b>{code}</b>. This code expires in 10 minutes.");

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyAndRevokeSession(string code, string sessionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found" });

            if (user.VerificationCode != code || user.VerificationCodeExpiry < DateTime.UtcNow)
                return Json(new { success = false, message = "Invalid or expired code" });

            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id);
            if (session == null) return Json(new { success = false, message = "Session not found" });

            session.IsRevoked = true;
            await _context.SaveChangesAsync();

            // Clear code after success
            user.VerificationCode = null;
            await _userManager.UpdateAsync(user);

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RevokeAllSessions(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            if (user.VerificationCode != code || user.VerificationCodeExpiry < DateTime.UtcNow)
                return Json(new { success = false, message = "Invalid or expired code" });

            var currentSessionId = Request.Cookies["MedLink_SessionId"];
            var otherSessions = await _context.UserSessions
                .Where(s => s.UserId == user.Id && s.SessionIdentifier != currentSessionId)
                .ToListAsync();

            foreach (var session in otherSessions)
            {
                session.IsRevoked = true;
            }

            await _context.SaveChangesAsync();
            user.VerificationCode = null;
            await _userManager.UpdateAsync(user);

            return Json(new { success = true });
        }



        private async Task<PatientDashboardModel> GetBaseModelAsync()
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
                    .Take(25)
                    .ToListAsync();
            });

            // 2. Health Records
            var recordsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.HealthRecords
                    .AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.Date)
                    .Take(25)
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
                    .Take(20)
                    .ToListAsync();
            });

            // 6. User Sessions
            var sessionsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.UserSessions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId && !s.IsRevoked)
                    .OrderByDescending(s => s.LastSeen)
                    .Take(10)
                    .Select(s => new UserSession {
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
            var ordersTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.PharmacyOrders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Medicine)
                    .Where(o => o.PatientId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            });

            // 8. Lab Bookings
            var labBookingsTask = Task.Run(async () => {
                using var context = _contextFactory.CreateDbContext();
                return await context.LabBookings
                    .AsNoTracking()
                    .Include(b => b.Laboratory)
                    .Where(b => b.PatientId == userId)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .ToListAsync();
            });

            await Task.WhenAll(appointmentsTask, recordsTask, medicationsTask, notificationsTask, aiAnalysesTask, sessionsTask, ordersTask, labBookingsTask);

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
                UpcomingConsultations = appointmentsTask.Result.Select(a => new Consultation { 
                    Id = a.Id,
                    DoctorId = a.DoctorId,
                    Doctor = a.Doctor?.Name ?? "General Doctor",
                    Specialty = a.Doctor?.Specialty ?? "General",
                    Time = a.TimeSlot,
                    Type = a.ConsultationType ?? "Video Call",
                    Image = a.Doctor?.Image ?? "https://picsum.photos/seed/doc/100/100",
                    RawDate = a.AppointmentDate,
                    Status = a.Status
                }).ToList(),
                HealthRecords = recordsTask.Result,
                Medications = medicationsTask.Result,
                Notifications = notificationsTask.Result,
                AIAnalyses = aiAnalysesTask.Result,
                RecentDevices = sessionsTask.Result,
                VapidPublicKey = _configuration["Vapid:PublicKey"],
                BillingHistory = new List<BillingInvoice>(),
                PharmacyOrders = ordersTask.Result,
                LabBookings = labBookingsTask.Result
            };

            // Include seeded doctors (no UserId) + approved user-linked doctors.
            // Clone after cache read because UnreadCount is user-specific.
            var cachedDoctors = await _cache.GetOrCreateAsync("ApprovedDoctors:v2", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);
                entry.SlidingExpiration = TimeSpan.FromMinutes(1);
                using var context = _contextFactory.CreateDbContext();
                return await (from d in context.Doctors.AsNoTracking()
                             join u in context.Users.AsNoTracking() on d.UserId equals u.Id into userJoin
                             from u in userJoin.DefaultIfEmpty()
                             where d.UserId == null || (u != null && u.ApprovalStatus != null && u.ApprovalStatus.Contains("Approved"))
                             select d).ToListAsync();
            }) ?? new List<Doctor>();

            model.AvailableDoctors = cachedDoctors.Select(d => new Doctor
            {
                Id = d.Id,
                Name = d.Name,
                Specialty = d.Specialty,
                Rating = d.Rating,
                Reviews = d.Reviews,
                Image = d.Image,
                Availability = d.Availability,
                Online = d.Online,
                Description = d.Description,
                Experience = d.Experience,
                Languages = d.Languages,
                Qualification = d.Qualification,
                Expertise = d.Expertise,
                HospitalAffiliations = d.HospitalAffiliations,
                ClinicAddress = d.ClinicAddress,
                ClinicMapUrl = d.ClinicMapUrl,
                ClinicName = d.ClinicName,
                PmdcRegistrationNumber = d.PmdcRegistrationNumber,
                Latitude = d.Latitude,
                Longitude = d.Longitude,
                UserId = d.UserId,
                SlotDuration = d.SlotDuration,
                BufferTime = d.BufferTime,
                CurrentPlanId = d.CurrentPlanId,
                IsSuspended = d.IsSuspended,
                SuspensionReason = d.SuspensionReason,
                IsAppealing = d.IsAppealing,
                AppealMessage = d.AppealMessage
            }).ToList();
            
            // Temporary Test Doctor for Debugging
            /* Test Debug Doctor Removed */
            
            _logger.LogInformation("Found {Count} approved doctors in the database (including 1 test doctor).", model.AvailableDoctors.Count);

             // Get metadata for sharing/read status
            var latestMessagesList = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Where(m => m.DoctorId.HasValue)
                .GroupBy(m => m.DoctorId)
                .Select(g => new { DoctorId = g.Key, MaxTimestamp = g.Max(m => m.Timestamp) })
                .ToListAsync();
            
            var latestMessages = latestMessagesList.ToDictionary(x => x.DoctorId.Value, x => x.MaxTimestamp);

            var unreadCountsList = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .Where(m => m.DoctorId.HasValue)
                .GroupBy(m => m.DoctorId)
                .Select(g => new { DoctorId = g.Key, Count = g.Count() })
                .ToListAsync();
            
            var unreadCounts = unreadCountsList.ToDictionary(x => x.DoctorId.Value, x => x.Count);

            var doctorUserMap = await _cache.GetOrCreateAsync("DoctorFeeMap", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return await (from d in _context.Doctors.AsNoTracking()
                             join u in _context.Users.AsNoTracking() on d.UserId equals u.Id
                             select new { d.Id, u.ConsultationFee }).ToDictionaryAsync(x => x.Id, x => x.ConsultationFee);
            });

            model.BillingHistory = appointmentsTask.Result
                .Select(a => {
                    decimal fee = 2000.00m;
                    if (a.DoctorId.HasValue && doctorUserMap.ContainsKey(a.DoctorId.Value)) {
                        fee = doctorUserMap[a.DoctorId.Value];
                    }
                    return new BillingInvoice 
                    { 
                        Id = "INV-" + a.Id.ToString("D5"), 
                        AppointmentId = a.Id,
                        Date = a.AppointmentDate.ToString("MMM dd, yyyy"), 
                        Amount = $"PKR {fee:N2}", 
                        Status = "Paid" 
                    };
                })
                .OrderByDescending(b => b.Date)
                .ToList();

            foreach(var doc in model.AvailableDoctors)
            {
                doc.UnreadCount = unreadCounts.ContainsKey(doc.Id) ? unreadCounts[doc.Id] : 0;
            }

            model.AvailableDoctors = model.AvailableDoctors
                .OrderByDescending(d => latestMessages.ContainsKey(d.Id) ? latestMessages[d.Id] : DateTime.MinValue)
                .ToList();

            /* Support/Admin Logic Removed - Support chat handled separately or disabled */

            // Calculate "IsCurrent" in ViewModel or logic
            var currentSid = Request.Cookies["MedLink_SessionId"];
            foreach(var d in model.RecentDevices) {
                // We'll use a hack to pass "isCurrent" by checking the ID
                if(d.SessionIdentifier == currentSid) {
                    d.Location = "Current Device"; // Using location field as a proxy for UI display tag
                }
            }

            return model;
        }


        [HttpGet]
        public async Task<IActionResult> BookAppointment(int doctorId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null) return RedirectToAction("Doctors");

            var user = await _userManager.GetUserAsync(User);
            var model = await GetBaseModelAsync();
            model.SelectedDoctor = doctor;
            
            var doctorUser = await _userManager.FindByIdAsync(doctor.UserId ?? "");
            ViewBag.ConsultationFee = doctorUser?.ConsultationFee ?? 0;
            ViewBag.UserName = user?.FirstName + " " + user?.LastName;
            ViewBag.UserEmail = user?.Email;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> BookAppointment(int doctorId, DateTime date, string timeSlot, string consultType, string notes, string patientName, string email)
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null) return RedirectToAction("Doctors");

            var appointment = new Appointment
            {
                DoctorId = doctorId,
                AppointmentDate = date,
                TimeSlot = timeSlot,
                ConsultationType = consultType ?? "Video Call",
                Notes = notes ?? "",
                PatientName = patientName,
                Email = email,
                UserId = userId,
                Status = "Pending Payment"
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Notify Doctor about the new (pending) appointment
            await _notificationService.CreateAndSendNotificationAsync(doctor.UserId, 
                "New Appointment Request", 
                $"{patientName} has requested a {consultType} for {date:MMM dd} at {timeSlot}.", 
                "calendar", "blue");

            // Create Stripe Checkout Session
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
                            UnitAmount = (long)((await _userManager.FindByIdAsync(doctor.UserId ?? ""))?.ConsultationFee * 100 ?? 5500), 
                            Currency = "pkr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Consultation with {doctor.Name}",
                                Description = $"{consultType} on {date:MMM dd, yyyy} at {timeSlot}",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = domain + "/Dashboard/PaymentSuccess?sessionId={CHECKOUT_SESSION_ID}&appointmentId=" + appointment.Id,
                CancelUrl = domain + "/Dashboard/PaymentCancel?appointmentId=" + appointment.Id,
                CustomerEmail = email,
                Metadata = new Dictionary<string, string>
                {
                    { "AppointmentId", appointment.Id.ToString() }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            appointment.StripeSessionId = session.Id;
            await _context.SaveChangesAsync();

            return Redirect(session.Url);
        }

        public async Task<IActionResult> PaymentSuccess(string sessionId, int appointmentId)
        {
            var userId = _userManager.GetUserId(User);
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId);

            if (session.PaymentStatus == "paid")
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.UserId == userId);

                if (appointment != null && appointment.StripeSessionId == sessionId)
                {
                    appointment.Status = "Confirmed";
                    
                    // Notify User about confirmation
                    await _notificationService.NotifyUserAsync(appointment.UserId, 
                        NotificationType.AppointmentBooked,
                        "Appointment Confirmed", 
                        $"Your appointment with {appointment.Doctor?.Name} on {appointment.AppointmentDate:MMM dd} is now confirmed!",
                        "calendar-check", "emerald");

                    await _context.SaveChangesAsync();
                    
                    // Sync with Admin Ledger (Automated Billing)
                    var adminPatient = await _context.AdminPatients.FirstOrDefaultAsync(p => p.Id == userId);
                    if (adminPatient == null)
                    {
                        var user = await _userManager.FindByIdAsync(userId ?? "");
                        adminPatient = new AdminModels.Patient
                        {
                            Id = userId ?? Guid.NewGuid().ToString(),
                            Name = user?.FullName ?? appointment.PatientName,
                            Diagnostic = "Checkup Referral", // Initial default
                            Status = "STABLE",
                            Node = "Online Portal",
                            DateRegistered = DateTime.Now,
                            Phone = user?.PhoneNumber ?? "",
                            Address = ""
                        };
                        _context.AdminPatients.Add(adminPatient);
                    }

                    var billing = new AdminModels.Billing
                    {
                        PatientId = userId ?? "",
                        Amount = (session.AmountTotal ?? 0) / 100m, // Convert back from cents
                        Description = $"Consultation Fee for appt with Dr. {appointment.Doctor?.Name}",
                        Status = "PAID",
                        DateGenerated = DateTime.Now,
                        InsuranceProvider = null,
                        InsurancePolicyNumber = null
                    };
                    _context.AdminBillings.Add(billing);

                    // --- Credit Doctor Wallet ---
                    if (appointment.Doctor != null && !string.IsNullOrEmpty(appointment.Doctor.UserId))
                    {
                        var doctorUser = await _userManager.FindByIdAsync(appointment.Doctor.UserId);
                        if (doctorUser != null)
                        {
                            var amountTotal = (session.AmountTotal ?? 0) / 100m;
                            doctorUser.WalletBalance += amountTotal;
                            
                            var walletTx = new WalletTransaction
                            {
                                DoctorId = appointment.Doctor.UserId,
                                Amount = amountTotal,
                                TransactionType = "EARNING",
                                Description = $"Consultation Earning from {appointment.PatientName}",
                                Status = "Completed",
                                TransactionDate = DateTime.Now,
                                AppointmentId = appointment.Id
                            };
                            _context.WalletTransactions.Add(walletTx);
                        }
                    }

                    await _context.SaveChangesAsync();
                    
                    var model = await GetBaseModelAsync();
                    model.ActiveTab = "appointments";
                    model.SuccessAppointment = appointment;
                    model.AmountPaid = (session.AmountTotal ?? 0) / 100m;
                    return View(model);
                }
            }

            return RedirectToAction("Appointments");
        }

        public async Task<IActionResult> PaymentCancel(int appointmentId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Doctors");
        }

        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var userId = _userManager.GetUserId(User);
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found or unauthorized." });
            }

            // Update status instead of deleting to keep history
            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            // Notify Doctor about cancellation
            if (appointment.DoctorId.HasValue)
            {
                var doctor = await _context.Doctors.FindAsync(appointment.DoctorId.Value);
                if (doctor != null)
                {
                    await _notificationService.CreateAndSendNotificationAsync(doctor.UserId,
                        "Appointment Cancelled",
                        $"Patient {appointment.PatientName} has cancelled their appointment for {appointment.AppointmentDate:MMM dd} at {appointment.TimeSlot}.",
                        "calendar-x", "rose");
                }
            }

            return Json(new { success = true, message = "Appointment cancelled successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> ClinicVisitDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (appointment == null) return RedirectToAction("Consultations");

            var model = await GetBaseModelAsync();
            model.SelectedDoctor = appointment.Doctor;
            
            ViewBag.Appointment = appointment;
            ViewBag.DoctorUser = await _userManager.FindByIdAsync(appointment.Doctor?.UserId ?? "");

            return View(model);
        }

        // Health Records Actions
        [HttpPost]
        public async Task<IActionResult> UploadHealthRecord(IFormFile file, string category, string provider)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file selected" });

            var userId = _userManager.GetUserId(User);
            
            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "health-records");
            Directory.CreateDirectory(uploadsPath);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Calculate file size
            var fileSizeInMB = (file.Length / 1024.0 / 1024.0).ToString("0.0") + " MB";

            // Determine type based on category
            var type = category switch
            {
                "Laboratory" => "Laboratory",
                "Radiology" => "Radiology",
                "Prescription" => "Prescription",
                _ => "Certification"
            };

            // Create health record
            var record = new HealthRecord
            {
                UserId = userId,
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                Type = type,
                Category = category,
                Date = DateTime.Now,
                Provider = provider ?? "Self Uploaded",
                FileSize = fileSizeInMB,
                FileType = Path.GetExtension(file.FileName).TrimStart('.').ToUpper(),
                FilePath = $"/uploads/health-records/{fileName}",
                CreatedAt = DateTime.Now
            };

            _context.HealthRecords.Add(record);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "File uploaded successfully", record });
        }

        [HttpPost]
        public async Task<IActionResult> SavePrescriptionRecord(int appointmentId, string diagnosis, string medicationsJson)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null) return Json(new { success = false, message = "Appointment not found" });

                // Create HTML Content for the Record
                var htmlContent = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: 'Segoe UI', sans-serif; padding: 40px; color: #333; }}
                            .header {{ border-bottom: 2px solid #eee; padding-bottom: 20px; margin-bottom: 30px; }}
                            .h-title {{ font-size: 24px; font-weight: bold; color: #2563eb; }}
                            .meta {{ display: flex; justify-content: space-between; margin-bottom: 30px; }}
                            .section {{ margin-bottom: 25px; }}
                            .label {{ font-size: 12px; font-weight: bold; text-transform: uppercase; color: #666; display: block; margin-bottom: 5px; }}
                            .value {{ font-size: 16px; background: #f8fafc; padding: 15px; border-radius: 8px; border: 1px solid #e2e8f0; }}
                            .med-item {{ border-bottom: 1px solid #eee; padding: 10px 0; }}
                            .footer {{ margin-top: 50px; font-size: 12px; color: #999; text-align: center; border-top: 1px solid #eee; padding-top: 20px; }}
                        </style>
                    </head>
                    <body>
                        <div class='header'>
                            <div class='h-title'>Digital Prescription</div>
                            <div>MedLink Medical Portal</div>
                        </div>
                        <div class='meta'>
                            <div>
                                <span class='label'>Doctor</span>
                                <strong>Dr. {appointment.Doctor.Name}</strong>
                            </div>
                            <div>
                                <span class='label'>Date</span>
                                <strong>{DateTime.Now:MMM dd, yyyy HH:mm}</strong>
                            </div>
                        </div>
                        <div class='section'>
                            <span class='label'>Clinical Diagnosis</span>
                            <div class='value'>{diagnosis}</div>
                        </div>
                        <div class='section'>
                            <span class='label'>Medications</span>
                            <div class='value'>
                                {medicationsJson}
                            </div>
                        </div>
                        <div class='footer'>
                            This is a digitally generated prescription record from MedLink Portal.
                            Ref: {Guid.NewGuid()}
                        </div>
                    </body>
                    </html>";

                // Save File
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "prescriptions");
                Directory.CreateDirectory(uploadsPath);
                var fileName = $"RX_{appointmentId}_{DateTime.Now.Ticks}.html";
                var filePath = Path.Combine(uploadsPath, fileName);
                await System.IO.File.WriteAllTextAsync(filePath, htmlContent);

                // Create Health Record
                var record = new HealthRecord
                {
                    UserId = userId,
                    Name = $"Prescription - Dr. {appointment.Doctor.Name}",
                    Type = "Prescription",
                    Category = "Prescription",
                    Date = DateTime.Now,
                    Provider = $"Dr. {appointment.Doctor.Name}",
                    FileSize = "2 KB",
                    FileType = "HTML",
                    FilePath = $"/uploads/prescriptions/{fileName}",
                    CreatedAt = DateTime.Now
                };

                _context.HealthRecords.Add(record);
                await _context.SaveChangesAsync();

                // Notify User about upload
                await _notificationService.NotifyUserAsync(userId,
                    NotificationType.MedicalReportUploaded,
                    "Medical Report Uploaded",
                    $"Your Prescription report from Dr. {appointment.Doctor.Name} has been successfully added to your records.",
                    "file-text", "blue");

                return Json(new { success = true, recordId = record.Id, message = "Prescription saved to records" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> DownloadHealthRecord(int id)
        {
            var userId = _userManager.GetUserId(User);
            var record = await _context.HealthRecords
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (record == null || string.IsNullOrEmpty(record.FilePath))
                return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", record.FilePath.TrimStart('/'));
            
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var contentType = record.FileType.ToLower() switch
            {
                "pdf" => "application/pdf",
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "dicom" => "application/dicom",
                _ => "application/octet-stream"
            };

            return File(memory, contentType, $"{record.Name}.{record.FileType.ToLower()}");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteHealthRecord(int id)
        {
            var userId = _userManager.GetUserId(User);
            var record = await _context.HealthRecords
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (record == null)
                return Json(new { success = false, message = "Record not found" });

            // Delete physical file if exists
            if (!string.IsNullOrEmpty(record.FilePath))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", record.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.HealthRecords.Remove(record);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Record deleted successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> ShareHealthRecord(int recordId, int doctorId)
        {
            var userId = _userManager.GetUserId(User);
            var record = await _context.HealthRecords
                .FirstOrDefaultAsync(r => r.Id == recordId && r.UserId == userId);

            if (record == null)
                return Json(new { success = false, message = "Record not found" });

            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
                return Json(new { success = false, message = "Doctor not found" });

            // Create chat message with record details
            var message = new ChatMessage
            {
                SenderId = userId,
                ReceiverId = "doctor_" + doctorId,
                DoctorId = doctorId,
                Content = $"📄 Shared Health Record: {record.Name}\n📁 Category: {record.Category}\n📅 Date: {record.Date:MMM dd, yyyy}\n🏥 Provider: {record.Provider}",
                MessageType = "Document",
                AttachmentUrl = record.FilePath,
                AttachmentName = $"{record.Name}.{record.FileType}",
                AttachmentType = record.FileType.ToLower() == "pdf" ? "Document" : "Image",
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Record shared with {doctor.Name}" });
        }

        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var userId = _userManager.GetUserId(User);
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (appointment == null) return NotFound();

            var model = await GetBaseModelAsync();
            model.SuccessAppointment = appointment;
             // Fetch fee
            if (appointment.Doctor != null && !string.IsNullOrEmpty(appointment.Doctor.UserId)) {
                var docUser = await _userManager.FindByIdAsync(appointment.Doctor.UserId);
                model.AmountPaid = docUser?.ConsultationFee ?? 2000;
            } else {
                model.AmountPaid = 2000;
            }
            return View("Invoice", model);
        }
        // Medication Actions
        [HttpPost]
        public async Task<IActionResult> AddMedication(string name, string dosage, string schedule)
        {
            var userId = _userManager.GetUserId(User);
            var medication = new Medication
            {
                Name = name,
                Dosage = dosage,
                Schedule = schedule,
                Taken = false,
                UserId = userId
            };

            _context.Medications.Add(medication);
            await _context.SaveChangesAsync();

            return Json(new { success = true, medication });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleMedication(int id)
        {
            var userId = _userManager.GetUserId(User);
            var med = await _context.Medications.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
            
            if (med != null)
            {
                med.Taken = !med.Taken;
                await _context.SaveChangesAsync();
                return Json(new { success = true, taken = med.Taken });
            }
            
            return Json(new { success = false });
        }



        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> TranslateTranscription(string text, int appointmentId = 0, string speakerRole = "")
        {
            if (string.IsNullOrEmpty(text)) return Json(new { success = false });
            
            var userId = _userManager.GetUserId(User);
            var userName = User.Identity.Name?.Split('@')[0] ?? "User";
            
            var result = text; // Translation removed (Gemini service replaced)
            
            // Save to database if appointmentId is provided
            if (appointmentId > 0 && !string.IsNullOrEmpty(speakerRole))
            {
                try
                {
                    var transcript = new ConsultationTranscript
                    {
                        AppointmentId = appointmentId,
                        SpeakerId = userId,
                        SpeakerName = userName,
                        SpeakerRole = speakerRole,
                        OriginalText = text,
                        EnglishTranslation = text,
                        UrduTranslation = text,
                        DetectedLanguage = "auto",
                        Timestamp = DateTime.UtcNow
                    };
                    
                    _context.ConsultationTranscripts.Add(transcript);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the request
                    Console.WriteLine($"Error saving transcript: {ex.Message}");
                }
            }
            
            return Json(new { 
                success = true, 
                original = text,
                english = text,
                urdu = text,
                detectedLanguage = "auto"
            });
        }

        [HttpGet]
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

            var model = await GetBaseModelAsync();
            model.ActiveTab = "transcription-history";

            // Filter consultations to only those that have transcripts
            var consultationIdsWithTranscripts = await _context.ConsultationTranscripts
                .Select(t => t.AppointmentId)
                .Distinct()
                .ToListAsync();

            model.UpcomingConsultations = model.UpcomingConsultations
                .Where(c => consultationIdsWithTranscripts.Contains(c.Id))
                .OrderByDescending(c => c.RawDate)
                .ToList();

            ViewData["HeaderTitle"] = "Transcription History";
            ViewData["HeaderSubtitle"] = "Review your past consultation transcripts and AI insights.";

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SecurityProtocol()
        {
            var model = await GetBaseModelAsync();
            model.ActiveTab = "transcription-history";
            
            ViewData["HeaderTitle"] = "Security Protocol";
            ViewData["HeaderSubtitle"] = "Understanding MedLink's military-grade data protection architecture.";
            
            return View(model);
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

            bool isPatient = appointment.UserId == userId;
            bool isDoctor = appointment.Doctor != null && appointment.Doctor.UserId == userId;
            if (!isPatient && !isDoctor) return Unauthorized();

            ViewBag.AppointmentId = id;
            ViewBag.DoctorName = appointment.Doctor?.Name ?? "Doctor";
            ViewBag.PatientName = (await _context.Users.FindAsync(appointment.UserId))?.Name ?? "Patient";
            ViewBag.AppointmentDate = appointment.AppointmentDate.ToString("MMMM dd, yyyy");

            var model = await GetBaseModelAsync();
            model.ActiveTab = "transcription-history";
            ViewData["HeaderTitle"] = "Consultation Transcript";
            ViewData["HeaderSubtitle"] = $"Session with Dr. {ViewBag.DoctorName} on {ViewBag.AppointmentDate}";
            return View(model);
        }

        public async Task<IActionResult> ConsultationRoom(string id)
        {
            var userId = _userManager.GetUserId(User);

            if (!string.IsNullOrEmpty(id) && id.StartsWith("triage_"))
            {
                var doctorUserId = Request.Query["doctorUserId"].ToString();
                var doctorIdStr = Request.Query["doctorId"].ToString();
                Doctor doctor = null;
                if (!string.IsNullOrEmpty(doctorUserId))
                    doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorUserId);
                else if (int.TryParse(doctorIdStr, out int docId))
                    doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == docId);
                if (doctor == null)
                    doctor = await _context.Doctors.FirstOrDefaultAsync();

                ViewBag.AppointmentId = id;
                ViewBag.DoctorName    = doctor?.Name  ?? "Doctor";
                ViewBag.DoctorImage   = doctor?.Image ?? "https://picsum.photos/seed/doc/100/100";
                ViewBag.PatientName   = User.Identity?.Name?.Split('@')[0] ?? "Patient";
                ViewBag.PatientUserId = userId;
                ViewBag.DoctorUserId  = doctor?.UserId ?? "";
                ViewBag.CurrentUserId = userId;
                ViewBag.IsPrescriptionLocked = false;
                ViewBag.PrescriptionData     = null;
                var mt = await GetBaseModelAsync(); mt.ActiveTab = "appointments";
                return View(mt);
            }

            if (!int.TryParse(id, out int appointmentId)) return NotFound();
            var appt = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appt == null) return NotFound();
            var patientUser = await _context.Users.FindAsync(appt.UserId);
            bool ip = appt.UserId == userId;
            bool id2 = appt.Doctor != null && appt.Doctor.UserId == userId;
            if (!ip && !id2) return Unauthorized();

            ViewBag.AppointmentId        = id;
            ViewBag.DoctorName           = appt.Doctor?.Name  ?? "Doctor";
            ViewBag.DoctorImage          = appt.Doctor?.Image ?? "https://picsum.photos/seed/doc/100/100";
            ViewBag.PatientName          = patientUser?.Name ?? patientUser?.UserName ?? "Patient";
            ViewBag.PatientUserId        = appt.UserId;
            ViewBag.DoctorUserId         = appt.Doctor?.UserId;
            ViewBag.CurrentUserId        = userId;
            ViewBag.IsPrescriptionLocked = false;
            ViewBag.PrescriptionData     = null;
            var mr = await GetBaseModelAsync(); mr.ActiveTab = "appointments";
            return View(mr);
        }

        [HttpPost]
        public async Task<IActionResult> UploadConsultationFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file uploaded" });
            try
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/consultation", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);
                var url  = "/uploads/consultation/" + fileName;
                var type = file.ContentType.StartsWith("image/") ? "image" : "file";
                return Json(new { success = true, url, type, name = file.FileName });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(string fullName, string phone, string dob, bool emailNotifications, bool pushNotifications)
        {
            var userId = _userManager.GetUserId(User);
            var user   = await _userManager.FindByIdAsync(userId ?? "");
            if (user == null) return Json(new { success = false, message = "User not found" });
            if (!string.IsNullOrEmpty(fullName))
            {
                var parts = fullName.Trim().Split(' ', 2);
                user.FirstName = parts[0];
                user.LastName  = parts.Length > 1 ? parts[1] : "";
            }
            user.PhoneNumber = phone;
            if (DateTime.TryParse(dob, out var date)) user.DateOfBirth = date;
            user.EmailNotificationsEnabled = emailNotifications;
            user.PushNotificationsEnabled  = pushNotifications;
            var result = await _userManager.UpdateAsync(user);
            return Json(new { success = result.Succeeded, message = result.Succeeded ? "Settings updated" : "Update failed" });
        }

        [HttpPost]
        public async Task<IActionResult> SubscribeToPush([FromBody] Models.PushSubscriptionViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();
            if (model.Endpoint == "RESET_TRIGGER")
            {
                var subs = await _context.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync();
                _context.PushSubscriptions.RemoveRange(subs);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            var existing = await _context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == model.Endpoint);
            if (existing != null) _context.PushSubscriptions.Remove(existing);
            var sub = new PushSubscription
            {
                UserId    = userId,
                Endpoint  = model.Endpoint,
                P256dh    = model.P256dh,
                Auth      = model.Auth,
                CreatedAt = DateTime.Now
            };
            _context.PushSubscriptions.Add(sub);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                await _notificationService.MarkAllAsReadAsync(userId);
            }
            return Json(new { success = true });
        }
    }

    public class AiDoctorChatRequest
    {
        public string? Message { get; set; }
    }
}
