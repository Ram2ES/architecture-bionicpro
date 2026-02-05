namespace BionicPRO.Api.Models;

/// <summary>
/// Модель отчёта о состоянии протеза пользователя
/// </summary>
public class UserProsthesisReport
{
    /// <summary>
    /// ID пользователя
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// ID протеза
    /// </summary>
    public Guid ProsthesisId { get; set; }

    /// <summary>
    /// Дата отчёта
    /// </summary>
    public DateOnly ReportDate { get; set; }

    /// <summary>
    /// Полное имя пользователя
    /// </summary>
    public string UserFullName { get; set; } = string.Empty;

    /// <summary>
    /// Email пользователя
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Модель протеза
    /// </summary>
    public string ProsthesisModel { get; set; } = string.Empty;

    /// <summary>
    /// Серийный номер протеза
    /// </summary>
    public string ProsthesisSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Дата покупки
    /// </summary>
    public DateOnly? PurchaseDate { get; set; }

    /// <summary>
    /// Статус гарантии (active/expired)
    /// </summary>
    public string WarrantyStatus { get; set; } = string.Empty;

    /// <summary>
    /// Общее время использования (часы)
    /// </summary>
    public double TotalUsageHours { get; set; }

    /// <summary>
    /// Общее количество движений
    /// </summary>
    public long TotalMovements { get; set; }

    /// <summary>
    /// Статистика по типам движений
    /// </summary>
    public MovementStatistics Movements { get; set; } = new();

    /// <summary>
    /// Статистика по сигналу
    /// </summary>
    public SignalStatistics Signal { get; set; } = new();

    /// <summary>
    /// Среднее время отклика (мс)
    /// </summary>
    public double AvgResponseTimeMs { get; set; }

    /// <summary>
    /// 95-й перцентиль времени отклика (мс)
    /// </summary>
    public double P95ResponseTimeMs { get; set; }

    /// <summary>
    /// Статистика по силе захвата
    /// </summary>
    public GripStatistics Grip { get; set; } = new();

    /// <summary>
    /// Статистика по батарее
    /// </summary>
    public BatteryStatistics Battery { get; set; } = new();

    /// <summary>
    /// Статистика по температуре
    /// </summary>
    public TemperatureStatistics Temperature { get; set; } = new();

    /// <summary>
    /// Количество аномалий
    /// </summary>
    public int AnomalyCount { get; set; }

    /// <summary>
    /// Количество предупреждений
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Показатель здоровья (0-100)
    /// </summary>
    public double HealthScore { get; set; }

    /// <summary>
    /// Дата создания записи
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата обновления записи
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Статистика по типам движений
/// </summary>
public class MovementStatistics
{
    public long Grip { get; set; }
    public long Release { get; set; }
    public long Rotation { get; set; }
    public long FineMotor { get; set; }
}

/// <summary>
/// Статистика по сигналу
/// </summary>
public class SignalStatistics
{
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
}

/// <summary>
/// Статистика по силе захвата
/// </summary>
public class GripStatistics
{
    public double Average { get; set; }
    public double Max { get; set; }
}

/// <summary>
/// Статистика по батарее
/// </summary>
public class BatteryStatistics
{
    public double Average { get; set; }
    public double Min { get; set; }
    public int Cycles { get; set; }
}

/// <summary>
/// Статистика по температуре
/// </summary>
public class TemperatureStatistics
{
    public double Average { get; set; }
    public double Max { get; set; }
    public int OverheatingEvents { get; set; }
}
