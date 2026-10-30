using Template.Application.Abstractions;

namespace Template.Api.Infrastructure.Auth;

internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string UserId =>
        User.FindFirst(ClerkClaimTypes.Subject)?.Value
        ?? throw new InvalidOperationException("No authenticated user. Is the endpoint missing authorization?");

    public string? OrganizationId =>
        AuthExtensions.ReadOrganizationId(User.FindFirst(ClerkClaimTypes.Organization)?.Value);

    private System.Security.Claims.ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("ICurrentUser was used outside an HTTP request.");
}
