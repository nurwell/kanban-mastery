using System.Security.Claims;
using KanbanApi.Models;
using KanbanApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace KanbanApi.Endpoints
{
    public static class ColumnEndpoints
    {
        public static void MapColumnEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/boards/{boardId}/columns")
                .RequireAuthorization();

            group.MapPost("/", async (
                int boardId,
                CreateColumnRequest request,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                IDbBoardService boardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
                if (!authResult.Succeeded) return Results.Forbid();

                var column = await boardService.CreateColumnAsync(boardId, request.Title, request.Position);
                return Results.Created($"/api/boards/{boardId}/columns/{column.Id}", new
                {
                    column.Id,
                    column.Title,
                    column.Position,
                    column.BoardId
                });
            })
            .WithName("CreateColumn");

            group.MapPut("/{columnId}", async (
                int boardId,
                int columnId,
                UpdateColumnRequest request,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                IDbBoardService boardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
                if (!authResult.Succeeded) return Results.Forbid();

                var column = await boardService.UpdateColumnAsync(boardId, columnId, request.Title);
                if (column is null) return Results.NotFound();

                return Results.Ok(new
                {
                    column.Id,
                    column.Title,
                    column.Position,
                    column.BoardId
                });
            })
            .WithName("UpdateColumn");

            group.MapDelete("/{columnId}", async (
                int boardId,
                int columnId,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                IDbBoardService boardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
                if (!authResult.Succeeded) return Results.Forbid();

                var result = await boardService.DeleteColumnAsync(boardId, columnId);

                return result switch
                {
                    DeleteColumnResult.Deleted => Results.NoContent(),
                    DeleteColumnResult.HasCards => Results.BadRequest("Cannot delete column with existing cards."),
                    _ => Results.NotFound()
                };
            })
            .WithName("DeleteColumn");
        }
    }
}
