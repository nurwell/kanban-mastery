using KanbanApi.Data;
using KanbanApi.Models;

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
    }
}
