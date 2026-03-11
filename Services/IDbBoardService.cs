using KanbanApi.Models;

namespace KanbanApi.Services
{
    public interface IDbBoardService
    {
        Task<Board> CreateBoardAsync(string name, string userId);
        Task<Board?> GetBoardAsync(int boardId);
        Task AddMemberAsync(int boardId, string userId);
    }
}
