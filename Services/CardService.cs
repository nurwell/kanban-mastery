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

            var maxPosition = await _db.Cards
                .Where(c => c.ColumnId == columnId)
                .MaxAsync(c => (int?)c.Position) ?? -1;

            var card = new Card
            {
                Title = title,
                Description = description ?? string.Empty,
                ColumnId = columnId,
                Position = maxPosition + 1,
                CreatedAt = DateTime.UtcNow
            };
            _db.Cards.Add(card);
            await _db.SaveChangesAsync();
            return card;
        }

        public async Task<Card?> UpdateCardAsync(int boardId, int cardId, string title, string? description, int columnId, int? position, string? assignedToUserId)
        {
            var card = await _db.Cards
                .Include(c => c.Column)
                .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
            if (card is null) return null;

            var targetColumn = await _db.Columns.FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
            if (targetColumn is null) return null;

            if (assignedToUserId is not null && assignedToUserId != "")
            {
                var isMember = await _db.BoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == assignedToUserId);
                if (!isMember) return null;
            }

            card.Title = title;
            card.Description = description ?? string.Empty;
            card.ColumnId = columnId;
            if (position.HasValue) card.Position = position.Value;
            if (assignedToUserId is not null) card.AssignedToUserId = assignedToUserId == "" ? null : assignedToUserId;
            await _db.SaveChangesAsync();
            return card;
        }

        public async Task<bool> ReorderCardsAsync(int boardId, int columnId, int[] cardIds)
        {
            var column = await _db.Columns.FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
            if (column is null) return false;

            var cards = await _db.Cards.Where(c => c.ColumnId == columnId).ToListAsync();
            for (var i = 0; i < cardIds.Length; i++)
            {
                var card = cards.FirstOrDefault(c => c.Id == cardIds[i]);
                if (card is not null) card.Position = i;
            }
            await _db.SaveChangesAsync();
            return true;
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
