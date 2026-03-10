using System.Security.Claims;
using KanbanApi.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KanbanApi.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/api/users/me", async (ClaimsPrincipal user, ApplicationDbContext db) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                var appUser = await db.Users.FindAsync(userId);

                if (appUser is null) return Results.NotFound();

                return Results.Ok(new
                {
                    appUser.Id,
                    appUser.UserName,
                    appUser.Email
                });
            })
            .RequireAuthorization()
            .WithName("GetCurrentUser");
        }
    }
}
