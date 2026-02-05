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
    /// Получить отчёт пользователя в текстовом формате (для скачивания)
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Текстовый файл с отчётом</returns>
    /// <response code="200">Отчёт успешно сгенерирован</response>
    /// <response code="401">Пользователь не аутентифицирован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Отчёты не найдены</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("user/{userId:guid}/download")]
    [Authorize(Policy = "UserOwnsResource")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadUserReport([FromRoute] Guid userId)
    {
        try
        {
            var systemUserId = User.FindFirst("system_user_id")?.Value;
            _logger.LogInformation(
                "GET /api/reports/user/{UserId}/download called by user {SystemUserId}",
                userId, systemUserId);

            var reports = await _reportsService.GetReportsByUserIdAsync(userId, null, null);

            if (reports.Count == 0)
            {
                return NotFound(new { message = $"No reports found for user {userId}" });
            }

            var reportContent = GenerateMarkdownReport(reports);
            var bytes = System.Text.Encoding.UTF8.GetBytes(reportContent);

            var userName = reports.First().UserFullName.Replace(" ", "_");
            var fileName = $"BionicPRO_Report_{userName}_{DateTime.Now:yyyy-MM-dd}.md";

            return File(bytes, "text/markdown", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report download for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    private static string GenerateMarkdownReport(List<UserProsthesisReport> reports)
    {
        var report = reports.First();
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# BionicPRO - Отчёт о состоянии протезов");
        sb.AppendLine();
        sb.AppendLine($"**Пользователь:** {report.UserFullName}");
        sb.AppendLine($"**Email:** {report.UserEmail}");
        sb.AppendLine($"**Дата генерации:** {DateTime.Now:dd.MM.yyyy HH:mm}");
        sb.AppendLine($"**Период:** {reports.Min(r => r.ReportDate):dd.MM.yyyy} - {reports.Max(r => r.ReportDate):dd.MM.yyyy}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        sb.AppendLine("## Сводная статистика");
        sb.AppendLine();
        sb.AppendLine($"- **Количество отчётов:** {reports.Count}");
        sb.AppendLine($"- **Среднее время использования:** {reports.Average(r => r.TotalUsageHours):F1} часов");
        sb.AppendLine($"- **Всего движений:** {reports.Sum(r => r.TotalMovements):N0}");
        sb.AppendLine($"- **Средний заряд батареи:** {reports.Average(r => r.Battery.Average):F1}%");
        sb.AppendLine($"- **Средний показатель здоровья:** {reports.Average(r => r.HealthScore):F1}/100");
        sb.AppendLine($"- **Общее количество аномалий:** {reports.Sum(r => r.AnomalyCount)}");
        sb.AppendLine($"- **Общее количество предупреждений:** {reports.Sum(r => r.WarningCount)}");
        sb.AppendLine();

        var prosthesisGroups = reports.GroupBy(r => r.ProsthesisId);

        foreach (var group in prosthesisGroups)
        {
            var firstReport = group.First();
            sb.AppendLine($"## Протез: {firstReport.ProsthesisModel}");
            sb.AppendLine();
            sb.AppendLine($"**Серийный номер:** {firstReport.ProsthesisSerialNumber}");
            if (firstReport.PurchaseDate.HasValue)
            {
                sb.AppendLine($"**Дата покупки:** {firstReport.PurchaseDate.Value:dd.MM.yyyy}");
            }
            sb.AppendLine($"**Статус гарантии:** {firstReport.WarrantyStatus}");
            sb.AppendLine();

            sb.AppendLine("### Детальные данные");
            sb.AppendLine();
            sb.AppendLine("| Дата отчёта | Общее время (ч) | Движения | Батарея (%) | Здоровье | Аномалии | Предупр. |");
            sb.AppendLine("|-------------|----------------|----------|-------------|----------|----------|----------|");

            foreach (var r in group.OrderByDescending(r => r.ReportDate))
            {
                sb.AppendLine($"| {r.ReportDate:dd.MM.yyyy} | {r.TotalUsageHours:F1} | {r.TotalMovements:N0} | {r.Battery.Average:F1} | {r.HealthScore:F1}/100 | {r.AnomalyCount} | {r.WarningCount} |");
            }

            sb.AppendLine();

            var latestReport = group.OrderByDescending(r => r.ReportDate).First();
            sb.AppendLine("### Подробная статистика (последний отчёт)");
            sb.AppendLine();

            sb.AppendLine("**Движения:**");
            sb.AppendLine($"- Захваты: {latestReport.Movements.Grip:N0}");
            sb.AppendLine($"- Разжатия: {latestReport.Movements.Release:N0}");
            sb.AppendLine($"- Вращения: {latestReport.Movements.Rotation:N0}");
            sb.AppendLine($"- Точные движения: {latestReport.Movements.FineMotor:N0}");
            sb.AppendLine();

            sb.AppendLine("**Батарея:**");
            sb.AppendLine($"- Средний уровень: {latestReport.Battery.Average:F1}%");
            sb.AppendLine($"- Минимальный уровень: {latestReport.Battery.Min:F1}%");
            sb.AppendLine($"- Циклы зарядки: {latestReport.Battery.Cycles}");
            sb.AppendLine();

            sb.AppendLine("**Производительность:**");
            sb.AppendLine($"- Среднее время отклика: {latestReport.AvgResponseTimeMs:F1} мс");
            sb.AppendLine($"- 95-й перцентиль: {latestReport.P95ResponseTimeMs:F1} мс");
            sb.AppendLine($"- Средняя сила захвата: {latestReport.Grip.Average:F1} Н");
            sb.AppendLine($"- Макс. сила захвата: {latestReport.Grip.Max:F1} Н");
            sb.AppendLine();

            sb.AppendLine("**Температура:**");
            sb.AppendLine($"- Средняя: {latestReport.Temperature.Average:F1}°C");
            sb.AppendLine($"- Максимальная: {latestReport.Temperature.Max:F1}°C");
            sb.AppendLine($"- События перегрева: {latestReport.Temperature.OverheatingEvents}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Отчёт сгенерирован автоматически системой BionicPRO*");
        sb.AppendLine();
        sb.AppendLine("Для получения консультации обратитесь к специалисту по телефону: +7 (XXX) XXX-XX-XX");

        return sb.ToString();
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
