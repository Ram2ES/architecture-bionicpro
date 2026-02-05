namespace BionicPRO.Api.Services;

/// <summary>
/// Сервис проверки принадлежности протеза пользователю
/// </summary>
public interface IProsthesisAuthorizationService
{
    /// <summary>
    /// Проверить, принадлежит ли протез пользователю
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="prosthesisId">ID протеза</param>
    /// <returns>True, если протез принадлежит пользователю</returns>
    Task<bool> UserOwnsProsthesisAsync(Guid userId, Guid prosthesisId);
}
