using AppMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AppMVC.Requirement
{
    public class IsPostAuthorHandler : AuthorizationHandler<IsPostAuthorRequirement>
    {
        private readonly UserManager<AppUser> _userManager;

        private readonly AppDbContext _context;

        public IsPostAuthorHandler(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsPostAuthorRequirement requirement)
        {
            var userId = _userManager.GetUserId(context.User);

            // Lấy postId từ context.Resource (được truyền từ controller)
            var postId = context.Resource as int?;

            if (postId.HasValue)
            {
                var post = await _context.Posts.FindAsync(postId.Value);

                if (post != null && post.AuthorId == userId)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
