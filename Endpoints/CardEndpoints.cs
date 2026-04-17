using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KanbanApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace KanbanApi.Endpoints
{
    public record CreateCardDto([Required, StringLength(200, MinimumLength = 1)] string Title, [StringLength(2000)] string? Description, int ColumnId);
    public record UpdateCardDto([Required, StringLength(200, MinimumLength = 1)] string Title, [StringLength(2000)] string? Description, int ColumnId, int? Position, string? AssignedToUserId);
    public record ReorderCardsDto(int[] CardIds);

    public static class CardEndpoints
    {
        public static void MapCardEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/boards/{boardId}/cards")
                .RequireAuthorization();

            group.MapPost("/", async (
                int boardId,
                CreateCardDto dto,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                ICardService cardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
                if (!authResult.Succeeded) return Results.Forbid();
                var title = dto.Title?.Trim();
                if (string.IsNullOrEmpty(title) || title.Length > 200) return Results.BadRequest(new { message = "Card title must be 1–200 characters." });
                if (dto.Description?.Length > 2000) return Results.BadRequest(new { message = "Description must be under 2000 characters." });

                var card = await cardService.CreateCardAsync(boardId, dto.ColumnId, title, dto.Description);
                if (card is null) return Results.NotFound();

                return Results.Created($"/api/boards/{boardId}/cards/{card.Id}", new
                {
                    card.Id,
                    card.Title,
                    card.Description,
                    card.ColumnId,
                    card.CreatedAt
                });
            })
            .WithName("CreateCard");

            group.MapPut("/{cardId}", async (
                int boardId,
                int cardId,
                UpdateCardDto dto,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                ICardService cardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
                if (!authResult.Succeeded) return Results.Forbid();
                var title = dto.Title?.Trim();
                if (string.IsNullOrEmpty(title) || title.Length > 200) return Results.BadRequest(new { message = "Card title must be 1–200 characters." });
                if (dto.Description?.Length > 2000) return Results.BadRequest(new { message = "Description must be under 2000 characters." });

                var card = await cardService.UpdateCardAsync(boardId, cardId, title, dto.Description, dto.ColumnId, dto.Position, dto.AssignedToUserId);
                if (card is null) return Results.NotFound();

                return Results.Ok(new
                {
                    card.Id,
                    card.Title,
                    card.Description,
                    card.ColumnId,
                    card.Position,
                    card.AssignedToUserId,
                    card.CreatedAt
                });
            })
            .WithName("UpdateCard");

            routes.MapGroup("/api/boards/{boardId}/columns")
                .RequireAuthorization()
                .MapPut("/{columnId}/reorder", async (
                    int boardId,
                    int columnId,
                    ReorderCardsDto dto,
                    ClaimsPrincipal user,
                    IAuthorizationService authService,
                    ICardService cardService) =>
                {
                    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
                    if (!authResult.Succeeded) return Results.Forbid();

                    var ok = await cardService.ReorderCardsAsync(boardId, columnId, dto.CardIds);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
                .WithName("ReorderCards");

            group.MapDelete("/{cardId}", async (
                int boardId,
                int cardId,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                ICardService cardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardOwner");
                if (!authResult.Succeeded) return Results.Forbid();

                var deleted = await cardService.DeleteCardAsync(boardId, cardId);
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteCard");
        }
    }
}
