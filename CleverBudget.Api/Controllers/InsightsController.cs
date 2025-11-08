using Asp.Versioning;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CleverBudget.Api.Controllers;

[ApiVersion("2.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class InsightsController : ControllerBase
{
    private readonly IFinancialInsightService _financialInsightService;

    public InsightsController(IFinancialInsightService financialInsightService)
    {
        _financialInsightService = financialInsightService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FinancialInsightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] bool includeIncomeInsights = true,
        [FromQuery] bool includeExpenseInsights = true,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var filter = new FinancialInsightFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            CategoryId = categoryId,
            IncludeIncomeInsights = includeIncomeInsights,
            IncludeExpenseInsights = includeExpenseInsights
        };

        var insights = await _financialInsightService.GenerateInsightsAsync(userId, filter, cancellationToken);
        return Ok(insights);
    }
}
