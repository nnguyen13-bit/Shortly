using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Api.Contracts;
using Shortly.Application.Common;
using Shortly.Application.CustomDomains;
using Shortly.Domain.CustomDomains;

namespace Shortly.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class DomainsController : ControllerBase
{
    private readonly CustomDomainService _domainService;

    public DomainsController(CustomDomainService domainService)
    {
        _domainService = domainService;
    }

    /// <summary>
    /// Register a new custom domain prefix.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomDomainResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterCustomDomainRequest request, CancellationToken cancellationToken)
    {
        var result = await _domainService.RegisterAsync(
            request.Prefix,
            request.Name,
            request.Description,
            cancellationToken);

        if (!result.IsSuccess)
            return MapError(result);

        var response = MapToResponse(result.Value!);
        return CreatedAtAction(nameof(List), null, response);
    }

    /// <summary>
    /// List all registered domain prefixes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomDomainResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery(Name = "active")] bool? active, CancellationToken cancellationToken)
    {
        var domains = await _domainService.ListAsync(active, cancellationToken);
        var response = domains.Select(d => MapToResponse(d)).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Deactivate a domain prefix.
    /// </summary>
    [HttpDelete("{prefix}")]
    [ProducesResponseType(typeof(CustomDomainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string prefix, CancellationToken cancellationToken)
    {
        var result = await _domainService.DeactivateAsync(prefix, cancellationToken);

        if (!result.IsSuccess)
            return MapError(result);

        var deactivation = result.Value!;
        var response = MapToResponse(deactivation.Domain, deactivation.Warning);
        return Ok(response);
    }

    private static CustomDomainResponse MapToResponse(CustomDomain domain, string? warning = null) => new()
    {
        Id = domain.Id.Value,
        Prefix = domain.Prefix.Value,
        Name = domain.Name,
        Description = domain.Description,
        IsActive = domain.IsActive,
        CreatedAt = domain.CreatedAt,
        Warning = warning
    };

    private IActionResult MapError<T>(Result<T> result)
    {
        var error = new ErrorResponse { Error = result.Error!, StatusCode = result.ErrorType switch
        {
            ErrorType.NotFound => 404,
            ErrorType.Conflict => 409,
            _ => 400
        }};

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(error),
            ErrorType.Conflict => Conflict(error),
            _ => BadRequest(error)
        };
    }
}
