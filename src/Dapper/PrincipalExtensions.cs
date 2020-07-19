using System.Security.Claims;
using System.Security.Principal;

namespace Fulgoribus.Luxae.Dapper
{
    public static class PrincipalExtensions
    {
        public static string? GetUserId(this IPrincipal user)
        {
            return user is ClaimsPrincipal cp
                ? cp.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;
        }
    }
}
