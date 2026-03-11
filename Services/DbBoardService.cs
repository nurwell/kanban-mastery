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
    }
}
