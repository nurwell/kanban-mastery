using System.Collections.Generic;
using System.Threading.Tasks;
using KanbanApi.Models;

namespace KanbanApi.Services
{
    public interface IBoardService
    {
        Task<IEnumerable<Board>> GetAllAsync(string userId);
        Task<Board?> GetByIdAsync(int id);
        Task<Board?> CreateAsync(Board board);
        Task<Board?> UpdateAsync(Board board);
        Task<bool> DeleteAsync(int id);
        Task<bool> UserOwnsBoardAsync(int boardId, string userId);
    }
}
