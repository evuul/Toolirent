using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TooliRent.Services.Exceptions; // ToolUnavailableException

namespace TooliRent.WebAPI.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            // Mappa typ -> status
            var (status, title) = ex switch
            {
                ToolUnavailableException => (HttpStatusCode.Conflict,       "Resource conflict"),
                ArgumentException        => (HttpStatusCode.BadRequest,     "Invalid request"),
                InvalidOperationException=> (HttpStatusCode.BadRequest,     "Invalid operation"),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized,"Unauthorized"),
                _                        => (HttpStatusCode.InternalServerError, "Unexpected error")
            };

            var problem = new ProblemDetails
            {
                Status = (int)status,
                Title  = title,
                Detail = ex.Message,
                Instance = ctx.Request.Path
            };

            ctx.Response.ContentType = "application/problem+json";
            ctx.Response.StatusCode = problem.Status ?? 500;

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await ctx.Response.WriteAsync(json);
        }
    }
}