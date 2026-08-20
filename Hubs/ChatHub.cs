using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using MedLinkPortal.Models;

namespace MedLinkPortal.Hubs
{
    // Allow both JWT (mobile) and cookie-based (web) auth to use the same hub
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
    public class ChatHub : Hub
    {
        private readonly Models.ApplicationDbContext _context;
        private readonly Services.INotificationService _notificationService;

        public ChatHub(Models.ApplicationDbContext context, Services.INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // Send a message to a specific user
        public async Task SendMessageToUser(string receiverId, string message, string type = "text", string attachmentUrl = "", string attachmentName = "")
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId))
            {
                throw new HubException("Sender or Receiver ID cannot be null.");
            }

            var normalizedType = type?.ToLower() ?? "text";
            
            // 1. Save to Database
            try
            {
                var chatMessage = new ChatMessage
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = message ?? "",
                    Timestamp = DateTime.UtcNow,
                    IsRead = false,
                    MessageType = normalizedType,
                    AttachmentUrl = attachmentUrl,
                    AttachmentName = attachmentName
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatHub] DB save failed: {ex.Message}");
                // Continue — still deliver the message via SignalR even if DB write fails
            }

            // 2. Send via SignalR
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message ?? "", normalizedType, attachmentUrl, attachmentName, senderId);
            
            // 3. Trigger Notification (fire-and-forget, non-blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    var senderUser = await _context.Users.FindAsync(senderId);
                    var senderName = senderUser != null 
                        ? $"{senderUser.FirstName} {senderUser.LastName}".Trim() 
                        : (Context.User?.Identity?.Name?.Split('@')[0] ?? "User");
                    if (string.IsNullOrEmpty(senderName)) senderName = "User";

                    var customData = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "senderId", senderId },
                        { "senderName", senderName },
                        { "senderImage", senderUser?.ProfileImage ?? "" },
                        { "specialty", senderUser?.Specialist ?? "" }
                    };

                    var notificationContent = !string.IsNullOrEmpty(message) 
                        ? (message.Length > 50 ? message.Substring(0, 47) + "..." : message)
                        : "Sent an attachment";

                    await _notificationService.NotifyUserAsync(receiverId, 
                        NotificationType.NewMessageReceived,
                        $"New message from {senderName}", 
                        notificationContent, 
                        "message-square", "blue",
                        customData);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ChatHub] Notification failed: {ex.Message}");
                }
            });
        }

        // WebRTC Signaling for Audio/Video Calls
        public async Task InitiateCall(string receiverId, string callType, string offer)
        {
            var senderId = Context.UserIdentifier;
            Console.WriteLine($"[ChatHub] InitiateCall from {senderId} to {receiverId} (Type: {callType})");
            
            if (string.IsNullOrEmpty(senderId)) {
                Console.WriteLine("[ChatHub] Warning: senderId is null. User might not be fully authenticated in SignalR context.");
            }

            // Also send the caller's name so the receiver can display it
            var senderUser = await _context.Users.FindAsync(senderId);
            var callerName = senderUser != null 
                ? (!string.IsNullOrEmpty(senderUser.FullName?.Trim()) ? senderUser.FullName.Trim() : (!string.IsNullOrEmpty(senderUser.Name?.Trim()) ? senderUser.Name.Trim() : (Context.User?.Identity?.Name?.Split('@')[0] ?? "Patient")))
                : (Context.User?.Identity?.Name?.Split('@')[0] ?? senderId ?? "Patient");
            if (string.IsNullOrEmpty(callerName)) callerName = "Patient";
            
            await Clients.User(receiverId).SendAsync("IncomingCall", senderId, callType, offer, callerName);
            
            // Also send FCM notification for background support
            _ = Task.Run(async () => {
                try {
                    Console.WriteLine($"[ChatHub] Triggering background FCM for {receiverId}...");
                    await _notificationService.NotifyCallAsync(receiverId, senderId ?? "doctor", callerName, callType, offer);
                } catch (Exception ex) {
                    Console.WriteLine($"[ChatHub] FCM Trigger Error: {ex.Message}");
                }
            });
        }

        public async Task AcceptCall(string callerId, string answer)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(callerId)) return;
            await Clients.User(callerId).SendAsync("CallAccepted", senderId, answer);
        }

        public async Task RejectCall(string callerId)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(callerId)) return;
            await Clients.User(callerId).SendAsync("CallRejected", senderId);
        }

        public async Task SendIceCandidate(string targetUserId, string candidate)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(targetUserId)) return;
            await Clients.User(targetUserId).SendAsync("ReceiveIceCandidate", senderId, candidate);
        }

        public async Task EndCall(string targetUserId)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(targetUserId)) return;
            await Clients.User(targetUserId).SendAsync("CallEnded", senderId);
        }

        // Typing indicators
        public async Task UserTyping(string receiverId)
        {
            await Clients.User(receiverId).SendAsync("UserIsTyping", Context.UserIdentifier);
        }

        public async Task UserStoppedTyping(string receiverId)
        {
            await Clients.User(receiverId).SendAsync("UserStoppedTyping", Context.UserIdentifier);
        }

        // Read Receipts
        public async Task MarkMessagesAsRead(string senderId)
        {
            var currentUserId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(currentUserId)) return;

            try
            {
                var messages = _context.ChatMessages
                    .Where(m => m.SenderId == senderId && m.ReceiverId == currentUserId && !m.IsRead)
                    .ToList();

                if (messages.Any())
                {
                    foreach (var msg in messages)
                    {
                        msg.IsRead = true;
                    }
                    await _context.SaveChangesAsync();

                    await Clients.User(senderId).SendAsync("MessagesRead", currentUserId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatHub] MarkMessagesAsRead failed: {ex.Message}");
            }
        }

        public async Task DeleteMessage(int messageId, string deleteType)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            try
            {
                var msg = await _context.ChatMessages.FindAsync(messageId);
                if (msg == null) return;

                if (deleteType == "Everyone" && msg.SenderId != userId) return;
                if (msg.SenderId != userId && msg.ReceiverId != userId) return;

                if (deleteType == "Everyone")
                {
                    msg.IsDeleted = true;
                    msg.DeletedBy = "Everyone";
                    msg.Content = null;
                    msg.AttachmentUrl = null;
                    
                    await _context.SaveChangesAsync();
                    
                    await Clients.User(msg.SenderId).SendAsync("MessageDeleted", messageId, "Everyone");
                    await Clients.User(msg.ReceiverId).SendAsync("MessageDeleted", messageId, "Everyone");
                }
                else
                {
                    msg.IsDeleted = true;
                    msg.DeletedBy = userId;
                    await _context.SaveChangesAsync();
                    
                    await Clients.Caller.SendAsync("MessageDeleted", messageId, userId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatHub] DeleteMessage failed: {ex.Message}");
            }
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
