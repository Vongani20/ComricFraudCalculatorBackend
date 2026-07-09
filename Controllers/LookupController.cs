using ComricFraudCalculatorBackend.Authorization;
using ComricFraudCalculatorBackend.Models.Requests;
using ComricFraudCalculatorBackend.Models.Responses;
using ComricFraudCalculatorBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComricFraudCalculatorBackend.Controllers;

[ApiController]
[Route("api/v1/lookup")]
[Authorize(Policy = AuthPolicies.SignalsRead)]
public class LookupController(IFraudSignalService signalService) : ControllerBase
{
    [HttpPost("id-check")]
    public async Task<ActionResult<IdCheckResponse>> IdCheck(
        [FromBody] IdCheckRequest request,
        CancellationToken ct = default)
    {
        return Ok(await signalService.CheckIdAsync(request.IdNumber, ct));
    }
}
