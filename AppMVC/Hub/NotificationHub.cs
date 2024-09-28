using AppMVC.Data;
using AppMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace AppMVC.Hub
{
    public class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
    {
        //public async Task SendNotification(string message)
        //{
        //    await Clients.All.SendAsync("ReceiveNotification", message);
        //}

        private readonly UserManager<AppUser> _userManager;

        public NotificationHub(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            var userId = _userManager.GetUserId(user);
            var appUser = await _userManager.FindByIdAsync(userId);

            // Kiểm tra xem người dùng có phải là Admin không
            if (await _userManager.IsInRoleAsync(appUser, RoleName.Administrator))
            {
                // Nếu là Admin, thêm người dùng vào group Admin
                await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Khi người dùng ngắt kết nối, xóa khỏi nhóm Admin nếu họ là Admin
            var user = Context.User;
            var userId = _userManager.GetUserId(user);
            var appUser = await _userManager.FindByIdAsync(userId);

            if (await _userManager.IsInRoleAsync(appUser, RoleName.Administrator))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminGroup");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
