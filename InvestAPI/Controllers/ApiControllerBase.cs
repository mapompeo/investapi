using InvestAPI.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace InvestAPI.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected Guid GetCurrentUserIdOrThrow()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedException();
    }
}
