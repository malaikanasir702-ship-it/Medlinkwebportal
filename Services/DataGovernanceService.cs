using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;

namespace MedLinkPortal.Services
{
    public class DataGovernanceService : IDataGovernanceService
    {
        private readonly ApplicationDbContext _db;

        public DataGovernanceService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAccessAsync(string? userId, string role, string? patientUserId, string resource, string? recordId, string accessType, string? ipAddress, string? purpose)
        {
            var log = new DataAccessLog
            {
                AccessedByUserId = userId,
                AccessedByRole = role,
                PatientUserId = patientUserId,
                ResourceName = resource,
                RecordId = recordId,
                AccessType = accessType,
                IpAddress = ipAddress,
                Purpose = purpose ?? "Clinical Access",
                Timestamp = DateTime.UtcNow
            };

            _db.DataAccessLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> CheckConsentAsync(string patientUserId, string consentType)
        {
            var consent = await _db.ConsentRecords
                .Where(c => c.PatientUserId == patientUserId && c.ConsentType == consentType)
                .OrderByDescending(c => c.GrantedAt)
                .FirstOrDefaultAsync();

            return consent?.IsGranted ?? true; // Default opt-in with ability to revoke
        }

        public async Task<ConsentRecord> RecordConsentAsync(string patientUserId, string consentType, bool isGranted, string? ipAddress)
        {
            var existing = await _db.ConsentRecords
                .Where(c => c.PatientUserId == patientUserId && c.ConsentType == consentType)
                .OrderByDescending(c => c.GrantedAt)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.IsGranted = isGranted;
                if (!isGranted) existing.RevokedAt = DateTime.UtcNow;
                else existing.RevokedAt = null;
                existing.IpAddress = ipAddress;
                await _db.SaveChangesAsync();
                return existing;
            }

            var record = new ConsentRecord
            {
                PatientUserId = patientUserId,
                ConsentType = consentType,
                IsGranted = isGranted,
                GrantedAt = DateTime.UtcNow,
                RevokedAt = isGranted ? null : DateTime.UtcNow,
                ConsentVersion = "1.0",
                IpAddress = ipAddress
            };

            _db.ConsentRecords.Add(record);
            await _db.SaveChangesAsync();
            return record;
        }

        public async Task<List<ConsentRecord>> GetPatientConsentsAsync(string patientUserId)
        {
            return await _db.ConsentRecords
                .Where(c => c.PatientUserId == patientUserId)
                .OrderByDescending(c => c.GrantedAt)
                .ToListAsync();
        }

        public async Task<List<DataAccessLog>> GetAccessLogsAsync(int limit = 100)
        {
            return await _db.DataAccessLogs
                .Include(l => l.AccessedByUser)
                .Include(l => l.PatientUser)
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<DataAccessLog>> GetPatientAccessAuditTrailAsync(string patientUserId)
        {
            return await _db.DataAccessLogs
                .Include(l => l.AccessedByUser)
                .Where(l => l.PatientUserId == patientUserId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
