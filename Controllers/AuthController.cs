using ComricFraudCalculatorBackend.Models.Requests;
using ComricFraudCalculatorBackend.Models.Responses;
using ComricFraudCalculatorBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComricFraudCalculatorBackend.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(ITokenService tokenService) : ControllerBase
{
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> ExchangeToken(
        [FromBody] TokenRequest request,
        CancellationToken ct = default)
    {
        try
        {
            return Ok(await tokenService.ExchangeCredentialsAsync(request, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
