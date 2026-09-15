using IncidentHub.Application.Services.Health;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IncidentHub.Api.Controllers;

[ApiController]
[Route("api/services")]
public sealed class ServicesController(ISender sender) : ControllerBase
{
    [HttpGet("{serviceName}/health")]
    public async Task<ActionResult<ServiceHealthDto>> GetHealth(
        string serviceName,
        CancellationToken cancellationToken)
    {
        try
        {
            var health = await sender.Send(
                new GetServiceHealthQuery(serviceName),
                cancellationToken);

            return health is null ? NotFound() : Ok(health);
        }
        catch (ServiceHealthIntegrationException exception)
        {
            var (statusCode, title) = exception.FailureKind switch
            {
                ServiceHealthFailureKind.Timeout =>
                    (StatusCodes.Status504GatewayTimeout, "External service timed out."),
                ServiceHealthFailureKind.Unavailable =>
                    (StatusCodes.Status503ServiceUnavailable, "External service unavailable."),
                _ =>
                    (StatusCodes.Status502BadGateway, "Invalid external service response.")
            };

            return StatusCode(statusCode, new ProblemDetails
            {
                Status = statusCode,
                Title = title
            });
        }
    }
}
