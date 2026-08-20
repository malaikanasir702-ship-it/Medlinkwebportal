using MedLinkPortal.Models;
using MedLinkPortal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MedLinkPortal.BackgroundServices
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(IServiceProvider serviceProvider, ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing reminders.");
                }

                try
                {
                    // Check every minute
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected when the application is shutting down.
                    break;
                }
            }

            _logger.LogInformation("Notification Background Service is stopping.");
        }

        private async Task ProcessRemindersAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Find appointments happening in exactly 15 minutes (approx)
                // We'll look for appointments starting between 14 and 16 minutes from now
                var startTime = DateTime.Now.AddMinutes(14);
                var endTime = DateTime.Now.AddMinutes(16);

                var upcomingAppointments = await context.Appointments
                    .Include(a => a.Doctor)
                    .Where(a => a.Status == "Confirmed")
                    .Where(a => a.AppointmentDate >= startTime && a.AppointmentDate <= endTime)
                    .ToListAsync();

                foreach (var appt in upcomingAppointments)
                {
                    // 1. Notify Patient
                    if (!string.IsNullOrEmpty(appt.UserId))
                    {
                        await notificationService.NotifyUserAsync(
                            appt.UserId,
                            NotificationType.AppointmentReminder,
                            "Upcoming Appointment Reminder",
                            $"Your consultation with {appt.Doctor?.Name ?? "your doctor"} starts in 15 minutes.",
                            "clock",
                            "amber"
                        );
                    }

                    // 2. Notify Doctor
                    if (appt.Doctor != null && !string.IsNullOrEmpty(appt.Doctor.UserId))
                    {
                        await notificationService.NotifyUserAsync(
                            appt.Doctor.UserId,
                            NotificationType.AppointmentReminder,
                            "Session Starting Soon",
                            $"Your session with {appt.PatientName} starts in 15 minutes.",
                            "clock",
                            "blue"
                        );
                    }
                }
            }
        }
    }
}
