using System.Security.Claims;
using KanbanApi.Services;

namespace KanbanApi.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/api/users/me", async (ClaimsPrincipal user, IUserService userService) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId is null) return Results.Unauthorized();

                var profile = await userService.GetUserProfileAsync(userId);
                if (profile is null) return Results.NotFound();

                return Results.Ok(profile);
            })
            .RequireAuthorization()
            .WithName("GetCurrentUser");
        }
    }
}
