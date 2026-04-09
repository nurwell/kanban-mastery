using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace KanbanApi.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<BoardMember> BoardMembers { get; set; } = new List<BoardMember>();
        public ICollection<Board> OwnedBoards { get; set; } = new List<Board>();
        public ICollection<Card> AssignedCards { get; set; } = new List<Card>();
    }
}
