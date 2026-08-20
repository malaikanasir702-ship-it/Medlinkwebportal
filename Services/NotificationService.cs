using MedLinkPortal.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MedLinkPortal.Areas.Identity.Pages.Account;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;

namespace MedLinkPortal.Services
{
    public class NotificationService : INotificationService
    {
        private readonly MedLinkPortal.Models.ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public NotificationService(
            MedLinkPortal.Models.ApplicationDbContext context,
            IHubContext<NotificationHub> hubContext,
            Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _hubContext = hubContext;
            _emailSender = emailSender;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task NotifyUserAsync(string userId, MedLinkPortal.Models.NotificationType type, string title, string content, string icon = "bell", string color = "blue", System.Collections.Generic.Dictionary<string, string>? data = null)
        {
            // 1. Create Internal Notification (for Dropdown)
            await CreateAndSendNotificationAsync(userId, title, content, icon, color);

            // 2. Fetch User for Preferences
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            // 3. Send Email Notification if enabled
            if (user.EmailNotificationsEnabled && !string.IsNullOrEmpty(user.Email))
            {
                _ = Task.Run(async () => {
                    try
                    {
                        string emailBody = $@"
                            <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                                <h2 style='color: #2563eb;'>MedLink Notification</h2>
                                <p><strong>{title}</strong></p>
                                <p>{content}</p>
                                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                                <p style='font-size: 12px; color: #888;'>This is an automated message from MedLink Patient Portal.</p>
                            </div>";
                        await _emailSender.SendEmailAsync(user.Email, $"MedLink: {title}", emailBody);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email Notification Error: {ex.Message}");
                    }
                });
            }

            // 4. Send Web Push Notification if enabled
            if (user.PushNotificationsEnabled)
            {
                var subscriptions = await _context.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync();

                if (subscriptions.Any())
                {
                    _ = Task.Run(async () => {
                        try
                        {
                            var vapidDetails = new WebPush.VapidDetails(
                                _configuration["Vapid:Subject"],
                                _configuration["Vapid:PublicKey"],
                                _configuration["Vapid:PrivateKey"]
                            );

                            var webPushClient = new WebPush.WebPushClient();
                            var payload = Newtonsoft.Json.JsonConvert.SerializeObject(new
                            {
                                title = title,
                                body = content,
                                icon = "/images/logo-icon.png",
                                data = new { url = "/Dashboard" }
                            });

                            foreach (var sub in subscriptions)
                            {
                                try
                                {
                                    var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                                    await webPushClient.SendNotificationAsync(pushSub, payload, vapidDetails);
                                }
                                catch (WebPush.WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                                {
                                    _context.PushSubscriptions.Remove(sub);
                                    await _context.SaveChangesAsync();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Web Push Notification Error: {ex.Message}");
                        }
                    });
                }
            }

            // 5. Send FCM Mobile Push Notification if user has FcmToken registered
            if (!string.IsNullOrEmpty(user.FcmToken))
            {
                _ = Task.Run(async () => {
                    try
                    {
                        var fcmData = new Dictionary<string, string>()
                        {
                            { "type", type.ToString() },
                            { "title", title },
                            { "body", content },
                            { "click_action", "FLUTTER_NOTIFICATION_CLICK" }
                        };

                        if (data != null)
                        {
                            foreach (var kvp in data)
                            {
                                fcmData[kvp.Key] = kvp.Value ?? "";
                            }
                        }

                        var message = new Message()
                        {
                            Token = user.FcmToken,
                            Notification = new Notification()
                            {
                                Title = title,
                                Body = content
                            },
                            Data = fcmData,
                            Android = new AndroidConfig()
                            {
                                Priority = Priority.High,
                                Notification = new AndroidNotification()
                                {
                                    Title = title,
                                    Body = content,
                                    Sound = "default",
                                    ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                                    ChannelId = "high_importance_channel"
                                }
                            },
                            Apns = new ApnsConfig()
                            {
                                Headers = new Dictionary<string, string>()
                                {
                                    { "apns-priority", "10" }
                                },
                                Aps = new Aps()
                                {
                                    Sound = "default",
                                    Badge = 1,
                                    Category = "FLUTTER_NOTIFICATION_CLICK"
                                }
                            }
                        };

                        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                        Console.WriteLine($"[FCM] Successfully sent general push notification to user {userId}. Response: {response}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FCM] Error sending general push to user {userId}: {ex.Message}");
                    }
                });
            }
        }

        public async Task CreateAndSendNotificationAsync(string userId, string title, string content, string icon, string color)
        {
            var notification = new MedLinkPortal.Models.Notification
            {
                UserId = userId,
                Title = title,
                Content = content,
                Icon = icon,
                Color = color,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Broadcast real-time update
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                content = notification.Content,
                icon = notification.Icon,
                color = notification.Color,
                createdAt = notification.CreatedAt.ToString("o"), // ISO format
                isRead = notification.IsRead
            });
        }

        public async Task<List<MedLinkPortal.Models.Notification>> GetUserNotificationsAsync(string userId, int count = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Any())
            {
                foreach (var n in unread)
                {
                    n.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task SendPushNotificationAsync(string userId, string title, string body)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.FcmToken)) return;

                var message = new Message()
                {
                    Token = user.FcmToken,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Android = new AndroidConfig()
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification()
                        {
                            Title = title,
                            Body = body,
                            Sound = "default",
                            ChannelId = "high_importance_channel"
                        }
                    },
                    Apns = new ApnsConfig()
                    {
                        Headers = new Dictionary<string, string>() { { "apns-priority", "10" } },
                        Aps = new Aps() { Sound = "default", Badge = 1 }
                    }
                };

                await FirebaseMessaging.DefaultInstance.SendAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM] SendPushNotificationAsync error for user {userId}: {ex.Message}");
            }
        }

        public async Task NotifyCallAsync(string receiverId, string senderId, string senderName, string callType, string sdp)
        {
            Console.WriteLine($"[NotificationService] NotifyCallAsync started for {receiverId}");
            var user = await _userManager.FindByIdAsync(receiverId);
            if (user == null)
            {
                Console.WriteLine($"[NotificationService] Error: User {receiverId} not found in DB.");
                return;
            }

            if (string.IsNullOrEmpty(user.FcmToken))
            {
                Console.WriteLine($"[NotificationService] Error: User {user.Email} has no FCM token registered.");
                return;
            }

            Console.WriteLine($"[NotificationService] Sending FCM to user {user.Email} using token: {user.FcmToken.Substring(0, 10)}...");

            var sessionId = Guid.NewGuid().ToString();
            var message = new Message()
            {
                Token = user.FcmToken,
                Data = new Dictionary<string, string>()
                {
                    { "type", "incoming_call" },
                    { "sessionId", sessionId },
                    { "callerId", senderId },
                    { "callerName", senderName },
                    { "callType", callType },
                    { "sdp", sdp },
                    { "title", $"Incoming {callType} Call" },
                    { "body", $"Tap to answer call from {senderName}" }
                },
                Android = new AndroidConfig()
                {
                    Priority = Priority.High,
                    TimeToLive = TimeSpan.FromSeconds(60),
                    CollapseKey = $"call_{receiverId}",
                    Notification = null,
                    DirectBootOk = true
                },
                Apns = new ApnsConfig()
                {
                    Headers = new Dictionary<string, string>()
                    {
                        { "apns-priority", "10" },
                        { "apns-push-type", "background" },
                        { "apns-topic", "com.example.med-link-flutter" } // Ensure this matches your bundle id
                    },
                    Aps = new Aps()
                    {
                        ContentAvailable = true,
                        Category = "incoming_call"
                    },
                    CustomData = new Dictionary<string, object>()
                    {
                        { "type", "incoming_call" },
                        { "sessionId", sessionId },
                        { "callerId", senderId },
                        { "callerName", senderName },
                        { "callType", callType },
                        { "sdp", sdp }
                    }
                }
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine($"[FCM] Successfully sent call notification to {receiverId}. Response: {response}");
            }
            catch (FirebaseMessagingException ex)
            {
                Console.WriteLine($"[FCM] Firebase error sending to {receiverId}: {ex.Message} (Error Code: {ex.ErrorCode})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM] General error sending to {receiverId}: {ex.Message}");
            }
        }
    }
}
