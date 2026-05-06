using FluentValidation;
using InvestAPI.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            await WriteProblemDetails(
                context,
                StatusCodes.Status400BadRequest,
                "Falha de validação.",
                ex.Message,
                new Dictionary<string, object?> { ["errors"] = errors });
        }
        catch (ApiException ex)
        {
            await WriteProblemDetails(
                context,
                ex.StatusCode,
                "Erro de negócio.",
                ex.Message,
                ex.ErrorCode is null
                    ? null
                    : new Dictionary<string, object?> { ["errorCode"] = ex.ErrorCode });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro de persistência no banco de dados.");
            await WriteProblemDetails(
                context,
                StatusCodes.Status409Conflict,
                "Conflito de persistência.",
                "Não foi possível persistir os dados solicitados.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado durante o processamento da requisição.");

            var detail = _environment.IsDevelopment()
                ? ex.ToString()
                : "Ocorreu um erro interno. Tente novamente.";

            await WriteProblemDetails(
                context,
                StatusCodes.Status500InternalServerError,
                "Erro interno do servidor.",
                detail);
        }
    }

    private static async Task WriteProblemDetails(
        HttpContext context,
        int status,
        string title,
        string detail,
        IDictionary<string, object?>? extensions = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problem.Extensions[extension.Key] = extension.Value;
            }
        }

        await context.Response.WriteAsJsonAsync(problem);
    }
}
