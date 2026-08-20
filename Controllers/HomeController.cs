  using MedLinkPortal.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;



namespace MedLinkPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly List<Doctor> _doctors;
        private readonly List<Department> _departments;
        private readonly List<Capability> _capabilities;
        private readonly List<FAQ> _faqs;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
            // Initialize sample data
            _doctors = new List<Doctor>
            {
                new Doctor
                {
                    Id = 1,
                    Name = "Dr. Elena Rodriguez",
                    Specialty = "Senior Cardiologist",
                    Rating = 4.9,
                    Reviews = 1240,
                    Image = "https://images.unsplash.com/photo-1559839734-2b71f1536785?auto=format&fit=crop&q=80&w=400&h=500",
                    Availability = "Available Today",
                    Online = true
                },
                new Doctor
                {
                    Id = 2,
                    Name = "Dr. James Wilson",
                    Specialty = "Neurology Specialist",
                    Rating = 4.8,
                    Reviews = 890,
                    Image = "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&q=80&w=400&h=500",
                    Availability = "Next: 2:00 PM",
                    Online = true
                },
                new Doctor
                {
                    Id = 3,
                    Name = "Dr. Sarah Chen",
                    Specialty = "Pediatric Expert",
                    Rating = 5.0,
                    Reviews = 2100,
                    Image = "https://images.unsplash.com/photo-1594824476967-48c8b964273f?auto=format&fit=crop&q=80&w=400&h=500",
                    Availability = "Available Tomorrow",
                    Online = false
                },
                new Doctor
                {
                    Id = 4,
                    Name = "Dr. Marcus Thorne",
                    Specialty = "Dermatologist",
                    Rating = 4.7,
                    Reviews = 560,
                    Image = "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&q=80&w=400&h=500",
                    Availability = "Available Today",
                    Online = true
                }
            };

            _departments = new List<Department>
            {
                new Department
                {
                    Id = 1,
                    Title = "Cardiology",
                    Icon = "Heart",
                    Description = "Advanced heart care including preventative screening and chronic management.",
                    Specialists = 42,
                    Color = "bg-rose-500",
                    Services = new List<string> { "Heart Rate Monitoring", "ECG Analysis", "Hypertension Control" }
                },
                new Department
                {
                    Id = 2,
                    Title = "Neurology",
                    Icon = "Brain",
                    Description = "Specialized diagnostics for brain health and complex neurological disorders.",
                    Specialists = 28,
                    Color = "bg-indigo-500",
                    Services = new List<string> { "Sleep Studies", "Migraine Clinic", "Memory Care" }
                },
                new Department
                {
                    Id = 3,
                    Title = "Pediatrics",
                    Icon = "Baby",
                    Description = "Dedicated care for infants and children with 24/7 emergency support.",
                    Specialists = 56,
                    Color = "bg-amber-500",
                    Services = new List<string> { "Vaccinations", "Growth Tracking", "Child Nutrition" }
                },
                new Department
                {
                    Id = 4,
                    Title = "Dermatology",
                    Icon = "Sparkles",
                    Description = "Expert skin, hair, and nail analysis with AI-assisted mole screening.",
                    Specialists = 19,
                    Color = "bg-emerald-500",
                    Services = new List<string> { "Acne Treatment", "Skin Cancer Triage", "Allergy Tests" }
                },
                new Department
                {
                    Id = 5,
                    Title = "Ophthalmology",
                    Icon = "Eye",
                    Description = "Vision care and surgical consultations using high-resolution video triage.",
                    Specialists = 15,
                    Color = "bg-sky-500",
                    Services = new List<string> { "Digital Eye Strain", "Vision Correction", "Glaucoma Check" }
                },
                new Department
                {
                    Id = 6,
                    Title = "Diagnostics",
                    Icon = "Microscope",
                    Description = "Full-scale lab result interpretation and integrated radiology review.",
                    Specialists = 34,
                    Color = "bg-violet-500",
                    Services = new List<string> { "Blood Work Analysis", "MRI Reviews", "Genomic Triage" }
                }
            };

            _capabilities = new List<Capability>
            {
                new Capability
                {
                    Title = "Symptom Triage",
                    Icon = "Brain",
                    Description = "Our neural networks process millions of medical journals to analyze your symptoms with 99.4% precision.",
                    Stats = "Instant Results"
                },
                new Capability
                {
                    Title = "Scan Analysis",
                    Icon = "Scan",
                    Description = "Advanced computer vision detects micro-anomalies in X-rays, MRIs, and CT scans that the human eye might miss.",
                    Stats = "0.2s Processing"
                },
                new Capability
                {
                    Title = "Vitals Prediction",
                    Icon = "LineChart",
                    Description = "Proprietary algorithms predict potential heart or respiratory events up to 48 hours before symptoms occur.",
                    Stats = "92% Accuracy"
                }
            };

            _faqs = new List<FAQ>
            {
                new FAQ
                {
                    Question = "How secure is my medical data?",
                    Answer = "We use military-grade 256-bit AES encryption for all data storage and transmission. MedLink is fully compliant with global regulations including HIPAA (USA), GDPR (EU), and PIPEDA (Canada). You have full control over who accesses your records."
                }
            };
        }

        public IActionResult Index()
        {
            // Use DB if data exists, otherwise fallback to local list
            var doctors = _context.Doctors.Any() ? _context.Doctors.ToList() : _doctors;
            var departments = _context.Departments.Any() ? _context.Departments.ToList() : _departments;
            var faqs = _context.FAQs.Any() ? _context.FAQs.OrderBy(f => f.DisplayOrder).ToList() : _faqs;

            ViewBag.Doctors = doctors.Take(4).ToList();
            ViewBag.Departments = departments;
            ViewBag.Capabilities = _capabilities;
            ViewBag.FAQs = faqs;

            return View();
        }

        public IActionResult DepartmentDetails(int id)
        {
            var departments = _context.Departments.Any() ? _context.Departments.ToList() : _departments;
            var dept = departments.FirstOrDefault(d => d.Id == id);
            
            if (dept == null) return RedirectToAction("Index");

            // Filter doctors by specialty matching the department title
            var allDoctors = _context.Doctors.Any() ? _context.Doctors.ToList() : _doctors;
            var deptDoctors = allDoctors.Where(d => d.Specialty.Contains(dept.Title, StringComparison.OrdinalIgnoreCase)).ToList();

            ViewBag.Department = dept;
            ViewBag.Specialists = deptDoctors;

            return View();
        }

        [HttpPost]
        public IActionResult BookAppointment(BookingModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Please fill all required fields." });
            }

            return Json(new
            {
                success = true,
                message = "Appointment booked successfully!",
                data = model
            });
        }

        [HttpGet]
        public IActionResult GetDoctor(int id)
        {
            var doctors = _context.Doctors.Any() ? _context.Doctors.ToList() : _doctors;
            var doctor = doctors.FirstOrDefault(d => d.Id == id);
            if (doctor == null)
                return Json(new { success = false });

            return Json(new { success = true, doctor });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSlotsForDoctor(int doctorId, string date)
        {
            if (!DateTime.TryParse(date, out var selectedDate))
            {
                return Json(new { success = false, message = "Invalid date" });
            }

            var dayOfWeek = selectedDate.DayOfWeek.ToString();
            
            // First, find the doctor to get their UserId (which is the string ID used in AvailabilitySlots)
            var doctorObj = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctorObj == null || string.IsNullOrEmpty(doctorObj.UserId))
            {
                return Json(new { success = true, slots = new List<object>() });
            }

            // Note: In a real system, we would also filter out slots that are already booked for this specific date
            var slots = await _context.DoctorAvailabilitySlots
                .Where(s => s.DoctorId == doctorObj.UserId && s.DayOfWeek == dayOfWeek && s.IsActive)
                .OrderBy(s => s.StartTime)
                .Select(s => new {
                    id = s.Id,
                    time = s.StartTime.ToString(@"hh\:mm") + (s.StartTime.Hours >= 12 ? " PM" : " AM")
                })
                .ToListAsync();

            return Json(new { success = true, slots });
        }

        public IActionResult GPNow()
        {
            return View();
        }

        public IActionResult VaultSecurity()
        {
            return View();
        }

        public IActionResult LearnMore()
        {
            return View();
        }

        public IActionResult Offline()
        {
            return View();
        }
    }
}