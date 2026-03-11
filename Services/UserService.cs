using KanbanApi.Models;
using Microsoft.AspNetCore.Identity;

namespace KanbanApi.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return null;

            return new UserProfileDto(user.Id, user.UserName, user.Email);
        }
    }
}
