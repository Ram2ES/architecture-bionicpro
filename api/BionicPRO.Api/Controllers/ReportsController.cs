using BionicPRO.Api.Models;
using BionicPRO.Api.Services;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IProsthesisAuthorizationService _prosthesisAuthService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportsService reportsService,
        IProsthesisAuthorizationService prosthesisAuthService,
        ILogger<ReportsController> logger)
    {
        _reportsService = reportsService;
        _prosthesisAuthService = prosthesisAuthService;
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
    /// <response code="401">Пользователь не аутентифицирован</response>
    /// <response code="403">Доступ запрещён (пользователь пытается получить чужие данные)</response>
    /// <response code="404">Отчёты не найдены</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("user/{userId:guid}")]
    [Authorize(Policy = "UserOwnsResource")]
    [ProducesResponseType(typeof(List<UserProsthesisReport>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<UserProsthesisReport>>> GetReportsByUserId(
        [FromRoute] Guid userId,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            var systemUserId = User.FindFirst("system_user_id")?.Value;
            _logger.LogInformation(
                "GET /api/reports/user/{UserId} called by user {SystemUserId}",
                userId, systemUserId);

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
    /// <response code="401">Пользователь не аутентифицирован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Отчёт не найден</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("user/{userId:guid}/latest")]
    [Authorize(Policy = "UserOwnsResource")]
    [ProducesResponseType(typeof(UserProsthesisReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserProsthesisReport>> GetLatestReportByUserId(
        [FromRoute] Guid userId)
    {
        try
        {
            var systemUserId = User.FindFirst("system_user_id")?.Value;
            _logger.LogInformation(
                "GET /api/reports/user/{UserId}/latest called by user {SystemUserId}",
                userId, systemUserId);

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
    /// <response code="401">Пользователь не аутентифицирован</response>
    /// <response code="403">Доступ запрещён (протез не принадлежит пользователю)</response>
    /// <response code="404">Отчёты не найдены</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("prosthesis/{prosthesisId:guid}")]
    [Authorize(Roles = "prothetic_user,administrator")]
    [ProducesResponseType(typeof(List<UserProsthesisReport>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<UserProsthesisReport>>> GetReportsByProsthesisId(
        [FromRoute] Guid prosthesisId,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            var systemUserIdClaim = User.FindFirst("system_user_id")?.Value;
            if (systemUserIdClaim == null || !Guid.TryParse(systemUserIdClaim, out var systemUserId))
            {
                return Unauthorized(new { error = "system_user_id claim missing or invalid" });
            }

            var isAdmin = User.IsInRole("administrator");

            _logger.LogInformation(
                "GET /api/reports/prosthesis/{ProsthesisId} called by user {SystemUserId} (admin: {IsAdmin})",
                prosthesisId, systemUserId, isAdmin);

            // Проверяем, принадлежит ли протез пользователю (если не администратор)
            if (!isAdmin)
            {
                var owns = await _prosthesisAuthService.UserOwnsProsthesisAsync(systemUserId, prosthesisId);
                if (!owns)
                {
                    _logger.LogWarning(
                        "User {SystemUserId} attempted to access prosthesis {ProsthesisId} they don't own",
                        systemUserId, prosthesisId);
                    return Forbid();
                }
            }

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
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
