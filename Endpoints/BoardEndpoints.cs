using System.Security.Claims;
using KanbanApi.Models;
using KanbanApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace KanbanApi.Endpoints
{
    public static class BoardEndpoints
    {
        public static void MapBoardEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/boards");

            group.MapGet("/", async (ClaimsPrincipal user, IDbBoardService dbBoardService) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId is null) return Results.Unauthorized();

                var boards = await dbBoardService.GetBoardsAsync(userId);
                return Results.Ok(boards.Select(b => new { b.Id, b.Name, b.OwnerId, b.CreatedAt }));
            })
            .RequireAuthorization()
            .WithName("GetBoards");

            group.MapGet("/{boardId}", async (
                int boardId,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                IDbBoardService dbBoardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
                if (!authResult.Succeeded) return Results.Forbid();

                var board = await dbBoardService.GetBoardAsync(boardId);

                if (board is null) return Results.NotFound();

                return Results.Ok(new
                {
                    board.Id,
                    board.Name,
                    board.OwnerId,
                    board.CreatedAt,
                    Columns = board.Columns
                        .OrderBy(c => c.Position)
                        .Select(c => new
                        {
                            c.Id,
                            c.Title,
                            c.Position,
                            Cards = c.Cards
                                .OrderBy(card => card.Position)
                                .Select(card => new
                                {
                                    card.Id,
                                    card.Title,
                                    card.Description,
                                    card.Position,
                                    card.CreatedAt,
                                    card.AssignedToUserId
                                })
                        })
                });
            })
            .RequireAuthorization()
            .WithName("GetBoardById");

            group.MapPost("/", async (CreateBoardRequest request, ClaimsPrincipal user, IDbBoardService dbBoardService) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId is null) return Results.Unauthorized();

                var board = await dbBoardService.CreateBoardAsync(request.Name, userId);
                return Results.Created($"/api/boards/{board.Id}", new
                {
                    board.Id,
                    board.Name,
                    board.OwnerId,
                    board.CreatedAt
                });
            })
            .RequireAuthorization()
            .WithName("CreateBoard");

            group.MapPut("/{boardId}", async (
                int boardId,
                UpdateBoardRequest request,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                IDbBoardService dbBoardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardOwner");
                if (!authResult.Succeeded) return Results.Forbid();

                var board = await dbBoardService.UpdateBoardAsync(boardId, request.Name);
                if (board is null) return Results.NotFound();

                return Results.Ok(new { board.Id, board.Name, board.OwnerId, board.CreatedAt });
            })
            .RequireAuthorization()
            .WithName("UpdateBoard");

            group.MapDelete("/{boardId}", async (
                int boardId,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                IDbBoardService dbBoardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardOwner");
                if (!authResult.Succeeded) return Results.Forbid();

                var deleted = await dbBoardService.DeleteBoardAsync(boardId);
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization()
            .WithName("DeleteBoard");

            group.MapGet("/{boardId}/members", async (
                int boardId,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                IDbBoardService dbBoardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
                if (!authResult.Succeeded) return Results.Forbid();

                var members = await dbBoardService.GetMembersAsync(boardId);
                return Results.Ok(members.Select(m => new
                {
                    m.UserId,
                    m.Role,
                    Email = m.ApplicationUser?.Email,
                    UserName = m.ApplicationUser?.UserName
                }));
            })
            .RequireAuthorization()
            .WithName("GetBoardMembers");

            group.MapPost("/{boardId}/members", async (
                int boardId,
                AddMemberRequest request,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                IDbBoardService dbBoardService) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardOwner");
                if (!authResult.Succeeded) return Results.Forbid();

                await dbBoardService.AddMemberAsync(boardId, request.UserId);

                return Results.Created($"/api/boards/{boardId}/members/{request.UserId}", new
                {
                    boardId,
                    request.UserId,
                    Role = "Member"
                });
            })
            .RequireAuthorization()
            .WithName("AddBoardMember");
        }
    }
}
