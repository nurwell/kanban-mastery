using System.ComponentModel.DataAnnotations;

namespace KanbanApi.Models
{
    public record CreateColumnRequest(
        [Required, StringLength(100, MinimumLength = 1)] string Title,
        int? Position);
}
