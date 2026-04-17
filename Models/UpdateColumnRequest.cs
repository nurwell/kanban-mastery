using System.ComponentModel.DataAnnotations;

namespace KanbanApi.Models
{
    public record UpdateColumnRequest(
        [Required, StringLength(100, MinimumLength = 1)] string Title);
}
