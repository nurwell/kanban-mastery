using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KanbanApi.Models;

namespace KanbanApi.Services
{
    public class BoardService : IBoardService
    {
        // Using ConcurrentDictionary for thread safety during in-memory operations
        private static readonly ConcurrentDictionary<int, Board> _boards = new();
        private static int _nextId = 1;

        public Task<IEnumerable<Board>> GetAllAsync(string userId)
        {
            var boards = _boards.Values.Where(b => b.OwnerId == userId);
            return Task.FromResult(boards);
        }

        public Task<Board?> GetByIdAsync(int id)
        {
            _boards.TryGetValue(id, out var board);
            return Task.FromResult(board);
        }

        public Task<Board?> CreateAsync(Board board)
        {
            board.Id = _nextId++;
            board.CreatedAt = DateTime.UtcNow;
            if (_boards.TryAdd(board.Id, board))
                return Task.FromResult<Board?>(board);
            return Task.FromResult<Board?>(null);
        }

        public Task<Board?> UpdateAsync(Board board)
        {
            if (_boards.TryGetValue(board.Id, out var existingBoard))
            {
                existingBoard.Name = board.Name;
                return Task.FromResult<Board?>(existingBoard);
            }
            return Task.FromResult<Board?>(null);
        }

        public Task<bool> DeleteAsync(int id)
        {
            return Task.FromResult(_boards.TryRemove(id, out _));
        }

        public Task<bool> UserOwnsBoardAsync(int boardId, string userId)
        {
            if (_boards.TryGetValue(boardId, out var board))
                return Task.FromResult(board.OwnerId == userId);
            return Task.FromResult(false);
        }
    }
}
