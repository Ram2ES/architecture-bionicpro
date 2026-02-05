using BionicPRO.Api.Configuration;
using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;

namespace BionicPRO.Api.Services;

/// <summary>
/// Сервис проверки принадлежности протеза пользователю через ClickHouse
/// </summary>
public class ProsthesisAuthorizationService : IProsthesisAuthorizationService
{
    private readonly ClickHouseSettings _settings;
    private readonly ILogger<ProsthesisAuthorizationService> _logger;

    public ProsthesisAuthorizationService(
        IOptions<ClickHouseSettings> settings,
        ILogger<ProsthesisAuthorizationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> UserOwnsProsthesisAsync(Guid userId, Guid prosthesisId)
    {
        try
        {
            using var connection = new ClickHouseConnection(_settings.ConnectionString);
            await connection.OpenAsync();

            var query = $@"
                SELECT count() as cnt
                FROM dm_user_prosthesis_reports
                WHERE user_id = '{userId}'
                  AND prosthesis_id = '{prosthesisId}'
                LIMIT 1";

            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var count = reader.GetFieldValue<ulong>(0); // ClickHouse count() returns UInt64
                var owns = count > 0;

                _logger.LogInformation(
                    "Prosthesis ownership check: user={UserId}, prosthesis={ProsthesisId}, owns={Owns}",
                    userId, prosthesisId, owns);

                return owns;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking prosthesis ownership: user={UserId}, prosthesis={ProsthesisId}",
                userId, prosthesisId);
            return false;
        }
    }
}
