using BionicPRO.Api.Models;
using BionicPRO.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BionicPRO.Api.Controllers;

/// <summary>
/// Контроллер для работы с отчётами о состоянии протезов
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportsService reportsService,
        ILogger<ReportsController> logger)
    {
        _reportsService = reportsService;
        _logger = logger;
    }

    /// <summary>
    /// Получить отчёты по ID пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="startDate">Начальная дата (формат: yyyy-MM-dd)</param>
    /// <param name="endDate">Конечная дата (формат: yyyy-MM-dd)</param>
    /// <returns>Список отчётов пользователя</returns>
    /// <response code="200">Отчёты успешно получены</response>
    /// <response code="400">Неверный формат параметров</response>
    /// <response code="404">Отчёты не найдены</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(List<UserProsthesisReport>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<UserProsthesisReport>>> GetReportsByUserId(
        [FromRoute] Guid userId,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            _logger.LogInformation("GET /api/reports/user/{UserId} called", userId);

            DateOnly? parsedStartDate = null;
            DateOnly? parsedEndDate = null;

            if (!string.IsNullOrEmpty(startDate))
            {
                if (!DateOnly.TryParse(startDate, out var sd))
                {
                    return BadRequest(new { error = "Invalid startDate format. Expected yyyy-MM-dd" });
                }
                parsedStartDate = sd;
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                if (!DateOnly.TryParse(endDate, out var ed))
                {
                    return BadRequest(new { error = "Invalid endDate format. Expected yyyy-MM-dd" });
                }
                parsedEndDate = ed;
            }

            var reports = await _reportsService.GetReportsByUserIdAsync(
                userId, parsedStartDate, parsedEndDate);

            if (reports.Count == 0)
            {
                return NotFound(new { message = $"No reports found for user {userId}" });
            }

            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Получить последний отчёт по ID пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Последний отчёт пользователя</returns>
    /// <response code="200">Отчёт успешно получен</response>
    /// <response code="404">Отчёт не найден</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("user/{userId:guid}/latest")]
    [ProducesResponseType(typeof(UserProsthesisReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserProsthesisReport>> GetLatestReportByUserId(
        [FromRoute] Guid userId)
    {
        try
        {
            _logger.LogInformation("GET /api/reports/user/{UserId}/latest called", userId);

            var report = await _reportsService.GetLatestReportByUserIdAsync(userId);

            if (report == null)
            {
                return NotFound(new { message = $"No reports found for user {userId}" });
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest report for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Получить отчёты по ID протеза
    /// </summary>
    /// <param name="prosthesisId">ID протеза</param>
    /// <param name="startDate">Начальная дата (формат: yyyy-MM-dd)</param>
    /// <param name="endDate">Конечная дата (формат: yyyy-MM-dd)</param>
    /// <returns>Список отчётов протеза</returns>
    /// <response code="200">Отчёты успешно получены</response>
    /// <response code="400">Неверный формат параметров</response>
    /// <response code="404">Отчёты не найдены</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("prosthesis/{prosthesisId:guid}")]
    [ProducesResponseType(typeof(List<UserProsthesisReport>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<UserProsthesisReport>>> GetReportsByProsthesisId(
        [FromRoute] Guid prosthesisId,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            _logger.LogInformation("GET /api/reports/prosthesis/{ProsthesisId} called", prosthesisId);

            DateOnly? parsedStartDate = null;
            DateOnly? parsedEndDate = null;

            if (!string.IsNullOrEmpty(startDate))
            {
                if (!DateOnly.TryParse(startDate, out var sd))
                {
                    return BadRequest(new { error = "Invalid startDate format. Expected yyyy-MM-dd" });
                }
                parsedStartDate = sd;
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                if (!DateOnly.TryParse(endDate, out var ed))
                {
                    return BadRequest(new { error = "Invalid endDate format. Expected yyyy-MM-dd" });
                }
                parsedEndDate = ed;
            }

            var reports = await _reportsService.GetReportsByProsthesisIdAsync(
                prosthesisId, parsedStartDate, parsedEndDate);

            if (reports.Count == 0)
            {
                return NotFound(new { message = $"No reports found for prosthesis {prosthesisId}" });
            }

            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports for prosthesis {ProsthesisId}", prosthesisId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Статус сервиса</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
