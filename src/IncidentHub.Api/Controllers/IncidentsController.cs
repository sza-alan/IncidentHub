using IncidentHub.Api.Contracts.Incidents;
using IncidentHub.Application.Incidents;
using IncidentHub.Application.Incidents.Analyze;
using IncidentHub.Application.Common;
using IncidentHub.Application.Incidents.ChangeStatus;
using IncidentHub.Application.Incidents.Create;
using IncidentHub.Application.Incidents.GetById;
using IncidentHub.Application.Incidents.List;
using IncidentHub.Domain.Incidents;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IncidentHub.Api.Controllers;

[ApiController]
[Route("api/incidents")]
public sealed class IncidentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<IncidentDto>>> List(
        [FromQuery] ListIncidentsRequest request,
        CancellationToken cancellationToken)
    {
        var query = new ListIncidentsQuery(
            request.Page,
            request.PageSize,
            request.Status,
            request.Severity,
            request.Service);

        var incidents = await sender.Send(query, cancellationToken);

        return Ok(incidents);
    }

    [HttpPost]
    public async Task<ActionResult<IncidentDto>> Create(
        CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateIncidentCommand(
            request.Title,
            request.Description ?? string.Empty,
            request.Service,
            request.Severity!.Value);

        var incident = await sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = incident.Id }, incident);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncidentDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var incident = await sender.Send(new GetIncidentByIdQuery(id), cancellationToken);

        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<IncidentDto>> ChangeStatus(
        Guid id,
        ChangeIncidentStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var incident = await sender.Send(
                new ChangeIncidentStatusCommand(id, request.Status!.Value),
                cancellationToken);

            return incident is null ? NotFound() : Ok(incident);
        }
        catch (InvalidIncidentStatusTransitionException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invalid incident status transition.",
                Detail = exception.Message
            });
        }
    }

    [HttpPost("{id:guid}/analyze")]
    public async Task<ActionResult<IncidentAnalysisDto>> Analyze(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var analysis = await sender.Send(
                new AnalyzeIncidentQuery(id),
                cancellationToken);

            return analysis is null ? NotFound() : Ok(analysis);
        }
        catch (IncidentAnalysisException exception)
        {
            var (statusCode, title) = exception.FailureKind switch
            {
                IncidentAnalysisFailureKind.Timeout =>
                    (StatusCodes.Status504GatewayTimeout, "Local model timed out."),
                IncidentAnalysisFailureKind.Unavailable =>
                    (StatusCodes.Status503ServiceUnavailable, "Local model unavailable."),
                _ =>
                    (StatusCodes.Status502BadGateway, "Invalid local model response.")
            };

            return StatusCode(statusCode, new ProblemDetails
            {
                Status = statusCode,
                Title = title
            });
        }
    }
}
