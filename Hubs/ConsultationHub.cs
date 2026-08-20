using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using MedLinkPortal.Models;
using Microsoft.AspNetCore.Authorization; // Added this using directive

namespace MedLinkPortal.Hubs
{
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
    public class ConsultationHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.INotificationService _notificationService;

        public ConsultationHub(ApplicationDbContext context, Services.INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.OthersInGroup(roomId).SendAsync("UserJoined", Context.ConnectionId);
        }

        public async Task SendMessage(string roomId, string user, string message, string type = "text", string attachmentUrl = "", string attachmentName = "", string senderId = "", string receiverId = "")
        {
            // Broadcast to group in the room
            await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message, type, attachmentUrl, attachmentName, senderId);

            // Persist to database if we have IDs
            if (!string.IsNullOrEmpty(senderId) && !string.IsNullOrEmpty(receiverId))
            {
                try
                {
                    var chatMsg = new ChatMessage
                    {
                        SenderId = senderId,
                        ReceiverId = receiverId,
                        Content = message,
                        Timestamp = DateTime.UtcNow,
                        AttachmentUrl = attachmentUrl,
                        AttachmentType = type,
                        AttachmentName = attachmentName,
                        MessageType = (type == "text" || type == "Text") ? "Text" : "Document"
                    };

                    _context.ChatMessages.Add(chatMsg);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConsultationHub] Failed to persist message: {ex.Message}");
                    // Message was already broadcast — DB failure should not disconnect the client
                }
            }
        }

        // WebRTC Signaling within Room Groups
        public async Task InitiateCall(string roomId, string callType, string offer)
        {
            var senderId = Context.UserIdentifier;
            var senderName = Context.User?.Identity?.Name?.Split('@')[0] ?? "Doctor";

            await Clients.OthersInGroup(roomId).SendAsync("IncomingCall", Context.ConnectionId, callType, offer, senderName);

            // Send FCM push notification for background/terminated app support
            _ = Task.Run(async () =>
            {
                try
                {
                    // Resolve receiver ID from appointment (roomId is usually room_appointmentId)
                    if (roomId.StartsWith("room_"))
                    {
                        if (int.TryParse(roomId.Substring(5), out int appointmentId))
                        {
                            var appointment = await _context.Appointments.FindAsync(appointmentId);
                            if (appointment != null && !string.IsNullOrEmpty(appointment.UserId))
                            {
                                await _notificationService.NotifyCallAsync(appointment.UserId, senderId ?? "doctor", senderName, callType, offer);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending background notification: {ex.Message}");
                }
            });
        }

        public async Task AcceptCall(string roomId, string answer)
        {
            await Clients.OthersInGroup(roomId).SendAsync("CallAccepted", Context.ConnectionId, answer);
        }

        public async Task SendIceCandidate(string roomId, string candidate)
        {
            await Clients.OthersInGroup(roomId).SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidate);
        }

        public async Task EndCall(string roomId)
        {
            await Clients.Group(roomId).SendAsync("CallEnded", Context.ConnectionId);
        }

        public async Task AnnouncePresence(string roomId)
        {
            await Clients.OthersInGroup(roomId).SendAsync("UserIsPresent", Context.ConnectionId);
        }
        
        public async Task UpdatePrescription(string roomId, string prescriptionJson)
        {
            // Only doctors should technically trigger this, but we'll trust the group logic for now
            await Clients.OthersInGroup(roomId).SendAsync("PrescriptionUpdated", prescriptionJson);
        }

        public async Task SendTranscription(string roomId, string speakerName, string originalText, string englishText, string urduText)
        {
            await Clients.Group(roomId).SendAsync("ReceiveTranscription", new {
                speakerName = speakerName,
                originalText = originalText,
                englishText = englishText,
                urduText = urduText,
                timestamp = DateTime.UtcNow.ToString("HH:mm:ss")
            });
        }
    }
}
