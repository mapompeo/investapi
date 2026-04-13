using InvestAPI.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvestAPI.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected Guid GetCurrentUserIdOrThrow()
    {
        var sub = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (Guid.TryParse(sub, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedException();
    }
}
