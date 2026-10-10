using System;
using System.Threading.Tasks;
using MedLinkPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace MedLinkPortal.Services
{
    /// <summary>
    /// Persists audit events for all key rider tracking actions (Enhancement 14).
    /// Every important event is saved to TrackingAuditLogs for admin history viewing.
    /// Uses IDbContextFactory to avoid concurrent-context exceptions when called from
    /// parallel or SignalR hub contexts.
    /// </summary>
    public class TrackingAuditService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        // Event type constants
        public const string RiderCreated = "RiderCreated";
        public const string RiderAssigned = "RiderAssigned";
        public const string TrackingStarted = "TrackingStarted";
        public const string StatusChanged = "StatusChanged";
        public const string GeofenceTriggered = "GeofenceTriggered";
        public const string GPSSpoofDetected = "GPSSpoofDetected";
        public const string SessionTerminated = "SessionTerminated";
        public const string TrackingEnded = "TrackingEnded";

        public TrackingAuditService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Persists one audit log entry to the TrackingAuditLogs table.
        /// Each call creates and disposes its own DbContext to prevent concurrent-access errors.
        /// </summary>
        /// <param name="eventType">Event type constant (use TrackingAuditService.* constants)</param>
        /// <param name="actorId">UserId of the person who performed the action</param>
        /// <param name="targetId">OrderId or RiderId (as string)</param>
        /// <param name="targetType">"PharmacyOrder", "LabBooking", or "Rider"</param>
        /// <param name="oldValue">Previous status/value (nullable)</param>
        /// <param name="newValue">New status/value (nullable)</param>
        /// <param name="metadata">JSON string for extra context (nullable)</param>
        public async Task LogAsync(
            string eventType,
            string actorId,
            string targetId,
            string targetType,
            string? oldValue = null,
            string? newValue = null,
            string? metadata = null)
        {
            await using var context = _contextFactory.CreateDbContext();

            var log = new TrackingAuditLog
            {
                EventType = eventType,
                ActorId = actorId,
                TargetId = targetId,
                TargetType = targetType,
                OldValue = oldValue,
                NewValue = newValue,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow
            };

            context.TrackingAuditLogs.Add(log);
            await context.SaveChangesAsync();
        }
    }
}
