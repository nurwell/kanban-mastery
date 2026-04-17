using System.ComponentModel.DataAnnotations;

namespace KanbanApi.Models
{
    public record CreateBoardRequest(
        [Required, StringLength(100, MinimumLength = 1)] string Name);
}
