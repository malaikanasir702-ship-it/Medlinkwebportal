// PatientDashboardModel.cs
namespace MedLinkPortal.Models
{
    public class PatientDashboardModel
    {
        public string ActiveTab { get; set; } = "overview";
        public bool IsLoading { get; set; } = true;
        public string PatientName { get; set; } = "Alex";
        public string? PatientId { get; set; }
        public string? PatientEmail { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string ProfileImage { get; set; } = "https://picsum.photos/seed/patient/100/100";
        public bool EmailNotificationsEnabled { get; set; } = true;
        public bool PushNotificationsEnabled { get; set; } = true;
        public bool MarketingEmailsEnabled { get; set; }
        public bool DarkModeEnabled { get; set; }
        public Doctor SelectedDoctor { get; set; }
        public List<NavItem> NavItems { get; set; } = new List<NavItem>
        {
            new NavItem { Id = "overview", Icon = "layout-dashboard", Label = "Overview" },
            new NavItem { Id = "appointments", Icon = "calendar", Label = "Appointments" },
            new NavItem { Id = "records", Icon = "database", Label = "Health Records" },
            new NavItem { Id = "messages", Icon = "message-square", Label = "Messages" },
            new NavItem { Id = "ai-lab", Icon = "brain", Label = "AI Diagnostic Lab" },
            new NavItem { Id = "doctors", Icon = "stethoscope", Label = "Find Doctors" },
            new NavItem { Id = "medical-tourism", Icon = "plane", Label = "Medical Tourism" },
            new NavItem { Id = "lab-diagnostics", Icon = "test-tube", Label = "Lab & Diagnostics" },
            new NavItem { Id = "transcription-history", Icon = "file-text", Label = "Transcription History" },
            new NavItem { Id = "store", Icon = "shopping-cart", Label = "Pharmacy Store" },
            new NavItem { Id = "orders", Icon = "shopping-bag", Label = "My Orders" },
            new NavItem { Id = "settings", Icon = "settings", Label = "Settings" },
            new NavItem { Id = "wishlist", Icon = "heart", Label = "Wishlist" }
        };
        public List<DashboardVital> HealthVitals { get; set; }
        public List<Consultation> UpcomingConsultations { get; set; }
        public List<HealthRecord> HealthRecords { get; set; }
        public List<Medication> Medications { get; set; }
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public List<Doctor> AvailableDoctors { get; set; }
        public List<AIAnalysis> AIAnalyses { get; set; } = new List<AIAnalysis>();
        
        // Settings Data
        public List<UserSession> RecentDevices { get; set; } = new List<UserSession>();
        public List<BillingInvoice> BillingHistory { get; set; } = new List<BillingInvoice>();
        public decimal AmountPaid { get; set; }

        public Appointment SuccessAppointment { get; set; }
        public string? VapidPublicKey { get; set; }
        public PharmacyOrder CurrentOrder { get; set; }
        public Prescription CurrentPrescription { get; set; }
        public List<PharmacyOrder> PharmacyOrders { get; set; } = new List<PharmacyOrder>();
        public List<Medicine> StoreMedicines { get; set; } = new List<Medicine>();
        public List<LabBooking> LabBookings { get; set; } = new List<LabBooking>();
        public List<Laboratory> Laboratories { get; set; } = new List<Laboratory>();
        public List<MedicalTestCategory> MedicalTestCategories { get; set; } = new List<MedicalTestCategory>();
        public Laboratory SelectedLaboratory { get; set; }
        public AIHealthReport AIReport { get; set; }
    }

    public class AIHealthReport
    {
        public int OverallScore { get; set; }
        public string StatusLabel { get; set; }
        public string StatusColor { get; set; }
        public string Summary { get; set; }
        public string ResilienceTrend { get; set; }
        public List<DashboardVital> Vitals { get; set; } = new List<DashboardVital>();
        public List<DailyTip> DailyTips { get; set; } = new List<DailyTip>();
        public List<MealPlan> DietPlan { get; set; } = new List<MealPlan>();
        public List<ClinicalProtocol> Protocols { get; set; } = new List<ClinicalProtocol>();
    }

    public class DailyTip
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
    }

    public class MealPlan
    {
        public string MealTime { get; set; } // Breakfast, Lunch, Dinner, Snack
        public string FoodItems { get; set; }
        public string NutritionalValue { get; set; }
        public string Icon { get; set; }
    }

    public class ClinicalProtocol
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ActionText { get; set; }
        public string Color { get; set; }
        public string Icon { get; set; }
    }

    public class HealthAnalysisInput
    {
        public int Age { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }
        public string Gender { get; set; }
        public string ActivityLevel { get; set; }
        public string DietPreference { get; set; }
        public string Symptoms { get; set; }
        public string HealthGoals { get; set; }
    }

    public class BillingInvoice
    {
        public string Id { get; set; }
        public int AppointmentId { get; set; } // Added for linking to actual data
        public string Date { get; set; }
        public string Amount { get; set; }
        public string Status { get; set; }
    }

    public class SleepData
    {
        public int OverallSleepScore { get; set; }
        public string SleepQuality { get; set; }
        public List<SleepDayData> WeeklyPattern { get; set; } = new List<SleepDayData>();
        public SleepStages SleepStages { get; set; }
        public CircadianRhythm CircadianData { get; set; }
        public List<SleepInsight> Insights { get; set; } = new List<SleepInsight>();
        public SleepHygieneScore HygieneScore { get; set; }
    }

    public class SleepDayData
    {
        public string Date { get; set; }
        public string DayName { get; set; }
        public double TotalHours { get; set; }
        public int QualityScore { get; set; }
        public string BedTime { get; set; }
        public string WakeTime { get; set; }
        public int Interruptions { get; set; }
    }

    public class SleepStages
    {
        public double RemPercentage { get; set; }
        public double DeepPercentage { get; set; }
        public double LightPercentage { get; set; }
        public double AwakePercentage { get; set; }
    }

    public class CircadianRhythm
    {
        public string OptimalBedTime { get; set; }
        public string OptimalWakeTime { get; set; }
        public int CircadianAlignment { get; set; }
        public string Chronotype { get; set; }
    }

    public class SleepInsight
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Priority { get; set; }
        public string Color { get; set; }
    }

    public class SleepHygieneScore
    {
        public int OverallScore { get; set; }
        public List<HygieneFactor> Factors { get; set; } = new List<HygieneFactor>();
    }

    public class HygieneFactor
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public string Status { get; set; }
        public string Recommendation { get; set; }
    }

    public class NavItem
    {
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Label { get; set; }
    }

    public class DashboardVital
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string Icon { get; set; }
        public string Trend { get; set; }
        public string Color { get; set; }
    }

    public class Consultation
    {
        public int Id { get; set; }
        public int? DoctorId { get; set; } // Changed to nullable to match Appointment model
        public string Doctor { get; set; }
        public string Specialty { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
        public string Image { get; set; }
        public DateTime? RawDate { get; set; }
        public string Status { get; set; }
    }
}