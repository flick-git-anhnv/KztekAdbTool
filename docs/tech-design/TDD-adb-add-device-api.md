---
id: TDD-adb-add-device-api
title: TDD — API Kết Nối Thiết Bị theo IP và Kiểm Tra Trạng Thái Kết Nối ADB
author: Tech Lead
created: 2026-08-19
status: draft
prd: docs/prd/PRD-adb-add-device-api.md
user_story: docs/user-stories/US-adb-add-device-api.md
sprint: docs/planning/SPRINT-adb-add-device-api.md
---

# TDD-adb-add-device-api: API Kết Nối Thiết Bị theo IP và Kiểm Tra Trạng Thái Kết Nối ADB

## Tham chiếu
- PRD: `docs/prd/PRD-adb-add-device-api.md`
- User Story: `docs/user-stories/US-adb-add-device-api.md`
- RESOURCE: `docs/planning/RESOURCE-adb-add-device-api.md`
- SPRINT: `docs/planning/SPRINT-adb-add-device-api.md`

---

## Assumption đã CHỐT (giải quyết Q1-Q7 của US)

| # | Câu hỏi mở (US) | Quyết định | Lý do |
|---|-----------------|-----------|-------|
| Q1 | `AdbService.ConnectAsync` có trả về serial thiết bị không? | **Không** — trả `AdbCommandResult { ExitCode, StdOut, StdErr, Success }`, không có field serial (đã đọc `AdbService.cs:133-134`). Nhưng **serial của thiết bị WiFi CHÍNH LÀ chuỗi `ip:port`** vừa dùng để connect (xem `DevicePollWorker.PollAsync` — `adb.Serial` được lấy từ output `adb devices -l` và với thiết bị WiFi luôn là `ip:port`). ⇒ Response echo lại `serial = "ip:port"` để client dùng ngay cho API 2. | Đã đọc trực tiếp source `AdbService.cs` + `DevicePollWorker.cs`, không suy đoán. Kết quả `adb connect` StdOut cũng dạng "connected to `ip:port`" — nhất quán. |
| Q2 | 404 hay 200 + `status: "NotFound"` khi serial không tồn tại? | **404 Not Found** | Nhất quán với pattern hiện có: `LaunchAppEndpoints.CheckDeviceState` đã trả 404 `{ success:false, error:"DeviceNotFound" }` cho case tương tự (SC-03 trong TDD launch-app). RESTful đúng ngữ nghĩa. BA đã đề xuất, Tech Lead xác nhận. |
| Q3 | Mức validate `ip`: non-empty hay regex IPv4 đầy đủ? | **Non-empty + trim + không chứa `:` và không chứa khoảng trắng nội bộ** (KHÔNG bắt regex IPv4 strict) | ADB accept cả hostname (VD `emulator-5554.local`) — regex IPv4 strict sẽ chặn hợp lệ. Kiểm tra `ip` chứa `:` để tránh nhầm với ipPort (vì port đã là field riêng). SC-A5 (regex IPv4) **merge vào SC-A4** theo ghi chú trong US. |
| Q4 | `port` range hợp lệ? | **1–65535** (nếu truyền); mặc định `5555` nếu null | Range TCP tiêu chuẩn. `0` và số âm là invalid. |
| Q5 | Response schema endpoint 1 khi thành công: chỉ `{ success, message }` hay bao gồm cả `stdOut/stdErr/serial`? | **Bao gồm `serial`, `message`, `stdOut`, `exitCode`** (không trả `stdErr` khi success — thường rỗng) | Client cần `serial` cho API 2. `stdOut` để debug (VD phân biệt "connected" vs "already connected"). Pattern giống `LaunchAppEndpoints.MapAdbResult` khi success. |
| Q6 | Route param `serial` rỗng: router 404 hay validation 400? | **ASP.NET Core router tự trả 404** (không match route `/api/devices//status` vì segment `serial` rỗng) — logic app KHÔNG chạy tới. SC-B4 → thực tế là **404 từ router**, không phải 400. | Đã xác nhận behavior mặc định ASP.NET Core Minimal API: route template `{serial}` yêu cầu non-empty segment; nếu client encode `%20` → segment có nội dung → app chạy tới validation → trả 400. Cả 2 hành vi đều phòng thủ được. |
| Q7 | Env var override: `LaunchApp__ApiKey` đúng convention Linux/Docker? | **Đúng — `LaunchApp__ApiKey` (2 dấu underscore đôi)** | Convention chuẩn ASP.NET Core cho colon (`:`) trong config key trên Linux (`Section:Key` → `Section__Key`). Đã áp dụng cho `/api/launch-app` production hiện tại — không đổi. |

---

## Goals / Non-goals

### Goals
1. Chốt API contract chi tiết cho 2 endpoint mới: request body, response schema, status code, error code.
2. Chỉ rõ vị trí file mới, đăng ký route trong `Program.cs`.
3. Chỉ định pattern tái dùng: `AdbService.ConnectAsync`, `DeviceState.TryGet`, `ApiKeyEndpointFilter`, `PollControlService.TriggerAsync`.
4. Cung cấp pseudocode đủ để Senior Developer code trực tiếp không cần suy đoán.
5. Đánh giá bảo mật sơ bộ, quyết định không escalate CTO (kèm lý do).

### Non-goals
- Không thay đổi `AdbService`, `DeviceState`, `ApiKeyEndpointFilter` — chỉ tái dùng nguyên trạng.
- Không đụng UI, không đụng SignalR hub.
- Không tạo section config mới; không thêm API key mới.
- Không viết integration test end-to-end với thiết bị thật (giao QA phase 4).

---

## Kiến trúc đề xuất

```mermaid
flowchart LR
    Client[Client CI/CD] -->|POST /api/devices/connect-by-ip<br/>x-api-key + { ip, port? }| Filter1[ApiKeyEndpointFilter]
    Client -->|GET /api/devices/{serial}/status<br/>x-api-key| Filter2[ApiKeyEndpointFilter]
    Filter1 -->|401 nếu sai key| C1[Response 401]
    Filter2 -->|401 nếu sai key| C2[Response 401]
    Filter1 --> EP1[POST connect-by-ip handler]
    Filter2 --> EP2[GET status handler]
    EP1 -->|validate ip, port| V1{Valid?}
    V1 -->|No| B1[400 InvalidInput]
    V1 -->|Yes| ADB[AdbService.ConnectAsync ip:port]
    ADB -->|ExitCode == 0| Poll[PollControlService.TriggerAsync]
    Poll --> OK1[200 OK + serial]
    ADB -->|ExitCode != 0| Err1[422 AdbConnectFailed + stderr]
    EP2 -->|validate serial| V2{Valid?}
    V2 -->|No| B2[400 InvalidInput]
    V2 -->|Yes| DS[DeviceState.TryGet serial]
    DS -->|Not found| NF[404 DeviceNotFound]
    DS -->|Found| OK2[200 OK + status Online/Offline]
```

---

## API Contract

### API 1 — `POST /api/devices/connect-by-ip`

#### Request

```
POST /api/devices/connect-by-ip HTTP/1.1
Content-Type: application/json
x-api-key: <API_KEY>

{
  "ip": "192.168.1.100",
  "port": 5555          // optional, default 5555 nếu null/không truyền
}
```

#### DTO

```csharp
public sealed class ConnectByIpRequest
{
    public string? Ip { get; set; }
    public int? Port { get; set; }
}
```

#### Response

| Status | Trigger | Body |
|--------|---------|------|
| **200 OK** | `AdbCommandResult.ExitCode == 0` (bao gồm cả "already connected" — idempotent) | `{ success: true, message: "Device connected.", serial: "192.168.1.100:5555", exitCode: 0, stdOut: "connected to 192.168.1.100:5555\n" }` |
| **400 Bad Request** | Validation fail (ip rỗng, port ngoài 1–65535, ip chứa `:` hoặc whitespace) | `{ success: false, error: "InvalidInput", message: "ip là bắt buộc" }` (hoặc message tương ứng lý do) |
| **401 Unauthorized** | Do `ApiKeyEndpointFilter` — thiếu/sai key | `{ success: false, error: "Unauthorized", message: "Invalid or missing API key." }` |
| **422 Unprocessable Entity** | `ExitCode != 0` (device không phản hồi, timeout, connection refused) | `{ success: false, error: "AdbConnectFailed", message: <stderr trimmed>, exitCode: <n>, stdOut: <>, stdErr: <> }` |
| **500 Internal Server Error** | ADB binary không tồn tại (`StdErr` bắt đầu `"Không tìm thấy adb tại:"`) | `{ success: false, error: "AdbNotFound", message: "adb binary not found on server." }` |

> **Idempotency (SC-A7):** ADB coi `adb connect <ip:port>` trên thiết bị đã connected là thành công (`ExitCode = 0`, StdOut: `"already connected to ip:port"`). Không cần logic đặc biệt phía server — map thẳng 200 OK.

> **Trigger poll sau connect thành công:** Bắt buộc gọi `PollControlService.TriggerAsync(ct)` sau khi `ConnectAsync` trả `Success == true`. Lý do: `DeviceState` chỉ được update khi `DevicePollWorker` chạy vòng poll — nếu không trigger, client gọi API 2 ngay sau đó có thể nhận 404 (thiết bị chưa được `DevicePollWorker` phát hiện). Chu kỳ poll mặc định = 3000ms (`AdbSettings.PollIntervalMs`); trigger giúp giảm delay từ tối đa 3s xuống ~200ms. **Đây là hành vi identical với `/api/devices/connect` nội bộ**, giữ nhất quán.

---

### API 2 — `GET /api/devices/{serial}/status`

#### Request

```
GET /api/devices/192.168.1.100:5555/status HTTP/1.1
x-api-key: <API_KEY>
```

> **Encoding:** Serial dạng `ip:port` chứa `:` — client PHẢI URL-encode thành `%3A` (VD: `/api/devices/192.168.1.100%3A5555/status`). ASP.NET Core tự decode segment trước khi truyền vào handler. Serial USB (không có `:`) không cần encode.

#### Response

| Status | Trigger | Body |
|--------|---------|------|
| **200 OK** | Serial tồn tại trong `DeviceState`, `Status == "Online"` | `{ success: true, serial: "192.168.1.100:5555", status: "Online" }` |
| **200 OK** | Serial tồn tại, `Status == "Offline"` | `{ success: true, serial: "192.168.1.100:5555", status: "Offline" }` |
| **400 Bad Request** | Serial (sau trim) rỗng (edge case khi client encode `%20` hoặc chuỗi khoảng trắng) | `{ success: false, error: "InvalidInput", message: "serial không được rỗng" }` |
| **401 Unauthorized** | Do `ApiKeyEndpointFilter` | `{ success: false, error: "Unauthorized", message: "Invalid or missing API key." }` |
| **404 Not Found** | Serial không có trong `DeviceState` cache (chưa từng kết nối hoặc đã bị Remove) | `{ success: false, error: "DeviceNotFound", message: "Device '{serial}' not found." }` |

> **Không gọi ADB trực tiếp** — API này thuần đọc `DeviceState.TryGet(serial, out device)`. Nếu `TryGet == true` → trả `device.Status` (đã là chuỗi `"Online"` hoặc `"Offline"` do `DevicePollWorker` set). Latency P95 mục tiêu ≤ 200ms (PRD metric).

> **Eventual consistency (EC2):** `DeviceState` update theo chu kỳ `DevicePollWorker` (3s). Kết quả trả có thể trễ tối đa 3s so với thực tế mạng. Chấp nhận được theo yêu cầu PRD — client cần strict real-time thì retry hoặc trigger `/api/devices/poll` trước khi hỏi.

---

## File / Namespace / Đăng ký

### Vị trí file mới

**Đề xuất:** Tạo file mới `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs`.

Lý do tách file (không mở rộng `DeviceEndpoints.cs`):
- `DeviceEndpoints.cs` chứa route nội bộ dùng cho UI (không auth) — 6 endpoint.
- 2 endpoint mới là **public API có auth**, thuộc nhóm khác về mặt policy → tách file giúp rõ ràng khi review, khi grep tìm điểm gắn `ApiKeyEndpointFilter`.
- Pattern nhất quán với `LaunchAppEndpoints.cs` (cùng nhóm auth-protected public API).

### Đăng ký trong `Program.cs`

Thêm 1 dòng sau `app.MapLaunchAppEndpoints();`:

```csharp
// ── Endpoints [adb-add-device-api] ────────────────────────────────────────────
app.MapDeviceConnectionEndpoints();
```

### DTO — nằm cùng file endpoint

```csharp
public sealed class ConnectByIpRequest
{
    public string? Ip { get; set; }
    public int? Port { get; set; }
}
```

Không cần DTO cho API 2 (chỉ route param `{serial}`).

---

## Tái dùng — Reuse Strategy

| Thành phần | Cách dùng | Note |
|-----------|-----------|------|
| `AdbService.ConnectAsync(string ipPort, int timeoutMs, CancellationToken ct)` | Truyền `ipPort = $"{ip.Trim()}:{port ?? 5555}"` | Timeout mặc định 10000ms — giữ nguyên |
| `DeviceState.TryGet(string serial, out DeviceRecord? device)` | Đọc cache; trả 404 nếu `false`; trả `device.Status` nếu `true` | Không thay đổi state |
| `ApiKeyEndpointFilter` | Gắn `.AddEndpointFilter<ApiKeyEndpointFilter>()` cho **cả 2** endpoint mới | Đọc `LaunchApp:ApiKey` fail-safe; không đổi filter |
| `PollControlService.TriggerAsync(ct)` | Gọi **chỉ khi connect thành công** trong API 1 | Không gọi trong API 2 (chỉ đọc cache) |
| **`EnsurePort` helper của `DeviceEndpoints.cs`** | **KHÔNG tái dùng** — input mới là `(ip, port?)` tách rời, không phải chuỗi `ipPort`. Xây dựng target inline: `$"{ip.Trim()}:{port ?? DefaultAdbPort}"` với hằng số `const int DefaultAdbPort = 5555;` trong file mới. | Tránh refactor `DeviceEndpoints.cs` không cần thiết (giữ `EnsurePort` private static như hiện tại). Nếu tương lai có thêm nơi thứ 3 dùng chung logic default port thì refactor sau. |

---

## Pseudocode Senior Developer implement

```csharp
// File: src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs
using KztekAdbPublishTool.Web.Services;
using KztekAdbPublishTool.Web.State;

namespace KztekAdbPublishTool.Web.Endpoints;

/// <summary>
/// Public API — kết nối thiết bị theo IP và kiểm tra trạng thái kết nối ADB.
/// Bảo vệ bằng header x-api-key qua ApiKeyEndpointFilter (tái dùng cấu hình LaunchApp:ApiKey).
/// </summary>
public static class DeviceConnectionEndpoints
{
    private const int DefaultAdbPort = 5555;
    private const int MinPort = 1;
    private const int MaxPort = 65535;

    public static IEndpointRouteBuilder MapDeviceConnectionEndpoints(this IEndpointRouteBuilder app)
    {
        // ── POST /api/devices/connect-by-ip ────────────────────────────────
        app.MapPost("/api/devices/connect-by-ip", async (
            ConnectByIpRequest req,
            AdbService adb,
            PollControlService poll,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(DeviceConnectionEndpoints));

            // Bước 1: Validate
            var ip = req.Ip?.Trim() ?? string.Empty;
            var port = req.Port ?? DefaultAdbPort;
            var err = ValidateConnectInput(ip, req.Port);
            if (err is not null)
            {
                logger.LogInformation("connect-by-ip invalid input: {Reason}", err);
                return Results.BadRequest(new { success = false, error = "InvalidInput", message = err });
            }

            // Bước 2: Gọi ADB
            var target = $"{ip}:{port}";
            var result = await adb.ConnectAsync(target, ct: ct);

            // Bước 3: Thành công → trigger poll để DeviceState update sớm
            if (result.Success)
            {
                await poll.TriggerAsync(ct);
                logger.LogInformation("connect-by-ip OK: {Target}, stdOut={StdOut}", target, result.StdOut.Trim());
                return Results.Ok(new
                {
                    success = true,
                    message = "Device connected.",
                    serial = target,
                    exitCode = result.ExitCode,
                    stdOut = result.StdOut
                });
            }

            // Bước 4: ADB binary missing → 500
            if (result.StdErr.StartsWith("Không tìm thấy adb tại:", StringComparison.Ordinal))
            {
                logger.LogError("ADB binary missing — {StdErr}", result.StdErr);
                return Results.Json(
                    new { success = false, error = "AdbNotFound", message = "adb binary not found on server." },
                    statusCode: 500);
            }

            // Bước 5: ADB connect fail → 422
            logger.LogWarning("connect-by-ip failed: {Target}, exitCode={ExitCode}, stdErr={StdErr}",
                target, result.ExitCode, result.StdErr.Trim());
            return Results.UnprocessableEntity(new
            {
                success = false,
                error = "AdbConnectFailed",
                message = string.IsNullOrWhiteSpace(result.StdErr) ? "ADB connect failed" : result.StdErr.Trim(),
                exitCode = result.ExitCode,
                stdOut = result.StdOut,
                stdErr = result.StdErr
            });
        })
        .AddEndpointFilter<ApiKeyEndpointFilter>();

        // ── GET /api/devices/{serial}/status ───────────────────────────────
        app.MapGet("/api/devices/{serial}/status", (
            string serial,
            DeviceState deviceState,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(DeviceConnectionEndpoints));

            var s = serial?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(s))
            {
                return Results.BadRequest(new { success = false, error = "InvalidInput", message = "serial không được rỗng" });
            }

            if (!deviceState.TryGet(s, out var device) || device is null)
            {
                logger.LogInformation("status: device not found — serial={Serial}", s);
                return Results.NotFound(new
                {
                    success = false,
                    error = "DeviceNotFound",
                    message = $"Device '{s}' not found."
                });
            }

            return Results.Ok(new
            {
                success = true,
                serial = device.Serial,
                status = device.Status // "Online" hoặc "Offline"
            });
        })
        .AddEndpointFilter<ApiKeyEndpointFilter>();

        return app;
    }

    /// <summary>Validate input cho connect-by-ip. Trả null nếu hợp lệ, message lỗi nếu không.</summary>
    internal static string? ValidateConnectInput(string ip, int? port)
    {
        if (string.IsNullOrEmpty(ip))
            return "ip là bắt buộc";
        if (ip.Contains(':', StringComparison.Ordinal))
            return "ip không được chứa ':' — dùng field port riêng";
        if (ip.Any(char.IsWhiteSpace))
            return "ip không được chứa khoảng trắng";
        if (port.HasValue && (port.Value < MinPort || port.Value > MaxPort))
            return $"port phải nằm trong khoảng {MinPort}–{MaxPort}";
        return null;
    }
}

public sealed class ConnectByIpRequest
{
    public string? Ip { get; set; }
    public int? Port { get; set; }
}
```

**Ghi chú style:** Method `ValidateConnectInput` để `internal static` để unit test truy cập trực tiếp (giống pattern `LaunchAppEndpoints.ValidateInput` public static — nhưng ở đây scope hẹp hơn nên `internal` là đủ).

---

## Rủi ro & Cách giảm thiểu

| # | Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|--------|-----------|------------|
| R1 | `DeviceState` cache trễ so với thực tế mạng (chu kỳ poll 3s) | Trung bình — client gọi API 2 ngay sau API 1 có thể nhận trạng thái cũ | Trigger `poll.TriggerAsync` sau connect thành công (giảm delay ~200ms). Document trong OpenAPI/README rằng cache có tính eventual consistency, retry sau 500ms nếu cần chắc chắn. |
| R2 | ADB `connect` timeout mặc định 10s — client giữ HTTP connection lâu | Thấp | Kestrel default request timeout đủ; nếu deploy behind reverse proxy (nginx) cần đặt `proxy_read_timeout ≥ 15s` (ghi trong DEPLOY doc) |
| R3 | `ApiKeyEndpointFilter` fail-safe: nếu `LaunchApp:ApiKey` không cấu hình → 401 mọi request | Thấp (tính năng chứ không phải bug) | Đã document trong `ApiKeyEndpointFilter.cs`. DEPLOY checklist verify env var `LaunchApp__ApiKey` được set trước khi bật container. |
| R4 | Serial WiFi luôn dạng `ip:port` — nhưng nếu client truyền `ip:5555` vào path segment không encode → route matching có thể lỗi (`:` bị hiểu là separator trong 1 số proxy) | Thấp | Document rõ trong response API 1 rằng `serial` cần URL-encode khi dùng làm path param API 2 (`%3A` thay cho `:`). Test case QA cần cover. |
| R5 | `adb connect` chạy đồng thời nhiều request cho cùng ip:port | Rất thấp | ADB tự idempotent (SC-A7). Không cần lock server-side. |
| R6 | Attack vector: brute-force API key qua endpoint mới | Thấp | Filter đã dùng `CryptographicOperations.FixedTimeEquals` chống timing attack. Rate limiting ngoài scope (Non-goals PRD); nếu cần thì đặt ở reverse proxy. |

---

## Đánh giá bảo mật (STRIDE sơ bộ)

| Threat | Đánh giá | Kết luận |
|--------|----------|----------|
| **S**poofing (giả mạo client) | Có API key + constant-time compare — ổn | ✅ |
| **T**ampering (sửa request) | Body JSON, không có signature. Chấp nhận được vì transport-level (HTTPS phải bật ở reverse proxy — DEPLOY doc note). | ✅ nếu deploy sau HTTPS |
| **R**epudiation (chối bỏ) | Có log request info trong endpoint (ip target, exitCode, stderr). Không có audit log dedicated (Non-goals). | ⚠️ chấp nhận theo scope PRD |
| **I**nformation disclosure | Response trả `stdErr` khi ADB fail — có thể lộ path adb binary hoặc thông tin server. **Đã trim và giới hạn** (chỉ nội dung ADB StdErr, không phải server stacktrace). | ✅ |
| **D**enial of service | ADB connect có timeout 10s → không hang. Không có rate limit — nếu client spam có thể tốn tài nguyên ADB. | ⚠️ đặt rate limit ở reverse proxy (ngoài scope) |
| **E**levation of privilege | Không có role/permission — chỉ 1 key duy nhất. Trong scope PRD. | ✅ |

**Kết luận:** KHÔNG có threat nhóm rủi ro cao (auth pattern đã được kiểm chứng ở `/api/launch-app`). Bước 3.2 sẽ chạy full `security-audit-stride` skill trước khi merge.

---

## Quyết định KHÔNG escalate CTO

- Feature **tái dùng nguyên trạng** `ApiKeyEndpointFilter` + config `LaunchApp:ApiKey` — không tạo cơ chế auth mới.
- Không đụng DB schema, không đụng SignalR contract, không đụng kiến trúc lõi.
- Surface attack mới chỉ là 2 route HTTP — vẫn nằm trong thiết kế minimal API đã có.
- Đã có `security-audit-stride` bắt buộc ở Bước 3.2 (Tech Lead review) — đủ để catch vấn đề bảo mật cần escalate nếu xuất hiện.
- **KHÔNG escalate CTO ở phase design.** Nếu Bước 3.2 phát hiện Fail nhóm rủi ro cao → khi đó mới escalate.

---

## Migration plan

Không có DB migration. Chỉ code changes:

1. Tạo file `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs`.
2. Thêm 1 dòng đăng ký trong `Program.cs`.
3. Không đổi `appsettings.json` (dùng đúng `LaunchApp:ApiKey` hiện có).
4. Không đổi Docker image contract (dùng lại env var `LaunchApp__ApiKey`).

**Backward compatibility:** 100% — không đụng route cũ (`/api/devices/connect`, `/api/install`, `/api/launch-app` vẫn nguyên hành vi).

---

## Task breakdown

| ID | Tên task | Owner | Estimate | Phụ thuộc |
|----|----------|-------|----------|-----------|
| T-2.1-A | Tạo `DeviceConnectionEndpoints.cs`, implement 2 endpoint theo pseudocode | Senior Developer | 2h | TDD (bước hiện tại) |
| T-2.1-B | Đăng ký `MapDeviceConnectionEndpoints` trong `Program.cs` | Senior Developer | 15m | T-2.1-A |
| T-2.1-C | Unit test cho `ValidateConnectInput` (10+ case: ip rỗng, ip whitespace, ip chứa `:`, port 0/65536/-1/null/hợp lệ) | Senior Developer | 1h | T-2.1-A |
| T-2.1-D | Unit test handler API 2 với `DeviceState` mock (Online/Offline/NotFound/serial rỗng) | Senior Developer | 1h | T-2.1-A |
| T-2.1-E | Integration test happy path 2 endpoint với `WebApplicationFactory` (auth OK, auth fail 401) | Senior Developer | 1.5h | T-2.1-A, T-2.1-B |
| T-2.1-F | Cập nhật CODE-GRAPH.md (thêm 2 endpoint, quan hệ ApiKeyEndpointFilter + AdbService + DeviceState) | Senior Developer | 30m | T-2.1-E |
| T-2.1-G | Cập nhật PR checklist + `/verify-pr` | Senior Developer | 30m | T-2.1-F |

**Tổng estimate: ~6.75h** — khớp cận trên với RESOURCE ước ~5-8h. Không đổi Sprint plan.

---

## Code Review Checklist (Tech Lead dùng ở Bước 3.2)

- [ ] 2 endpoint mới đều có `.AddEndpointFilter<ApiKeyEndpointFilter>()` — SC-A3, SC-B5
- [ ] Route cũ (`/api/devices/connect`, `/api/install`, `/api/launch-app`) không bị đụng — grep xác nhận
- [ ] Xử lý `ExitCode == 0` bao gồm cả "already connected" — SC-A7
- [ ] Có gọi `poll.TriggerAsync` sau khi connect thành công — SC-A1, SC-A2
- [ ] `serial` echo về trong response API 1 = đúng chuỗi `ip:port` đã dùng gọi ADB — Q1
- [ ] Validation `ip` chặn empty + whitespace + chứa `:` — SC-A4, EC6
- [ ] Validation `port` chặn ngoài range 1–65535 — EC5
- [ ] API 2 không gọi ADB — chỉ đọc `DeviceState` — BR5
- [ ] API 2 trả 404 khi serial không tồn tại (không phải 200) — Q2
- [ ] Log level đúng: `LogInformation` cho lỗi input, `LogWarning` cho ADB fail, `LogError` cho ADB missing binary
- [ ] Chạy `security-audit-stride` — không có Fail nhóm rủi ro cao
- [ ] CODE-GRAPH.md cập nhật với 2 node endpoint mới
- [ ] Unit test coverage: `ValidateConnectInput` + API 2 handler ≥ 90%; integration test happy path đủ 2 endpoint

---

*Tài liệu này do Tech Lead viết ngày 2026-08-19. Chuyển Senior Developer ở STEP-3.1.*
