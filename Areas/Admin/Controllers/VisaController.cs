using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using MedLinkPortal.Models;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VisaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ICloudinaryService _cloudinaryService;

        public VisaController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _cloudinaryService = cloudinaryService;
        }

        // GET: All visa applications
        public async Task<IActionResult> Index()
        {
            var apps = await _context.VisaApplications
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            ViewBag.PendingCount = apps.Count(a => a.Status < VisaStatus.VisaApproved && a.Status != VisaStatus.VisaRejected && a.Status != VisaStatus.Cancelled);
            ViewBag.ApprovedCount = apps.Count(a => a.Status == VisaStatus.VisaApproved);

            return View(apps);
        }

        // GET: Application details
        public async Task<IActionResult> Details(int id)
        {
            var app = await _context.VisaApplications
                .Include(v => v.TourismRequest)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (app == null) return NotFound();

            var user = await _userManager.FindByIdAsync(app.UserId);
            ViewBag.PatientEmail = user?.Email ?? "Unknown";
            ViewBag.PatientName = user?.FullName ?? "Unknown";
            ViewBag.PatientPhone = user?.PhoneNumber ?? "—";

            return View(app);
        }

        // POST: Update status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, VisaStatus status, string? adminNotes, string? invitationLetterUrl)
        {
            var app = await _context.VisaApplications.FindAsync(id);
            if (app == null) return NotFound();

            app.Status = status;
            app.UpdatedAt = DateTime.UtcNow;
            app.ProcessedBy = User.Identity?.Name ?? "Admin";
            app.ProcessedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(adminNotes))
                app.AdminNotes = adminNotes;

            if (!string.IsNullOrWhiteSpace(invitationLetterUrl))
                app.InvitationLetterUrl = invitationLetterUrl;

            await _context.SaveChangesAsync();

            // Notify patient
            var statusLabel = status switch
            {
                VisaStatus.DocumentsUnderReview => "Your visa documents are under review.",
                VisaStatus.InvitationIssued => "Your hospital invitation letter has been issued! Download it from Visa Support.",
                VisaStatus.EmbassySubmitted => "Your application has been submitted to the embassy.",
                VisaStatus.VisaApproved => "Congratulations! Your medical visa has been approved. Welcome to Pakistan!",
                VisaStatus.VisaRejected => "Unfortunately, your visa application was not approved. Please check admin notes for details.",
                _ => $"Your visa application status has been updated to: {status}."
            };

            await _notificationService.CreateAndSendNotificationAsync(
                app.UserId,
                "Visa Application Update",
                $"Ref: {app.ReferenceId} — {statusLabel}",
                "passport",
                status == VisaStatus.VisaApproved ? "emerald" :
                status == VisaStatus.VisaRejected ? "rose" : "indigo"
            );

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Generate invitation letter PDF
        [HttpPost]
        public async Task<IActionResult> GenerateInvitationLetter(int id)
        {
            var app = await _context.VisaApplications
                .Include(v => v.TourismRequest)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (app == null) return NotFound();

            // Get assigned hospital if available
            string hospitalName = "MedLink Partner Hospital";
            if (app.TourismRequest?.AssignedPackageId.HasValue == true)
            {
                var pkg = await _context.MedicalTourismPackages
                    .Include(p => p.Hospital)
                    .FirstOrDefaultAsync(p => p.Id == app.TourismRequest.AssignedPackageId);
                if (pkg?.Hospital != null)
                    hospitalName = pkg.Hospital.Name;
            }
            else if (!string.IsNullOrEmpty(app.HospitalName))
            {
                hospitalName = app.HospitalName;
            }

            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("MEDLINK HEALTH PLATFORM")
                                    .FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                row.ConstantItem(100).Text($"Ref: {app.ReferenceId}")
                                    .FontSize(9).FontColor(Colors.Grey.Medium).AlignRight();
                            });
                            col.Item().PaddingTop(4).Text($"{hospitalName} — Medical Tourism Division")
                                .FontSize(10).FontColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Blue.Lighten3);
                        });

                    page.Content().PaddingTop(20).Column(col =>
                    {
                        col.Spacing(14);

                        col.Item().Text($"Date: {DateTime.Now:MMMM dd, yyyy}").FontSize(10).FontColor(Colors.Grey.Medium);

                        col.Item().PaddingTop(6).Text("TO WHOM IT MAY CONCERN").Bold().FontSize(12);

                        col.Item().Text(text =>
                        {
                            text.Span("Subject: ").Bold();
                            text.Span($"Medical Visa Invitation for {app.FullName} — {app.TreatmentType}");
                        });

                        col.Item().PaddingTop(8).Text($@"We, {hospitalName}, a licensed medical facility operating under MedLink Health Platform, hereby extend this formal invitation to:").FontSize(11);

                        // Patient info box
                        col.Item().Border(1).BorderColor(Colors.Blue.Lighten3).Padding(12).Column(inner =>
                        {
                            inner.Item().Text($"Full Name:          {app.FullName}").Bold();
                            inner.Item().Text($"Nationality:         {app.Nationality}");
                            inner.Item().Text($"Passport No.:      {app.PassportNumber}");
                            if (app.PassportExpiry.HasValue)
                                inner.Item().Text($"Passport Expiry:  {app.PassportExpiry.Value:MMMM dd, yyyy}");
                            inner.Item().Text($"Treatment Type:   {app.TreatmentType}");
                            if (app.PlannedArrivalDate.HasValue)
                                inner.Item().Text($"Planned Arrival:  {app.PlannedArrivalDate.Value:MMMM dd, yyyy}");
                        });

                        col.Item().PaddingTop(6).Text($@"The above-named individual is required to travel to Pakistan for medical treatment at our facility. All necessary arrangements for the patient's treatment, accommodation, and post-procedure care will be coordinated by MedLink Health Platform.

We kindly request the concerned embassy to grant a Medical Visa to the applicant for the purpose of receiving medical treatment in Pakistan. We assure you that the patient will adhere to all visa regulations and will return to their home country upon completion of treatment.");

                        col.Item().PaddingTop(12).Text("Sincerely,").Italic();
                        col.Item().PaddingTop(4).Column(sig =>
                        {
                            sig.Item().Text("MedLink Health Platform").Bold().FontSize(12);
                            sig.Item().Text("Medical Tourism Coordination Unit").FontSize(10).FontColor(Colors.Grey.Medium);
                            sig.Item().Text("contact@medlink.pk  |  www.medlink.pk").FontSize(9).FontColor(Colors.Blue.Medium);
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("This letter is computer-generated and valid without signature. MedLink Reference: ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span(app.ReferenceId).FontSize(8).Bold().FontColor(Colors.Blue.Medium);
                    });
                });
            });

            var stream = new MemoryStream();
            pdf.GeneratePdf(stream);
            stream.Position = 0;

            // Upload to Cloudinary
            var formFile = new FormFile(stream, 0, stream.Length, "file", $"Invitation_{app.ReferenceId}.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var uploadedUrl = await _cloudinaryService.UploadRawFileAsync(formFile, "visa_invitations");

            if (!string.IsNullOrEmpty(uploadedUrl))
            {
                app.InvitationLetterUrl = uploadedUrl;
                app.Status = VisaStatus.InvitationIssued;
                app.UpdatedAt = DateTime.UtcNow;
                app.ProcessedBy = User.Identity?.Name ?? "Admin";
                await _context.SaveChangesAsync();

                // Notify patient
                await _notificationService.CreateAndSendNotificationAsync(
                    app.UserId,
                    "Invitation Letter Ready",
                    $"Ref: {app.ReferenceId} — Your hospital invitation letter has been issued. Download it from Visa Support.",
                    "file-text",
                    "indigo"
                );

                return Json(new { success = true, url = uploadedUrl });
            }

            // Fallback: return PDF directly
            stream.Position = 0;
            return File(stream, "application/pdf", $"Invitation_{app.ReferenceId}.pdf");
        }
    }
}
