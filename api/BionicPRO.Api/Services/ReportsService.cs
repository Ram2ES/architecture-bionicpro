using BionicPRO.Api.Configuration;
using BionicPRO.Api.Models;
using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace BionicPRO.Api.Services;

/// <summary>
/// Сервис для работы с отчётами из ClickHouse
/// </summary>
public class ReportsService : IReportsService
{
    private readonly ClickHouseSettings _settings;
    private readonly ILogger<ReportsService> _logger;

    public ReportsService(
        IOptions<ClickHouseSettings> settings,
        ILogger<ReportsService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<List<UserProsthesisReport>> GetReportsByUserIdAsync(
        Guid userId,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        _logger.LogInformation(
            "Getting reports for user {UserId}, date range: {StartDate} - {EndDate}",
            userId, startDate, endDate);

        using var connection = new ClickHouseConnection(_settings.ConnectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT
                user_id,
                prosthesis_id,
                report_date,
                user_full_name,
                user_email,
                prosthesis_model,
                prosthesis_serial_number,
                purchase_date,
                warranty_status,
                total_usage_hours,
                total_movements,
                grip_movements,
                release_movements,
                rotation_movements,
                fine_motor_movements,
                avg_signal_strength,
                min_signal_strength,
                max_signal_strength,
                avg_response_time_ms,
                p95_response_time_ms,
                avg_grip_force,
                max_grip_force,
                avg_battery_level,
                min_battery_level,
                battery_cycles,
                avg_temperature,
                max_temperature,
                overheating_events,
                anomaly_count,
                warning_count,
                health_score,
                created_at,
                updated_at
            FROM dm_user_prosthesis_reports
            WHERE user_id = '{userId}'";

        if (startDate.HasValue)
        {
            query += $" AND report_date >= '{startDate.Value:yyyy-MM-dd}'";
        }

        if (endDate.HasValue)
        {
            query += $" AND report_date <= '{endDate.Value:yyyy-MM-dd}'";
        }

        query += " ORDER BY report_date DESC";

        using var command = connection.CreateCommand();
        command.CommandText = query;

        var reports = new List<UserProsthesisReport>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reports.Add(MapToReport(reader));
        }

        _logger.LogInformation("Found {Count} reports for user {UserId}", reports.Count, userId);
        return reports;
    }

    public async Task<UserProsthesisReport?> GetLatestReportByUserIdAsync(Guid userId)
    {
        _logger.LogInformation("Getting latest report for user {UserId}", userId);

        using var connection = new ClickHouseConnection(_settings.ConnectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT
                user_id,
                prosthesis_id,
                report_date,
                user_full_name,
                user_email,
                prosthesis_model,
                prosthesis_serial_number,
                purchase_date,
                warranty_status,
                total_usage_hours,
                total_movements,
                grip_movements,
                release_movements,
                rotation_movements,
                fine_motor_movements,
                avg_signal_strength,
                min_signal_strength,
                max_signal_strength,
                avg_response_time_ms,
                p95_response_time_ms,
                avg_grip_force,
                max_grip_force,
                avg_battery_level,
                min_battery_level,
                battery_cycles,
                avg_temperature,
                max_temperature,
                overheating_events,
                anomaly_count,
                warning_count,
                health_score,
                created_at,
                updated_at
            FROM dm_user_prosthesis_reports
            WHERE user_id = '{userId}'
            ORDER BY report_date DESC
            LIMIT 1";

        using var command = connection.CreateCommand();
        command.CommandText = query;

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var report = MapToReport(reader);
            _logger.LogInformation("Found latest report for user {UserId}, date: {ReportDate}",
                userId, report.ReportDate);
            return report;
        }

        _logger.LogInformation("No reports found for user {UserId}", userId);
        return null;
    }

    public async Task<List<UserProsthesisReport>> GetReportsByProsthesisIdAsync(
        Guid prosthesisId,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        _logger.LogInformation(
            "Getting reports for prosthesis {ProsthesisId}, date range: {StartDate} - {EndDate}",
            prosthesisId, startDate, endDate);

        using var connection = new ClickHouseConnection(_settings.ConnectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT
                user_id,
                prosthesis_id,
                report_date,
                user_full_name,
                user_email,
                prosthesis_model,
                prosthesis_serial_number,
                purchase_date,
                warranty_status,
                total_usage_hours,
                total_movements,
                grip_movements,
                release_movements,
                rotation_movements,
                fine_motor_movements,
                avg_signal_strength,
                min_signal_strength,
                max_signal_strength,
                avg_response_time_ms,
                p95_response_time_ms,
                avg_grip_force,
                max_grip_force,
                avg_battery_level,
                min_battery_level,
                battery_cycles,
                avg_temperature,
                max_temperature,
                overheating_events,
                anomaly_count,
                warning_count,
                health_score,
                created_at,
                updated_at
            FROM dm_user_prosthesis_reports
            WHERE prosthesis_id = '{prosthesisId}'";

        if (startDate.HasValue)
        {
            query += $" AND report_date >= '{startDate.Value:yyyy-MM-dd}'";
        }

        if (endDate.HasValue)
        {
            query += $" AND report_date <= '{endDate.Value:yyyy-MM-dd}'";
        }

        query += " ORDER BY report_date DESC";

        using var command = connection.CreateCommand();
        command.CommandText = query;

        var reports = new List<UserProsthesisReport>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reports.Add(MapToReport(reader));
        }

        _logger.LogInformation("Found {Count} reports for prosthesis {ProsthesisId}",
            reports.Count, prosthesisId);
        return reports;
    }

    private static UserProsthesisReport MapToReport(DbDataReader reader)
    {
        return new UserProsthesisReport
        {
            UserId = reader.GetGuid(0),
            ProsthesisId = reader.GetGuid(1),
            ReportDate = DateOnly.FromDateTime(reader.GetDateTime(2)),
            UserFullName = reader.GetString(3),
            UserEmail = reader.GetString(4),
            ProsthesisModel = reader.GetString(5),
            ProsthesisSerialNumber = reader.GetString(6),
            PurchaseDate = reader.IsDBNull(7) ? null : DateOnly.FromDateTime(reader.GetDateTime(7)),
            WarrantyStatus = reader.GetString(8),
            TotalUsageHours = Convert.ToDouble(reader.GetValue(9)),
            TotalMovements = Convert.ToInt64(reader.GetValue(10)),
            Movements = new MovementStatistics
            {
                Grip = Convert.ToInt64(reader.GetValue(11)),
                Release = Convert.ToInt64(reader.GetValue(12)),
                Rotation = Convert.ToInt64(reader.GetValue(13)),
                FineMotor = Convert.ToInt64(reader.GetValue(14))
            },
            Signal = new SignalStatistics
            {
                Average = Convert.ToDouble(reader.GetValue(15)),
                Min = Convert.ToDouble(reader.GetValue(16)),
                Max = Convert.ToDouble(reader.GetValue(17))
            },
            AvgResponseTimeMs = Convert.ToDouble(reader.GetValue(18)),
            P95ResponseTimeMs = Convert.ToDouble(reader.GetValue(19)),
            Grip = new GripStatistics
            {
                Average = Convert.ToDouble(reader.GetValue(20)),
                Max = Convert.ToDouble(reader.GetValue(21))
            },
            Battery = new BatteryStatistics
            {
                Average = Convert.ToDouble(reader.GetValue(22)),
                Min = Convert.ToDouble(reader.GetValue(23)),
                Cycles = Convert.ToInt32(reader.GetValue(24))
            },
            Temperature = new TemperatureStatistics
            {
                Average = Convert.ToDouble(reader.GetValue(25)),
                Max = Convert.ToDouble(reader.GetValue(26)),
                OverheatingEvents = Convert.ToInt32(reader.GetValue(27))
            },
            AnomalyCount = Convert.ToInt32(reader.GetValue(28)),
            WarningCount = Convert.ToInt32(reader.GetValue(29)),
            HealthScore = Convert.ToDouble(reader.GetValue(30)),
            CreatedAt = reader.GetDateTime(31),
            UpdatedAt = reader.GetDateTime(32)
        };
    }
}
