using System;

namespace KanbanApi.Models
{
    public class Card
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public int Position { get; set; }
        public int ColumnId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Column Column { get; set; } = null!;
        public string? AssignedToUserId { get; set; }
        public ApplicationUser? AssignedTo { get; set; }
    }
}
