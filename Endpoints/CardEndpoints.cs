using System.Security.Claims;
using KanbanApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KanbanApi.Endpoints
{
    public record CreateCardDto(string Title, string? Description, int ColumnId);
    public record UpdateCardDto(string Title, string? Description, int ColumnId);
    public record AssignCardDto(string UserId);

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

                var card = await cardService.CreateCardAsync(boardId, dto.ColumnId, dto.Title, dto.Description);
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

                var card = await cardService.UpdateCardAsync(boardId, cardId, dto.Title, dto.Description, dto.ColumnId);
                if (card is null) return Results.NotFound();

                return Results.Ok(new
                {
                    card.Id,
                    card.Title,
                    card.Description,
                    card.ColumnId,
                    card.CreatedAt
                });
            })
            .WithName("UpdateCard");

            group.MapDelete("/{cardId}", async (
                int boardId,
                int cardId,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                ICardService cardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
                if (!authResult.Succeeded) return Results.Forbid();

                var deleted = await cardService.DeleteCardAsync(boardId, cardId);
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteCard");

            group.MapPut("/{cardId}/assign", async (
                int boardId,
                int cardId,
                AssignCardDto dto,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                IDbBoardService boardService,
                ICardService cardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
                if (!authResult.Succeeded) return Results.Forbid();

                var isMember = await boardService.IsMemberAsync(boardId, dto.UserId);
                if (!isMember)
                    return Results.BadRequest("User is not a board member");

                var card = await cardService.AssignCardAsync(boardId, cardId, dto.UserId);
                if (card is null) return Results.NotFound();

                return Results.Ok(new
                {
                    card.Id,
                    card.Title,
                    card.Description,
                    card.ColumnId,
                    card.AssignedToUserId,
                    card.CreatedAt
                });
            })
            .WithName("AssignCard");
        }
    }
}
