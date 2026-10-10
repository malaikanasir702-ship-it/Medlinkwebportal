namespace MedLinkPortal.Services
{
    public interface INotificationService
    {
        Task CreateAndSendNotificationAsync(string userId, string title, string content, string icon, string color);
        Task NotifyUserAsync(string userId, MedLinkPortal.Models.NotificationType type, string title, string content, string icon = "bell", string color = "blue", System.Collections.Generic.Dictionary<string, string>? data = null);
        Task<List<MedLinkPortal.Models.Notification>> GetUserNotificationsAsync(string userId, int count = 10);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task NotifyCallAsync(string receiverId, string senderId, string senderName, string callType, string sdp);
    }
}
