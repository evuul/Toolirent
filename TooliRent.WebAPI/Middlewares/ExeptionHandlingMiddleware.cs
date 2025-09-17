using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TooliRent.Services.Exceptions; // ToolUnavailableException, BatchReservationFailedException

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
            // Välj statuskod + titel baserat på typ
            var (status, title) = ex switch
            {
                ToolUnavailableException         => (HttpStatusCode.Conflict, "Resource conflict"),
                BatchReservationFailedException  => (HttpStatusCode.Conflict, "Batch reservation failed"),
                ArgumentException                => (HttpStatusCode.BadRequest, "Invalid request"),
                InvalidOperationException        => (HttpStatusCode.BadRequest, "Invalid operation"),
                UnauthorizedAccessException      => (HttpStatusCode.Unauthorized, "Unauthorized"),
                _                                => (HttpStatusCode.InternalServerError, "Unexpected error")
            };

            var problem = new ProblemDetails
            {
                Status = (int)status,
                Title = title,
                Detail = ex.Message,
                Instance = ctx.Request.Path
            };

            // Extra payload för klienten att kunna visa exakt vilka verktyg som blockerar
            if (ex is ToolUnavailableException tu)
            {
                problem.Extensions["unavailableToolIds"] = tu.ToolIds;
            }

            // Behåll stöd om ni fortfarande kan kasta denna i något hörn
            if (ex is BatchReservationFailedException batchEx)
            {
                problem.Extensions["availableToolIds"]   = batchEx.AvailableToolIds;
                problem.Extensions["unavailableToolIds"] = batchEx.UnavailableToolIds;
            }

            ctx.Response.ContentType = "application/problem+json";
            ctx.Response.StatusCode  = problem.Status ?? 500;

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await ctx.Response.WriteAsync(json);
        }
    }
}