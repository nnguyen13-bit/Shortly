using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Api.Contracts;
using Shortly.Api.Validation;
using Shortly.Application.Common;
using Shortly.Application.Links;
using Shortly.Domain.LinkManagement;

namespace Shortly.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class LinksController : ControllerBase
{
    private readonly LinkService _linkService;

    public LinksController(LinkService linkService)
    {
        _linkService = linkService;
    }

    /// <summary>
    /// Create a new short link.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LinkResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLinkRequest request, CancellationToken cancellationToken)
    {
        var sanitisedTags = request.Tags?.ToDictionary(
            kvp => InputSanitiser.Sanitise(kvp.Key)!,
            kvp => InputSanitiser.Sanitise(kvp.Value)!);

        var result = await _linkService.CreateAsync(
            request.DomainPrefix,
            request.DestinationUrl,
            InputSanitiser.Sanitise(request.CreatedBy)!,
            request.ExpiresAt,
            sanitisedTags,
            cancellationToken);

        if (!result.IsSuccess)
            return MapError(result);

        var response = MapToResponse(result.Value!);
        return CreatedAtAction(nameof(GetByPrefixAndCode),
            new { prefix = response.DomainPrefix, code = response.ShortCode },
            response);
    }

    /// <summary>
    /// Get link details by domain prefix and short code.
    /// </summary>
    [HttpGet("{prefix}/{code}")]
    [ProducesResponseType(typeof(LinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPrefixAndCode(string prefix, string code, CancellationToken cancellationToken)
    {
        var result = await _linkService.GetByPrefixAndCodeAsync(prefix, code, cancellationToken);

        if (!result.IsSuccess)
            return MapError(result);

        return Ok(MapToResponse(result.Value!));
    }

    /// <summary>
    /// Get link details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _linkService.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return MapError(result);

        return Ok(MapToResponse(result.Value!));
    }

    /// <summary>
    /// Disable a link.
    /// </summary>
    [HttpDelete("{prefix}/{code}")]
    [ProducesResponseType(typeof(LinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable(string prefix, string code, CancellationToken cancellationToken)
    {
        var result = await _linkService.DisableAsync(prefix, code, cancellationToken);

        if (!result.IsSuccess)
            return MapError(result);

        return Ok(MapToResponse(result.Value!));
    }

    private LinkResponse MapToResponse(Link link)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return new LinkResponse
        {
            Id = link.Id.Value,
            ShortCode = link.ShortCode.Value,
            DomainPrefix = link.DomainPrefix.Value,
            DestinationUrl = link.DestinationUrl.Value,
            Status = link.Status.ToString(),
            CreatedBy = link.CreatedBy,
            CreatedAt = link.CreatedAt,
            ExpiresAt = link.ExpiresAt,
            Tags = link.Metadata.Tags.Any()
                ? new Dictionary<string, string>(link.Metadata.Tags)
                : null,
            ShortUrl = $"{baseUrl}/{link.DomainPrefix.Value}/{link.ShortCode.Value}"
        };
    }

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
