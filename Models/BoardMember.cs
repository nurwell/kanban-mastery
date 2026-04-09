namespace KanbanApi.Models
{
    public class BoardMember
    {
        public int BoardId { get; set; }
        public required string UserId { get; set; }
        public required string Role { get; set; }
        public Board Board { get; set; } = null!;
        public ApplicationUser ApplicationUser { get; set; } = null!;
    }
}
