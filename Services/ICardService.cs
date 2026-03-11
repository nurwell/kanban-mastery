using KanbanApi.Models;

namespace KanbanApi.Services
{
    public interface ICardService
    {
        Task<Card?> CreateCardAsync(int boardId, int columnId, string title, string? description);
        Task<Card?> UpdateCardAsync(int boardId, int cardId, string title, string? description, int columnId);
        Task<bool> DeleteCardAsync(int boardId, int cardId);
        Task<Card?> AssignCardAsync(int boardId, int cardId, string userId);
    }
}
