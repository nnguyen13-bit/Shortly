using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Application.Links;

namespace Shortly.Api.Controllers;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class RedirectController : ControllerBase
{
    private readonly RedirectService _redirectService;

    public RedirectController(RedirectService redirectService)
    {
        _redirectService = redirectService;
    }

    /// <summary>
    /// Resolve a short link and redirect to the destination URL.
    /// </summary>
    [HttpGet("{prefix:regex(^[[a-z0-9]]{{2,4}}$)}/{code:regex(^[[a-zA-Z0-9]]{{5,8}}$)}")]
    public async Task<IActionResult> Redirect(string prefix, string code, CancellationToken cancellationToken)
    {
        var result = await _redirectService.ResolveAsync(prefix, code, cancellationToken);

        if (result.IsGone)
            return StatusCode(StatusCodes.Status410Gone, new { error = "This link is no longer available." });

        if (!result.IsFound)
            return NotFound(new { error = "Short link not found." });

        return new Microsoft.AspNetCore.Mvc.RedirectResult(result.DestinationUrl!, permanent: false);
    }
}
