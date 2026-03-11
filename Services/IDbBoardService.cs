using KanbanApi.Models;

namespace KanbanApi.Services
{
    public interface IDbBoardService
    {
        Task<Board> CreateBoardAsync(string name, string userId);
        Task<Board?> GetBoardAsync(int boardId);
        Task AddMemberAsync(int boardId, string userId);
        Task<bool> IsMemberAsync(int boardId, string userId);
        Task<Board?> UpdateBoardAsync(int boardId, string name);
        Task<bool> DeleteBoardAsync(int boardId);
        Task<Column> CreateColumnAsync(int boardId, string title, int? position);
        Task<Column?> UpdateColumnAsync(int boardId, int columnId, string title);
        Task<DeleteColumnResult> DeleteColumnAsync(int boardId, int columnId);
    }
}
