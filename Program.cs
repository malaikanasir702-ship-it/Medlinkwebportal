using MedLinkPortal.Models;
using MedLinkPortal.Areas.Identity.Pages.Account;
using MedLinkPortal.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Stripe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.ResponseCompression;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// QuestPDF License Configuration
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "image/svg+xml",
        "application/wasm"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.AddResponseCaching();

// CORS for Mobile App
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobilePolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Database Context
// Railway injects DATABASE_URL as a full Postgres connection string.
// Supabase direct connection string is in appsettings.json as fallback.
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Npgsql requires key=value format; Railway provides postgres:// URI — convert if needed
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={Uri.UnescapeDataString(userInfo[1])};SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;Connection Idle Lifetime=300";
}

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
           .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Register the regular DbContext for scoped use (controllers, services)
builder.Services.AddScoped<ApplicationDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

// Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.AddAuthentication(options =>
{
    // Default schemes are set by Identity, so we only add JwtBearer for API use.
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, // Relaxed for easier development connectivity
        ValidateAudience = false, // Relaxed for easier development connectivity
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? "MedLink-Mobile-Super-Secret-Key-2026-Security-Node")),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && (path.Value.Contains("Hub")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Google OAuth — only add if credentials are configured (Railway env vars or appsettings)
var googleClientId     = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
                      ?? builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
                      ?? builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId)
    && !string.IsNullOrWhiteSpace(googleClientSecret)
    && googleClientId != "YOUR_GOOGLE_CLIENT_ID")
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId     = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

// Email Configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddSingleton<MedLinkPortal.Services.IEncryptionService, MedLinkPortal.Services.AesEncryptionService>();

// Gemini AI Service
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("GeminiAI", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.AcceptEncoding.Clear();
    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("identity");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MedLinkPortal/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.None
});
// MedLinkAI (FastAPI → Ollama): default 100s HttpClient timeout breaks long streamed LLM responses; use dedicated client
builder.Services.AddHttpClient("MedLinkAI", client =>
{
    client.Timeout = TimeSpan.FromHours(24);
});
builder.Services.AddScoped<MedLinkPortal.Services.IAiChatService, MedLinkPortal.Services.AiChatService>();
builder.Services.AddScoped<MedLinkPortal.Services.INotificationService, MedLinkPortal.Services.NotificationService>();
builder.Services.AddScoped<MedLinkPortal.Services.INeuralReportService, MedLinkPortal.Services.NeuralReportService>();
builder.Services.AddScoped<MedLinkPortal.Services.GeofenceService>();
builder.Services.AddScoped<MedLinkPortal.Services.GoogleDirectionsService>();
builder.Services.AddScoped<MedLinkPortal.Services.TrackingAuditService>();
builder.Services.AddHostedService<MedLinkPortal.BackgroundServices.NotificationBackgroundService>();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Stripe Configuration
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Firebase Admin SDK Initialization
var firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT");
if (!string.IsNullOrEmpty(firebaseJson))
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromJson(firebaseJson)
    });
}
else if (System.IO.File.Exists(Path.Combine(builder.Environment.ContentRootPath, "firebase-service-account.json")))
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromFile(
            Path.Combine(builder.Environment.ContentRootPath, "firebase-service-account.json"))
    });
}

var app = builder.Build();

// Forwarded Headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Global JSON exception handler for all API routes (prevents raw 500 HTML on crashes)
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        if (!context.Response.HasStarted && context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "An unexpected error occurred. Please try again.",
                error = app.Environment.IsDevelopment() ? ex.Message : null
            });
        }
        else if (!context.Response.HasStarted)
        {
            throw; // Let the MVC exception handler deal with non-API routes
        }
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var headers = context.Context.Response.Headers;
        headers.CacheControl = app.Environment.IsDevelopment()
            ? "no-cache"
            : "public,max-age=31536000,immutable";
    }
});
app.UseRouting();
app.UseResponseCaching();
app.UseCors("MobilePolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseSessionTracking();
app.UseSession();

app.MapControllers();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ── Health check — Railway uses this to know the app is ready ─────────────
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapHub<MedLinkPortal.Hubs.ConsultationHub>("/consultationHub");
app.MapHub<MedLinkPortal.Hubs.ChatHub>("/chatHub");
app.MapHub<MedLinkPortal.Hubs.NotificationHub>("/notificationHub");
app.MapHub<MedLinkPortal.Hubs.TrackingHub>("/trackingHub");

// Seed Roles and Admin User (Optional)
using (var scope = app.Services.CreateScope())
{
    // Ensure Database Schema is up to date
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    dbContext.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Create roles if they don't exist
    string[] roleNames = { "Admin", "Doctor", "Patient", "Pharmacist", "LabAdmin", "Rider" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Seed Admin
    var adminEmail = "admin@medlink.com";
    var adminPassword = "Admin@123";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FirstName = "Admin", LastName = "User", EmailConfirmed = true, PhoneNumber = "+1234567890" };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded) await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // Seed Lab Admin
    var labAdminEmail = "labadmin@medlink.com";
    var labAdminPassword = "LabAdmin@123";
    if (await userManager.FindByEmailAsync(labAdminEmail) == null)
    {
        var labAdminUser = new ApplicationUser { UserName = labAdminEmail, Email = labAdminEmail, FirstName = "Lab", LastName = "Manager", EmailConfirmed = true, PhoneNumber = "+1122334466" };
        var labResult = await userManager.CreateAsync(labAdminUser, labAdminPassword);
        if (labResult.Succeeded) await userManager.AddToRoleAsync(labAdminUser, "LabAdmin");
    }

    // Seed Doctor
    var doctorEmail = "doctor@medlink.com";
    var doctorPassword = "Doctor@123";
    if (await userManager.FindByEmailAsync(doctorEmail) == null)
    {
        var doctorUser = new ApplicationUser { UserName = doctorEmail, Email = doctorEmail, FirstName = "Doctor", LastName = "Who", EmailConfirmed = true, PhoneNumber = "+1987654321", Specialist = "General Practitioner", IsAvailable = true };
        var docResult = await userManager.CreateAsync(doctorUser, doctorPassword);
        if (docResult.Succeeded) await userManager.AddToRoleAsync(doctorUser, "Doctor");
    }

    // Seed Pharmacist
    var pharmacistEmail = "pharmacist@medlink.com";
    var pharmacistPassword = "Pharmacist@123";
    if (await userManager.FindByEmailAsync(pharmacistEmail) == null)
    {
        var pharmacistUser = new ApplicationUser { UserName = pharmacistEmail, Email = pharmacistEmail, FirstName = "Main", LastName = "Pharmacist", EmailConfirmed = true, PhoneNumber = "+1122334455" };
        var pharmResult = await userManager.CreateAsync(pharmacistUser, pharmacistPassword);
        if (pharmResult.Succeeded) await userManager.AddToRoleAsync(pharmacistUser, "Pharmacist");
    }

    // Seed Medicines
    if (!dbContext.Medicines.Any())
    {
        dbContext.Medicines.AddRange(new List<Medicine>
        {
            new Medicine { Name = "Panadol", Brand = "GSK", Category = "Painkiller", Price = 50, StockQuantity = 1000, ExpiryDate = DateTime.Now.AddYears(2), PrescriptionRequired = false },
            new Medicine { Name = "Augmentin", Brand = "GSK", Category = "Antibiotic", Price = 450, StockQuantity = 200, ExpiryDate = DateTime.Now.AddYears(1), PrescriptionRequired = true },
            new Medicine { Name = "Arinac", Brand = "Abbott", Category = "Flu/Cold", Price = 80, StockQuantity = 500, ExpiryDate = DateTime.Now.AddYears(2), PrescriptionRequired = false },
            new Medicine { Name = "Lisinopril", Brand = "Pfizer", Category = "Blood Pressure", Price = 300, StockQuantity = 150, ExpiryDate = DateTime.Now.AddYears(1), PrescriptionRequired = true }
        });
        await dbContext.SaveChangesAsync();
    }
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
if (!app.Environment.IsDevelopment())
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();
