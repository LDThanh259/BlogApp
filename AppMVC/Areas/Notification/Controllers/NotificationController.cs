using AppMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppMVC.Areas.Notification.Controllers
{
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "AdminPolicy")]
        public IActionResult Index()
        {
            var notifications = _context.Notifications
                .Include(n => n.Post)
                .OrderByDescending(n => n.Post.DateCreated).ToList();

            return Json(notifications);
        }
    }
}
