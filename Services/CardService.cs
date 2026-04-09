using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KanbanApi.Services
{
    public class CardService : ICardService
    {
        private readonly ApplicationDbContext _db;

        public CardService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Card?> CreateCardAsync(int boardId, int columnId, string title, string? description)
        {
            var column = await _db.Columns.FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
            if (column is null) return null;

            var card = new Card
            {
                Title = title,
                Description = description ?? string.Empty,
                ColumnId = columnId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Cards.Add(card);
            await _db.SaveChangesAsync();
            return card;
        }

        public async Task<Card?> UpdateCardAsync(int boardId, int cardId, string title, string? description, int columnId)
        {
            var card = await _db.Cards
                .Include(c => c.Column)
                .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
            if (card is null) return null;

            // Validate target column also belongs to this board
            var targetColumn = await _db.Columns.FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
            if (targetColumn is null) return null;

            card.Title = title;
            card.Description = description ?? string.Empty;
            card.ColumnId = columnId;
            await _db.SaveChangesAsync();
            return card;
        }

        public async Task<bool> DeleteCardAsync(int boardId, int cardId)
        {
            var card = await _db.Cards
                .Include(c => c.Column)
                .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
            if (card is null) return false;

            _db.Cards.Remove(card);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
