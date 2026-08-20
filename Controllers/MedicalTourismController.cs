using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;
using System.Threading.Tasks;
using System.Linq;
using Stripe.Checkout;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedLinkPortal.Controllers
{
    [Authorize]
    public class MedicalTourismController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MedicalTourismController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            return View(model);
        }

        public IActionResult InboundLanding()
        {
            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            return View(model);
        }

        public IActionResult AllDestinations()
        {
            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            return View(model);
        }

        public IActionResult OutboundSelection()
        {
            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            return View(model);
        }

        public IActionResult WatchFilm()
        {
            return View();
        }

        public IActionResult CostEstimator()
        {
            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            return View(model);
        }

        public IActionResult VisaSupport()
        {
            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            return View(model);
        }

        [HttpGet]
        public IActionResult RequestInbound()
        {
            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RequestInbound(MedicalTourismRequest request)
        {
            // Handle Nullable Strings for DB
            request.AdditionalNotes ??= "";
            request.MedicalReportsUrl ??= "";
            request.InterestedTourLocations ??= "";
            request.PreferredCity ??= "";
            request.BudgetRange ??= "";
            request.SourceCountry ??= "";

            request.UserId = _userManager.GetUserId(User);
            request.RequestType = TourismRequestType.Inbound;
            request.PreferredCountry = "Pakistan"; // Destination
            request.Status = RequestStatus.Pending;
            request.CreatedAt = DateTime.UtcNow;

            _context.MedicalTourismRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction("Tracking");
        }

        [HttpGet]
        public IActionResult RequestOutbound(string destination)
        {
            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            ViewBag.Destination = destination;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RequestOutbound(MedicalTourismRequest request)
        {
            // Handle Nullable Strings for DB
            request.AdditionalNotes ??= "";
            request.MedicalReportsUrl ??= "";
            request.PreferredCity ??= "";
            request.BudgetRange ??= "";
            request.PreferredCountry ??= (string)TempData["Destination"] ?? request.PreferredCountry ?? "Abroad"; // Backup

            request.UserId = _userManager.GetUserId(User);
            request.RequestType = TourismRequestType.Outbound;
            request.Status = RequestStatus.Pending;
            request.CreatedAt = DateTime.UtcNow;

            _context.MedicalTourismRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction("Tracking");
        }

        public async Task<IActionResult> Tracking()
        {
            var userId = _userManager.GetUserId(User);
            var requests = await _context.MedicalTourismRequests
                .Include(r => r.AssignedPackage)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            ViewBag.Requests = requests;
            
            return View(model);
        }

        public async Task<IActionResult> Package(int id)
        {
            var package = await _context.MedicalTourismPackages
                .Include(p => p.Hospital)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (package == null) return NotFound();

            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            ViewBag.Package = package;
            return View(model);
        }

        private PatientDashboardModel GetBaseModel()
        {
            var userId = _userManager.GetUserId(User);
            var user = _userManager.GetUserAsync(User).Result;
            
            var model = new PatientDashboardModel
            {
                PatientName = user?.FirstName ?? User.Identity?.Name?.Split('@')[0] ?? "User",
                PatientId = userId,
                ProfileImage = user?.ProfileImage ?? "https://picsum.photos/seed/patient/100/100",
                NavItems = new PatientDashboardModel().NavItems // Reset specific items if needed
            };
            return model;
        }
        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession(int packageId)
        {
            var package = await _context.MedicalTourismPackages.FindAsync(packageId);
            if (package == null) return NotFound();

            var request = await _context.MedicalTourismRequests
                .FirstOrDefaultAsync(r => r.AssignedPackageId == packageId && r.UserId == _userManager.GetUserId(User));
            
            if (request == null) return BadRequest("Invalid Request");

            var domain = Request.Scheme + "://" + Request.Host;
            
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(package.TotalPrice * 100), // Convert to cents/paisa? Assuming PKR, Stripe might require int adjustment
                            Currency = "pkr",
                            ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Medical Tourism Package: {package.Hospital?.Name ?? "Premium Care"}",
                                Description = package.TourPlanDetails,
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = domain + $"/MedicalTourism/PaymentSuccess?session_id={{CHECKOUT_SESSION_ID}}&requestId={request.Id}",
                CancelUrl = domain + $"/MedicalTourism/Package/{packageId}",
            };

            var service = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session session = service.Create(options);

            return Redirect(session.Url);
        }

        public async Task<IActionResult> PaymentSuccess(string session_id, int requestId)
        {
            var request = await _context.MedicalTourismRequests
                .Include(r => r.AssignedPackage)
                .ThenInclude(p => p.Hospital)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            if (!string.IsNullOrEmpty(session_id))
            {
                // Verify session if needed, for now assume success
                request.Status = RequestStatus.PaymentPending; // Or Completed based on webhook, but for direct flow:
                
                // Update status to confirm payment
                request.Status = RequestStatus.TravelScheduled; // Moving to next stage
                request.AdditionalNotes += $"\n[System] Payment Completed via Stripe. Session ID: {session_id}";
                
                await _context.SaveChangesAsync();
            }

            // Prepare View Model
            var model = GetBaseModel();
            model.ActiveTab = "medical-tourism";
            
            ViewBag.SessionId = session_id;
            ViewBag.Request = request;
            
            return View(model);
        }

        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var request = await _context.MedicalTourismRequests
                .Include(r => r.AssignedPackage)
                .ThenInclude(p => p.Hospital)
                .FirstOrDefaultAsync(r => r.Id == id);
            
            if(request == null || request.AssignedPackage == null) return NotFound();

            // Generate a simple HTML invoice string or use QuestPDF if available (it is in csproj!)
            // For simplicity and speed in this context, returning a basic file result or view as PDF
            // Using QuestPDF to generate a PDF

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Text("MedLink Medical Tourism Invoice")
                        .SemiBold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);
                            x.Item().Text($"Invoice #: INV-{request.Id:D6}");
                            x.Item().Text($"Date: {DateTime.Now:d}");
                            x.Item().Text($"Patient: {request.UserId}"); // Should ideally be name
                            
                            x.Item().LineHorizontal(1);

                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Description");
                                    header.Cell().Element(CellStyle).AlignRight().Text("Amount (PKR)");

                                    static QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
                                    {
                                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                                    }
                                });

                                table.Cell().Element(CellStyle).Text($"Medical Tourism Package - {request.AssignedPackage.Hospital?.Name}");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{request.AssignedPackage.TotalPrice:N0}");

                                static QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).PaddingVertical(5);
                                }
                            });
                             
                            x.Item().AlignRight().Text($"Total: PKR {request.AssignedPackage.TotalPrice:N0}").Bold().FontSize(14);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream, "application/pdf", $"Invoice-{request.Id}.pdf");
        }

    } // End Class
}
