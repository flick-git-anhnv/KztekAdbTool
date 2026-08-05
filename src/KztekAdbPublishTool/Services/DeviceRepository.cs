using System.Globalization;
using KztekAdbPublishTool.Models;
using Microsoft.Data.Sqlite;

namespace KztekAdbPublishTool.Services;

/// <summary>
/// Lưu trạng thái thiết bị + phần mềm/version đã cài vào SQLite local (adbpublishtool.db).
/// </summary>
public sealed class DeviceRepository
{
    private readonly string _connectionString;

    public DeviceRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Devices (
                Serial TEXT PRIMARY KEY,
                Model TEXT,
                ConnectionType TEXT,
                Status TEXT,
                InstalledVersion TEXT,
                LastInstallStatus TEXT,
                LastInstallTime TEXT,
                FirstSeen TEXT,
                LastSeen TEXT
            );
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );";
        cmd.ExecuteNonQuery();
    }

    public void Upsert(DeviceRecord device)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Devices (Serial, Model, ConnectionType, Status, InstalledVersion, LastInstallStatus, LastInstallTime, FirstSeen, LastSeen)
            VALUES ($serial, $model, $conn, $status, $ver, $lastStatus, $lastTime, $first, $last)
            ON CONFLICT(Serial) DO UPDATE SET
                Model = excluded.Model,
                ConnectionType = excluded.ConnectionType,
                Status = excluded.Status,
                InstalledVersion = COALESCE(excluded.InstalledVersion, Devices.InstalledVersion),
                LastInstallStatus = COALESCE(excluded.LastInstallStatus, Devices.LastInstallStatus),
                LastInstallTime = COALESCE(excluded.LastInstallTime, Devices.LastInstallTime),
                LastSeen = excluded.LastSeen;";
        cmd.Parameters.AddWithValue("$serial", device.Serial);
        cmd.Parameters.AddWithValue("$model", device.Model ?? string.Empty);
        cmd.Parameters.AddWithValue("$conn", device.ConnectionType ?? string.Empty);
        cmd.Parameters.AddWithValue("$status", device.Status ?? string.Empty);
        cmd.Parameters.AddWithValue("$ver", (object?)device.InstalledVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$lastStatus", (object?)device.LastInstallStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$lastTime", (object?)device.LastInstallTime?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$first", device.FirstSeen.ToString("o"));
        cmd.Parameters.AddWithValue("$last", device.LastSeen.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void UpdateInstallResult(string serial, string status, DateTime time, string? version)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Devices
            SET LastInstallStatus = $status,
                LastInstallTime = $time,
                InstalledVersion = COALESCE($ver, InstalledVersion)
            WHERE Serial = $serial;";
        cmd.Parameters.AddWithValue("$status", status);
        cmd.Parameters.AddWithValue("$time", time.ToString("o"));
        cmd.Parameters.AddWithValue("$ver", (object?)version ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$serial", serial);
        cmd.ExecuteNonQuery();
    }

    public void Remove(string serial)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Devices WHERE Serial = $serial;";
        cmd.Parameters.AddWithValue("$serial", serial);
        cmd.ExecuteNonQuery();
    }

    public List<DeviceRecord> GetAll()
    {
        var list = new List<DeviceRecord>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Serial, Model, ConnectionType, Status, InstalledVersion,
                                    LastInstallStatus, LastInstallTime, FirstSeen, LastSeen
                             FROM Devices ORDER BY LastSeen DESC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new DeviceRecord
            {
                Serial = reader.GetString(0),
                Model = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                ConnectionType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Status = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                InstalledVersion = reader.IsDBNull(4) ? null : reader.GetString(4),
                LastInstallStatus = reader.IsDBNull(5) ? null : reader.GetString(5),
                LastInstallTime = reader.IsDBNull(6) ? null : ParseDate(reader.GetString(6)),
                FirstSeen = ParseDate(reader.GetString(7)),
                LastSeen = ParseDate(reader.GetString(8)),
            });
        }
        return list;
    }

    public string? GetSetting(string key)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Settings WHERE Key = $key;";
        cmd.Parameters.AddWithValue("$key", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetSetting(string key, string value)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Settings (Key, Value) VALUES ($key, $value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", value);
        cmd.ExecuteNonQuery();
    }

    private static DateTime ParseDate(string value)
        => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
