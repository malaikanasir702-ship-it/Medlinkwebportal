using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace MedLinkPortal.Hubs
{
    // Allow both cookie (web) and JWT (mobile) auth
    [Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
    public class NotificationHub : Hub
    {
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
