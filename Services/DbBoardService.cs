using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KanbanApi.Services
{
    public class DbBoardService : IDbBoardService
    {
        private readonly ApplicationDbContext _db;

        public DbBoardService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Board> CreateBoardAsync(string name, string userId)
        {
            var board = new Board { Name = name, OwnerId = userId };
            var membership = new BoardMember { Board = board, UserId = userId, Role = "Owner" };

            _db.Boards.Add(board);
            _db.BoardMembers.Add(membership);
            await _db.SaveChangesAsync();

            return board;
        }

        public async Task<Board?> GetBoardAsync(int boardId)
        {
            return await _db.Boards
                .Include(b => b.Columns)
                    .ThenInclude(c => c.Cards)
                .FirstOrDefaultAsync(b => b.Id == boardId);
        }

        public async Task AddMemberAsync(int boardId, string userId)
        {
            var member = new BoardMember { BoardId = boardId, UserId = userId, Role = "Member" };
            _db.BoardMembers.Add(member);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> IsMemberAsync(int boardId, string userId)
        {
            return await _db.BoardMembers.AnyAsync(bm => bm.BoardId == boardId && bm.UserId == userId);
        }

        public async Task<Board?> UpdateBoardAsync(int boardId, string name)
        {
            var board = await _db.Boards.FindAsync(boardId);
            if (board is null) return null;

            board.Name = name;
            await _db.SaveChangesAsync();
            return board;
        }

        public async Task<bool> DeleteBoardAsync(int boardId)
        {
            var board = await _db.Boards.FindAsync(boardId);
            if (board is null) return false;

            _db.Boards.Remove(board);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<Column> CreateColumnAsync(int boardId, string title, int? position)
        {
            var pos = position ?? await _db.Columns.CountAsync(c => c.BoardId == boardId);
            var column = new Column { BoardId = boardId, Title = title, Position = pos };
            _db.Columns.Add(column);
            await _db.SaveChangesAsync();
            return column;
        }

        public async Task<Column?> UpdateColumnAsync(int boardId, int columnId, string title)
        {
            var column = await _db.Columns.FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
            if (column is null) return null;

            column.Title = title;
            await _db.SaveChangesAsync();
            return column;
        }

        public async Task<DeleteColumnResult> DeleteColumnAsync(int boardId, int columnId)
        {
            var column = await _db.Columns
                .Include(c => c.Cards)
                .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);

            if (column is null) return DeleteColumnResult.NotFound;
            if (column.Cards.Count > 0) return DeleteColumnResult.HasCards;

            _db.Columns.Remove(column);
            await _db.SaveChangesAsync();
            return DeleteColumnResult.Deleted;
        }
    }
}
