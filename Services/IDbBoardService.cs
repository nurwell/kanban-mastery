using KanbanApi.Models;

namespace KanbanApi.Services
{
    public interface IDbBoardService
    {
        Task<Board> CreateBoardAsync(string name, string userId);
    }
}
