using MedLinkPortal.Attributes;
using MedLinkPortal.Services;
using MedLinkPortal.Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq;
using System.Reflection;

namespace MedLinkPortal.Models
{
    // Aliases to handle name collisions
    using AdminModels = MedLinkPortal.Areas.Admin.Models;
    using DoctorModels = MedLinkPortal.Areas.Doctor.Models;

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IEncryptionService _encryptionService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IEncryptionService encryptionService)
            : base(options)
        {
            _encryptionService = encryptionService;
        }

        // --- Core MedLink Entities ---
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<AIAnalysis> AIAnalyses { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<HealthRecord> HealthRecords { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PushSubscription> PushSubscriptions { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<ConsultationTranscript> ConsultationTranscripts { get; set; }
        public DbSet<AiChatMessage> AiChatMessages { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<HealthVital> HealthVitals { get; set; }
        public DbSet<FamilyLink> FamilyLinks { get; set; }

        // --- Subscription Entities ---
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<DoctorSubscription> DoctorSubscriptions { get; set; }

        // --- Medical Tourism Entities ---
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<MedicalTourismRequest> MedicalTourismRequests { get; set; }
        public DbSet<MedicalTourismPackage> MedicalTourismPackages { get; set; }

        // --- Lab & Diagnostics Entities ---
        public DbSet<City> Cities { get; set; }
        public DbSet<Laboratory> Laboratories { get; set; }
        public DbSet<MedicalTestCategory> MedicalTestCategories { get; set; }
        public DbSet<MedicalTest> MedicalTests { get; set; }
        public DbSet<LabBooking> LabBookings { get; set; }
        public DbSet<LabBookingItem> LabBookingItems { get; set; }
        public DbSet<LabTestResult> LabTestResults { get; set; }

        // --- Pharmacy & Medicine Ordering Entities ---
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<PrescriptionMedicine> PrescriptionMedicines { get; set; }
        public DbSet<PharmacyOrder> PharmacyOrders { get; set; }
        public DbSet<PharmacyOrderItem> PharmacyOrderItems { get; set; }

        // --- Rider Tracking Entities ---
        public DbSet<Rider> Riders { get; set; }
        public DbSet<RiderSession> RiderSessions { get; set; }
        public DbSet<TrackingAuditLog> TrackingAuditLogs { get; set; }
        public DbSet<RiderRating> RiderRatings { get; set; }

        // --- Admin Area Entities ---
        public DbSet<AdminModels.Physician> AdminPhysicians { get; set; }
        public DbSet<AdminModels.Patient> AdminPatients { get; set; }
        public DbSet<AdminModels.AdminProfile> AdminProfiles { get; set; }
        public DbSet<AdminModels.Appointment> AdminAppointments { get; set; }
        public DbSet<AdminModels.MedicalRecord> AdminMedicalRecords { get; set; }
        public DbSet<AdminModels.Billing> AdminBillings { get; set; }

        // --- Doctor Area Entities ---
        public DbSet<DoctorModels.Message> DoctorMessages { get; set; }
        public DbSet<DoctorModels.PatientRecord> DoctorPatientRecords { get; set; }
        public DbSet<DoctorModels.Appointment> DoctorAppointments { get; set; }
        public DbSet<DoctorModels.DoctorAvailabilitySlot> DoctorAvailabilitySlots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Admin Mappings ---
            modelBuilder.Entity<AdminModels.Physician>().ToTable("Admin_Physicians");
            modelBuilder.Entity<AdminModels.Patient>().ToTable("Admin_Patients");
            modelBuilder.Entity<AdminModels.AdminProfile>().ToTable("Admin_Profiles");
            modelBuilder.Entity<AdminModels.Appointment>().ToTable("Admin_Appointments");
            modelBuilder.Entity<AdminModels.MedicalRecord>().ToTable("Admin_MedicalRecords");
            modelBuilder.Entity<AdminModels.Billing>().ToTable("Admin_Billings")
                .Property(b => b.Amount).HasColumnType("decimal(18,2)");

            // --- Doctor Mappings ---
            modelBuilder.Entity<DoctorModels.Message>().ToTable("Doc_Messages")
                .HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DoctorModels.Message>()
                .HasOne(m => m.Receiver).WithMany().HasForeignKey(m => m.ReceiverId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DoctorModels.PatientRecord>().ToTable("Doc_PatientRecords")
                .HasOne(pr => pr.Patient).WithMany().HasForeignKey(pr => pr.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DoctorModels.PatientRecord>()
                .HasOne(pr => pr.Doctor).WithMany().HasForeignKey(pr => pr.DoctorId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DoctorModels.Appointment>().ToTable("Doc_Appointments")
                .HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DoctorModels.Appointment>()
                .HasOne(a => a.Doctor).WithMany().HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DoctorModels.DoctorAvailabilitySlot>().ToTable("Doc_DoctorAvailabilitySlots");

            // --- ApplicationUser Mappings ---
            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.ConsultationFee)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.WalletBalance)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.TotalWithdrawn)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<WalletTransaction>()
                .Property(w => w.Amount)
                .HasColumnType("decimal(18,2)");

            // Seed Admin Data (Simplified for merge)
            modelBuilder.Entity<AdminModels.AdminProfile>().HasData(
                new AdminModels.AdminProfile
                {
                    Id = 1,
                    Name = "Dr. Alex Rivers",
                    Specialty = "Director",
                    Email = "a.rivers@medical.io",
                    Phone = "+1 (555) 000-1111",
                    Office = "Executive Suite 401",
                    Bio = "Leading medical diagnostics with 20 years of experience in system administration."
                }
            );

            // --- Subscription Plan Seeding ---
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = 1,
                    Name = "Starter",
                    Price = 0,
                    PatientLimit = 50,
                    Description = "Basic plan for small practices",
                    Features = "50 Patient Records|Basic Scheduling|Standard Analytics",
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "MedLink PRO",
                    Price = 49,
                    PatientLimit = -1,
                    Description = "Elite tier for growing clinics",
                    Features = "Unlimited Patient Records|AI Diagnostic Suggestions|Priority 24/7 Support|Advanced Analytics Suite",
                    IsActive = true
                }
            );

            // --- City Seeding ---
            modelBuilder.Entity<City>().HasData(
                new City { Id = 1, Name = "Karachi" },
                new City { Id = 2, Name = "Lahore" },
                new City { Id = 3, Name = "Islamabad" }
            );

            // --- Medical Test Category Seeding ---
            modelBuilder.Entity<MedicalTestCategory>().HasData(
                new MedicalTestCategory { Id = 1, Name = "Hematology" },
                new MedicalTestCategory { Id = 2, Name = "Clinical Chemistry" },
                new MedicalTestCategory { Id = 3, Name = "Microbiology" },
                new MedicalTestCategory { Id = 4, Name = "Immunology" },
                new MedicalTestCategory { Id = 5, Name = "Radiology & Imaging" },
                new MedicalTestCategory { Id = 6, Name = "Pathology" },
                new MedicalTestCategory { Id = 7, Name = "Molecular Diagnostics" },
                new MedicalTestCategory { Id = 8, Name = "Toxicology" },
                new MedicalTestCategory { Id = 9, Name = "Cultures" },
                new MedicalTestCategory { Id = 10, Name = "Special Tests" }
            );

            // --- Doctor Seeding (Pakistani Names, 25 Categories, IDs 101-125) ---
            modelBuilder.Entity<Doctor>().HasData(
                new Doctor { Id = 101, Name = "Dr. Ahmed Khan", Specialty = "General Physician", Rating = 4.8, Reviews = 124, Experience = "12 Years", Qualification = "MBBS, FCPS", Languages = "English, Urdu, Punjabi", Availability = "Available Today", Online = true, Description = "Experienced general physician specializing in family medicine and chronic disease management.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 102, Name = "Dr. Fatima Zahra", Specialty = "Cardiologist", Rating = 4.9, Reviews = 89, Experience = "15 Years", Qualification = "MBBS, MD (Cardiology)", Languages = "English, Urdu", Availability = "Mon - Sat", Online = true, Description = "Expert cardiologist focusing on interventional cardiology and preventive heart care.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 103, Name = "Dr. Usman Ali", Specialty = "Dermatologist", Rating = 4.7, Reviews = 156, Experience = "8 Years", Qualification = "MBBS, MCPS (Dermatology)", Languages = "English, Urdu", Availability = "Available Today", Online = false, Description = "Specialist in skin disorders, aesthetic dermatology, and laser treatments.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 104, Name = "Dr. Ayesha Malik", Specialty = "Pediatrician", Rating = 4.9, Reviews = 210, Experience = "10 Years", Qualification = "MBBS, DCH, FCPS", Languages = "English, Urdu, Pashto", Availability = "Daily", Online = true, Description = "Dedicated pediatrician with a focus on neonatal care and childhood nutrition.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 105, Name = "Dr. Zainab Bibi", Specialty = "Gynecologist", Rating = 4.8, Reviews = 178, Experience = "14 Years", Qualification = "MBBS, MS (OBGYN)", Languages = "English, Urdu", Availability = "Mon - Fri", Online = true, Description = "Comprehensive women's health specialist specializing in high-risk pregnancies.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 106, Name = "Dr. Bilal Hassan", Specialty = "Orthopedic Surgeon", Rating = 4.6, Reviews = 67, Experience = "11 Years", Qualification = "MBBS, FRCS", Languages = "English, Urdu", Availability = "Tue - Sat", Online = false, Description = "Surgeon specializing in joint replacement, sports injuries, and fracture management.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 107, Name = "Dr. Sarah Ahmed", Specialty = "Neurologist", Rating = 4.9, Reviews = 45, Experience = "9 Years", Qualification = "MBBS, FCPS (Neurology)", Languages = "English, Urdu", Availability = "Available Today", Online = true, Description = "Expert in treating neurological disorders including epilepsy, stroke, and migraines.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 108, Name = "Dr. Hamza Siddiqui", Specialty = "Psychiatrist", Rating = 4.7, Reviews = 92, Experience = "7 Years", Qualification = "MBBS, MD (Psychiatry)", Languages = "English, Urdu, Sindhi", Availability = "Mon - Thu", Online = true, Description = "Specializing in mental health, stress management, and behavioral therapy.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 109, Name = "Dr. Mariam Farooq", Specialty = "Otolaryngologist (ENT Specialist)", Rating = 4.8, Reviews = 112, Experience = "13 Years", Qualification = "MBBS, DLO, FCPS", Languages = "English, Urdu", Availability = "Available Today", Online = false, Description = "Expert in ear, nose, and throat surgeries and allergy management.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 110, Name = "Dr. Zeeshan Haider", Specialty = "Ophthalmologist", Rating = 4.9, Reviews = 134, Experience = "16 Years", Qualification = "MBBS, FRCS (Ophthalmology)", Languages = "English, Urdu", Availability = "Wed - Sun", Online = true, Description = "Eye specialist focusing on cataract surgery and retinal disorders.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 111, Name = "Dr. Hina Nasir", Specialty = "Urologist", Rating = 4.7, Reviews = 58, Experience = "10 Years", Qualification = "MBBS, MS (Urology)", Languages = "English, Urdu", Availability = "Available Today", Online = true, Description = "Specialist in kidney stones, urinary tract infections, and male infertility.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 112, Name = "Dr. Omar Sheikh", Specialty = "Gastroenterologist", Rating = 4.8, Reviews = 88, Experience = "12 Years", Qualification = "MBBS, FCPS (Gastro)", Languages = "English, Urdu", Availability = "Mon - Fri", Online = false, Description = "Expert in liver diseases, endoscopy, and digestive health.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 113, Name = "Dr. Sana Javed", Specialty = "Pulmonologist", Rating = 4.6, Reviews = 74, Experience = "8 Years", Qualification = "MBBS, DTCD", Languages = "English, Urdu", Availability = "Available Today", Online = true, Description = "Specialist in asthma, COPD, and respiratory infections.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 114, Name = "Dr. Faisal Qureshi", Specialty = "Endocrinologist", Rating = 4.9, Reviews = 63, Experience = "11 Years", Qualification = "MBBS, MD (Endo)", Languages = "English, Urdu", Availability = "Tue - Sat", Online = true, Description = "Expert in diabetes management and hormonal disorders.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 115, Name = "Dr. Kiran Shahzadi", Specialty = "Oncologist", Rating = 5.0, Reviews = 42, Experience = "15 Years", Qualification = "MBBS, FCPS (Oncology)", Languages = "English, Urdu", Availability = "Available Today", Online = false, Description = "Dedicated to providing compassionate cancer care and chemotherapy.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 116, Name = "Dr. Adnan Mahmood", Specialty = "Radiologist", Rating = 4.8, Reviews = 31, Experience = "9 Years", Qualification = "MBBS, DMRD", Languages = "English, Urdu", Availability = "Daily", Online = true, Description = "Expert in medical imaging interpretation including MRI, CT, and Ultrasound.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 117, Name = "Dr. Nida Yousaf", Specialty = "Anesthesiologist", Rating = 4.7, Reviews = 25, Experience = "10 Years", Qualification = "MBBS, DA", Languages = "English, Urdu", Availability = "On Call", Online = true, Description = "Specializing in pain management and surgical anesthesia.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 118, Name = "Dr. Rashid Minhas", Specialty = "General Surgeon", Rating = 4.9, Reviews = 145, Experience = "18 Years", Qualification = "MBBS, FRCS (Surgery)", Languages = "English, Urdu, Punjabi", Availability = "Mon - Sat", Online = false, Description = "Highly experienced surgeon performing laparoscopic and general surgeries.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 119, Name = "Dr. Amna Gul", Specialty = "Dentist", Rating = 4.8, Reviews = 230, Experience = "6 Years", Qualification = "BDS, RDS", Languages = "English, Urdu", Availability = "Available Today", Online = true, Description = "Focusing on family dentistry, orthodontics, and oral hygiene.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 120, Name = "Dr. Waqas Ahmed", Specialty = "Physiotherapist", Rating = 4.7, Reviews = 115, Experience = "7 Years", Qualification = "DPT (Doctor of Physical Therapy)", Languages = "English, Urdu", Availability = "Daily", Online = true, Description = "Specialist in physical rehabilitation and sports injury recovery.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 121, Name = "Dr. Sadia Noreen", Specialty = "Nutritionist/Dietitian", Rating = 4.9, Reviews = 95, Experience = "5 Years", Qualification = "MSc Nutrition", Languages = "English, Urdu", Availability = "Mon - Fri", Online = true, Description = "Expert in weight management and clinical nutrition planning.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 122, Name = "Dr. Naveed Iqbal", Specialty = "Pathologist", Rating = 4.8, Reviews = 18, Experience = "14 Years", Qualification = "MBBS, M.Phil (Pathology)", Languages = "English, Urdu", Availability = "Mon - Sat", Online = false, Description = "Expert in laboratory medicine and diagnostic pathology.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 123, Name = "Dr. Rabia Basri", Specialty = "Nephrologist", Rating = 4.7, Reviews = 39, Experience = "11 Years", Qualification = "MBBS, FCPS (Nephro)", Languages = "English, Urdu", Availability = "Available Today", Online = true, Description = "Specialist in kidney health, dialysis, and hypertension.", Image = "https://img.icons8.com/color/96/doctor-female.png" },
                new Doctor { Id = 124, Name = "Dr. Junaid Akram", Specialty = "Hematologist", Rating = 4.8, Reviews = 27, Experience = "9 Years", Qualification = "MBBS, FCPS (Hematology)", Languages = "English, Urdu", Availability = "Tue - Sat", Online = true, Description = "Specialist in blood disorders and transfusion medicine.", Image = "https://img.icons8.com/color/96/doctor-male.png" },
                new Doctor { Id = 125, Name = "Dr. Mahira Noor", Specialty = "Rheumatologist", Rating = 4.9, Reviews = 52, Experience = "13 Years", Qualification = "MBBS, MRCP", Languages = "English, Urdu", Availability = "Available Today", Online = true, Description = "Expert in autoimmune diseases and joint health.", Image = "https://img.icons8.com/color/96/doctor-female.png" }
            );

            // --- Performance Indexes ---
            modelBuilder.Entity<Doctor>().HasIndex(d => d.UserId);
            modelBuilder.Entity<Doctor>().HasIndex(d => d.Specialty);

            modelBuilder.Entity<Appointment>().HasIndex(a => a.UserId);
            modelBuilder.Entity<Appointment>().HasIndex(a => a.DoctorId);
            modelBuilder.Entity<Appointment>().HasIndex(a => a.AppointmentDate);

            modelBuilder.Entity<MedicalTourismRequest>().HasIndex(r => r.UserId);
            modelBuilder.Entity<MedicalTourismRequest>().HasIndex(r => r.Status);

            modelBuilder.Entity<ChatMessage>().HasIndex(m => m.SenderId);
            modelBuilder.Entity<ChatMessage>().HasIndex(m => m.ReceiverId);
            modelBuilder.Entity<ChatMessage>().HasIndex(m => m.Timestamp);

            modelBuilder.Entity<Notification>().HasIndex(n => n.UserId);
            modelBuilder.Entity<Notification>().HasIndex(n => n.IsRead);

            modelBuilder.Entity<PharmacyOrder>().HasIndex(o => o.PatientId);
            modelBuilder.Entity<LabBooking>().HasIndex(b => b.PatientId);

            // --- FamilyLinks Mappings ---
            modelBuilder.Entity<FamilyLink>().ToTable("FamilyLinks")
                .HasOne(f => f.Requester).WithMany().HasForeignKey(f => f.RequesterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FamilyLink>()
                .HasOne(f => f.MemberUser).WithMany().HasForeignKey(f => f.MemberId).OnDelete(DeleteBehavior.Restrict);

            // --- Transparent AES-256 Encryption ---
            var encryptionConverter = new ValueConverter<string, string>(
                v => _encryptionService.Encrypt(v),
                v => _encryptionService.Decrypt(v));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string) &&
                        property.PropertyInfo != null &&
                        property.PropertyInfo.GetCustomAttribute<EncryptedAttribute>() != null)
                    {
                        property.SetValueConverter(encryptionConverter);
                    }
                }
            }
        }
    }
}