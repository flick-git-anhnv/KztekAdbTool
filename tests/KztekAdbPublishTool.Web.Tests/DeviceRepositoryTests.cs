using KztekAdbPublishTool.Web.Configuration;
using KztekAdbPublishTool.Web.Models;
using KztekAdbPublishTool.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Xunit;

namespace KztekAdbPublishTool.Web.Tests;

public sealed class DeviceRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DeviceRepository _repo;

    public DeviceRepositoryTests()
    {
        // Dùng file tạm riêng biệt cho mỗi test instance — tránh xung đột song song
        _dbPath = Path.Combine(Path.GetTempPath(), $"test-adb-{Guid.NewGuid():N}.db");
        var options = Options.Create(new AdbSettings { DbPath = _dbPath });
        _repo = new DeviceRepository(options);
    }

    [Fact]
    public void Upsert_ThenGetAll_ReturnsSameRecord()
    {
        var now = DateTime.UtcNow;
        var device = new DeviceRecord
        {
            Serial = "192.168.1.100:5555",
            Model = "Test Device",
            ConnectionType = "WiFi",
            Status = "Online",
            InstalledVersion = "1.0.0",
            FirstSeen = now,
            LastSeen = now
        };

        _repo.Upsert(device);
        var all = _repo.GetAll();

        Assert.Single(all);
        var saved = all[0];
        Assert.Equal(device.Serial, saved.Serial);
        Assert.Equal(device.Model, saved.Model);
        Assert.Equal(device.ConnectionType, saved.ConnectionType);
        Assert.Equal(device.Status, saved.Status);
        Assert.Equal(device.InstalledVersion, saved.InstalledVersion);
    }

    [Fact]
    public void Upsert_Twice_UpdatesExistingRecord()
    {
        var now = DateTime.UtcNow;
        var device = new DeviceRecord
        {
            Serial = "192.168.1.101:5555",
            Model = "Device A",
            ConnectionType = "WiFi",
            Status = "Online",
            FirstSeen = now,
            LastSeen = now
        };

        _repo.Upsert(device);

        device.Status = "Offline";
        device.LastSeen = now.AddSeconds(3);
        _repo.Upsert(device);

        var all = _repo.GetAll();
        Assert.Single(all);
        Assert.Equal("Offline", all[0].Status);
    }

    [Fact]
    public void Remove_DeletesRecord()
    {
        var now = DateTime.UtcNow;
        _repo.Upsert(new DeviceRecord
        {
            Serial = "10.0.0.1:5555",
            Model = "To Remove",
            ConnectionType = "WiFi",
            Status = "Online",
            FirstSeen = now,
            LastSeen = now
        });

        _repo.Remove("10.0.0.1:5555");

        Assert.Empty(_repo.GetAll());
    }

    [Fact]
    public void GetSetting_SetSetting_RoundTrip()
    {
        _repo.SetSetting("PackageName", "com.example.app");
        var value = _repo.GetSetting("PackageName");
        Assert.Equal("com.example.app", value);
    }

    [Fact]
    public void GetSetting_MissingKey_ReturnsNull()
    {
        var value = _repo.GetSetting("NonExistentKey");
        Assert.Null(value);
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite dùng connection pool — file handle vẫn được giữ sau khi conn.Dispose().
        // Phải gọi ClearAllPools() trước khi xóa file, nếu không sẽ IOException trên Windows.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
