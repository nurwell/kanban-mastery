using System;
using System.Collections.Generic;

namespace KanbanApi.Models
{
    public class Board
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string OwnerId { get; set; }
        public ApplicationUser Owner { get; set; } = null!;
        public ICollection<Column> Columns { get; set; } = new List<Column>();
        public ICollection<BoardMember> BoardMembers { get; set; } = new List<BoardMember>();
    }
}
