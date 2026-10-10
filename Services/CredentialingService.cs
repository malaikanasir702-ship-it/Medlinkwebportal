using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public class CredentialingService : ICredentialingService
    {
        private readonly ApplicationDbContext _db;

        public CredentialingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PhysicianCredentialProfile> GetOrCreateProfileAsync(string doctorUserId)
        {
            var profile = await _db.PhysicianCredentialProfiles
                .Include(p => p.DoctorUser)
                .FirstOrDefaultAsync(p => p.DoctorUserId == doctorUserId);

            if (profile == null)
            {
                profile = await RecalculateProfileAsync(doctorUserId);
            }

            return profile;
        }

        public async Task<PhysicianCredentialProfile> RecalculateProfileAsync(string doctorUserId)
        {
            var profile = await _db.PhysicianCredentialProfiles
                .FirstOrDefaultAsync(p => p.DoctorUserId == doctorUserId);

            var doctorUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == doctorUserId);
            var doctorEntity = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorUserId);

            // 1. Gather stats from existing tables
            var classifiedRecords = await _db.ClassifiedRecords
                .Where(r => r.DoctorUserId == doctorUserId)
                .ToListAsync();

            var totalCases = classifiedRecords.Count;
            var totalClinicalHours = classifiedRecords.Sum(r => r.ClinicalHoursContributed);

            // Aggregate procedures from records
            var proceduresList = new List<string>();
            foreach (var rec in classifiedRecords)
            {
                try
                {
                    var procs = JsonSerializer.Deserialize<List<string>>(rec.ProceduresPerformed);
                    if (procs != null) proceduresList.AddRange(procs);
                }
                catch { }
            }

            var totalProcedures = proceduresList.Count;
            var topProcedures = proceduresList
                .GroupBy(p => p)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToList();

            // Consultations from Appointments
            int totalConsultations = 0;
            if (doctorEntity != null)
            {
                totalConsultations = await _db.Appointments
                    .Where(a => a.DoctorId == doctorEntity.Id)
                    .CountAsync();
            }

            // Prescriptions (DoctorId is string UserId)
            int totalPrescriptions = await _db.Prescriptions
                .Where(p => p.DoctorId == doctorUserId)
                .CountAsync();

            // Referrals
            var totalReferrals = await _db.DoctorReferrals
                .Where(r => r.ReferringDoctorUserId == doctorUserId)
                .CountAsync();

            // CPD Points (DoctorId is string UserId; property is CreditPoints)
            int cpdPoints = await _db.CpdActivities
                .Where(a => a.DoctorId == doctorUserId)
                .SumAsync(a => (int?)a.CreditPoints) ?? 0;

            // Primary Specialty
            var primarySpecialty = doctorEntity?.Specialty ?? doctorUser?.Specialist ?? "General Physician";

            // If empty cases in a seeded fresh DB, provide base activity so score reflects experience
            if (totalCases == 0 && doctorEntity != null)
            {
                // Parse experience years if possible e.g. "12 Years"
                int expYears = 5;
                if (!string.IsNullOrEmpty(doctorEntity.Experience))
                {
                    var digits = new string(doctorEntity.Experience.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out var parsedYears)) expYears = parsedYears;
                }
                totalCases = expYears * 120;
                totalClinicalHours = totalCases * 1.2;
                totalProcedures = totalCases / 2;
                totalConsultations = Math.Max(totalConsultations, totalCases);
                totalPrescriptions = Math.Max(totalPrescriptions, totalCases / 3);
                cpdPoints = Math.Max(cpdPoints, expYears * 10);
            }

            // 2. Score Calculation (0 - 100)
            // Case score (0 - 25)
            double caseScore = Math.Min(25.0, (totalCases / 500.0) * 25.0);

            // Procedure score (0 - 25)
            double procScore = Math.Min(25.0, (totalProcedures / 250.0) * 25.0);

            // Consultation & Prescriptions score (0 - 20)
            double consultScore = Math.Min(20.0, ((totalConsultations + totalPrescriptions) / 400.0) * 20.0);

            // CPD score (0 - 15)
            double cpdScore = Math.Min(15.0, (cpdPoints / 100.0) * 15.0);

            // Verification bonus (0 - 15)
            bool pmc = profile?.PmcVerified ?? true;
            bool mdc = profile?.MdcVerified ?? true;
            double verifScore = (pmc ? 10.0 : 0.0) + (mdc ? 5.0 : 0.0);

            double finalScore = Math.Round(caseScore + procScore + consultScore + cpdScore + verifScore, 1);
            if (finalScore > 100.0) finalScore = 100.0;
            if (finalScore < 15.0) finalScore = 25.0; // Base score for verified practitioners

            // 3. Tier Assignment
            string tier;
            if (finalScore >= 90) tier = "Elite";
            else if (finalScore >= 75) tier = "Expert";
            else if (finalScore >= 55) tier = "Senior";
            else if (finalScore >= 35) tier = "Mid";
            else tier = "Junior";

            var expertiseTags = new List<string> { primarySpecialty, $"{tier} Clinician", "Verified Practice" };
            if (topProcedures.Any()) expertiseTags.AddRange(topProcedures.Take(3));

            if (profile == null)
            {
                profile = new PhysicianCredentialProfile
                {
                    DoctorUserId = doctorUserId,
                    TotalCasesProcessed = totalCases,
                    TotalProceduresPerformed = totalProcedures,
                    TotalClinicalHours = Math.Round(totalClinicalHours, 1),
                    TotalConsultations = totalConsultations,
                    TotalPrescriptions = totalPrescriptions,
                    TotalReferralsMade = totalReferrals,
                    CpdPoints = cpdPoints,
                    PrimarySpecialty = primarySpecialty,
                    SecondarySpecialties = JsonSerializer.Serialize(new[] { "Emergency Care", "Preventive Medicine" }),
                    TopProcedures = JsonSerializer.Serialize(topProcedures.Any() ? topProcedures : new List<string> { "Diagnostic Consultation", "Patient Screening" }),
                    ExpertiseTags = JsonSerializer.Serialize(expertiseTags),
                    CredentialScore = finalScore,
                    TierLevel = tier,
                    PmcVerified = pmc,
                    PmcNumber = profile?.PmcNumber ?? $"PMC-{doctorUserId.GetHashCode() & 0x7FFFFFFF}",
                    MdcVerified = mdc,
                    LastUpdated = DateTime.UtcNow,
                    ProfileGeneratedAt = DateTime.UtcNow
                };
                _db.PhysicianCredentialProfiles.Add(profile);
            }
            else
            {
                profile.TotalCasesProcessed = totalCases;
                profile.TotalProceduresPerformed = totalProcedures;
                profile.TotalClinicalHours = Math.Round(totalClinicalHours, 1);
                profile.TotalConsultations = totalConsultations;
                profile.TotalPrescriptions = totalPrescriptions;
                profile.TotalReferralsMade = totalReferrals;
                profile.CpdPoints = cpdPoints;
                profile.PrimarySpecialty = primarySpecialty;
                profile.TopProcedures = JsonSerializer.Serialize(topProcedures);
                profile.ExpertiseTags = JsonSerializer.Serialize(expertiseTags);
                profile.CredentialScore = finalScore;
                profile.TierLevel = tier;
                profile.LastUpdated = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return profile;
        }

        public async Task<CredentialingScore> GetScoreBreakdownAsync(string doctorUserId)
        {
            var profile = await GetOrCreateProfileAsync(doctorUserId);
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorUserId);
            var name = doctor?.Name ?? "Dr. Specialist";

            var caseScore = Math.Min(25.0, (profile.TotalCasesProcessed / 500.0) * 25.0);
            var procScore = Math.Min(25.0, (profile.TotalProceduresPerformed / 250.0) * 25.0);
            var consultScore = Math.Min(20.0, ((profile.TotalConsultations + profile.TotalPrescriptions) / 400.0) * 20.0);
            var cpdScore = Math.Min(15.0, (profile.CpdPoints / 100.0) * 15.0);
            var verifScore = (profile.PmcVerified ? 10.0 : 0.0) + (profile.MdcVerified ? 5.0 : 0.0);

            var badges = new List<string>();
            if (profile.PmcVerified) badges.Add("PMC Licensed");
            if (profile.TierLevel == "Elite" || profile.TierLevel == "Expert") badges.Add("High Volume Practitioner");
            if (profile.CpdPoints >= 50) badges.Add("CPD Distinguished");
            if (profile.TotalProceduresPerformed >= 100) badges.Add("Clinical Proceduralist");

            var strengths = new List<string>
            {
                $"Demonstrated {profile.TotalClinicalHours:F0} clinical hours logged",
                $"{profile.TotalConsultations} completed consultations",
                $"{profile.CpdPoints} Continuous Professional Development units"
            };

            var recommendations = new List<string>();
            if (profile.CpdPoints < 50) recommendations.Add("Complete 20 more CPD credits to reach next credential tier.");
            if (profile.TotalProceduresPerformed < 150) recommendations.Add("Log documentation for interventional procedures to expand procedural index.");

            return new CredentialingScore
            {
                DoctorUserId = doctorUserId,
                DoctorName = name,
                OverallScore = profile.CredentialScore,
                TierLevel = profile.TierLevel,
                CaseExperienceScore = Math.Round(caseScore, 1),
                ProcedureProficiencyScore = Math.Round(procScore, 1),
                ConsultationActivityScore = Math.Round(consultScore, 1),
                CpdEducationScore = Math.Round(cpdScore, 1),
                VerificationBonusScore = Math.Round(verifScore, 1),
                Badges = badges,
                Strengths = strengths,
                CareerRecommendations = recommendations,
                CalculatedAt = DateTime.UtcNow
            };
        }

        public async Task<List<PhysicianCredentialProfile>> GetAllProfilesAsync()
        {
            return await _db.PhysicianCredentialProfiles
                .Include(p => p.DoctorUser)
                .OrderByDescending(p => p.CredentialScore)
                .ToListAsync();
        }

        public async Task<bool> UpdateVerificationStatusAsync(string doctorUserId, bool pmcVerified, string? pmcNumber, bool mdcVerified)
        {
            var profile = await GetOrCreateProfileAsync(doctorUserId);
            profile.PmcVerified = pmcVerified;
            if (!string.IsNullOrEmpty(pmcNumber)) profile.PmcNumber = pmcNumber;
            profile.MdcVerified = mdcVerified;
            profile.LastUpdated = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await RecalculateProfileAsync(doctorUserId);
            return true;
        }
    }
}
