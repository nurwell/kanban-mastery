using KanbanApi.Models;

namespace KanbanApi.Services
{
    public interface ICardService
    {
        Task<Card?> CreateCardAsync(int boardId, int columnId, string title, string? description);
        Task<Card?> UpdateCardAsync(int boardId, int cardId, string title, string? description, int columnId, int? position, string? assignedToUserId);
        Task<bool> DeleteCardAsync(int boardId, int cardId);
        Task<bool> ReorderCardsAsync(int boardId, int columnId, int[] cardIds);
    }
}
