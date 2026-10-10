using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;
using MedLinkPortal.Areas.Admin.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace MedLinkPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BillingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var billings = await _context.AdminBillings
                .Include(b => b.Patient)
                .OrderByDescending(b => b.DateGenerated)
                .ToListAsync();

            ViewBag.Patients = await _context.AdminPatients.ToListAsync();
            return View(billings);
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.AdminBillings
                .Include(b => b.Patient)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (invoice == null)
                return NotFound();

            var viewModel = new InvoiceDetailsViewModel
            {
                Invoice = invoice,
                Patient = invoice.Patient,
                MedicalRecords = await _context.AdminMedicalRecords
                    .Where(m => m.PatientId == invoice.PatientId)
                    .Include(m => m.Physician)
                    .OrderByDescending(m => m.DateCreated)
                    .ToListAsync(),
                RecentAppointments = await _context.AdminAppointments
                    .Where(a => a.PatientId == invoice.PatientId)
                    .Include(a => a.Physician)
                    .OrderByDescending(a => a.AppointmentTime)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Billing billing)
        {
            if (ModelState.IsValid)
            {
                billing.DateGenerated = DateTime.Now;
                _context.Add(billing);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var bill = await _context.AdminBillings.FindAsync(id);
            if (bill != null)
            {
                bill.Status = status;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // ======================= PDF + QR =======================
        public async Task<IActionResult> PrintInvoicePdf(int id)
        {
            var invoice = await _context.AdminBillings
                .Include(b => b.Patient)
                .FirstOrDefaultAsync(b => b.Id == id); // End update

            if (invoice == null)
                return NotFound();

            // QR DATA (Invoice Details Page)
            string qrData = $"{Request.Scheme}://{Request.Host}/Admin/Billing/Details/{invoice.Id}";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20);

            // Fetch additional data for comprehensive export
            var medicalRecords = await _context.AdminMedicalRecords
                .Where(m => m.PatientId == invoice.PatientId)
                .Include(m => m.Physician)
                .OrderByDescending(m => m.DateCreated)
                .ToListAsync();

            var recentAppointments = await _context.AdminAppointments
                .Where(a => a.PatientId == invoice.PatientId)
                .Include(a => a.Physician)
                .OrderByDescending(a => a.AppointmentTime)
                .Take(5)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // ================= HEADER =================
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("MEDLINK UNIFIED").FontSize(22).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Financial Verification & Clinical Summary")
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken2)
                                .SemiBold();
                        });

                        row.ConstantItem(200).AlignRight().Column(col =>
                        {
                            col.Item().Text($"Invoice #: INV-{invoice.Id}").Bold();
                            col.Item().Text($"Date: {invoice.DateGenerated:dd MMM yyyy}");
                            col.Item().Text($"Status: {invoice.Status}").FontColor(invoice.Status == "PAID" ? Colors.Green.Medium : Colors.Amber.Medium);
                        });
                    });

                    // ================= CONTENT =================
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Spacing(20);

                        // -------- SUMMARY BOX --------
                        col.Item().Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.Grey.Lighten5)
                            .Padding(15)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Billed To").Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
                                    c.Item().Text(invoice.Patient.Name).FontSize(14).Bold();
                                    c.Item().Text($"Resident ID: {invoice.Patient.Id}").FontSize(9);
                                    c.Item().Text($"Phone: {invoice.Patient.Phone}").FontSize(9);
                                });

                                row.ConstantItem(180).Column(c =>
                                {
                                    c.Item().Text("Total Amount").Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
                                    c.Item().Text($"PKR {invoice.Amount:N2}")
                                        .FontSize(18)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken2);
                                });
                            });

                        // -------- CLINICAL ENCOUNTERS --------
                        if (medicalRecords.Any())
                        {
                            col.Item().Column(c =>
                            {
                                c.Item().PaddingBottom(10).Text("Clinical Encounters").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                                
                                foreach (var record in medicalRecords)
                                {
                                    c.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(8).Row(r =>
                                    {
                                        r.RelativeItem().Column(rc =>
                                        {
                                            rc.Item().Text(record.Title).Bold();
                                            rc.Item().Text(record.Content).Italic().FontSize(9).FontColor(Colors.Grey.Darken2);
                                            if (record.Physician != null)
                                                rc.Item().Text($"Physician: Dr. {record.Physician.Name}").FontSize(8).SemiBold();
                                        });
                                        r.ConstantItem(80).AlignRight().Text(record.DateCreated.ToString("dd MMM yyyy")).FontSize(9).FontColor(Colors.Grey.Darken1);
                                    });
                                }
                            });
                        }

                        // -------- VISIT TIMELINE --------
                        if (recentAppointments.Any())
                        {
                            col.Item().Column(c =>
                            {
                                c.Item().PaddingBottom(10).Text("Recent Visit Timeline").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                                
                                foreach (var appt in recentAppointments)
                                {
                                    c.Item().PaddingVertical(4).Row(r =>
                                    {
                                        r.ConstantItem(100).Text(appt.AppointmentTime.ToString("MMM dd, yyyy")).FontSize(9).Bold();
                                        r.RelativeItem().Text($"{appt.Reason} (w/ Dr. {appt.Physician?.Name ?? "Staff"})").FontSize(9);
                                    });
                                }
                            });
                        }

                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // -------- QR VERIFICATION --------
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Spacing(6);
                                c.Item().Text("Record Verification")
                                    .FontSize(12)
                                    .Bold();
                                c.Item().Text("This document is a certified copy of the medical financial record. Scan the QR code to verify the authenticity of this invoice on the secure MedLink portal.").FontSize(9).FontColor(Colors.Grey.Darken2);
                            });

                            row.ConstantItem(150).AlignCenter().Column(c =>
                            {
                                c.Item()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5)
                                    .AlignCenter()
                                    .Width(100)
                                    .Height(100)
                                    .Image(qrBytes);
                                
                                c.Item().PaddingTop(4).Text("SCAN TO VERIFY")
                                    .FontSize(8)
                                    .Bold()
                                    .AlignCenter()
                                    .FontColor(Colors.Grey.Darken3);
                            });
                        });
                    });

                    // ================= FOOTER =================
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Generated by HealthPortal System").FontSize(9);
                    });
                });
            });

            var pdfBytes = pdf.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Invoice_{invoice.Id}.pdf");
        }
    }
}
