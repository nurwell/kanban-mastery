using KanbanApi.Models;
using KanbanApi.Services;

namespace KanbanApi.Endpoints
{
    public static class BoardEndpoints
    {
        public static void MapBoardEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/boards");

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

            group.MapPost("/", async (Board board, IBoardService boardService) =>
            {
                var createdBoard = await boardService.CreateAsync(board);
                if (createdBoard is null) return Results.BadRequest("Could not create board.");
                return Results.Created($"/boards/{createdBoard.Id}", createdBoard);
            })
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
        }
    }
}
