using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Identity.Pages.Account;

namespace MedLinkPortal.Services
{
    public class DatabaseSeederService
    {
        public static async Task SeedAllAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeederService>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            logger.LogInformation("=== STARTING COMPREHENSIVE SEEDING ===");

            // 1. Ensure Roles
            string[] roles = { "Admin", "Doctor", "Patient", "Pharmacist", "LabAdmin", "Rider" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Created role: {Role}", role);
                }
            }

            // 2. Seed Lab and Medical Tests
            await SeedLaboratoryAndTestsAsync(dbContext, userManager, roleManager, logger);

            // 3. Seed Pharmacy Medicines
            await SeedPharmacyMedicinesAsync(dbContext, logger);

            // 4. Seed Doctors across all 25 categories (3 doctors each = 75 doctors)
            await SeedDoctorsAsync(dbContext, userManager, logger);

            logger.LogInformation("=== COMPREHENSIVE SEEDING FINISHED SUCCESSFULLY ===");

            await VerifySeedingAsync(dbContext, userManager, logger);
        }

        public static async Task VerifySeedingAsync(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ILogger logger)
        {
            logger.LogInformation("=== VERIFYING SEEDED DATA ===");

            // 1. Doctors verification
            var doctorsInRole = await userManager.GetUsersInRoleAsync("Doctor");
            var doctorEntities = await dbContext.Doctors.ToListAsync();
            logger.LogInformation("Total Doctors in Role: {RoleCount}, Total Doctors in Table: {TableCount}", doctorsInRole.Count, doctorEntities.Count);

            int fullyCompleteCount = 0;
            foreach (var user in doctorsInRole)
            {
                int totalFields = 18;
                int filledFields = 0;
                if (!string.IsNullOrEmpty(user.Name)) filledFields++;
                if (!string.IsNullOrEmpty(user.FatherHusbandName)) filledFields++;
                if (!string.IsNullOrEmpty(user.Gender)) filledFields++;
                if (!string.IsNullOrEmpty(user.CNIC)) filledFields++;
                if (user.ConsultationFee > 0) filledFields++;
                if (!string.IsNullOrEmpty(user.PhoneNumber)) filledFields++;
                if (!string.IsNullOrEmpty(user.Email)) filledFields++;
                if (!string.IsNullOrEmpty(user.ResidentialAddress)) filledFields++;
                if (!string.IsNullOrEmpty(user.City)) filledFields++;
                if (!string.IsNullOrEmpty(user.Province)) filledFields++;
                if (!string.IsNullOrEmpty(user.PMDCRegistrationNumber)) filledFields++;
                if (user.PMDCValidityDate.HasValue) filledFields++;
                if (!string.IsNullOrEmpty(user.Specialist)) filledFields++;
                if (!string.IsNullOrEmpty(user.Qualification)) filledFields++;
                if (!string.IsNullOrEmpty(user.Experience)) filledFields++;
                if (!string.IsNullOrEmpty(user.Workplace)) filledFields++;
                if (!string.IsNullOrEmpty(user.BankAccountNumber)) filledFields++;
                if (!string.IsNullOrEmpty(user.CNICFrontUrl) && !string.IsNullOrEmpty(user.CNICBackUrl) && !string.IsNullOrEmpty(user.PMDCCertificateUrl)) filledFields++;

                int completion = (int)((double)filledFields / totalFields * 100);
                if (completion >= 100) fullyCompleteCount++;
            }
            logger.LogInformation("Doctors with 100% complete profile: {CompleteCount} / {TotalCount}", fullyCompleteCount, doctorsInRole.Count);

            // 2. Pharmacy Medicines verification
            var medCategories = await dbContext.Medicines
                .GroupBy(m => m.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();
            logger.LogInformation("--- Pharmacy Medicines by Category ---");
            foreach (var mc in medCategories)
            {
                logger.LogInformation("  Category '{Category}': {Count} medicines", mc.Category, mc.Count);
            }
            logger.LogInformation("Total Medicines: {TotalMeds}", await dbContext.Medicines.CountAsync());

            // 3. Lab and Tests verification
            var labs = await dbContext.Laboratories.Include(l => l.City).ToListAsync();
            logger.LogInformation("Total Laboratories: {LabCount}", labs.Count);
            foreach (var l in labs)
            {
                var testCount = await dbContext.MedicalTests.CountAsync(t => t.LaboratoryId == l.Id);
                logger.LogInformation("  Lab '{LabName}' ({City}): {TestCount} tests configured", l.Name, l.City?.Name, testCount);
            }
            logger.LogInformation("=== VERIFICATION COMPLETE ===");
        }

        private static async Task SeedLaboratoryAndTestsAsync(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger logger)
        {
            logger.LogInformation("--- Seeding Laboratory & Medical Tests ---");

            // 1. City
            var city = await dbContext.Cities.FirstOrDefaultAsync(c => c.Name == "Lahore");
            if (city == null)
            {
                city = new City { Name = "Lahore" };
                dbContext.Cities.Add(city);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Created default city: Lahore");
            }

            var karachi = await dbContext.Cities.FirstOrDefaultAsync(c => c.Name == "Karachi");
            if (karachi == null)
            {
                karachi = new City { Name = "Karachi" };
                dbContext.Cities.Add(karachi);
                await dbContext.SaveChangesAsync();
            }

            var islamabad = await dbContext.Cities.FirstOrDefaultAsync(c => c.Name == "Islamabad");
            if (islamabad == null)
            {
                islamabad = new City { Name = "Islamabad" };
                dbContext.Cities.Add(islamabad);
                await dbContext.SaveChangesAsync();
            }

            // 2. Laboratory
            var lab = await dbContext.Laboratories.FirstOrDefaultAsync();
            if (lab == null)
            {
                lab = new Laboratory
                {
                    Name = "Chughtai Central Diagnostic Lab",
                    Address = "Main Boulevard, Gulberg III, Lahore",
                    PhoneNumber = "+9242111255577",
                    CityId = city.Id,
                    Rating = 4.9,
                    HomeCollectionAvailable = true,
                    OpenTime = "08:00 AM",
                    CloseTime = "10:00 PM",
                    LogoUrl = "/uploads/lab-logos/chughtai_lab.png"
                };
                dbContext.Laboratories.Add(lab);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Created default Laboratory: {LabName}", lab.Name);
            }

            // Ensure LabAdmin User is linked to this laboratory
            var labAdmin = await userManager.FindByEmailAsync("labadmin@medlink.com");
            if (labAdmin != null && labAdmin.LaboratoryId != lab.Id)
            {
                labAdmin.LaboratoryId = lab.Id;
                labAdmin.ApprovalStatus = "Approved";
                await userManager.UpdateAsync(labAdmin);
                logger.LogInformation("Linked labadmin@medlink.com to Laboratory ID {LabId}", lab.Id);
            }

            // 3. Test Categories
            var categories = new[]
            {
                "Hematology",
                "Biochemistry",
                "Immunology & Hormones",
                "Radiology & Imaging",
                "Routine & Pathology"
            };

            var catMap = new Dictionary<string, MedicalTestCategory>();
            foreach (var catName in categories)
            {
                var cat = await dbContext.MedicalTestCategories.FirstOrDefaultAsync(c => c.Name == catName);
                if (cat == null)
                {
                    cat = new MedicalTestCategory { Name = catName };
                    dbContext.MedicalTestCategories.Add(cat);
                    await dbContext.SaveChangesAsync();
                }
                catMap[catName] = cat;
            }

            // 4. Medical Tests definitions
            var standardTests = new List<(string Name, decimal Price, string ReportTime, string SampleType, string Category)>
            {
                // Hematology
                ("Complete Blood Count (CBC) with ESR", 850m, "12 Hours", "Blood", "Hematology"),
                ("Blood Grouping & Rh Factor", 450m, "6 Hours", "Blood", "Hematology"),
                ("Peripheral Blood Film (PBF)", 950m, "24 Hours", "Blood", "Hematology"),
                ("Platelet Count", 400m, "6 Hours", "Blood", "Hematology"),

                // Biochemistry
                ("Lipid Profile (Cholesterol, HDL, LDL, Triglycerides)", 2200m, "24 Hours", "Blood", "Biochemistry"),
                ("Liver Function Test (LFT) - Complete", 1800m, "24 Hours", "Blood", "Biochemistry"),
                ("Renal / Kidney Function Test (RFT / Urea / Creatinine)", 1600m, "24 Hours", "Blood", "Biochemistry"),
                ("HbA1c (Glycated Hemoglobin)", 1500m, "12 Hours", "Blood", "Biochemistry"),
                ("Fasting Blood Sugar (Glucose)", 350m, "4 Hours", "Blood", "Biochemistry"),
                ("Serum Uric Acid", 600m, "12 Hours", "Blood", "Biochemistry"),
                ("Serum Calcium & Vitamin D3", 3500m, "24 Hours", "Blood", "Biochemistry"),

                // Immunology & Hormones
                ("Thyroid Profile (T3, T4, TSH)", 3200m, "24 Hours", "Blood", "Immunology & Hormones"),
                ("Hepatitis B Surface Antigen (HBsAg)", 1200m, "12 Hours", "Blood", "Immunology & Hormones"),
                ("Hepatitis C Antibody (Anti-HCV)", 1400m, "12 Hours", "Blood", "Immunology & Hormones"),
                ("Dengue Serology (NS1, IgG, IgM)", 1900m, "6 Hours", "Blood", "Immunology & Hormones"),
                ("Serum Ferritin", 1600m, "24 Hours", "Blood", "Immunology & Hormones"),

                // Radiology & Imaging
                ("Chest X-Ray (PA View)", 1500m, "2 Hours", "N/A", "Radiology & Imaging"),
                ("Abdominal & Pelvic Ultrasound", 2500m, "Same Day", "N/A", "Radiology & Imaging"),
                ("Electrocardiogram (ECG 12-Lead)", 1000m, "1 Hour", "N/A", "Radiology & Imaging"),

                // Routine & Pathology
                ("Urine Complete Examination (R/E)", 500m, "6 Hours", "Urine", "Routine & Pathology"),
                ("Stool Routine Examination (R/E)", 600m, "6 Hours", "Stool", "Routine & Pathology"),
                ("Sputum for AFB", 900m, "24 Hours", "Sputum", "Routine & Pathology")
            };

            foreach (var t in standardTests)
            {
                var exists = await dbContext.MedicalTests.AnyAsync(mt => mt.Name == t.Name && mt.LaboratoryId == lab.Id);
                if (!exists)
                {
                    var catId = catMap[t.Category].Id;
                    dbContext.MedicalTests.Add(new MedicalTest
                    {
                        Name = t.Name,
                        Price = t.Price,
                        ReportTime = t.ReportTime,
                        SampleType = t.SampleType,
                        LaboratoryId = lab.Id,
                        CategoryId = catId
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded medical tests for Laboratory ID: {LabId}", lab.Id);
        }

        private static async Task SeedPharmacyMedicinesAsync(ApplicationDbContext dbContext, ILogger logger)
        {
            logger.LogInformation("--- Seeding Pharmacy Medicines ---");

            var medicineImages = new[]
            {
                "/images/medicines/0eb3fa10-c8e1-46bd-8cc2-8de2e3828bd7.jpg",
                "/images/medicines/12c136c6-5237-4a37-8030-12d3edca66cb.webp",
                "/images/medicines/1c707385-f8f0-4495-9873-18c44623b765.jpg",
                "/images/medicines/2c9a9559-3135-47ce-abee-4d38e3874312.webp",
                "/images/medicines/64e95032-1eff-4de0-8156-09ac8b268895.jpg",
                "/images/medicines/66bb88ff-6b52-4b82-9eec-0eb8eed72367.webp",
                "/images/medicines/781d51f2-e774-4fc7-8109-fde91f38a118.jpg",
                "/images/medicines/7ea68dd4-0c75-4d1b-8472-5e2f07ae5e4e.JPG",
                "/images/medicines/83c1dd9f-1aae-4553-801d-10058ae17e27.webp",
                "/images/medicines/9ece08fe-749d-4ff1-be08-4e76761acf2b.jpg",
                "/images/medicines/a72bd82e-c77b-49a3-b564-38d53e9af165.webp",
                "/images/medicines/a7f5789b-534e-4912-8147-1249eaa14ae0.webp",
                "/images/medicines/a805e631-9e0f-4212-acab-89ca1551e80a.jpg",
                "/images/medicines/a8a239f4-47c9-46aa-94e5-05311c1af1e0.jpg",
                "/images/medicines/abf3ffe5-0039-407e-a65a-9f891e981c2a.jpg",
                "/images/medicines/b608b8eb-0800-4a30-8fc8-970d0558ca8a.webp",
                "/images/medicines/d15770b6-0038-4a56-a95b-bed36769bf44.webp",
                "/images/medicines/f1470f4c-d0eb-45e1-8ca9-5e4769f0101d.png"
            };

            var medicinesData = new List<(string Name, string Brand, string Category, decimal Price, int Stock, bool Rx, string Desc)>
            {
                // Fever
                ("Panadol 500mg (Paracetamol)", "GSK", "Fever", 65m, 1200, false, "Fast and effective relief of fever, headache, body aches and pains."),
                ("Calpol 250mg Suspension", "GSK", "Fever", 145m, 400, false, "Gentle pediatric fever and mild to moderate pain relief syrup."),
                ("Disprin 300mg Soluble Tablets", "Reckitt", "Fever", 50m, 800, false, "Soluble aspirin tablets for rapid relief of fever and pain."),
                ("Paracetamol Extra 500mg/65mg", "Getz Pharma", "Fever", 90m, 650, false, "Extra strength paracetamol with caffeine for fever and migraine relief."),

                // Pain
                ("Brufen 400mg (Ibuprofen)", "Abbott", "Pain", 120m, 950, false, "Anti-inflammatory pain reliever for muscular pain, dental pain, and arthritis."),
                ("Ponstan Forte 500mg", "Pfizer", "Pain", 180m, 600, false, "Mefenamic acid for severe body pain, period cramps, and post-surgery aches."),
                ("Nuberol Forte (Paracetamol + Orphenadrine)", "Searle", "Pain", 220m, 750, false, "Muscle relaxant and analgesic for spasms, back pain, and muscle tension."),
                ("Voltral Emulgel 1% 50g", "Novartis", "Pain", 310m, 350, false, "Topical diclofenac gel for localized joint and muscular inflammation."),

                // Vitality
                ("Surbex Z (Zinc + B-Complex + Vitamin C)", "Abbott", "Vitality", 420m, 500, false, "Comprehensive daily multivitamin with zinc for energy, immunity and stamina."),
                ("Cac 1000 Plus Effervescent Tablets", "GSK", "Vitality", 380m, 450, false, "Effervescent calcium, vitamin C and D3 drink for bone health and vitality."),
                ("Neurobion Tablets (Vitamin B1, B6, B12)", "Merck", "Vitality", 260m, 800, false, "Neurotropic vitamins supporting healthy nerve function and combating fatigue."),
                ("Centrum Silver 50+ Multivitamin", "Pfizer", "Vitality", 1850m, 150, false, "Complete multivitamin and mineral supplement tailored for mature adults."),

                // Cardiac
                ("Cardinit 2.6mg Controlled Release", "Searle", "Cardiac", 280m, 400, true, "Glyceryl trinitrate for management and prevention of angina pectoris."),
                ("Lowplat 75mg (Clopidogrel)", "Getz Pharma", "Cardiac", 340m, 600, true, "Antiplatelet medication preventing clots in patients with heart disease."),
                ("Lopresor 50mg (Metoprolol)", "Novartis", "Cardiac", 210m, 350, true, "Beta-blocker for hypertension, angina, and heart rhythm disorders."),
                ("Concor 5mg (Bisoprolol)", "Merck", "Cardiac", 480m, 550, true, "Cardio-selective beta-blocker for blood pressure and chronic heart failure."),

                // Gastric
                ("Risek 40mg (Omeprazole)", "Getz Pharma", "Gastric", 420m, 800, false, "Proton pump inhibitor treating GERD, acidity, heartburn, and peptic ulcers."),
                ("Gaviscon Double Action Syrup", "Reckitt", "Gastric", 290m, 600, false, "Fast-acting antacid forming a protective barrier against acid reflux."),
                ("Nexum 40mg (Esomeprazole)", "Getz Pharma", "Gastric", 510m, 500, false, "Advanced gastric acid suppression for reflux esophagitis and stomach ulcers."),
                ("Flagyl 400mg (Metronidazole)", "Sanofi", "Gastric", 95m, 1100, true, "Antiprotozoal and antibacterial medication for digestive infections and amebiasis."),

                // Antibiotics
                ("Augmentin 625mg (Amoxicillin + Clavulanic Acid)", "GSK", "Antibiotics", 560m, 700, true, "Broad-spectrum antibiotic for respiratory, ENT, and soft tissue infections."),
                ("Ciproxin 500mg (Ciprofloxacin)", "Bayer", "Antibiotics", 480m, 400, true, "Fluoroquinolone antibiotic for urinary tract, bacterial, and skin infections."),
                ("Azomax 500mg (Azithromycin)", "Getz Pharma", "Antibiotics", 390m, 650, true, "Macrolide antibiotic treating chest, throat, ear, and sinus bacterial infections."),

                // Respiratory
                ("Ventolin Inhaler 100mcg (Salbutamol)", "GSK", "Respiratory", 350m, 400, true, "Fast-acting bronchodilator for rapid relief of asthma and bronchospasm."),
                ("Pulmonol Cough Syrup", "Ccl Pharma", "Respiratory", 180m, 600, false, "Expectorant cough formula easing chest congestion and persistent cough."),
                ("Montika 10mg (Montelukast)", "Sami", "Respiratory", 420m, 550, true, "Leukotriene receptor antagonist for chronic asthma and allergic rhinitis."),

                // Skin Care
                ("Betnovate-N Cream 20g", "GSK", "Skin Care", 140m, 500, true, "Topical corticosteroid with neomycin for eczema, dermatitis, and skin allergies."),
                ("Hydrozole Cream 20g", "Stiefel", "Skin Care", 220m, 350, false, "Antifungal and anti-inflammatory cream for fungal infections accompanied by itching."),
                ("Sunblock SPF 60 Gel 50ml", "Dermacos", "Skin Care", 850m, 200, false, "Broad spectrum UVA/UVB protection against sunburn, photo-aging, and skin damage."),

                // Diabetes
                ("Glucophage 500mg (Metformin)", "Merck", "Diabetes", 160m, 900, true, "First-line oral antidiabetic therapy improving glycemic control in type 2 diabetes."),
                ("Getryl 2mg (Glimepiride)", "Getz Pharma", "Diabetes", 290m, 600, true, "Sulfonylurea stimulating insulin secretion for type 2 diabetes management."),
                ("Januvia 100mg (Sitagliptin)", "MSD", "Diabetes", 2400m, 250, true, "DPP-4 inhibitor regulating blood sugar levels after meals in diabetic patients.")
            };

            int imgIdx = 0;
            foreach (var m in medicinesData)
            {
                var existing = await dbContext.Medicines.FirstOrDefaultAsync(x => x.Name == m.Name);
                if (existing == null)
                {
                    var img = medicineImages[imgIdx % medicineImages.Length];
                    imgIdx++;

                    dbContext.Medicines.Add(new Medicine
                    {
                        Name = m.Name,
                        Brand = m.Brand,
                        Category = m.Category,
                        Price = m.Price,
                        StockQuantity = m.Stock,
                        PrescriptionRequired = m.Rx,
                        Description = m.Desc,
                        ExpiryDate = DateTime.UtcNow.AddMonths(18),
                        IsActive = true,
                        ImageUrl = img,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded medicines across all categories.");
        }

        private static async Task SeedDoctorsAsync(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ILogger logger)
        {
            logger.LogInformation("--- Seeding 75 Doctors Across 25 Categories ---");

            var categories = new[]
            {
                ("General Physician", "genphys"),
                ("Cardiologist", "cardio"),
                ("Dermatologist", "derma"),
                ("Pediatrician", "pedia"),
                ("Gynecologist", "gyne"),
                ("Orthopedic Surgeon", "ortho"),
                ("Neurologist", "neuro"),
                ("Psychiatrist", "psych"),
                ("Otolaryngologist (ENT Specialist)", "ent"),
                ("Ophthalmologist", "eye"),
                ("Urologist", "uro"),
                ("Gastroenterologist", "gastro"),
                ("Pulmonologist", "pulmo"),
                ("Endocrinologist", "endo"),
                ("Oncologist", "onco"),
                ("Radiologist", "radio"),
                ("Anesthesiologist", "anest"),
                ("General Surgeon", "gensurg"),
                ("Dentist", "dentist"),
                ("Physiotherapist", "physio"),
                ("Nutritionist/Dietitian", "nutri"),
                ("Pathologist", "patho"),
                ("Nephrologist", "nephro"),
                ("Hematologist", "hemato"),
                ("Rheumatologist", "rheuma")
            };

            var profileImages = new[]
            {
                "/uploads/profiles/18f4a104-7218-476d-9a62-feccc7f389f1.jpg",
                "/uploads/profiles/66ff2b94-1487-4811-82bc-82ed2642aee8.jpg",
                "/uploads/profiles/7de28c39-64ff-4c85-a86f-dd3e6f997fba.jpg",
                "/uploads/profiles/9e28c604-bf27-4470-882f-f51d46e4b395.jpg",
                "/uploads/profiles/f28a2ef2-8c08-47f1-bcf9-0d30a6241d1f.jpg",
                "/uploads/profiles/6c2d9797-6c61-419b-b19f-6b753f530193_IMG_1013.jpg"
            };

            var cnicFrontDoc = "/uploads/documents/062ebf25-bfe2-4fb3-a121-344893c9a5d0_ignacio-aguilar-PK32VXa8LGM-unsplash.jpg";
            var cnicBackDoc = "/uploads/documents/4cbf4878-a077-4c28-aede-65fd934a2003_matthew-brodeur-DH_u2aV3nGM-unsplash.jpg";
            var pmdcDoc = "/uploads/documents/d707db82-6437-4a03-8233-29164c01f8a0_blake-verdoorn-cssvEZacHvQ-unsplash.jpg";
            var degreeDoc = "/uploads/documents/a4ddcb3b-963e-4b4c-ba45-548b0bdebba7_schiba-yqk_I67IBbo-unsplash.jpg";

            var maleFirstNames = new[] { "Ahmed", "Ali", "Usman", "Hamza", "Bilal", "Zubair", "Fahad", "Tariq", "Imran", "Kamran", "Salman", "Adnan", "Omer", "Asad", "Waleed" };
            var femaleFirstNames = new[] { "Ayesha", "Fatima", "Zainab", "Maryam", "Sana", "Hina", "Sadia", "Rabia", "Farah", "Nida", "Mahnoor", "Sidra", "Amna", "Bushra", "Zara" };
            var lastNames = new[] { "Khan", "Malik", "Sheikh", "Qureshi", "Chaudhry", "Raza", "Siddiqui", "Abbasi", "Bhatti", "Mirza", "Shah", "Hashmi", "Gill", "Rehman", "Akhtar" };
            var cities = new[] { "Lahore", "Karachi", "Islamabad", "Rawalpindi", "Faisalabad" };
            var provinces = new[] { "Punjab", "Sindh", "Federal Capital", "Punjab", "Punjab" };
            var hospitals = new[] { "Aga Khan Hospital", "Shaukat Khanum Hospital", "National Hospital", "Doctors Hospital", "Chughtai Medical Center", "MedLink Specialist Clinic" };

            int doctorIndex = 0;
            const string doctorPassword = "Doctor@123";

            foreach (var (specialty, slug) in categories)
            {
                for (int i = 1; i <= 3; i++)
                {
                    doctorIndex++;
                    var email = $"dr.{slug}{i}@medlink.com";
                    bool isFemale = (doctorIndex % 3 == 2); // Rotate genders
                    var firstName = isFemale 
                        ? femaleFirstNames[(doctorIndex) % femaleFirstNames.Length] 
                        : maleFirstNames[(doctorIndex) % maleFirstNames.Length];
                    var lastName = lastNames[(doctorIndex * 7) % lastNames.Length];
                    var fullName = $"Dr. {firstName} {lastName}";
                    var fatherHusband = isFemale ? $"Tariq {lastName}" : $"Mohammad {lastName}";
                    var gender = isFemale ? "Female" : "Male";

                    int cityIdx = doctorIndex % cities.Length;
                    var city = cities[cityIdx];
                    var province = provinces[cityIdx];
                    var hospital = hospitals[doctorIndex % hospitals.Length];
                    var profileImg = profileImages[doctorIndex % profileImages.Length];

                    var cnicNumber = $"35201-{(1000000 + doctorIndex * 1337):D7}-{(isFemale ? 2 : 1)}";
                    var pmdcNumber = $"PMDC-{(40000 + doctorIndex):D5}-P";
                    var phone = $"+92300{(5000000 + doctorIndex):D7}";
                    var expYears = 5 + (doctorIndex % 15);
                    decimal fee = 1500m + ((doctorIndex % 6) * 500m); // 1500 to 4000 PKR

                    // Check if User already exists
                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = true,
                            FirstName = firstName,
                            LastName = lastName,
                            Name = fullName,
                            FatherHusbandName = fatherHusband,
                            Gender = gender,
                            CNIC = cnicNumber,
                            PhoneNumber = phone,
                            ConsultationFee = fee,
                            ResidentialAddress = $"House #{10 + (doctorIndex % 90)}, Block {(char)('A' + (doctorIndex % 6))}, Phase {(doctorIndex % 8) + 1}, DHA",
                            City = city,
                            Province = province,
                            PMDCRegistrationNumber = pmdcNumber,
                            PMDCValidityDate = DateTime.UtcNow.AddYears(3).Date,
                            Specialist = specialty,
                            Qualification = $"MBBS, FCPS ({specialty})",
                            Experience = $"{expYears} Years",
                            Workplace = hospital,
                            BankAccountNumber = $"PK76HABB000{(100000000000 + doctorIndex):D12}",
                            CNICFrontUrl = cnicFrontDoc,
                            CNICBackUrl = cnicBackDoc,
                            PMDCCertificateUrl = pmdcDoc,
                            DegreeCertificateUrl = degreeDoc,
                            ProfilePictureUrl = profileImg,
                            ProfileImage = profileImg,
                            ApprovalStatus = "Pending", // Ready for admin approval
                            TermsConsent = true,
                            IsAvailable = true
                        };

                        var result = await userManager.CreateAsync(user, doctorPassword);
                        if (!result.Succeeded)
                        {
                            logger.LogError("Failed to create doctor user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                            continue;
                        }

                        await userManager.AddToRoleAsync(user, "Doctor");
                    }
                    else
                    {
                        // Ensure all 18 fields are populated if existing
                        user.Name = fullName;
                        user.FatherHusbandName = fatherHusband;
                        user.Gender = gender;
                        user.CNIC = cnicNumber;
                        user.ConsultationFee = fee;
                        user.PhoneNumber = phone;
                        user.ResidentialAddress = $"House #{10 + (doctorIndex % 90)}, Block {(char)('A' + (doctorIndex % 6))}, Phase {(doctorIndex % 8) + 1}, DHA";
                        user.City = city;
                        user.Province = province;
                        user.PMDCRegistrationNumber = pmdcNumber;
                        user.PMDCValidityDate = DateTime.UtcNow.AddYears(3).Date;
                        user.Specialist = specialty;
                        user.Qualification = $"MBBS, FCPS ({specialty})";
                        user.Experience = $"{expYears} Years";
                        user.Workplace = hospital;
                        user.BankAccountNumber = $"PK76HABB000{(100000000000 + doctorIndex):D12}";
                        user.CNICFrontUrl = cnicFrontDoc;
                        user.CNICBackUrl = cnicBackDoc;
                        user.PMDCCertificateUrl = pmdcDoc;
                        user.DegreeCertificateUrl = degreeDoc;
                        user.ProfilePictureUrl = profileImg;
                        user.ProfileImage = profileImg;
                        user.ApprovalStatus = "Pending";
                        user.TermsConsent = true;
                        user.IsAvailable = true;

                        await userManager.UpdateAsync(user);
                    }

                    // Check and create/update Doctor core entity
                    var docEntity = await dbContext.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (docEntity == null)
                    {
                        docEntity = new Doctor
                        {
                            UserId = user.Id,
                            Name = fullName,
                            Specialty = specialty,
                            Rating = 4.8 + (doctorIndex % 3) * 0.1,
                            Reviews = 15 + (doctorIndex * 3),
                            Image = profileImg,
                            Availability = "Available Today",
                            Online = true,
                            Description = $"{fullName} is a highly experienced {specialty} with over {expYears} years of clinical excellence, serving at {hospital}.",
                            Experience = expYears.ToString(),
                            Languages = "English, Urdu",
                            Qualification = $"MBBS, FCPS ({specialty})",
                            Expertise = $"{specialty} Consultations, Clinical Diagnosis, Treatment Protocols",
                            HospitalAffiliations = hospital,
                            ClinicAddress = $"{hospital}, {city}",
                            ClinicName = $"{hospital} OPD Wing",
                            PmdcRegistrationNumber = pmdcNumber,
                            SlotDuration = 20,
                            BufferTime = 5
                        };
                        dbContext.Doctors.Add(docEntity);
                    }
                    else
                    {
                        docEntity.Name = fullName;
                        docEntity.Specialty = specialty;
                        docEntity.Image = profileImg;
                        docEntity.ClinicAddress = $"{hospital}, {city}";
                        docEntity.Experience = expYears.ToString();
                        docEntity.PmdcRegistrationNumber = pmdcNumber;
                        docEntity.Qualification = $"MBBS, FCPS ({specialty})";
                    }
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Successfully seeded all 75 doctors!");
        }
    }
}
