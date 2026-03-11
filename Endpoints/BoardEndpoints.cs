using System.Security.Claims;
using KanbanApi.Data;
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

            group.MapGet("/", async (string userId, IBoardService boardService) =>
            {
                return await boardService.GetAllAsync(userId);
            })
            .WithName("GetBoards");

            group.MapGet("/{id}", async (int id, IBoardService boardService) =>
            {
                var board = await boardService.GetByIdAsync(id);
                return board is not null ? Results.Ok(board) : Results.NotFound();
            })
            .WithName("GetBoardById");

            group.MapPost("/", async (CreateBoardRequest request, ClaimsPrincipal user, IDbBoardService dbBoardService) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId is null) return Results.Unauthorized();

                var board = await dbBoardService.CreateBoardAsync(request.BoardName, userId);
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

            group.MapPut("/{id}", async (int id, Board board, IBoardService boardService) =>
            {
                if (id != board.Id) return Results.BadRequest("Mismatched Board ID.");
                var updated = await boardService.UpdateAsync(board);
                return updated is not null ? Results.Ok(updated) : Results.NotFound();
            })
            .WithName("UpdateBoard");

            group.MapDelete("/{id}", async (int id, IBoardService boardService) =>
            {
                var success = await boardService.DeleteAsync(id);
                return success ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteBoard");

            group.MapPost("/{boardId}/members", async (
                int boardId,
                AddMemberRequest request,
                ClaimsPrincipal user,
                IAuthorizationService authService,
                ApplicationDbContext db) =>
            {
                var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardOwner");
                if (!authResult.Succeeded) return Results.Forbid();

                var member = new BoardMember { BoardId = boardId, UserId = request.UserId, Role = "Member" };
                db.BoardMembers.Add(member);
                await db.SaveChangesAsync();

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
