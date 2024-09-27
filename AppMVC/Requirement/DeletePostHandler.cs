using AppMVC.Data;
using AppMVC.Models;
using AppMVC.Models.Blog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace AppMVC.Requirement
{
    public class DeletePostAuthorizationHandler : AuthorizationHandler<DeletePostRequirement, Post>
    {
        private readonly UserManager<AppUser> _userManager;

        private readonly AppDbContext _context;

        public DeletePostAuthorizationHandler(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DeletePostRequirement requirement, Post post)
        {

            var userId = _userManager.GetUserId(context.User);

            if (userId == post.AuthorId.ToString())
            {
                context.Succeed(requirement);
            }

            if (context.User.IsInRole(RoleName.Administrator))
            {
                context.Succeed(requirement);
            }
            if (context.User.IsInRole(RoleName.Editor))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
