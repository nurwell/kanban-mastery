using KanbanApi.Models;

namespace KanbanApi.Services
{
    public interface IUserService
    {
        Task<UserProfileDto?> GetUserProfileAsync(string userId);
    }
}
