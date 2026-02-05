namespace BionicPRO.Api.Configuration;

/// <summary>
/// Настройки подключения к ClickHouse
/// </summary>
public class ClickHouseSettings
{
    public const string SectionName = "ClickHouse";

    /// <summary>
    /// Хост ClickHouse
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Порт ClickHouse
    /// </summary>
    public int Port { get; set; } = 9000;

    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string Username { get; set; } = "default";

    /// <summary>
    /// Пароль
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// База данных
    /// </summary>
    public string Database { get; set; } = "bionic_reports";

    /// <summary>
    /// Строка подключения
    /// </summary>
    public string ConnectionString =>
        $"Host={Host};Port={Port};Username={Username};Password={Password};Database={Database}";
}
