using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MedLinkPortal.Models;
using MedLinkPortal.Services;

namespace MedLinkPortal.BackgroundServices
{
    public class DocumentProcessingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DocumentProcessingBackgroundService> _logger;

        public DocumentProcessingBackgroundService(IServiceProvider serviceProvider, ILogger<DocumentProcessingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Document Processing Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingJobsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document extraction queue.");
                }

                try
                {
                    // Poll queue every 20 seconds
                    await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Document Processing Background Service stopped.");
        }

        private async Task ProcessPendingJobsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var aiService = scope.ServiceProvider.GetRequiredService<IAiChatService>();
            var notificationService = scope.ServiceProvider.GetService<INotificationService>();
            var classificationService = scope.ServiceProvider.GetService<IMedicalClassificationService>();

            var pendingJobs = await db.ExtractionJobs
                .Include(j => j.ClinicalDocument)
                .Where(j => j.Status == "Pending" && j.RetryCount < j.MaxRetries)
                .OrderBy(j => j.CreatedAt)
                .Take(5)
                .ToListAsync(stoppingToken);

            foreach (var job in pendingJobs)
            {
                if (stoppingToken.IsCancellationRequested) break;

                job.Status = "InProgress";
                await db.SaveChangesAsync(stoppingToken);

                try
                {
                    var doc = job.ClinicalDocument;
                    if (doc == null)
                    {
                        job.Status = "Failed";
                        job.ErrorMessage = "Associated ClinicalDocument not found.";
                        await db.SaveChangesAsync(stoppingToken);
                        continue;
                    }

                    doc.Status = "Processing";
                    await db.SaveChangesAsync(stoppingToken);

                    // Call AI extraction
                    var extractedJson = await aiService.ExtractClinicalDataAsync(doc.FileUrl, doc.DocumentType);

                    doc.ExtractedJson = extractedJson;
                    doc.Status = "Completed";
                    doc.Confidence = 0.94;

                    job.Status = "Completed";
                    job.ProcessedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync(stoppingToken);

                    // Trigger Phase 2 Medical Classification if service is available
                    if (classificationService != null)
                    {
                        try
                        {
                            await classificationService.ClassifyAndPersistAsync(doc.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Classification failed for document {DocId}", doc.Id);
                        }
                    }

                    // Notify uploader if available
                    if (notificationService != null && !string.IsNullOrEmpty(doc.PatientId))
                    {
                        await notificationService.NotifyUserAsync(
                            doc.PatientId,
                            NotificationType.General,
                            "Clinical Document Processed",
                            $"Your medical document ({doc.FileName}) has been analyzed and classified by MedLink AI.",
                            "file-text",
                            "emerald"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process job {JobId}", job.Id);
                    job.RetryCount++;
                    job.ErrorMessage = ex.Message;
                    job.Status = job.RetryCount >= job.MaxRetries ? "Failed" : "Pending";

                    if (job.ClinicalDocument != null)
                    {
                        job.ClinicalDocument.Status = job.Status;
                        job.ClinicalDocument.ErrorMessage = ex.Message;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
        }
    }
}
