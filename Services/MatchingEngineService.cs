using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public class MatchingEngineService : IMatchingEngineService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAiChatService _aiService;
        private readonly ICredentialingService _credentialingService;

        public MatchingEngineService(
            ApplicationDbContext db,
            IAiChatService aiService,
            ICredentialingService credentialingService)
        {
            _db = db;
            _aiService = aiService;
            _credentialingService = credentialingService;
        }

        public async Task<List<DoctorMatchResult>> FindBestDoctorsAsync(string? patientUserId, string symptoms, string urgency, string? location = null)
        {
            var doctors = await _db.Doctors.ToListAsync();
            var results = new List<DoctorMatchResult>();

            // Keywords to match specialties
            var symLower = (symptoms ?? string.Empty).ToLower();
            string targetedSpecialty = DetectTargetSpecialty(symLower);

            foreach (var doc in doctors)
            {
                double score = 0.0;
                var highlights = new List<string>();

                // 1. Specialty match (0-40)
                var docSpec = doc.Specialty?.ToLower() ?? "";
                if (docSpec.Contains(targetedSpecialty.ToLower()) || targetedSpecialty.ToLower().Contains(docSpec))
                {
                    score += 40.0;
                    highlights.Add($"Primary Specialty Alignment: {doc.Specialty}");
                }
                else if (docSpec.Contains("general physician"))
                {
                    score += 25.0; // General Physician fallback
                    highlights.Add("Primary Care Triaging Capability");
                }
                else
                {
                    score += 10.0;
                }

                // 2. Credential Tier & Case History match (0-25)
                var credProfile = await _db.PhysicianCredentialProfiles
                    .FirstOrDefaultAsync(p => p.DoctorUserId == doc.UserId);

                string tier = credProfile?.TierLevel ?? "Mid";
                if (tier == "Elite") { score += 25.0; highlights.Add("Elite Tier Credentialed Specialist"); }
                else if (tier == "Expert") { score += 22.0; highlights.Add("Expert Tier Specialist (>2,000 cases)"); }
                else if (tier == "Senior") { score += 18.0; highlights.Add("Senior Physician with verified volume"); }
                else if (tier == "Mid") { score += 14.0; }
                else { score += 10.0; }

                // 3. Availability score (0-15)
                if (doc.Online || (doc.Availability != null && doc.Availability.Contains("Today", StringComparison.OrdinalIgnoreCase)))
                {
                    score += 15.0;
                    highlights.Add("Available for Immediate Consultation");
                }
                else
                {
                    score += 8.0;
                }

                // 4. Rating score (0-10)
                var ratingPoints = Math.Min(10.0, (doc.Rating / 5.0) * 10.0);
                score += ratingPoints;
                if (doc.Rating >= 4.8) highlights.Add($"Top Rated: {doc.Rating} Stars");

                // 5. Urgency weighting adjustments (0-10)
                if (urgency.Equals("Emergency", StringComparison.OrdinalIgnoreCase) || urgency.Equals("Urgent", StringComparison.OrdinalIgnoreCase))
                {
                    if (doc.Online) score += 10.0;
                }
                else
                {
                    score += 7.0;
                }

                score = Math.Min(100.0, Math.Round(score, 1));

                // Generate reasoning
                var reason = await _aiService.GenerateMatchReasoningAsync(doc.Name, doc.Specialty, symptoms, urgency);

                results.Add(new DoctorMatchResult
                {
                    DoctorId = doc.Id,
                    DoctorUserId = doc.UserId ?? "",
                    DoctorName = doc.Name,
                    Specialty = doc.Specialty,
                    Image = doc.Image,
                    MatchScore = score,
                    MatchReason = reason,
                    CredentialTier = tier,
                    Rating = doc.Rating,
                    ReviewsCount = doc.Reviews,
                    ConsultationFee = 1500,
                    IsAvailable = doc.Online,
                    MatchingHighlights = highlights
                });
            }

            var sortedResults = results.OrderByDescending(r => r.MatchScore).Take(5).ToList();

            // Store Matching Request asynchronously in background
            try
            {
                await RecordMatchingRequestAsync(patientUserId, symptoms, urgency, sortedResults);
            }
            catch { }

            return sortedResults;
        }

        public async Task<List<EmployerMatchResult>> MatchDoctorToEmployersAsync(string doctorUserId)
        {
            var profile = await _credentialingService.GetOrCreateProfileAsync(doctorUserId);
            var requirements = await _db.EmployerRequirements
                .Where(r => r.IsActive)
                .ToListAsync();

            // Seed default requirements if table is empty
            if (!requirements.Any())
            {
                requirements = GetDefaultEmployerRequirements();
                _db.EmployerRequirements.AddRange(requirements);
                await _db.SaveChangesAsync();
            }

            var results = new List<EmployerMatchResult>();

            foreach (var req in requirements)
            {
                double score = 0.0;
                var gaps = new List<string>();
                var matches = new List<string>();

                // 1. Specialty match (0-30)
                if (profile.PrimarySpecialty.Contains(req.RequiredSpecialty, StringComparison.OrdinalIgnoreCase) ||
                    req.RequiredSpecialty.Contains(profile.PrimarySpecialty, StringComparison.OrdinalIgnoreCase))
                {
                    score += 30.0;
                    matches.Add($"Specialty matched: {req.RequiredSpecialty}");
                }
                else
                {
                    score += 5.0;
                    gaps.Add($"Specialty mismatch: Requires {req.RequiredSpecialty} (Current: {profile.PrimarySpecialty})");
                }

                // 2. Experience & Case volume match (0-25)
                if (profile.TotalCasesProcessed >= req.MinCasesCount)
                {
                    score += 25.0;
                    matches.Add($"Volume requirement met: {profile.TotalCasesProcessed} cases logged (Min: {req.MinCasesCount})");
                }
                else
                {
                    var partial = (double)profile.TotalCasesProcessed / req.MinCasesCount * 25.0;
                    score += Math.Max(5.0, partial);
                    gaps.Add($"Requires {req.MinCasesCount - profile.TotalCasesProcessed} more logged cases for full benchmark");
                }

                // 3. Procedure match (0-25)
                if (profile.TotalProceduresPerformed >= 50)
                {
                    score += 25.0;
                    matches.Add($"Procedural proficiency satisfied: {profile.TotalProceduresPerformed} procedures verified");
                }
                else
                {
                    score += 15.0;
                    gaps.Add("Additional advanced interventional procedure logs required");
                }

                // 4. Certification & Licensure match (0-20)
                if (profile.PmcVerified)
                {
                    score += 20.0;
                    matches.Add("PMC Official Medical Council Verification verified");
                }
                else
                {
                    score += 5.0;
                    gaps.Add("Active PMC Licensing verification needed");
                }

                score = Math.Min(100.0, Math.Round(score, 1));

                string recommendation = score >= 85
                    ? "Exceptional candidate. Fully eligible for sponsored appointment and credentialing fast-track."
                    : score >= 65
                    ? "Strong candidate profile. Address indicated procedural or documentation gaps to finalize eligibility."
                    : "Developmental candidate. Accumulate additional surgical/clinical volume for international requirements.";

                results.Add(new EmployerMatchResult
                {
                    EmployerId = req.Id,
                    HospitalName = req.HospitalName,
                    Country = req.Country,
                    RequiredSpecialty = req.RequiredSpecialty,
                    MatchScore = score,
                    SalaryRange = req.SalaryRange ?? "Competitive",
                    VisaSponsorship = req.VisaSponsorship,
                    Gaps = gaps,
                    Matches = matches,
                    Recommendation = recommendation
                });
            }

            return results.OrderByDescending(r => r.MatchScore).ToList();
        }

        public async Task<MatchingRequest> RecordMatchingRequestAsync(string? patientUserId, string symptoms, string urgency, List<DoctorMatchResult> results)
        {
            var req = new MatchingRequest
            {
                PatientUserId = patientUserId,
                Symptoms = symptoms,
                Urgency = urgency,
                CreatedDate = DateTime.UtcNow,
                Status = "Completed",
                ResultsJson = JsonSerializer.Serialize(results)
            };

            _db.MatchingRequests.Add(req);
            await _db.SaveChangesAsync();
            return req;
        }

        public async Task<List<MatchingRequest>> GetRecentMatchesAsync(int limit = 50)
        {
            return await _db.MatchingRequests
                .Include(r => r.PatientUser)
                .OrderByDescending(r => r.CreatedDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<EmployerRequirement>> GetEmployerRequirementsAsync()
        {
            return await _db.EmployerRequirements
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<EmployerRequirement> AddEmployerRequirementAsync(EmployerRequirement requirement)
        {
            _db.EmployerRequirements.Add(requirement);
            await _db.SaveChangesAsync();
            return requirement;
        }

        public async Task<bool> DeleteEmployerRequirementAsync(int id)
        {
            var req = await _db.EmployerRequirements.FindAsync(id);
            if (req == null) return false;

            _db.EmployerRequirements.Remove(req);
            await _db.SaveChangesAsync();
            return true;
        }

        private static string DetectTargetSpecialty(string sym)
        {
            if (sym.Contains("heart") || sym.Contains("chest") || sym.Contains("cardiac") || sym.Contains("palpitation"))
                return "Cardiologist";
            if (sym.Contains("skin") || sym.Contains("rash") || sym.Contains("acne") || sym.Contains("itching"))
                return "Dermatologist";
            if (sym.Contains("brain") || sym.Contains("headache") || sym.Contains("migraine") || sym.Contains("nerve") || sym.Contains("seizure"))
                return "Neurologist";
            if (sym.Contains("stomach") || sym.Contains("gastric") || sym.Contains("acid") || sym.Contains("liver") || sym.Contains("digestion"))
                return "Gastroenterologist";
            if (sym.Contains("child") || sym.Contains("baby") || sym.Contains("infant") || sym.Contains("pediatric"))
                return "Pediatrician";
            if (sym.Contains("bone") || sym.Contains("joint") || sym.Contains("fracture") || sym.Contains("knee") || sym.Contains("back pain"))
                return "Orthopedic Surgeon";
            if (sym.Contains("ear") || sym.Contains("nose") || sym.Contains("throat") || sym.Contains("sinus"))
                return "Otolaryngologist (ENT Specialist)";
            if (sym.Contains("eye") || sym.Contains("vision") || sym.Contains("cataract"))
                return "Ophthalmologist";
            if (sym.Contains("kidney") || sym.Contains("urine") || sym.Contains("bladder"))
                return "Urologist";
            if (sym.Contains("depress") || sym.Contains("anxiety") || sym.Contains("mental") || sym.Contains("stress"))
                return "Psychiatrist";
            if (sym.Contains("tooth") || sym.Contains("teeth") || sym.Contains("gum") || sym.Contains("dental"))
                return "Dentist";

            return "General Physician";
        }

        private static List<EmployerRequirement> GetDefaultEmployerRequirements()
        {
            return new List<EmployerRequirement>
            {
                new EmployerRequirement
                {
                    HospitalName = "King's College Hospital NHS Trust",
                    Country = "United Kingdom",
                    RequiredSpecialty = "Cardiologist",
                    MinExperienceYears = 7,
                    MinCasesCount = 1000,
                    SalaryRange = "£95,000 - £125,000 / yr",
                    VisaSponsorship = true,
                    IsActive = true
                },
                new EmployerRequirement
                {
                    HospitalName = "Cleveland Clinic Abu Dhabi",
                    Country = "United Arab Emirates",
                    RequiredSpecialty = "Dermatologist",
                    MinExperienceYears = 5,
                    MinCasesCount = 800,
                    SalaryRange = "AED 55,000 - 75,000 / mo",
                    VisaSponsorship = true,
                    IsActive = true
                },
                new EmployerRequirement
                {
                    HospitalName = "Toronto General Hospital",
                    Country = "Canada",
                    RequiredSpecialty = "Neurologist",
                    MinExperienceYears = 8,
                    MinCasesCount = 1200,
                    SalaryRange = "$260,000 - $340,000 CAD / yr",
                    VisaSponsorship = true,
                    IsActive = true
                },
                new EmployerRequirement
                {
                    HospitalName = "Singapore General Hospital",
                    Country = "Singapore",
                    RequiredSpecialty = "General Physician",
                    MinExperienceYears = 5,
                    MinCasesCount = 600,
                    SalaryRange = "SGD 18,000 - 24,000 / mo",
                    VisaSponsorship = true,
                    IsActive = true
                }
            };
        }
    }
}
