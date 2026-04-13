namespace InvestAPI.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public string? ErrorCode { get; }

    public ApiException(int statusCode, string message, string? errorCode = null) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public sealed class BadRequestException : ApiException
{
    public BadRequestException(string message, string? errorCode = null)
        : base(StatusCodes.Status400BadRequest, message, errorCode)
    {
    }
}

public sealed class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "Não autorizado.", string? errorCode = null)
        : base(StatusCodes.Status401Unauthorized, message, errorCode)
    {
    }
}

public sealed class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "Acesso negado.", string? errorCode = null)
        : base(StatusCodes.Status403Forbidden, message, errorCode)
    {
    }
}

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string message, string? errorCode = null)
        : base(StatusCodes.Status404NotFound, message, errorCode)
    {
    }
}

public sealed class ConflictException : ApiException
{
    public ConflictException(string message, string? errorCode = null)
        : base(StatusCodes.Status409Conflict, message, errorCode)
    {
    }
}
