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
            logger.LogInformation("--- Seeding Pharmacy Medicines (500 medicines, 20+ categories) ---");

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

            // (Name, Brand, Category, Price, Stock, PrescriptionRequired, Description)
            var medicinesData = new List<(string Name, string Brand, string Category, decimal Price, int Stock, bool Rx, string Desc)>
            {
                // ══════════════════════════════════════════
                // FEVER (20)
                // ══════════════════════════════════════════
                ("Panadol 500mg Tablets", "GSK", "Fever", 65m, 1200, false, "Fast relief of fever, headache and body aches."),
                ("Calpol 250mg Suspension 60ml", "GSK", "Fever", 145m, 400, false, "Paediatric paracetamol suspension for fever and mild pain."),
                ("Disprin 300mg Soluble Tablets", "Reckitt", "Fever", 50m, 800, false, "Aspirin soluble tablets for rapid fever and pain relief."),
                ("Paracetamol Extra 500mg/65mg", "Getz Pharma", "Fever", 90m, 650, false, "Extra strength paracetamol with caffeine for fever and migraine."),
                ("Panadol CF Day/Night Tablets", "GSK", "Fever", 120m, 550, false, "Day & night formula for cold, flu fever and congestion relief."),
                ("Febrilix 120mg/5ml Syrup 90ml", "Sami", "Fever", 110m, 350, false, "Ibuprofen suspension for paediatric fever management."),
                ("Tylenol 650mg Extended Release", "J&J", "Fever", 180m, 300, false, "Extended release paracetamol providing 8-hour fever control."),
                ("Meftal-P Drops 15ml", "Blue Cross", "Fever", 95m, 280, false, "Mefenamic acid drops for infants and toddlers with fever."),
                ("Ibuprofen 200mg Tablets", "Getz Pharma", "Fever", 70m, 900, false, "OTC ibuprofen for mild to moderate fever and pain."),
                ("Panadol Extend 665mg", "GSK", "Fever", 160m, 420, false, "Prolonged release formula for sustained fever control."),
                ("Brufen Syrup 100mg/5ml", "Abbott", "Fever", 165m, 310, false, "Ibuprofen syrup for children with fever above 38°C."),
                ("Aspirin 75mg Dispersible", "Bayer", "Fever", 45m, 700, false, "Low-dose aspirin dispersible tablets for fever and pain."),
                ("Nurofen 400mg Tablets", "Reckitt", "Fever", 130m, 500, false, "Ibuprofen for effective fever, headache and toothache relief."),
                ("Panadol Baby 80mg Suppositories", "GSK", "Fever", 200m, 180, false, "Rectal paracetamol suppositories for infants unable to swallow."),
                ("Naproxen 250mg Tablets", "Sanofi", "Fever", 150m, 400, true, "NSAID for fever, pain and inflammatory conditions."),
                ("Combiflam (Ibuprofen+Paracetamol)", "Sanofi", "Fever", 95m, 600, false, "Dual-action fever and pain tablet combination."),
                ("Panadol Fizzy 500mg Effervescent", "GSK", "Fever", 140m, 350, false, "Effervescent paracetamol for fast fever dissolution."),
                ("Metacin 500mg Tablets", "Smith Kline", "Fever", 60m, 750, false, "Paracetamol tablets for effective fever and pain management."),
                ("Ibugesic Plus Syrup 100ml", "Cipla", "Fever", 175m, 290, false, "Ibuprofen and paracetamol combination syrup for children."),
                ("Coldrex HotRem Sachets", "GSK", "Fever", 85m, 420, false, "Hot lemon drink for relief of fever, aches and cold symptoms."),

                // ══════════════════════════════════════════
                // PAIN (25)
                // ══════════════════════════════════════════
                ("Brufen 400mg (Ibuprofen)", "Abbott", "Pain", 120m, 950, false, "Anti-inflammatory for muscular, dental and arthritic pain."),
                ("Ponstan Forte 500mg", "Pfizer", "Pain", 180m, 600, false, "Mefenamic acid for period cramps and post-surgical pain."),
                ("Nuberol Forte", "Searle", "Pain", 220m, 750, false, "Paracetamol + orphenadrine muscle relaxant for back pain."),
                ("Voltral Emulgel 1% 50g", "Novartis", "Pain", 310m, 350, false, "Topical diclofenac gel for localised joint inflammation."),
                ("Celebrex 200mg (Celecoxib)", "Pfizer", "Pain", 980m, 250, true, "COX-2 inhibitor for osteoarthritis and rheumatoid arthritis pain."),
                ("Naproxen Sodium 550mg", "Roche", "Pain", 210m, 400, false, "Long-acting NSAID for musculoskeletal and dental pain."),
                ("Ketorol 10mg (Ketorolac)", "Sun Pharma", "Pain", 150m, 500, true, "Short-term moderate to severe post-operative pain relief."),
                ("Tramol 50mg (Tramadol)", "Sami", "Pain", 280m, 300, true, "Opioid analgesic for moderate to severe acute pain."),
                ("Feldene 20mg (Piroxicam)", "Pfizer", "Pain", 190m, 350, true, "Long-acting NSAID for arthritis and musculoskeletal pain."),
                ("Dicloran 50mg Tablets", "Getz Pharma", "Pain", 110m, 600, false, "Diclofenac sodium for pain, swelling and inflammation."),
                ("Voltaren 75mg Injection", "Novartis", "Pain", 380m, 200, true, "Diclofenac sodium injection for acute severe pain."),
                ("Lyrica 75mg (Pregabalin)", "Pfizer", "Pain", 1850m, 180, true, "Neuropathic pain management and fibromyalgia treatment."),
                ("Neurontin 300mg (Gabapentin)", "Pfizer", "Pain", 1200m, 220, true, "Anticonvulsant for neuropathic pain and nerve damage."),
                ("Muscle Ease Gel 100g", "Reckitt", "Pain", 350m, 280, false, "Cooling menthol gel for muscle aches, sprains and strains."),
                ("Iodex Ultra Gel 30g", "GSK", "Pain", 220m, 320, false, "Topical pain relief gel with methyl salicylate."),
                ("Skelan 8mg (Eperisone)", "Searle", "Pain", 480m, 200, true, "Muscle relaxant for spasticity and lower back pain."),
                ("Brufen Retard 800mg", "Abbott", "Pain", 260m, 300, false, "Slow-release ibuprofen for chronic pain management."),
                ("Painstop Suspension 100ml", "Aspin", "Pain", 190m, 350, false, "Combination analgesic suspension for paediatric pain."),
                ("Codeine Phosphate 30mg", "GSK", "Pain", 320m, 180, true, "Opioid analgesic for moderate pain, combined with paracetamol."),
                ("Piroxicam Gel 0.5% 60g", "Getz Pharma", "Pain", 175m, 280, false, "Topical piroxicam gel for joint and soft tissue pain."),
                ("Flurbiprofen 50mg Tabs", "Knoll", "Pain", 140m, 400, false, "NSAID for dental, postoperative and menstrual pain."),
                ("Indocin 25mg (Indomethacin)", "MSD", "Pain", 160m, 300, true, "Potent NSAID for gout attacks and ankylosing spondylitis."),
                ("Mobic 7.5mg (Meloxicam)", "Boehringer", "Pain", 480m, 250, true, "Selective COX-2 NSAID for osteoarthritis with less GI side effects."),
                ("Ultracet (Tramadol+Paracetamol)", "J&J", "Pain", 420m, 280, true, "Combined opioid and non-opioid tablet for moderate-severe pain."),
                ("Carisoprodol 350mg", "Sun Pharma", "Pain", 560m, 180, true, "Skeletal muscle relaxant for acute musculoskeletal pain."),

                // ══════════════════════════════════════════
                // ANTIBIOTICS (30)
                // ══════════════════════════════════════════
                ("Augmentin 625mg", "GSK", "Antibiotics", 560m, 700, true, "Amoxicillin/Clavulanic acid for respiratory and ENT infections."),
                ("Ciproxin 500mg (Ciprofloxacin)", "Bayer", "Antibiotics", 480m, 400, true, "Fluoroquinolone for UTI, GI and skin bacterial infections."),
                ("Azomax 500mg (Azithromycin)", "Getz Pharma", "Antibiotics", 390m, 650, true, "Macrolide for chest, throat and sinus infections."),
                ("Amoxil 500mg (Amoxicillin)", "GSK", "Antibiotics", 180m, 900, true, "Broad-spectrum penicillin for common bacterial infections."),
                ("Flagyl 400mg (Metronidazole)", "Sanofi", "Antibiotics", 95m, 1100, true, "Antiprotozoal and antibacterial for GI and anaerobic infections."),
                ("Doxycycline 100mg Capsules", "Pfizer", "Antibiotics", 220m, 500, true, "Tetracycline antibiotic for chest, tick-borne and acne infections."),
                ("Ceftriaxone 1g Injection", "Roche", "Antibiotics", 850m, 300, true, "3rd generation cephalosporin injection for severe infections."),
                ("Cephalexin 500mg Capsules", "Sami", "Antibiotics", 280m, 420, true, "First-gen cephalosporin for skin, UTI and respiratory infections."),
                ("Clindamycin 300mg Capsules", "Pfizer", "Antibiotics", 480m, 280, true, "Lincosamide antibiotic for dental, bone and soft tissue infections."),
                ("Erythromycin 500mg Tablets", "Abbott", "Antibiotics", 160m, 600, true, "Macrolide antibiotic alternative for penicillin-allergic patients."),
                ("Meropenem 1g Injection", "AstraZeneca", "Antibiotics", 2800m, 120, true, "Carbapenem injection for severe hospital-acquired infections."),
                ("Vancomycin 500mg Injection", "Eli Lilly", "Antibiotics", 3500m, 80, true, "Glycopeptide for MRSA and resistant gram-positive infections."),
                ("Nitrofurantoin 100mg Capsules", "MSD", "Antibiotics", 380m, 350, true, "Urinary antiseptic for uncomplicated lower UTI treatment."),
                ("Linezolid 600mg Tablets", "Pfizer", "Antibiotics", 4200m, 100, true, "Oxazolidinone for resistant gram-positive bacterial infections."),
                ("Trimethoprim 200mg Tablets", "Sami", "Antibiotics", 150m, 500, true, "Antifolate antibiotic for UTI and respiratory tract infections."),
                ("Fosfomycin 3g Granules", "Zambon", "Antibiotics", 1200m, 200, true, "Single-dose antibiotic sachet for uncomplicated cystitis."),
                ("Roxithromycin 150mg Tablets", "Sanofi", "Antibiotics", 340m, 380, true, "Macrolide for respiratory and soft tissue bacterial infections."),
                ("Cefuroxime 500mg Tablets", "GSK", "Antibiotics", 620m, 300, true, "2nd gen cephalosporin for sinusitis, bronchitis and skin infections."),
                ("Levofloxacin 500mg Tablets", "J&J", "Antibiotics", 580m, 350, true, "Fluoroquinolone for CAP, UTI and complicated skin infections."),
                ("Moxifloxacin 400mg Tablets", "Bayer", "Antibiotics", 780m, 220, true, "4th gen fluoroquinolone for community-acquired pneumonia."),
                ("Piperacillin 4g/Tazobactam 500mg", "Wyeth", "Antibiotics", 3200m, 100, true, "Combination beta-lactam injection for hospital infections."),
                ("Amikacin 500mg Injection", "Bristol", "Antibiotics", 580m, 180, true, "Aminoglycoside injection for gram-negative septicaemia."),
                ("Gentamicin 80mg Injection", "Schering", "Antibiotics", 220m, 250, true, "Aminoglycoside for serious gram-negative bacterial infections."),
                ("Chloramphenicol Eye Drops 5ml", "Martindale", "Antibiotics", 180m, 400, true, "Broad-spectrum antibiotic eye drops for bacterial conjunctivitis."),
                ("Fusidic Acid Cream 2% 15g", "Leo Pharma", "Antibiotics", 320m, 300, true, "Topical antibiotic for skin infections and infected eczema."),
                ("Neomycin + Polymyxin Drops", "Alcon", "Antibiotics", 260m, 280, true, "Combination antibiotic drops for external ear and eye infections."),
                ("Cefixime 400mg Capsules", "Sami", "Antibiotics", 420m, 320, true, "3rd gen oral cephalosporin for ENT and urinary infections."),
                ("Ampicillin 500mg Capsules", "GSK", "Antibiotics", 130m, 500, true, "Broad-spectrum penicillin for respiratory and GI infections."),
                ("Colistin 150mg Tablets", "Pharmalab", "Antibiotics", 280m, 200, true, "Polymyxin antibiotic for resistant gram-negative infections."),
                ("Rifampicin 600mg Capsules", "Sanofi", "Antibiotics", 480m, 280, true, "Rifamycin antibiotic for tuberculosis and leprosy treatment."),

                // ══════════════════════════════════════════
                // CARDIAC (25)
                // ══════════════════════════════════════════
                ("Cardinit 2.6mg CR Tablets", "Searle", "Cardiac", 280m, 400, true, "Glyceryl trinitrate for angina pectoris prevention."),
                ("Lowplat 75mg (Clopidogrel)", "Getz Pharma", "Cardiac", 340m, 600, true, "Antiplatelet agent preventing thrombotic cardiovascular events."),
                ("Lopresor 50mg (Metoprolol)", "Novartis", "Cardiac", 210m, 350, true, "Beta-blocker for hypertension, angina and arrhythmia."),
                ("Concor 5mg (Bisoprolol)", "Merck", "Cardiac", 480m, 550, true, "Cardioselective beta-blocker for heart failure and hypertension."),
                ("Zocor 20mg (Simvastatin)", "MSD", "Cardiac", 620m, 400, true, "Statin for reducing LDL cholesterol and cardiovascular risk."),
                ("Lipitor 20mg (Atorvastatin)", "Pfizer", "Cardiac", 850m, 500, true, "High-potency statin for hypercholesterolaemia and CVD prevention."),
                ("Crestor 10mg (Rosuvastatin)", "AstraZeneca", "Cardiac", 980m, 350, true, "Statin for mixed dyslipidaemia and cardiovascular risk reduction."),
                ("Norvasc 5mg (Amlodipine)", "Pfizer", "Cardiac", 360m, 600, true, "Calcium channel blocker for hypertension and stable angina."),
                ("Coversyl 5mg (Perindopril)", "Servier", "Cardiac", 520m, 450, true, "ACE inhibitor for hypertension and heart failure management."),
                ("Diovan 80mg (Valsartan)", "Novartis", "Cardiac", 720m, 350, true, "ARB for hypertension and post-myocardial infarction management."),
                ("Lasix 40mg (Furosemide)", "Sanofi", "Cardiac", 85m, 700, true, "Loop diuretic for oedema associated with heart failure."),
                ("Aldactone 25mg (Spironolactone)", "Pfizer", "Cardiac", 180m, 400, true, "Potassium-sparing diuretic for heart failure and hypertension."),
                ("Digoxin 0.25mg Tablets", "GSK", "Cardiac", 120m, 350, true, "Cardiac glycoside for heart failure and atrial fibrillation."),
                ("Amiodarone 200mg Tablets", "Sanofi", "Cardiac", 680m, 200, true, "Class III antiarrhythmic for life-threatening ventricular arrhythmias."),
                ("Warfarin 5mg Tablets", "Bristol", "Cardiac", 150m, 300, true, "Anticoagulant for DVT, PE and atrial fibrillation."),
                ("Pradaxa 150mg (Dabigatran)", "Boehringer", "Cardiac", 3600m, 120, true, "NOAC for stroke prevention in non-valvular atrial fibrillation."),
                ("Xarelto 20mg (Rivaroxaban)", "Bayer", "Cardiac", 4200m, 100, true, "Factor Xa inhibitor for DVT treatment and AF stroke prevention."),
                ("Isoptin 80mg (Verapamil)", "Abbott", "Cardiac", 280m, 300, true, "Calcium channel blocker for hypertension and supraventricular arrhythmias."),
                ("Tenormin 50mg (Atenolol)", "AstraZeneca", "Cardiac", 160m, 550, true, "Beta-blocker for hypertension, angina and myocardial infarction."),
                ("Ramipril 5mg Capsules", "Sanofi", "Cardiac", 380m, 400, true, "ACE inhibitor for hypertension, heart failure and post-MI protection."),
                ("Trental 400mg (Pentoxifylline)", "Sanofi", "Cardiac", 420m, 250, true, "Improves microcirculation in peripheral vascular disease."),
                ("Minipress 1mg (Prazosin)", "Pfizer", "Cardiac", 195m, 300, true, "Alpha-blocker for hypertension and benign prostatic hyperplasia."),
                ("Nitrocontin 2.6mg CR", "AstraZeneca", "Cardiac", 240m, 350, true, "Long-acting isosorbide mononitrate for chronic angina prevention."),
                ("Eplerenone 25mg Tablets", "Pfizer", "Cardiac", 1850m, 150, true, "Selective aldosterone blocker for heart failure post-MI."),
                ("Hydralazine 25mg Tablets", "Shire", "Cardiac", 220m, 250, true, "Vasodilator for hypertensive emergencies and heart failure."),

                // ══════════════════════════════════════════
                // DIABETES (25)
                // ══════════════════════════════════════════
                ("Glucophage 500mg (Metformin)", "Merck", "Diabetes", 160m, 900, true, "First-line biguanide for type 2 diabetes glycaemic control."),
                ("Getryl 2mg (Glimepiride)", "Getz Pharma", "Diabetes", 290m, 600, true, "Sulphonylurea stimulating pancreatic insulin secretion."),
                ("Januvia 100mg (Sitagliptin)", "MSD", "Diabetes", 2400m, 250, true, "DPP-4 inhibitor regulating post-meal blood glucose."),
                ("Jardiance 10mg (Empagliflozin)", "Boehringer", "Diabetes", 5200m, 150, true, "SGLT-2 inhibitor reducing glucose and cardiovascular risk."),
                ("Forxiga 10mg (Dapagliflozin)", "AstraZeneca", "Diabetes", 4800m, 180, true, "SGLT-2 inhibitor for type 2 diabetes and heart failure."),
                ("Ozempic 0.5mg Injection", "Novo Nordisk", "Diabetes", 18000m, 60, true, "Weekly GLP-1 agonist for type 2 diabetes and weight loss."),
                ("Victoza 1.2mg Injection", "Novo Nordisk", "Diabetes", 14500m, 80, true, "Daily liraglutide injection for blood glucose reduction."),
                ("Lantus 100IU/ml (Insulin Glargine)", "Sanofi", "Diabetes", 2800m, 200, true, "Long-acting basal insulin for type 1 and 2 diabetes."),
                ("Novorapid 100IU/ml FlexPen", "Novo Nordisk", "Diabetes", 2400m, 220, true, "Rapid-acting insulin analogue for mealtime glucose control."),
                ("Humulin 30/70 100IU/ml", "Eli Lilly", "Diabetes", 1800m, 350, true, "Biphasic human insulin for type 1 and 2 diabetes."),
                ("Glucobay 50mg (Acarbose)", "Bayer", "Diabetes", 380m, 300, true, "Alpha-glucosidase inhibitor slowing post-meal glucose rise."),
                ("Pioglitazone 30mg Tablets", "Takeda", "Diabetes", 520m, 250, true, "Thiazolidinedione improving insulin sensitivity in type 2 diabetes."),
                ("Gliclazide MR 60mg Tablets", "Servier", "Diabetes", 340m, 400, true, "Modified-release sulphonylurea with lower hypoglycaemia risk."),
                ("Repaglinide 1mg Tablets", "Novo Nordisk", "Diabetes", 680m, 200, true, "Meglitinide insulin secretagogue for postprandial glucose control."),
                ("Tradjenta 5mg (Linagliptin)", "Boehringer", "Diabetes", 2800m, 180, true, "DPP-4 inhibitor safe for patients with renal impairment."),
                ("Glucovance (Metformin+Glibenclamide)", "Merck", "Diabetes", 320m, 400, true, "Combination tablet for type 2 diabetes dual-action control."),
                ("Galvus 50mg (Vildagliptin)", "Novartis", "Diabetes", 1800m, 200, true, "DPP-4 inhibitor for blood sugar management in type 2 diabetes."),
                ("Invokamet (Canagliflozin+Metformin)", "J&J", "Diabetes", 4200m, 120, true, "Fixed-dose combination SGLT-2 + biguanide tablet."),
                ("Actos 30mg (Pioglitazone)", "Eli Lilly", "Diabetes", 650m, 250, true, "Insulin sensitizer for type 2 diabetes with dyslipidaemia."),
                ("Diabose 50mg (Miglitol)", "Pfizer", "Diabetes", 420m, 200, true, "Alpha-glucosidase inhibitor for post-meal hyperglycaemia."),
                ("Byetta 5mcg Injection (Exenatide)", "AstraZeneca", "Diabetes", 9800m, 80, true, "Twice-daily GLP-1 receptor agonist for type 2 diabetes."),
                ("Soliqua 100/33 Injection", "Sanofi", "Diabetes", 12000m, 60, true, "Fixed-ratio insulin glargine + lixisenatide combination."),
                ("Amaryl 1mg (Glimepiride)", "Sanofi", "Diabetes", 260m, 500, true, "Once-daily sulphonylurea for type 2 diabetes."),
                ("Gluformin 1000mg SR Tablets", "Sami", "Diabetes", 280m, 450, true, "Sustained-release metformin reducing GI side effects."),
                ("Glucoject Lancets 30G Box/100", "AgaMatrix", "Diabetes", 480m, 600, false, "Ultra-fine lancets for comfortable blood glucose monitoring."),

                // ══════════════════════════════════════════
                // GASTRIC (25)
                // ══════════════════════════════════════════
                ("Risek 40mg (Omeprazole)", "Getz Pharma", "Gastric", 420m, 800, false, "Proton pump inhibitor for GERD, acidity and peptic ulcers."),
                ("Gaviscon Double Action Syrup 150ml", "Reckitt", "Gastric", 290m, 600, false, "Antacid forming protective raft against acid reflux."),
                ("Nexum 40mg (Esomeprazole)", "Getz Pharma", "Gastric", 510m, 500, false, "Advanced acid suppression for reflux oesophagitis."),
                ("Flagyl 400mg (Metronidazole)", "Sanofi", "Gastric", 95m, 1100, true, "Antiprotozoal for amebiasis, giardiasis and Clostridium infection."),
                ("Motilium 10mg (Domperidone)", "J&J", "Gastric", 180m, 700, false, "Prokinetic for nausea, vomiting and gastroparesis."),
                ("Zantac 150mg (Ranitidine)", "GSK", "Gastric", 140m, 600, false, "H2-blocker for heartburn, GERD and gastric ulcer healing."),
                ("Lopermide 2mg (Loperamide)", "J&J", "Gastric", 80m, 800, false, "Antidiarrhoeal reducing intestinal motility in acute diarrhoea."),
                ("Buscopan 10mg (Hyoscine)", "Boehringer", "Gastric", 220m, 500, false, "Antispasmodic for abdominal cramps, IBS and renal colic."),
                ("Duphalac Lactulose Syrup 200ml", "Abbott", "Gastric", 380m, 400, false, "Osmotic laxative for chronic constipation and hepatic encephalopathy."),
                ("Librax (Chlordiazepoxide+Clidinium)", "Roche", "Gastric", 280m, 350, true, "Combined anxiolytic-antispasmodic for IBS and functional GI disorders."),
                ("Colpermin Peppermint Oil Caps", "Tillotts", "Gastric", 520m, 250, false, "Enteric-coated peppermint oil for IBS-related pain and bloating."),
                ("Normix 200mg (Rifaximin)", "Alfa Wassermann", "Gastric", 1800m, 180, true, "Non-systemic antibiotic for traveller's diarrhoea and IBS-D."),
                ("Mylanta Antacid Liquid 200ml", "J&J", "Gastric", 220m, 500, false, "Aluminium hydroxide and magnesium antacid for fast heartburn relief."),
                ("Eno Fruit Salt Sachet", "GSK", "Gastric", 35m, 1500, false, "Effervescent antacid granules for rapid indigestion relief."),
                ("Smecta 3g Sachets", "Ipsen", "Gastric", 120m, 400, false, "Diosmectite for acute diarrhoea and irritable bowel syndrome."),
                ("Panto 40mg (Pantoprazole)", "Sun Pharma", "Gastric", 380m, 600, false, "PPI for GERD and Zollinger-Ellison syndrome management."),
                ("Lansoprazole 30mg Capsules", "Takeda", "Gastric", 450m, 400, false, "PPI for peptic ulcer disease and NSAID-induced gastritis."),
                ("Rabeprazole 20mg Tablets", "Eisai", "Gastric", 490m, 380, false, "PPI with rapid onset for GERD and H. pylori eradication."),
                ("Sucralfate 1g Suspension 200ml", "Pfizer", "Gastric", 320m, 350, true, "Cytoprotective agent forming mucosal barrier over ulcer site."),
                ("Misoprostol 200mcg Tablets", "Pfizer", "Gastric", 420m, 200, true, "Prostaglandin analogue for NSAID-induced peptic ulcer prevention."),
                ("Reglan 10mg (Metoclopramide)", "Sanofi", "Gastric", 95m, 600, true, "Prokinetic and antiemetic for gastroparesis and nausea."),
                ("Dulcolax 5mg (Bisacodyl)", "Boehringer", "Gastric", 180m, 500, false, "Stimulant laxative for constipation and bowel preparation."),
                ("Heliox Sachet (Triple Therapy)", "Getz Pharma", "Gastric", 1200m, 280, true, "Helicobacter pylori eradication combination therapy."),
                ("Normacol Plus Granules 500g", "Norgine", "Gastric", 680m, 200, false, "Stimulant bulk-forming laxative for chronic constipation."),
                ("Imodium Plus Comfort 2mg", "J&J", "Gastric", 280m, 350, false, "Loperamide with simethicone for diarrhoea and bloating."),

                // ══════════════════════════════════════════
                // RESPIRATORY (25)
                // ══════════════════════════════════════════
                ("Ventolin Inhaler 100mcg", "GSK", "Respiratory", 350m, 400, true, "Fast-acting salbutamol bronchodilator for asthma relief."),
                ("Pulmonol Cough Syrup 120ml", "Ccl Pharma", "Respiratory", 180m, 600, false, "Expectorant syrup for chest congestion and persistent cough."),
                ("Montika 10mg (Montelukast)", "Sami", "Respiratory", 420m, 550, true, "Leukotriene antagonist for asthma and allergic rhinitis."),
                ("Seretide 250/25 Evohaler", "GSK", "Respiratory", 2800m, 180, true, "Fluticasone/salmeterol combination inhaler for persistent asthma."),
                ("Pulmicort 200mcg Turbuhaler", "AstraZeneca", "Respiratory", 1800m, 200, true, "Budesonide dry-powder inhaler for asthma prevention."),
                ("Spiriva Handihaler 18mcg", "Boehringer", "Respiratory", 3200m, 150, true, "Tiotropium once-daily LAMA inhaler for COPD maintenance."),
                ("Atrovent 20mcg Inhaler", "Boehringer", "Respiratory", 1400m, 220, true, "Ipratropium bromide for COPD and acute bronchospasm."),
                ("Singulair 5mg Chewable Tabs", "MSD", "Respiratory", 1950m, 280, true, "Paediatric leukotriene receptor antagonist for asthma."),
                ("Beclate 250mcg Inhaler", "Cipla", "Respiratory", 950m, 300, true, "Beclomethasone inhaler for daily asthma and rhinitis control."),
                ("Mucinex 600mg ER (Guaifenesin)", "Reckitt", "Respiratory", 420m, 350, false, "Extended-release expectorant thinning and loosening mucus."),
                ("Rhinocort Aqua 64mcg Spray", "AstraZeneca", "Respiratory", 680m, 280, true, "Budesonide nasal spray for allergic and non-allergic rhinitis."),
                ("Nasacort Aqua 55mcg Spray", "Sanofi", "Respiratory", 720m, 260, true, "Triamcinolone nasal spray for seasonal allergic rhinitis."),
                ("Flixonase 50mcg Nasal Spray", "GSK", "Respiratory", 780m, 240, true, "Fluticasone propionate for persistent allergic rhinitis."),
                ("Bromhexine 8mg Tablets", "Boehringer", "Respiratory", 95m, 600, false, "Mucolytic reducing sputum viscosity in productive cough."),
                ("Ambroxol 30mg Tablets", "Sanofi", "Respiratory", 110m, 700, false, "Mucolytic and secretolytic agent for COPD and bronchitis."),
                ("Dextromethorphan 15mg Syrup 120ml", "Reckitt", "Respiratory", 140m, 500, false, "Non-narcotic antitussive for dry, irritating cough."),
                ("Codeine Linctus 200ml", "GSK", "Respiratory", 280m, 250, true, "Opioid antitussive syrup for severe unproductive cough."),
                ("Prednisolone 5mg Tablets", "Sanofi", "Respiratory", 75m, 800, true, "Corticosteroid for asthma exacerbations and severe allergic reactions."),
                ("Theophylline SR 300mg Tablets", "Sanofi", "Respiratory", 180m, 350, true, "Methylxanthine bronchodilator for COPD and refractory asthma."),
                ("Zafirlukast 20mg Tablets", "AstraZeneca", "Respiratory", 680m, 200, true, "Leukotriene antagonist for mild to moderate asthma."),
                ("Fluticasone 500mcg/Salmeterol 50mcg", "GSK", "Respiratory", 3600m, 120, true, "High-strength combination inhaler for severe persistent asthma."),
                ("Ipratropium 500mcg Nebules", "Boehringer", "Respiratory", 350m, 280, true, "Nebuliser solution for acute COPD and asthma exacerbations."),
                ("Formoterol 12mcg Turbuhaler", "AstraZeneca", "Respiratory", 1600m, 200, true, "Long-acting beta-2 agonist for COPD and asthma maintenance."),
                ("N-Acetylcysteine 600mg Effervescent", "Zambon", "Respiratory", 380m, 350, false, "Mucolytic and antioxidant for chronic bronchitis and COPD."),
                ("Carbocisteine 375mg Capsules", "Sanofi", "Respiratory", 260m, 400, false, "Mucolyric reducing mucus viscosity in COPD and bronchiectasis."),

                // ══════════════════════════════════════════
                // SKIN CARE (20)
                // ══════════════════════════════════════════
                ("Betnovate-N Cream 20g", "GSK", "Skin Care", 140m, 500, true, "Betamethasone + neomycin for eczema and infected dermatitis."),
                ("Hydrozole Cream 20g", "Stiefel", "Skin Care", 220m, 350, false, "Hydrocortisone + clotrimazole for tinea and inflamed fungal infections."),
                ("Sunblock SPF 60 Gel 50ml", "Dermacos", "Skin Care", 850m, 200, false, "Broad-spectrum UVA/UVB sunscreen for daily photoprotection."),
                ("Clindac-A Gel 20g (Clindamycin)", "Galderma", "Skin Care", 380m, 300, true, "Topical antibiotic gel for inflammatory and comedonal acne."),
                ("Tretinoin 0.025% Cream 20g", "J&J", "Skin Care", 420m, 250, true, "Retinoid for acne vulgaris, fine lines and photo-damaged skin."),
                ("Nizoral 2% Shampoo 100ml", "J&J", "Skin Care", 580m, 300, false, "Ketoconazole shampoo for dandruff and seborrhoeic dermatitis."),
                ("Terbinafine 1% Cream 15g", "Novartis", "Skin Care", 280m, 400, false, "Allylamine antifungal for tinea pedis, cruris and corporis."),
                ("Lamisil Spray 1% 30ml", "Novartis", "Skin Care", 680m, 200, false, "Terbinafine spray for athlete's foot and ringworm."),
                ("Mometasone 0.1% Cream 15g", "MSD", "Skin Care", 320m, 350, true, "Potent topical steroid for psoriasis, eczema and contact dermatitis."),
                ("Elocon 0.1% Ointment 15g", "MSD", "Skin Care", 380m, 300, true, "Mometasone furoate ointment for dry, lichenified skin conditions."),
                ("Calamine Lotion 100ml", "Searle", "Skin Care", 85m, 600, false, "Zinc oxide soothing lotion for insect bites, prickly heat and rashes."),
                ("Clobetasol 0.05% Cream 30g", "GSK", "Skin Care", 290m, 280, true, "Very potent steroid for refractory psoriasis and severe eczema."),
                ("Whitfield's Ointment 30g", "Martindale", "Skin Care", 95m, 400, false, "Benzoic and salicylic acid antifungal for tinea infections."),
                ("Silver Sulfadiazine Cream 50g", "Sanofi", "Skin Care", 480m, 200, true, "Antimicrobial cream for burn wound infection prevention."),
                ("Benzoyl Peroxide 5% Gel 30g", "Galderma", "Skin Care", 320m, 350, false, "Topical acne treatment with antibacterial and keratolytic action."),
                ("Tacrolimus 0.03% Ointment 30g", "Astellas", "Skin Care", 2200m, 150, true, "Immunomodulator for atopic eczema when steroids are inappropriate."),
                ("Daktacort Cream 30g", "J&J", "Skin Care", 260m, 320, true, "Miconazole + hydrocortisone for inflamed fungal skin infections."),
                ("Sebamed Clear Face Gel 50ml", "Sebapharma", "Skin Care", 950m, 180, false, "pH 5.5 face gel for acne-prone and oily skin."),
                ("Vitiligo Cream 30g (Tacrolimus)", "Galderma", "Skin Care", 1800m, 120, true, "Immunomodulator for repigmentation in stable vitiligo patches."),
                ("Psoriaid Lotion 100ml", "Stiefel", "Skin Care", 780m, 200, true, "Coal tar and salicylic acid lotion for scalp psoriasis."),

                // ══════════════════════════════════════════
                // VITAMINS & SUPPLEMENTS (25)
                // ══════════════════════════════════════════
                ("Surbex Z", "Abbott", "Vitamins", 420m, 500, false, "B-complex, vitamin C and zinc for energy and immunity."),
                ("Cac 1000 Plus Effervescent", "GSK", "Vitamins", 380m, 450, false, "Calcium, vitamin C and D3 effervescent tablet."),
                ("Neurobion (B1, B6, B12)", "Merck", "Vitamins", 260m, 800, false, "Neurotropic vitamins for nerve function and fatigue."),
                ("Centrum Silver 50+ Multivitamin", "Pfizer", "Vitamins", 1850m, 150, false, "Complete multivitamin for mature adults 50 and over."),
                ("Vitamin D3 1000IU Softgels", "Getz Pharma", "Vitamins", 320m, 600, false, "Vitamin D3 supplement for bone health and immunity."),
                ("Ferrous Sulphate 200mg Tablets", "Searle", "Vitamins", 95m, 700, false, "Oral iron supplement for iron-deficiency anaemia."),
                ("Folic Acid 5mg Tablets", "GSK", "Vitamins", 45m, 900, false, "Folate supplement for pregnancy and megaloblastic anaemia."),
                ("Calcium 600mg + D3 Tablets", "Wyeth", "Vitamins", 380m, 500, false, "Calcium and vitamin D combination for bone mineralisation."),
                ("Zinc Sulphate 220mg Tablets", "Abbott", "Vitamins", 180m, 500, false, "Zinc supplement for immune support and wound healing."),
                ("Vitamin C 1000mg Effervescent", "Roche", "Vitamins", 350m, 400, false, "High-dose vitamin C for antioxidant and immune support."),
                ("Omega-3 Fish Oil 1000mg Softgels", "Abbott", "Vitamins", 680m, 350, false, "EPA and DHA for cardiovascular and cognitive health."),
                ("Biotin 5mg Tablets", "Pharmalab", "Vitamins", 280m, 400, false, "Vitamin B7 for hair growth, nail strength and skin health."),
                ("Magnesium 375mg Tablets", "Sanofi", "Vitamins", 320m, 350, false, "Magnesium supplement for muscle function and energy metabolism."),
                ("Vitamin B12 1000mcg Sublingual", "Merck", "Vitamins", 420m, 300, false, "Sublingual methylcobalamin for B12 deficiency and fatigue."),
                ("Vitamin A 10,000IU Capsules", "Roche", "Vitamins", 180m, 400, false, "Fat-soluble vitamin for vision, immunity and skin integrity."),
                ("Vitamin E 400IU Softgels", "Pfizer", "Vitamins", 280m, 350, false, "Antioxidant tocopherol for cardiovascular and skin health."),
                ("Vitamin K2 100mcg MK-7 Tablets", "Pharmalab", "Vitamins", 680m, 200, false, "Menaquinone-7 for calcium utilisation and bone health."),
                ("Iron + Folic Acid Syrup 200ml", "Getz Pharma", "Vitamins", 220m, 400, false, "Combined iron and folate liquid for pregnancy-related anaemia."),
                ("Pregnacare Max Tablets", "Vitabiotics", "Vitamins", 1850m, 200, false, "Complete prenatal multivitamin with omega-3 for pregnancy."),
                ("Ensure Plus Nutrition Shake", "Abbott", "Vitamins", 2400m, 250, false, "Complete balanced nutrition powder for malnutrition recovery."),
                ("Protovit Multivitamin Drops", "Roche", "Vitamins", 480m, 300, false, "Paediatric multivitamin drops for infants and young children."),
                ("Coenzyme Q10 100mg Softgels", "Pharmalab", "Vitamins", 880m, 200, false, "CoQ10 ubiquinone for mitochondrial energy and heart health."),
                ("Probiotics Lactobacillus 10B CFU", "Sanofi", "Vitamins", 680m, 280, false, "Multi-strain probiotic for gut microbiome and immune health."),
                ("Selenium 200mcg Tablets", "Roche", "Vitamins", 380m, 250, false, "Essential trace mineral for antioxidant defence and thyroid function."),
                ("Iodine 150mcg Tablets", "Merck", "Vitamins", 120m, 400, false, "Iodine supplement for thyroid hormone synthesis."),

                // ══════════════════════════════════════════
                // ALLERGY (20)
                // ══════════════════════════════════════════
                ("Zyrtec 10mg (Cetirizine)", "UCB", "Allergy", 220m, 700, false, "Non-drowsy antihistamine for allergic rhinitis and urticaria."),
                ("Claritin 10mg (Loratadine)", "Bayer", "Allergy", 280m, 600, false, "Once-daily non-sedating antihistamine for seasonal allergies."),
                ("Allegra 120mg (Fexofenadine)", "Sanofi", "Allergy", 380m, 500, false, "Non-drowsy antihistamine for seasonal allergic rhinitis."),
                ("Xyzal 5mg (Levocetirizine)", "UCB", "Allergy", 320m, 450, false, "Next-generation cetirizine for chronic urticaria and rhinitis."),
                ("Polaramine 2mg (Dexchlorpheniramine)", "MSD", "Allergy", 95m, 600, false, "Classical antihistamine for acute allergic reactions."),
                ("Phenergan 25mg (Promethazine)", "Sanofi", "Allergy", 120m, 500, true, "First-gen antihistamine for allergy, nausea and premedication."),
                ("Avil 45mg (Pheniramine)", "Sanofi", "Allergy", 80m, 800, false, "Antihistamine for allergy, urticaria and insect sting reactions."),
                ("Dimetapp Cold & Allergy Liquid", "Pfizer", "Allergy", 180m, 350, false, "Brompheniramine + pseudoephedrine for cold and allergy congestion."),
                ("Atarax 25mg (Hydroxyzine)", "UCB", "Allergy", 180m, 400, true, "Antihistamine with anxiolytic effect for allergies and anxiety."),
                ("Bilastine 20mg Tablets", "Menarini", "Allergy", 380m, 300, false, "New-generation antihistamine with rapid onset and long action."),
                ("Rupatadine 10mg Tablets", "Pfizer", "Allergy", 420m, 250, false, "Dual-action antihistamine for urticaria and seasonal rhinitis."),
                ("Desloratadine 5mg Tablets", "MSD", "Allergy", 350m, 400, false, "Active loratadine metabolite with extended antihistamine action."),
                ("Mast Cell Stabilizer Eye Drops 5ml", "Alcon", "Allergy", 480m, 250, true, "Sodium cromoglicate for allergic conjunctivitis and eye itching."),
                ("Azelastine Nasal Spray 0.1%", "Meda", "Allergy", 680m, 200, true, "Topical antihistamine nasal spray for allergic rhinitis."),
                ("Flonase Allergy Spray 120 doses", "GSK", "Allergy", 820m, 220, false, "Fluticasone propionate nasal spray for year-round allergies."),
                ("Triludan 60mg (Terfenadine)", "Merrell", "Allergy", 140m, 400, false, "Antihistamine for seasonal hay fever and chronic urticaria."),
                ("Epipen 0.3mg Autoinjector", "Pfizer", "Allergy", 8500m, 80, true, "Adrenaline autoinjector for anaphylaxis emergency treatment."),
                ("Pred Forte Eye Drops 1%", "Allergan", "Allergy", 680m, 200, true, "Prednisolone acetate for severe allergic conjunctivitis."),
                ("Sodium Cromoglicate 4% Eye Drops", "Sanofi", "Allergy", 420m, 280, false, "Mast cell stabiliser for allergic and vernal conjunctivitis."),
                ("Betnesol 0.5mg (Betamethasone)", "GSK", "Allergy", 320m, 350, true, "Systemic corticosteroid for severe allergic and inflammatory disorders."),

                // ══════════════════════════════════════════
                // NEUROLOGY (20)
                // ══════════════════════════════════════════
                ("Epilim 500mg (Sodium Valproate)", "Sanofi", "Neurology", 680m, 300, true, "Broad-spectrum anticonvulsant for epilepsy and bipolar disorder."),
                ("Tegretol 200mg (Carbamazepine)", "Novartis", "Neurology", 280m, 400, true, "Anticonvulsant and mood stabiliser for epilepsy and trigeminal neuralgia."),
                ("Lamotrigine 50mg Tablets", "GSK", "Neurology", 520m, 280, true, "Antiepileptic for focal seizures and bipolar depression."),
                ("Phenobarbitone 30mg Tablets", "Bayer", "Neurology", 55m, 500, true, "Barbiturate anticonvulsant for tonic-clonic and focal seizures."),
                ("Topiramate 100mg Tablets", "J&J", "Neurology", 680m, 250, true, "Anticonvulsant for epilepsy and migraine prophylaxis."),
                ("Levetiracetam 500mg Tablets", "UCB", "Neurology", 1200m, 200, true, "Novel anticonvulsant for generalised and focal epileptic seizures."),
                ("Topamax 50mg (Topiramate)", "J&J", "Neurology", 780m, 200, true, "Anticonvulsant also used for weight management and migraines."),
                ("Sinemet 25/250 (Levodopa+Carbidopa)", "MSD", "Neurology", 980m, 180, true, "Gold-standard therapy for Parkinson's disease motor symptoms."),
                ("Pramipexole 1mg Tablets", "Boehringer", "Neurology", 1400m, 160, true, "Dopamine agonist for Parkinson's disease and restless legs syndrome."),
                ("Rasagiline 1mg Tablets", "Teva", "Neurology", 2800m, 120, true, "MAO-B inhibitor for early Parkinson's disease monotherapy."),
                ("Memantine 10mg Tablets", "Merz", "Neurology", 3200m, 100, true, "NMDA antagonist for moderate to severe Alzheimer's disease."),
                ("Donepezil 5mg Tablets", "Pfizer", "Neurology", 2600m, 140, true, "Cholinesterase inhibitor for mild to moderate Alzheimer's disease."),
                ("Riluzole 50mg Tablets", "Sanofi", "Neurology", 18000m, 40, true, "Neuroprotective for ALS (amyotrophic lateral sclerosis) treatment."),
                ("Natalizumab 300mg Injection", "Biogen", "Neurology", 85000m, 10, true, "Monoclonal antibody for relapsing-remitting multiple sclerosis."),
                ("Tizanidine 2mg Tablets", "Novartis", "Neurology", 380m, 300, true, "Centrally-acting muscle relaxant for spasticity in MS and spinal cord."),
                ("Baclofen 10mg Tablets", "Novartis", "Neurology", 180m, 400, true, "GABA-B agonist for spasticity in multiple sclerosis and spinal injury."),
                ("Sumatriptan 50mg Tablets", "GSK", "Neurology", 980m, 200, true, "Triptan for acute migraine with or without aura."),
                ("Zolmitriptan 2.5mg Tablets", "AstraZeneca", "Neurology", 1200m, 180, true, "5-HT1B/1D agonist for acute treatment of migraine headaches."),
                ("Pizotifen 0.5mg Tablets", "Novartis", "Neurology", 180m, 350, true, "5-HT antagonist for prevention of migraine and cluster headaches."),
                ("Propranolol 40mg Tablets", "Astra", "Neurology", 95m, 500, true, "Beta-blocker for migraine prophylaxis and essential tremor."),

                // ══════════════════════════════════════════
                // PSYCHIATRY (20)
                // ══════════════════════════════════════════
                ("Prozac 20mg (Fluoxetine)", "Eli Lilly", "Psychiatry", 420m, 400, true, "SSRI for major depression, OCD, bulimia and panic disorder."),
                ("Seroxat 20mg (Paroxetine)", "GSK", "Psychiatry", 480m, 350, true, "SSRI for depression, anxiety disorders, PTSD and OCD."),
                ("Zoloft 50mg (Sertraline)", "Pfizer", "Psychiatry", 560m, 350, true, "SSRI for depression, social anxiety, PTSD and panic disorder."),
                ("Lexapro 10mg (Escitalopram)", "Lundbeck", "Psychiatry", 620m, 320, true, "Selective SSRI for major depressive disorder and generalised anxiety."),
                ("Cipralex 10mg (Escitalopram)", "Lundbeck", "Psychiatry", 680m, 300, true, "SSRI with high receptor selectivity for depression and anxiety."),
                ("Effexor XR 75mg (Venlafaxine)", "Pfizer", "Psychiatry", 980m, 250, true, "SNRI for depression, generalised anxiety and social phobia."),
                ("Cymbalta 60mg (Duloxetine)", "Eli Lilly", "Psychiatry", 1200m, 220, true, "SNRI for depression, diabetic neuropathy and fibromyalgia."),
                ("Mirtazapine 30mg Tablets", "Organon", "Psychiatry", 580m, 280, true, "NaSSA antidepressant with sedative properties for depression."),
                ("Lithium Carbonate 300mg Tablets", "Pfizer", "Psychiatry", 120m, 300, true, "Mood stabiliser for bipolar disorder mania and depression."),
                ("Abilify 10mg (Aripiprazole)", "Otsuka", "Psychiatry", 2800m, 150, true, "Atypical antipsychotic for schizophrenia and bipolar I disorder."),
                ("Olanzapine 10mg Tablets", "Eli Lilly", "Psychiatry", 1850m, 180, true, "Atypical antipsychotic for schizophrenia and acute mania."),
                ("Risperidone 2mg Tablets", "J&J", "Psychiatry", 680m, 250, true, "Atypical antipsychotic for schizophrenia and aggression in autism."),
                ("Quetiapine 25mg Tablets", "AstraZeneca", "Psychiatry", 820m, 220, true, "Atypical antipsychotic for schizophrenia, bipolar and depression."),
                ("Clozapine 100mg Tablets", "Novartis", "Psychiatry", 280m, 150, true, "Atypical antipsychotic for treatment-resistant schizophrenia."),
                ("Lorazepam 1mg Tablets", "Wyeth", "Psychiatry", 95m, 400, true, "Short-acting benzodiazepine for anxiety, insomnia and status epilepticus."),
                ("Diazepam 5mg Tablets", "Roche", "Psychiatry", 65m, 500, true, "Benzodiazepine for anxiety, muscle spasm and alcohol withdrawal."),
                ("Alprazolam 0.5mg Tablets", "Pfizer", "Psychiatry", 120m, 450, true, "Benzodiazepine for panic disorder and generalised anxiety."),
                ("Clonazepam 0.5mg Tablets", "Roche", "Psychiatry", 140m, 380, true, "Benzodiazepine for panic disorder, epilepsy and anxiety."),
                ("Bupropion 150mg SR Tablets", "GSK", "Psychiatry", 1800m, 180, true, "NDRI antidepressant and smoking cessation aid."),
                ("Trazodone 100mg Tablets", "Bristol", "Psychiatry", 480m, 250, true, "Serotonin antagonist and reuptake inhibitor for depression and insomnia."),

                // ══════════════════════════════════════════
                // EYE CARE (15)
                // ══════════════════════════════════════════
                ("Vigamox 0.5% Eye Drops 3ml", "Alcon", "Eye Care", 680m, 300, true, "Moxifloxacin antibiotic eye drops for bacterial conjunctivitis."),
                ("Travatan 0.004% Eye Drops", "Alcon", "Eye Care", 1850m, 150, true, "Travoprost prostaglandin for open-angle glaucoma and ocular hypertension."),
                ("Lumigan 0.01% Eye Drops", "Allergan", "Eye Care", 1950m, 140, true, "Bimatoprost for lowering intraocular pressure in glaucoma."),
                ("Timolol 0.5% Eye Drops 5ml", "MSD", "Eye Care", 480m, 250, true, "Beta-blocker eye drops reducing aqueous humour production in glaucoma."),
                ("Pred Forte 1% Eye Drops", "Allergan", "Eye Care", 680m, 200, true, "Prednisolone acetate for post-surgical and allergic eye inflammation."),
                ("Tobramycin 0.3% Eye Drops 5ml", "Alcon", "Eye Care", 320m, 280, true, "Aminoglycoside antibiotic for bacterial external eye infections."),
                ("Refresh Tears Eye Drops 15ml", "Allergan", "Eye Care", 420m, 350, false, "Preservative-free lubricant for dry eye syndrome."),
                ("Systane Ultra Eye Drops 10ml", "Alcon", "Eye Care", 580m, 300, false, "Polyethylene glycol lubricating drops for dry eye relief."),
                ("Voltaren Ophtha 0.1% Eye Drops", "Novartis", "Eye Care", 680m, 200, true, "Topical NSAID for ocular inflammation after eye surgery."),
                ("Hypromellose 0.3% Eye Drops", "Bausch", "Eye Care", 280m, 400, false, "Viscous tear substitute for moderate dry eye conditions."),
                ("Atropine 1% Eye Drops", "Martindale", "Eye Care", 380m, 180, true, "Cycloplegic for refraction, uveitis and myopia management."),
                ("Cyclopentolate 1% Eye Drops", "Martindale", "Eye Care", 320m, 200, true, "Short-acting mydriatic for refraction and fundus examination."),
                ("Latanoprost 0.005% Eye Drops", "Pfizer", "Eye Care", 1650m, 160, true, "Prostaglandin analogue for primary open-angle glaucoma."),
                ("Dorzolamide 2% Eye Drops 5ml", "MSD", "Eye Care", 1200m, 180, true, "Carbonic anhydrase inhibitor eye drops for raised IOP."),
                ("Olopatadine 0.1% Eye Drops 5ml", "Alcon", "Eye Care", 780m, 220, true, "Antihistamine and mast cell stabiliser for allergic conjunctivitis."),

                // ══════════════════════════════════════════
                // ENT (15)
                // ══════════════════════════════════════════
                ("Otosporin Ear Drops 5ml", "GSK", "ENT", 320m, 350, true, "Neomycin + polymyxin + hydrocortisone for otitis externa."),
                ("Sofradex Ear/Eye Drops 8ml", "Roussel", "ENT", 280m, 300, true, "Framycetin + gramicidin + dexamethasone for ear and eye infections."),
                ("Coldrex Junior Tablets", "GSK", "ENT", 95m, 500, false, "Paediatric cold and flu relief with nasal decongestant."),
                ("Xylometazoline 0.1% Nasal Drops", "Novartis", "ENT", 180m, 500, false, "Sympathomimetic nasal decongestant for acute nasal congestion."),
                ("Nasonex 50mcg Nasal Spray", "MSD", "ENT", 980m, 220, true, "Mometasone furoate for seasonal and perennial allergic rhinitis."),
                ("Strepsils Throat Lozenges", "Reckitt", "ENT", 120m, 700, false, "Antiseptic lozenges for sore throat and mild oral infections."),
                ("Betadine Gargle 250ml", "Mundipharma", "ENT", 280m, 400, false, "Povidone-iodine oral antiseptic for throat and oral infections."),
                ("Nasivion Mini 0.01% Drops", "Merck", "ENT", 220m, 400, false, "Oxymetazoline nasal drops for infants with nasal congestion."),
                ("Clarityn-D 12hr (Loratadine+PSE)", "Bayer", "ENT", 420m, 300, false, "Antihistamine + decongestant for nasal congestion with allergy."),
                ("Cerumol Ear Drops 11ml", "Thornton&Ross", "ENT", 380m, 250, false, "Paradichlorobenzene drops for earwax softening and removal."),
                ("Oxymetazoline 0.05% Nasal Spray", "Afrin", "ENT", 210m, 450, false, "Nasal decongestant spray for blocked nose and sinusitis."),
                ("Saline Nasal Spray 100ml", "Sterimar", "ENT", 480m, 350, false, "Isotonic saline for daily nasal hygiene and congestion relief."),
                ("Prednisolone 25mg Soluble Tablets", "Sanofi", "ENT", 180m, 300, true, "Systemic steroid for severe sinusitis, tonsillitis and croup."),
                ("Carbocisteine 250mg/5ml Syrup", "Sanofi", "ENT", 220m, 350, false, "Mucolytic syrup for productive cough with thick mucus."),
                ("Kenalog 40mg/ml Suspension", "Squibb", "ENT", 380m, 200, true, "Triamcinolone acetonide injection for nasal polyps and rhinitis."),

                // ══════════════════════════════════════════
                // WOMEN'S HEALTH (20)
                // ══════════════════════════════════════════
                ("Diane-35 (Cyproterone+Ethinyl)", "Bayer", "Women's Health", 480m, 350, true, "Combined oral contraceptive for acne and hirsutism."),
                ("Yasmin (Drospirenone+EE)", "Bayer", "Women's Health", 680m, 300, true, "Low-dose COC for contraception, acne and PMS."),
                ("Marvelon (Desogestrel+EE)", "Organon", "Women's Health", 580m, 280, true, "Third-gen combined oral contraceptive with low androgenic activity."),
                ("Folic Acid 400mcg in Pregnancy", "Searle", "Women's Health", 65m, 800, false, "Periconceptional folate supplementation reducing neural tube defects."),
                ("Duphaston 10mg (Dydrogesterone)", "Abbott", "Women's Health", 680m, 350, true, "Progestogen for luteal phase support and threatened miscarriage."),
                ("Primolut N 5mg (Norethisterone)", "Bayer", "Women's Health", 280m, 400, true, "Progestogen for dysfunctional uterine bleeding and endometriosis."),
                ("Utrogestan 200mg (Progesterone)", "Besins", "Women's Health", 1200m, 200, true, "Micronised progesterone for hormone replacement and fertility."),
                ("Clomid 50mg (Clomifene)", "Sanofi", "Women's Health", 480m, 250, true, "Ovulation induction for anovulatory infertility."),
                ("Mifepristone 200mg Tablets", "Sun Pharma", "Women's Health", 1800m, 80, true, "Antiprogestogen for medical termination of pregnancy."),
                ("Misoprostol 200mcg Vaginal Tab", "Pfizer", "Women's Health", 380m, 120, true, "Prostaglandin for cervical ripening and postpartum haemorrhage."),
                ("Oxytocin 10IU Injection", "Novartis", "Women's Health", 95m, 200, true, "Uterotonic for induction of labour and postpartum haemorrhage."),
                ("Tranexamic Acid 500mg Tablets", "Pfizer", "Women's Health", 280m, 350, true, "Antifibrinolytic for heavy menstrual bleeding management."),
                ("Canesten Clotrimazole Vaginal", "Bayer", "Women's Health", 380m, 400, false, "Antifungal pessary for vaginal candidiasis treatment."),
                ("Flagyl 500mg Vaginal Tablet", "Sanofi", "Women's Health", 320m, 350, true, "Metronidazole vaginal tablet for bacterial vaginosis."),
                ("Premarin 0.625mg (Conjugated E)", "Pfizer", "Women's Health", 580m, 250, true, "HRT for menopausal symptoms including hot flushes."),
                ("Estraderm 50mcg Patch", "Novartis", "Women's Health", 1800m, 150, true, "Transdermal oestrogen for HRT in surgically menopausal women."),
                ("Femara 2.5mg (Letrozole)", "Novartis", "Women's Health", 980m, 200, true, "Aromatase inhibitor for breast cancer and ovulation induction."),
                ("Norethisterone 5mg Tablets", "Sanofi", "Women's Health", 180m, 450, true, "Progestogen for delaying menstruation and treating endometriosis."),
                ("Methotrexate 2.5mg Tablets", "Pfizer", "Women's Health", 180m, 200, true, "DMARd and antiproliferative for ectopic pregnancy management."),
                ("Glucocheck Pregnancy Test Strip", "Roche", "Women's Health", 180m, 600, false, "Urine hCG rapid test for early pregnancy detection."),

                // ══════════════════════════════════════════
                // UROLOGY (15)
                // ══════════════════════════════════════════
                ("Flomax 0.4mg (Tamsulosin)", "Boehringer", "Urology", 580m, 350, true, "Alpha-1A blocker for benign prostatic hyperplasia symptoms."),
                ("Avodart 0.5mg (Dutasteride)", "GSK", "Urology", 1200m, 220, true, "5-alpha reductase inhibitor reducing prostate size in BPH."),
                ("Proscar 5mg (Finasteride)", "MSD", "Urology", 980m, 250, true, "5-alpha reductase inhibitor for BPH and male pattern baldness."),
                ("Detrusitol 2mg (Tolterodine)", "Pfizer", "Urology", 680m, 200, true, "Antimuscarinic for overactive bladder and urge incontinence."),
                ("Vesicare 5mg (Solifenacin)", "Astellas", "Urology", 1200m, 180, true, "Selective M3 antimuscarinic for overactive bladder syndrome."),
                ("Cialis 10mg (Tadalafil)", "Eli Lilly", "Urology", 2800m, 150, true, "PDE-5 inhibitor for erectile dysfunction and pulmonary hypertension."),
                ("Viagra 50mg (Sildenafil)", "Pfizer", "Urology", 1800m, 200, true, "PDE-5 inhibitor for erectile dysfunction in adult males."),
                ("Levitra 10mg (Vardenafil)", "Bayer", "Urology", 2400m, 150, true, "PDE-5 inhibitor for erectile dysfunction with rapid onset."),
                ("Nitrofurantoin 100mg Capsules", "MSD", "Urology", 380m, 300, true, "Urinary antiseptic for uncomplicated lower UTI in women."),
                ("Pyridium 200mg (Phenazopyridine)", "Pfizer", "Urology", 280m, 350, false, "Urinary analgesic for burning and pain during UTI treatment."),
                ("Oxybutynin 5mg Tablets", "Sanofi", "Urology", 420m, 280, true, "Anticholinergic for overactive bladder and urge incontinence."),
                ("Mirabegron 50mg (Betmiga)", "Astellas", "Urology", 2200m, 150, true, "Beta-3 adrenergic agonist for overactive bladder symptoms."),
                ("Alfuzosin 10mg XL Tablets", "Sanofi", "Urology", 480m, 250, true, "Alpha-blocker for lower urinary tract symptoms in BPH."),
                ("Silodosin 8mg Capsules", "Kissei", "Urology", 680m, 200, true, "Highly selective alpha-1A antagonist for BPH-related symptoms."),
                ("Desmopressin 0.2mg Tablets", "Ferring", "Urology", 980m, 180, true, "Vasopressin analogue for nocturia and nocturnal enuresis."),

                // ══════════════════════════════════════════
                // PAEDIATRICS (20)
                // ══════════════════════════════════════════
                ("Calpol Infant Suspension 100ml", "GSK", "Paediatrics", 145m, 400, false, "Paracetamol 120mg/5ml suspension for children 2-6 years."),
                ("Nurofen Children 100mg/5ml", "Reckitt", "Paediatrics", 165m, 350, false, "Ibuprofen suspension for children's fever and mild pain."),
                ("Amoxicillin 125mg/5ml Suspension", "GSK", "Paediatrics", 180m, 400, true, "Paediatric amoxicillin suspension for common bacterial infections."),
                ("Azithromycin 200mg/5ml Suspension", "Pfizer", "Paediatrics", 380m, 300, true, "Paediatric azithromycin for chest and ENT bacterial infections."),
                ("Singulair 4mg Granules", "MSD", "Paediatrics", 1200m, 200, true, "Montelukast sachets for asthma and allergic rhinitis in infants."),
                ("Pediazole Suspension 100ml", "Abbott", "Paediatrics", 280m, 350, true, "Erythromycin + sulfisoxazole for paediatric otitis media."),
                ("ORS Sachets (WHO Formula)", "Searle", "Paediatrics", 45m, 1200, false, "Oral rehydration salts for diarrhoea-induced dehydration."),
                ("Zincovit Syrup 200ml", "Apex", "Paediatrics", 280m, 350, false, "Zinc + multivitamin syrup for paediatric growth and immunity."),
                ("Gripe Water 100ml", "Woodward's", "Paediatrics", 120m, 500, false, "Herbal colic mixture for infant wind, stomach pain and hiccups."),
                ("Vitamin A 100000IU Capsule", "Roche", "Paediatrics", 45m, 600, false, "High-dose vitamin A for measles and nutritional deficiency in children."),
                ("Prednisolone 15mg/5ml Liquid", "Sanofi", "Paediatrics", 180m, 300, true, "Corticosteroid liquid for asthma and severe allergic reactions."),
                ("Diazepam 5mg Rectal Tube", "Roche", "Paediatrics", 380m, 150, true, "Rectal diazepam for management of febrile convulsions in children."),
                ("Amikacin 100mg/2ml Injection", "Bristol", "Paediatrics", 280m, 120, true, "Aminoglycoside injection for neonatal septicaemia."),
                ("Gentamicin 40mg/ml Injection", "Schering", "Paediatrics", 120m, 180, true, "Paediatric aminoglycoside for gram-negative neonatal infections."),
                ("Lactulose 3.1g/5ml Syrup 200ml", "Abbott", "Paediatrics", 280m, 280, false, "Osmotic laxative for chronic childhood constipation."),
                ("Cetirizine 5mg/5ml Syrup 60ml", "UCB", "Paediatrics", 180m, 350, false, "Paediatric antihistamine syrup for allergic rhinitis and urticaria."),
                ("Probiotic Drops 8ml (Infant)", "Biogaia", "Paediatrics", 2800m, 150, false, "Lactobacillus reuteri drops for colic and gut flora in infants."),
                ("Iron Polymaltose 50mg/5ml Syrup", "Getz Pharma", "Paediatrics", 220m, 300, false, "Paediatric liquid iron for iron deficiency anaemia."),
                ("Zinc Sulphate 10mg/5ml Syrup", "Abbott", "Paediatrics", 150m, 350, false, "Paediatric zinc supplementation for diarrhoea management."),
                ("Salbutamol 0.5mg/ml Nebules", "GSK", "Paediatrics", 380m, 250, true, "Nebuliser solution for paediatric acute asthma and bronchospasm."),

                // ══════════════════════════════════════════
                // BONE & JOINTS (15)
                // ══════════════════════════════════════════
                ("Fosamax 70mg (Alendronate)", "MSD", "Bone & Joints", 980m, 200, true, "Weekly bisphosphonate for osteoporosis prevention and treatment."),
                ("Bonviva 150mg (Ibandronate)", "Roche", "Bone & Joints", 1400m, 150, true, "Monthly bisphosphonate tablet for postmenopausal osteoporosis."),
                ("Prolia 60mg Injection (Denosumab)", "Amgen", "Bone & Joints", 18000m, 40, true, "RANK-L inhibitor injection for osteoporosis and bone metastases."),
                ("Calcium Sandoz 500mg Effervescent", "Sandoz", "Bone & Joints", 380m, 350, false, "Effervescent calcium supplement for bone health."),
                ("Vitamin D3 50000IU Weekly Capsule", "Getz Pharma", "Bone & Joints", 480m, 300, true, "High-dose vitamin D for deficiency correction and bone health."),
                ("Methotrexate 7.5mg (RA Dose)", "Pfizer", "Bone & Joints", 280m, 250, true, "Disease-modifying drug for rheumatoid arthritis management."),
                ("Hydroxychloroquine 200mg Tabs", "Sanofi", "Bone & Joints", 320m, 300, true, "Antimalarial DMARD for rheumatoid arthritis and lupus."),
                ("Sulfasalazine 500mg EC Tablets", "Pfizer", "Bone & Joints", 180m, 350, true, "DMARD for rheumatoid arthritis and ulcerative colitis."),
                ("Humira 40mg Injection (Adalimumab)", "Abbott", "Bone & Joints", 85000m, 15, true, "TNF-alpha inhibitor biologic for RA, psoriasis and Crohn's."),
                ("Xeljanz 5mg (Tofacitinib)", "Pfizer", "Bone & Joints", 32000m, 30, true, "JAK inhibitor for moderate to severe rheumatoid arthritis."),
                ("Colchicine 0.5mg Tablets", "Pharmalab", "Bone & Joints", 95m, 500, true, "Anti-inflammatory for acute gout attack and pericarditis."),
                ("Allopurinol 300mg Tablets", "Getz Pharma", "Bone & Joints", 65m, 700, true, "Xanthine oxidase inhibitor for chronic gout and hyperuricaemia."),
                ("Febuxostat 80mg Tablets", "Menarini", "Bone & Joints", 780m, 200, true, "Selective xanthine oxidase inhibitor for refractory gout."),
                ("Synvisc-One 6ml Injection", "Genzyme", "Bone & Joints", 28000m, 25, true, "Hyaluronic acid viscosupplement for knee osteoarthritis."),
                ("Etoricoxib 90mg Tablets", "MSD", "Bone & Joints", 680m, 250, true, "Selective COX-2 inhibitor for osteoarthritis and acute gout."),

                // ══════════════════════════════════════════
                // ANTIFUNGALS (15)
                // ══════════════════════════════════════════
                ("Diflucan 150mg (Fluconazole)", "Pfizer", "Antifungals", 680m, 400, true, "Single-dose oral antifungal for vaginal and oral candidiasis."),
                ("Nizoral 200mg (Ketoconazole)", "J&J", "Antifungals", 380m, 350, true, "Oral azole antifungal for dermatophyte and systemic fungal infections."),
                ("Sporanox 100mg (Itraconazole)", "J&J", "Antifungals", 1200m, 200, true, "Triazole antifungal for aspergillosis, onychomycosis and blastomycosis."),
                ("Vfend 200mg (Voriconazole)", "Pfizer", "Antifungals", 8500m, 60, true, "Broad-spectrum triazole for invasive aspergillosis and candidiasis."),
                ("Lamisil 250mg (Terbinafine)", "Novartis", "Antifungals", 680m, 300, true, "Allylamine for dermatophyte nail and skin fungal infections."),
                ("Canesten 500mg Pessary", "Bayer", "Antifungals", 380m, 350, false, "Single-dose clotrimazole pessary for vaginal thrush."),
                ("Terbinafine 1% Cream 15g", "Novartis", "Antifungals", 280m, 400, false, "Topical allylamine cream for athlete's foot and ringworm."),
                ("Amphotericin B 50mg Injection", "Bristol", "Antifungals", 3200m, 60, true, "Polyene macrolide for severe life-threatening systemic fungal infections."),
                ("Micafungin 50mg Injection", "Astellas", "Antifungals", 8500m, 40, true, "Echinocandin for invasive candidiasis in immunocompromised patients."),
                ("Nystatin 100000IU Oral Suspension", "Squibb", "Antifungals", 280m, 350, true, "Topical polyene for oral and intestinal candidiasis."),
                ("Clotrimazole 1% Cream 20g", "Bayer", "Antifungals", 180m, 500, false, "Azole antifungal cream for tinea corporis and candidal intertrigo."),
                ("Miconazole 2% Oral Gel 40g", "J&J", "Antifungals", 320m, 300, false, "Topical oral gel for oral thrush in adults and infants."),
                ("Ciclopirox 8% Nail Lacquer 3ml", "Stiefel", "Antifungals", 1800m, 120, true, "Antifungal nail lacquer for mild to moderate onychomycosis."),
                ("Econazole 1% Cream 30g", "Squibb", "Antifungals", 220m, 380, false, "Imidazole antifungal for tinea, candidal and cutaneous fungal infections."),
                ("Ketoconazole 2% Cream 30g", "J&J", "Antifungals", 280m, 320, false, "Topical azole for seborrhoeic dermatitis and tinea infections."),

                // ══════════════════════════════════════════
                // ANTIVIRALS (15)
                // ══════════════════════════════════════════
                ("Zovirax 200mg (Acyclovir)", "GSK", "Antivirals", 380m, 400, true, "Nucleoside analogue for herpes simplex and varicella-zoster infections."),
                ("Tamiflu 75mg (Oseltamivir)", "Roche", "Antivirals", 2800m, 150, true, "Neuraminidase inhibitor for influenza A and B treatment and prophylaxis."),
                ("Viread 300mg (Tenofovir DF)", "Gilead", "Antivirals", 1800m, 120, true, "Nucleotide reverse transcriptase inhibitor for HBV and HIV."),
                ("Harvoni (Sofosbuvir/Ledipasvir)", "Gilead", "Antivirals", 85000m, 20, true, "Once-daily oral regimen for genotype 1 chronic hepatitis C."),
                ("Sovaldi 400mg (Sofosbuvir)", "Gilead", "Antivirals", 52000m, 20, true, "NS5B inhibitor for chronic hepatitis C in combination therapy."),
                ("Valtrex 1000mg (Valacyclovir)", "GSK", "Antivirals", 1200m, 200, true, "Oral prodrug of acyclovir for herpes zoster and genital herpes."),
                ("Famvir 500mg (Famciclovir)", "Novartis", "Antivirals", 1800m, 150, true, "Antiviral for herpes zoster and recurrent genital herpes."),
                ("Ribavirin 200mg Capsules", "Roche", "Antivirals", 380m, 200, true, "Nucleoside analogue used in HCV and RSV combination therapy."),
                ("Pegasys 180mcg Injection", "Roche", "Antivirals", 28000m, 25, true, "Pegylated interferon alfa-2a for chronic hepatitis B and C."),
                ("Nexavar 200mg (Sorafenib)", "Bayer", "Antivirals", 32000m, 20, true, "Multikinase inhibitor for hepatocellular and renal cell carcinoma."),
                ("Zovirax 5% Cream 2g", "GSK", "Antivirals", 320m, 300, false, "Topical acyclovir cream for herpes labialis (cold sores)."),
                ("Valganciclovir 450mg Tablets", "Roche", "Antivirals", 12000m, 40, true, "Oral prodrug of ganciclovir for CMV retinitis prophylaxis."),
                ("Oseltamivir 30mg/5ml Suspension", "Roche", "Antivirals", 2400m, 80, true, "Paediatric oseltamivir suspension for influenza treatment."),
                ("Acyclovir 400mg Tablets", "GSK", "Antivirals", 280m, 350, true, "Maintenance dose acyclovir for recurrent herpes suppression."),
                ("Entecavir 0.5mg Tablets", "Bristol", "Antivirals", 2800m, 100, true, "Nucleoside analogue for treatment-naive chronic hepatitis B."),

                // ══════════════════════════════════════════
                // HORMONES & THYROID (15)
                // ══════════════════════════════════════════
                ("Eltroxin 50mcg (Levothyroxine)", "GSK", "Hormones", 95m, 700, true, "Synthetic T4 for hypothyroidism and thyroid cancer suppression."),
                ("Thyrofort 100mcg (Levothyroxine)", "Getz Pharma", "Hormones", 110m, 600, true, "Synthetic thyroxine replacement for primary hypothyroidism."),
                ("Carbimazole 5mg Tablets", "Roche", "Hormones", 120m, 400, true, "Antithyroid agent for hyperthyroidism and Graves' disease."),
                ("Neomercazole 5mg (Carbimazole)", "Roche", "Hormones", 140m, 380, true, "Thionamide inhibiting thyroid hormone synthesis."),
                ("Hydrocortisone 20mg Tablets", "Pfizer", "Hormones", 95m, 500, true, "Glucocorticoid for adrenal insufficiency and anti-inflammatory therapy."),
                ("Prednisolone 5mg Tablets", "Sanofi", "Hormones", 75m, 800, true, "Corticosteroid for adrenal suppression and severe inflammation."),
                ("Dexamethasone 0.5mg Tablets", "MSD", "Hormones", 55m, 900, true, "Potent glucocorticoid for severe allergic reactions and cerebral oedema."),
                ("Methylprednisolone 4mg Tablets", "Pfizer", "Hormones", 180m, 400, true, "Intermediate-acting corticosteroid for autoimmune and inflammatory conditions."),
                ("Testosterone Enanthate 250mg Injection", "Schering", "Hormones", 680m, 150, true, "Androgen replacement for male hypogonadism and delayed puberty."),
                ("Somatropin 4IU Injection (HGH)", "Pfizer", "Hormones", 12000m, 40, true, "Recombinant growth hormone for GH deficiency in children and adults."),
                ("DHEA 50mg Capsules", "Pharmalab", "Hormones", 680m, 180, false, "Dehydroepiandrosterone for adrenal insufficiency and age-related decline."),
                ("Medroxyprogesterone 10mg Tabs", "Pfizer", "Hormones", 120m, 400, true, "Progestogen for secondary amenorrhoea and abnormal uterine bleeding."),
                ("Anastrozole 1mg Tablets", "AstraZeneca", "Hormones", 1800m, 150, true, "Aromatase inhibitor for hormone receptor-positive breast cancer."),
                ("Tamoxifen 20mg Tablets", "AstraZeneca", "Hormones", 180m, 300, true, "Selective oestrogen receptor modulator for breast cancer."),
                ("Calcitonin 200IU Nasal Spray", "Novartis", "Hormones", 2200m, 100, true, "Salmon calcitonin for Paget's disease and osteoporotic fractures."),

                // ══════════════════════════════════════════
                // ORAL CARE (10)
                // ══════════════════════════════════════════
                ("Corsodyl 0.2% Mouthwash 300ml", "GSK", "Oral Care", 380m, 350, false, "Chlorhexidine antiseptic mouthwash for gingivitis and periodontitis."),
                ("Colgate Total Sensitive Toothpaste", "Colgate", "Oral Care", 280m, 500, false, "Fluoride toothpaste for sensitive teeth and cavity protection."),
                ("Oraldene 0.1% Mouthwash 200ml", "J&J", "Oral Care", 320m, 300, false, "Hexetidine antiseptic for mouth infections and sore gums."),
                ("Paradontax Bleeding Gums Paste", "GSK", "Oral Care", 480m, 250, false, "Herbal toothpaste reducing bleeding gums and plaque."),
                ("Difflam Spray 30ml (Benzydamine)", "Reckitt", "Oral Care", 580m, 200, false, "Topical anti-inflammatory spray for mouth ulcers and sore throat."),
                ("Bonjela Teething Gel 15g", "Reckitt", "Oral Care", 320m, 350, false, "Choline salicylate gel for infant teething pain and mouth ulcers."),
                ("Orajel Toothache Gel 5.3g", "Church&Dwight", "Oral Care", 380m, 280, false, "Benzocaine topical anaesthetic gel for rapid toothache relief."),
                ("Chlorhexidine Gel 1% 50g", "GSK", "Oral Care", 280m, 300, true, "Antiseptic gel for gum disease, oral surgery and denture hygiene."),
                ("Anbesol Liquid 10ml", "Pfizer", "Oral Care", 280m, 300, false, "Antiseptic and anaesthetic liquid for mouth ulcers and denture pain."),
                ("Listerine Total Care Mouthwash", "J&J", "Oral Care", 420m, 400, false, "Antibacterial mouthwash for total oral health protection."),

                // ══════════════════════════════════════════
                // CONTRACEPTIVES (10)
                // ══════════════════════════════════════════
                ("Microgynon 30 (EE+LNG)", "Bayer", "Contraceptives", 380m, 400, true, "Combined oral contraceptive pill for reliable pregnancy prevention."),
                ("Cerazette 75mcg (Desogestrel)", "Organon", "Contraceptives", 480m, 350, true, "Progestogen-only mini-pill for contraception in breastfeeding women."),
                ("Jadelle Implant (Levonorgestrel)", "Bayer", "Contraceptives", 8500m, 50, true, "5-year subdermal implant for long-acting reversible contraception."),
                ("Copper IUD 380A (Nova-T)", "Bayer", "Contraceptives", 3200m, 80, true, "10-year copper intrauterine device for non-hormonal contraception."),
                ("Mirena IUS 52mg (LNG)", "Bayer", "Contraceptives", 18000m, 40, true, "5-year levonorgestrel-releasing IUS for contraception and menorrhagia."),
                ("NorPlant-2 Implant Rods", "Organon", "Contraceptives", 6500m, 60, true, "3-year subdermal implant releasing levonorgestrel continuously."),
                ("Depo-Provera 150mg Injection", "Pfizer", "Contraceptives", 580m, 200, true, "3-monthly medroxyprogesterone injection for contraception."),
                ("Postinor-2 (Levonorgestrel 0.75mg)", "Gedeon", "Contraceptives", 480m, 300, true, "Emergency contraceptive pill for use within 72 hours."),
                ("Noristerat 200mg Injection", "Schering", "Contraceptives", 680m, 150, true, "2-monthly norethisterone enanthate contraceptive injection."),
                ("Spermicide Gel 80g", "Pharmalab", "Contraceptives", 280m, 200, false, "Nonoxynol-9 topical spermicide for additional contraception."),

                // ══════════════════════════════════════════
                // NUTRITION (15)
                // ══════════════════════════════════════════
                ("Ensure Plus Nutrition 400g", "Abbott", "Nutrition", 2400m, 250, false, "Complete balanced nutrition powder for malnutrition and recovery."),
                ("Pediasure 400g (Vanilla)", "Abbott", "Nutrition", 2800m, 200, false, "Complete paediatric nutrition for fussy eaters and growth support."),
                ("Glucerna SR 400g", "Abbott", "Nutrition", 3200m, 180, false, "Diabetes-specific nutrition formula with slow-release carbohydrates."),
                ("Fresubin Energy 200ml Pack", "Fresenius", "Nutrition", 480m, 150, false, "Enteral nutrition for patients requiring high-calorie supplements."),
                ("Nepro HP 220ml Can", "Abbott", "Nutrition", 780m, 120, false, "High-protein nutrition for chronic kidney disease patients on dialysis."),
                ("Pedalyte ORS Sachet", "Abbott", "Nutrition", 95m, 800, false, "WHO formula oral rehydration salts for fluid and electrolyte replacement."),
                ("Whey Protein Isolate 1kg", "Isopure", "Nutrition", 4800m, 100, false, "Zero-carb whey protein for muscle recovery and post-surgical nutrition."),
                ("MCT Oil 500ml", "Now Foods", "Nutrition", 2800m, 80, false, "Medium-chain triglyceride oil for ketogenic diet and energy support."),
                ("L-Glutamine 500g Powder", "Now Foods", "Nutrition", 2200m, 100, false, "Amino acid supplement for gut health and immune function."),
                ("Creatine Monohydrate 500g", "GNC", "Nutrition", 3200m, 80, false, "Performance supplement for muscle strength and high-intensity exercise."),
                ("Dextrose Monohydrate 500g", "Now Foods", "Nutrition", 680m, 150, false, "Fast-absorbing carbohydrate for intravenous and oral use."),
                ("BCAA 5000mg Capsules 120s", "Now Foods", "Nutrition", 1800m, 120, false, "Branched-chain amino acids for muscle preservation and recovery."),
                ("Collagen Peptides Powder 300g", "Vital Proteins", "Nutrition", 3800m, 100, false, "Hydrolysed collagen for joint health, skin elasticity and bone strength."),
                ("Spirulina 500mg Tablets 200s", "Now Foods", "Nutrition", 980m, 150, false, "Algal superfood rich in protein, vitamins and antioxidants."),
                ("Elderberry Syrup 120ml", "Sambucol", "Nutrition", 1800m, 120, false, "Black elderberry extract for immune support and cold symptom relief."),

                // ══════════════════════════════════════════
                // FIRST AID (15)
                // ══════════════════════════════════════════
                ("Betadine 10% Antiseptic Solution 100ml", "Mundipharma", "First Aid", 280m, 500, false, "Povidone-iodine solution for wound cleansing and skin antisepsis."),
                ("Savlon Antiseptic Cream 30g", "J&J", "First Aid", 120m, 600, false, "Chlorhexidine + cetrimide cream for wound care and infection prevention."),
                ("Burnol Cream 30g", "Smith Kline", "First Aid", 95m, 500, false, "Silver sulfadiazine cream for minor burns and sunburn relief."),
                ("Micropore Surgical Tape 1.25cm", "3M", "First Aid", 180m, 400, false, "Hypoallergenic paper tape for securing dressings and tubes."),
                ("Elastoplast Assorted Plasters 20s", "Bayer", "First Aid", 280m, 350, false, "Sterile adhesive wound dressings in assorted sizes."),
                ("Melolin Non-Adherent Dressing 10x10cm", "Smith&Nephew", "First Aid", 95m, 400, false, "Low-adherent wound dressing for clean and lightly exuding wounds."),
                ("Activated Charcoal 250mg Capsules", "Now Foods", "First Aid", 380m, 200, false, "Adsorbent for oral poisoning, drug overdose and flatulence."),
                ("Hydrogen Peroxide 3% 100ml", "Martindale", "First Aid", 65m, 600, false, "Wound irrigation and antiseptic solution for first aid use."),
                ("Sterile Gauze Swabs 10x10cm Box", "Vernacare", "First Aid", 220m, 400, false, "Sterile woven gauze for wound packing, dressing and cleansing."),
                ("Triangular Bandage", "Leukoplast", "First Aid", 120m, 300, false, "Multipurpose bandage for arm slings and first aid immobilisation."),
                ("Thermometer Digital", "Omron", "First Aid", 680m, 200, false, "Digital thermometer for fast oral, rectal or axillary temperature reading."),
                ("Pulse Oximeter Fingertip", "Omron", "First Aid", 2800m, 150, false, "Non-invasive fingertip device measuring blood oxygen saturation."),
                ("Adhesive Wound Closure Strips 72s", "3M", "First Aid", 280m, 300, false, "Steri-strip closures for clean lacerations avoiding sutures."),
                ("Antiseptic Wipes Pouch 100s", "Dettol", "First Aid", 180m, 500, false, "Pre-moistened chlorhexidine wipes for skin and wound antisepsis."),
                ("Cold/Hot Gel Pack Reusable", "Theratherm", "First Aid", 380m, 200, false, "Flexible gel pack for cold or heat therapy of injuries and pain."),

                // ══════════════════════════════════════════
                // BLOOD (10)
                // ══════════════════════════════════════════
                ("Clexane 40mg (Enoxaparin)", "Sanofi", "Blood", 480m, 300, true, "LMWH for DVT prophylaxis and treatment of thromboembolic disorders."),
                ("Heparin 5000IU/ml Injection", "Pfizer", "Blood", 320m, 200, true, "Unfractionated heparin for anticoagulation in thrombosis."),
                ("Ferrum Hausmann Injection 5ml", "Vifor", "Blood", 780m, 200, true, "IV iron dextran for severe iron deficiency anaemia refractory to oral iron."),
                ("Erythropoietin 4000IU Injection", "Roche", "Blood", 4800m, 60, true, "ESA for anaemia of chronic kidney disease and chemotherapy."),
                ("Filgrastim 300mcg Injection", "Amgen", "Blood", 8500m, 40, true, "G-CSF for neutropenia after chemotherapy and bone marrow transplant."),
                ("Tranexamic Acid 500mg IV", "Pfizer", "Blood", 380m, 200, true, "Antifibrinolytic for haemorrhage in trauma and surgery."),
                ("Phytomenadione 10mg (Vitamin K)", "Roche", "Blood", 180m, 300, true, "Vitamin K1 injection for reversal of anticoagulant excess and haemorrhage."),
                ("Desferrioxamine 500mg Injection", "Novartis", "Blood", 1200m, 80, true, "Iron chelator for acute iron poisoning and chronic iron overload."),
                ("Hydroxycarbamide 500mg Capsules", "Bristol", "Blood", 380m, 150, true, "Cytoreductive agent for sickle cell disease and myeloproliferative disorders."),
                ("Deferoxamine 2g Injection", "Novartis", "Blood", 1800m, 60, true, "Chelation therapy for transfusional haemosiderosis and aluminium overload."),

                // ══════════════════════════════════════════
                // ONCOLOGY (15) — completing 500
                // ══════════════════════════════════════════
                ("Tamoxifen 20mg Tablets", "AstraZeneca", "Oncology", 180m, 300, true, "Oestrogen receptor modulator for hormone-sensitive breast cancer."),
                ("Capecitabine 500mg Tablets", "Roche", "Oncology", 6800m, 60, true, "Oral fluoropyrimidine for colorectal and breast cancer chemotherapy."),
                ("Imatinib 400mg Tablets", "Novartis", "Oncology", 28000m, 30, true, "Tyrosine kinase inhibitor for CML and gastrointestinal stromal tumours."),
                ("Rituximab 500mg Injection", "Roche", "Oncology", 85000m, 15, true, "Anti-CD20 monoclonal antibody for NHL and chronic lymphocytic leukaemia."),
                ("Trastuzumab 440mg Injection", "Roche", "Oncology", 125000m, 10, true, "HER2-targeted monoclonal antibody for HER2-positive breast cancer."),
                ("Bevacizumab 400mg Injection", "Roche", "Oncology", 95000m, 12, true, "VEGF inhibitor for colorectal, lung and renal cell carcinoma."),
                ("Carboplatin 450mg Injection", "Bristol", "Oncology", 3800m, 40, true, "Platinum-based chemotherapy for ovarian and lung cancers."),
                ("Paclitaxel 300mg Injection", "Bristol", "Oncology", 5800m, 30, true, "Taxane chemotherapy for breast, lung and ovarian cancers."),
                ("Gemcitabine 1g Injection", "Eli Lilly", "Oncology", 4200m, 35, true, "Nucleoside analogue for pancreatic, lung and bladder cancers."),
                ("Docetaxel 80mg Injection", "Sanofi", "Oncology", 8500m, 20, true, "Taxane for breast, prostate and gastric cancer chemotherapy."),
                ("Methotrexate 50mg Injection", "Pfizer", "Oncology", 480m, 80, true, "Antimetabolite for ALL, choriocarcinoma and high-dose regimens."),
                ("Ondansetron 8mg Tablets", "GSK", "Oncology", 380m, 300, true, "5-HT3 antagonist for prevention of chemotherapy-induced nausea."),
                ("Dexamethasone 8mg Injection", "MSD", "Oncology", 180m, 250, true, "High-dose steroid for cerebral oedema and chemo anti-emesis protocol."),
                ("Filgrastim 480mcg Injection", "Amgen", "Oncology", 12000m, 30, true, "G-CSF for severe neutropenia after intensive chemotherapy."),
                ("Mesna 400mg Injection", "Baxter", "Oncology", 680m, 80, true, "Uroprotectant preventing haemorrhagic cystitis with ifosfamide/cyclophosphamide."),
            };

            int imgIdx = 0;
            int added = 0;

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
                    added++;
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} new medicines across 20 categories. Total medicines in DB: {Total}",
                added, await dbContext.Medicines.CountAsync());
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
                            ApprovalStatus = "Approved", // Approved for patient portal visibility
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
                        user.ApprovalStatus = "Approved";
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
