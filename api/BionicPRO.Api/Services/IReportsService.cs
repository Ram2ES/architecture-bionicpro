using BionicPRO.Api.Models;

namespace BionicPRO.Api.Services;

/// <summary>
/// Интерфейс сервиса отчётов
/// </summary>
public interface IReportsService
{
    /// <summary>
    /// Получить отчёты по ID пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="startDate">Начальная дата (опционально)</param>
    /// <param name="endDate">Конечная дата (опционально)</param>
    /// <returns>Список отчётов</returns>
    Task<List<UserProsthesisReport>> GetReportsByUserIdAsync(
        Guid userId,
        DateOnly? startDate = null,
        DateOnly? endDate = null);

    /// <summary>
    /// Получить последний отчёт по ID пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Последний отчёт или null</returns>
    Task<UserProsthesisReport?> GetLatestReportByUserIdAsync(Guid userId);

    /// <summary>
    /// Получить отчёты по ID протеза
    /// </summary>
    /// <param name="prosthesisId">ID протеза</param>
    /// <param name="startDate">Начальная дата (опционально)</param>
    /// <param name="endDate">Конечная дата (опционально)</param>
    /// <returns>Список отчётов</returns>
    Task<List<UserProsthesisReport>> GetReportsByProsthesisIdAsync(
        Guid prosthesisId,
        DateOnly? startDate = null,
        DateOnly? endDate = null);
}
