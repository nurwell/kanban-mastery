using System.Security.Claims;
using KanbanApi.Models;
using KanbanApi.Services;
using Microsoft.AspNetCore.Identity;

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

            routes.MapGet("/api/users/lookup", async (
                string email,
                UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user is null) return Results.NotFound(new { message = "No user found with that email" });

                return Results.Ok(new { id = user.Id, email = user.Email });
            })
            .RequireAuthorization()
            .WithName("LookupUserByEmail");
        }
    }
}
