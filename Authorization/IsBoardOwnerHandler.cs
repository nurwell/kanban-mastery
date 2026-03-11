using System.Security.Claims;
using KanbanApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace KanbanApi.Authorization
{
    public class IsBoardOwnerHandler : AuthorizationHandler<IsBoardOwnerRequirement, int>
    {
        private readonly ApplicationDbContext _db;

        public IsBoardOwnerHandler(ApplicationDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            IsBoardOwnerRequirement requirement,
            int boardId)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return;

            var isOwner = await _db.BoardMembers
                .AnyAsync(m => m.BoardId == boardId && m.UserId == userId && m.Role == "Owner");

            if (isOwner) context.Succeed(requirement);
        }
    }
}
