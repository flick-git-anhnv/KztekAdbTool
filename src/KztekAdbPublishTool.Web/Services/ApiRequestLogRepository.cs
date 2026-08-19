using KztekAdbPublishTool.Web.Configuration;
using KztekAdbPublishTool.Web.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace KztekAdbPublishTool.Web.Services;

/// <summary>
/// Lưu bản ghi API request log vào SQLite — pattern mirror DeviceRepository (raw ADO.NET).
/// Constructor tạo bảng idempotent (CREATE TABLE IF NOT EXISTS) → an toàn qua restart.
/// Không dùng EF Core — nhất quán với pattern đã thiết lập.
/// </summary>
public sealed class ApiRequestLogRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ApiRequestLogRepository> _logger;

    public ApiRequestLogRepository(
        IOptions<AdbSettings> options,
        ILogger<ApiRequestLogRepository> logger)
    {
        _logger = logger;
        var dbPath = options.Value.DbPath;

        // Tạo thư mục cha nếu chưa có (critical trong container khi volume mount lần đầu)
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ApiRequestLog (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp      TEXT    NOT NULL,
                ApiName        TEXT    NOT NULL,
                HttpMethod     TEXT    NOT NULL,
                Path           TEXT    NOT NULL,
                Parameters     TEXT    NULL,
                Result         TEXT    NOT NULL,
                HttpStatusCode INTEGER NOT NULL,
                ErrorMessage   TEXT    NULL,
                CallerIp       TEXT    NOT NULL,
                DurationMs     INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_ApiRequestLog_Timestamp
                ON ApiRequestLog(Timestamp DESC);";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Insert 1 bản ghi log. Trả về Id vừa gán; -1 nếu lỗi.
    /// KHÔNG throw — exception được swallow + log (isolation: lỗi log KHÔNG fail request gốc).
    /// </summary>
    public async Task<long> InsertAsync(ApiRequestLogEntry entry, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);
            var cmd = conn.CreateCommand();
            // RETURNING Id: SQLite ≥ 3.35 (Microsoft.Data.Sqlite 8.x dùng SQLite ≥ 3.45)
            cmd.CommandText = @"
                INSERT INTO ApiRequestLog
                    (Timestamp, ApiName, HttpMethod, Path, Parameters, Result,
                     HttpStatusCode, ErrorMessage, CallerIp, DurationMs)
                VALUES
                    ($ts, $apiName, $method, $path, $params, $result,
                     $status, $errMsg, $callerIp, $durationMs)
                RETURNING Id;";
            cmd.Parameters.AddWithValue("$ts",         entry.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("$apiName",    entry.ApiName);
            cmd.Parameters.AddWithValue("$method",     entry.HttpMethod);
            cmd.Parameters.AddWithValue("$path",       entry.Path);
            cmd.Parameters.AddWithValue("$params",     (object?)entry.Parameters ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$result",     entry.Result);
            cmd.Parameters.AddWithValue("$status",     entry.HttpStatusCode);
            cmd.Parameters.AddWithValue("$errMsg",     (object?)entry.ErrorMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$callerIp",   entry.CallerIp);
            cmd.Parameters.AddWithValue("$durationMs", entry.DurationMs);

            var scalar = await cmd.ExecuteScalarAsync(ct);
            return scalar switch
            {
                long l  => l,
                int  i  => i,
                _       => -1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ApiRequestLogRepository.InsertAsync failed — apiName={ApiName}, path={Path}",
                entry.ApiName, entry.Path);
            return -1;
        }
    }
}
