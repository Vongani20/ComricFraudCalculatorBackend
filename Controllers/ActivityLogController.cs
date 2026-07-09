using ComricFraudCalculatorBackend.Authorization;
using ComricFraudCalculatorBackend.Models.Responses;
using ComricFraudCalculatorBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComricFraudCalculatorBackend.Controllers;

[ApiController]
[Route("api/v1/activity-log")]
[Authorize(Policy = AuthPolicies.AuditRead)]
public class ActivityLogController(IActivityLogService activityLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ActivityLogListResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await activityLogService.ListAsync(page, pageSize, ct));
    }
}
