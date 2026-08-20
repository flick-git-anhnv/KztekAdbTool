---
feature: adb-uninstall-before-install
priority: P2
tech_lead: Tech Lead
created: 2026-08-20
plan: docs/plans/PLAN-adb-uninstall-before-install-2026-08-20/PLAN-MASTER.md
---

# TDD-adb-uninstall-before-install: Tùy chọn Gỡ cài đặt App trước khi Cài đặt

## Tham chiếu
- PRD: `docs/prd/PRD-adb-uninstall-before-install.md`
- User Story: `docs/user-stories/US-adb-uninstall-before-install.md` (5 US, 13 scenario)
- Resource: `docs/planning/RESOURCE-adb-uninstall-before-install.md`
- Sprint: `docs/planning/SPRINT-adb-uninstall-before-install.md`
- Code liên quan (không đổi kiến trúc): `IAdbService.cs`, `AdbService.cs`, `InstallCoordinator.cs`, `InstallEndpoints.cs`, `DeviceEndpoints.cs`, `Index.cshtml`, `Index.cshtml.cs`, `wwwroot/js/dashboard.js`, `DeviceRepository.cs`

## Goals / Non-goals
- Goals:
  - G1: Thêm 1 checkbox trên Dashboard (id `chk-uninstall-before-install`) mặc định TẮT.
  - G2: Khi BẬT, `InstallCoordinator.DoInstallAsync` chạy `adb -s <serial> uninstall <package>` TRƯỚC `adb install -r`; luôn graceful — mọi lỗi uninstall chỉ log warning, không abort install.
  - G3: SignalR gửi thêm 2 message tiến độ ("Đang gỡ cài đặt...", "Đã gỡ cài đặt" / "Bỏ qua gỡ") khi flag BẬT; giữ nguyên hoàn toàn 6 progress step hiện tại khi flag TẮT.
  - G4: Trạng thái checkbox persist qua DB Settings (key `UninstallBeforeInstall`, value `"true"`/`"false"`), nhất quán pattern `PackageName`/`ApkPath`/`PollingEnabled`.
- Non-goals:
  - Không đưa `UninstallApkAsync` vào interface `IAdbService` (interface chỉ phục vụ mock DevicePollWorker — xem §Kiến trúc).
  - Không thêm cơ chế retry uninstall.
  - Không đổi UX/text của luồng install khi checkbox TẮT.
  - Không hỗ trợ per-user setting (KztekAdbPublishTool.Web single-tenant, DB Settings global).

## Kiến trúc đề xuất

```mermaid
sequenceDiagram
    participant U as User (Dashboard)
    participant JS as dashboard.js
    participant API as /api/install
    participant IC as InstallCoordinator
    participant ADB as AdbService
    participant Hub as SignalR DeviceHub

    U->>JS: Toggle chk-uninstall-before-install
    JS->>API: POST /api/settings/uninstall-before-install {enabled: true}
    API->>API: repo.SetSetting("UninstallBeforeInstall","true")

    U->>JS: Click Install
    JS->>API: POST /api/install {serials, selectedOnly, uninstallBeforeInstall: true}
    API->>IC: QueueInstalls(serials, pkg, apk, uninstallBeforeInstall=true)
    loop for each serial (per-device + global semaphore)
        IC->>Hub: InstallProgress(serial, 0, "Đang gỡ cài đặt <pkg>...")
        IC->>ADB: UninstallApkAsync(serial, pkg, ct)
        ADB-->>IC: AdbCommandResult
        alt Success (StdOut contains "Success")
            IC->>Hub: InstallProgress(serial, 15, "Đã gỡ cài đặt <pkg>")
        else Any failure (package chưa cài / MDM lock / other)
            IC->>Hub: InstallProgress(serial, 15, "Bỏ qua gỡ (chưa cài hoặc bị chặn)")
            Note over IC: LogWarning với StdOut+StdErr chi tiết; KHÔNG abort
        end
        IC->>Hub: InstallProgress(serial, 25, "Đang lấy danh sách package...")
        IC->>ADB: InstallApkAsync (flow cũ)
        Note over IC,ADB: các step cũ (40/55/70/85%) chạy tiếp với percent đã shift
        IC->>Hub: DeviceInstalled(serial, success, version) + FinishWithStatus 100%
    end
```

Nguyên tắc: KHÔNG chèn `UninstallApkAsync` vào `IAdbService` — interface hiện tại chỉ chứa 3 method (`GetDevicesAsync`, `ConnectAsync`, `GetPackageVersionAsync`) phục vụ mock cho `DevicePollWorker`. `InstallApkAsync`, `LaunchAppAsync`, `ListThirdPartyPackagesAsync` đã inject `AdbService` cụ thể — `UninstallApkAsync` giữ đúng convention đó.

## Quyết định chốt cho 3 câu hỏi mở (Handoff Payload 1.4)

### D1 — AC5/Q1: LƯU trạng thái checkbox vào DB Settings
- **Chốt:** CÓ lưu. Key = `"UninstallBeforeInstall"`, value string `"true"`/`"false"` (parse bằng `bool.TryParse`, fallback `false` — EC5).
- **Lý do:** AC5 trong PRD yêu cầu persist; nhất quán tuyệt đối với 3 setting hiện có (`PackageName`, `ApkPath`, `PollingEnabled`) — cùng dùng `DeviceRepository.GetSetting`/`SetSetting` (khớp lược đồ SQLite Settings key-value hiện tại, không thêm bảng mới).
- **Load timing:** `IndexModel.OnGet` đọc DB Settings; Razor render `checked` attribute conditional; JS đọc `checkbox.checked` từ DOM ban đầu (không cần fetch riêng).
- **Save timing:** JS gọi `POST /api/settings/uninstall-before-install` ngay ở event `change` của checkbox (không đợi Install click) — pattern giống `txt-package` blur handler.

### D2 — Q2: Phân biệt lỗi "package chưa cài" vs lỗi ADB thực sự
- **Chốt (rút gọn):** **LUÔN graceful — mọi lỗi uninstall đều log WARNING và tiếp tục install; không phân biệt case, không abort.**
- **Lý do:**
  - Uninstall là bước tùy chọn tiền xử lý (BR3, US-003). Không được là single point of failure cho luồng install.
  - Nếu là "package chưa cài" → install `-r` chạy thành công bình thường (thiết bị mới xuất xưởng, US-003).
  - Nếu là MDM/policy lock uninstall → install `-r` VẪN có thể thành công (chỉ uninstall bị chặn, install cùng cert vẫn OK). Nếu thất bại, error message của `adb install` rõ ràng hơn cho user.
  - Nếu là device offline/timeout → install ngay sau đó cũng sẽ fail với message rõ hơn → user thấy 1 lỗi thay vì 2.
- **Detection để chọn message hiển thị (không dùng để quyết abort):** `AdbCommandResult.Success == true && StdOut chứa "Success"` (case-insensitive) → tiến trình gửi `"Đã gỡ cài đặt <package>"`. Ngược lại (kể cả exit code 0 với stdout "Failure [...]") → gửi `"Bỏ qua gỡ (chưa cài hoặc bị chặn)"` + log warning kèm nguyên `StdOut`/`StdErr` để DevOps điều tra sau.
- **Log format:** `_logger.LogWarning("Uninstall {Package} trên {Serial} không thành công (vẫn tiếp tục install). StdOut={StdOut} StdErr={StdErr}", packageName, serial, result.StdOut.Trim(), result.StdErr.Trim());`
- **Ngoại lệ propagate:** `OperationCanceledException` (timeout tổng 10 phút hoặc app shutdown) vẫn được `try/catch` cũ trong `DoInstallAsync` xử lý — không thay đổi.

### D3 — EC1: MDM/device admin khóa uninstall
- **Chốt:** Cùng ứng xử D2 — graceful-skip. Log warning; tiếp tục install. Không hiện toast/lỗi UI riêng cho case này.
- **Lý do:** Xem D2. Một tương tự đơn giản: nếu MDM cấm uninstall thì rất có thể cũng cấm install → install fail sẽ tự báo rõ. Không cần rẽ nhánh riêng khiến code phức tạp thêm mà không có value thực tế.

## API Contract

### 1. Method mới trong `AdbService` (KHÔNG thêm vào `IAdbService`)

```csharp
// src/KztekAdbPublishTool.Web/Services/AdbService.cs
/// <summary>
/// Gỡ cài đặt package trên thiết bị. LUÔN được coi là graceful ở tầng caller —
/// caller (InstallCoordinator) chỉ dùng result để chọn message SignalR + log,
/// không abort luồng install dù uninstall thất bại. Xem TDD-adb-uninstall-before-install §D2.
/// </summary>
public Task<AdbCommandResult> UninstallApkAsync(string serial, string packageName, CancellationToken ct = default)
    => RunAsync($"-s {serial} uninstall {packageName}", timeoutMs: 30000, ct: ct);
```

Ghi chú:
- Không dùng `-k` flag (giữ cache/data) — mục tiêu clean install.
- Không dùng `--user 0` — hành vi mặc định adb đủ; adb hỗ trợ dạng đơn giản `uninstall <pkg>`.
- Không cần escape `packageName` bằng dấu ngoặc kép (unlike `apkPath`) — package name chỉ chứa `[a-zA-Z0-9._]`, không có space.
- Timeout 30s: uninstall thường 1–3s; 30s là safety net rộng gấp 10x.

### 2. `InstallRequest` thêm field mới

```csharp
// src/KztekAdbPublishTool.Web/Endpoints/InstallEndpoints.cs
public sealed class InstallRequest
{
    public List<string>? Serials { get; set; }
    public bool SelectedOnly { get; set; }
    public string? PackageName { get; set; }
    public string? ApkPath { get; set; }

    /// <summary>Khi true → chạy adb uninstall trước adb install. Mặc định false (giữ hành vi cũ).</summary>
    public bool UninstallBeforeInstall { get; set; }   // ← THÊM MỚI
}
```

### 3. Endpoint mới: `POST /api/settings/uninstall-before-install`

```csharp
// src/KztekAdbPublishTool.Web/Endpoints/DeviceEndpoints.cs (thêm vào MapDeviceEndpoints)
app.MapPost("/api/settings/uninstall-before-install", (
    UninstallBeforeInstallSettingRequest req,
    DeviceRepository repo) =>
{
    repo.SetSetting("UninstallBeforeInstall", req.Enabled ? "true" : "false");
    return Results.Ok(new { ok = true, enabled = req.Enabled });
});

// Thêm cuối file (khu Request DTOs)
public sealed record UninstallBeforeInstallSettingRequest(bool Enabled);
```

Contract:
```
POST /api/settings/uninstall-before-install
Content-Type: application/json
Body: { "enabled": true }
Response 200: { "ok": true, "enabled": true }
```

Không xác thực bổ sung — trùng pattern các endpoint settings hiện có.

### 4. `POST /api/install` — signature body chỉ thay đổi:

```
Body (thêm 1 field):
{
  "serials": ["ABC123", ...],
  "selectedOnly": true,
  "uninstallBeforeInstall": true    // ← THÊM MỚI, default false nếu thiếu
}
```
Response giữ nguyên.

### 5. `InstallCoordinator.QueueInstalls` — signature thay đổi

```csharp
public int QueueInstalls(
    IEnumerable<string> serials,
    string packageName,
    string apkPath,
    bool uninstallBeforeInstall)   // ← THÊM MỚI (KHÔNG có default value — bắt caller khai báo rõ để không ai vô tình bỏ qua)
```

`InstallOneAsync` và `DoInstallAsync` cùng thêm `bool uninstallBeforeInstall`.

## Database schema thay đổi
Không đổi schema. Chỉ thêm 1 row mới trong bảng `Settings` khi user tương tác lần đầu:

```
Key = "UninstallBeforeInstall"
Value = "true" | "false"
```

Không cần migration DDL — bảng `Settings` đã tồn tại.

## Thay đổi UI (Index.cshtml)

### Vị trí checkbox
Đặt ở **hàng 3 toolbar**, **ngay sau** `chk-auto-detect` (cùng nhóm option cấu hình luồng install), cùng padding `ms-2`:

```html
<!-- Trong Hàng 3 toolbar, thêm sau div chứa chk-auto-detect -->
<div class="col-auto ms-2">
    <div class="form-check form-switch mb-0">
        <input class="form-check-input" type="checkbox"
               id="chk-uninstall-before-install"
               @(Model.UninstallBeforeInstall ? "checked" : "")>
        <label class="form-check-label small" for="chk-uninstall-before-install">
            Gỡ cài đặt app trước khi cài
        </label>
    </div>
</div>
```

Không có tooltip riêng; label đã tự giải thích. UX Reviewer đánh giá ở step 3.3.

### `IndexModel` thêm property

```csharp
// src/KztekAdbPublishTool.Web/Pages/Index.cshtml.cs
public bool UninstallBeforeInstall { get; private set; }

public void OnGet()
{
    Devices = _repo.GetAll();
    PackageName = _repo.GetSetting("PackageName") ?? string.Empty;
    ApkPath = _repo.GetSetting("ApkPath") ?? string.Empty;
    // EC5: fallback false nếu key chưa có / parse thất bại
    UninstallBeforeInstall = bool.TryParse(_repo.GetSetting("UninstallBeforeInstall"), out var v) && v;
}
```

## Thay đổi JS (dashboard.js)

Thêm 3 chỗ:

### JS-1: Handler `change` lưu setting (đặt gần txt-package blur handler ~ dòng 452)

```javascript
const chkUninstall = $id('chk-uninstall-before-install');
if (chkUninstall) {
    chkUninstall.addEventListener('change', async function () {
        try {
            await apiPost('/api/settings/uninstall-before-install', { enabled: chkUninstall.checked });
        } catch (ex) {
            appendLog('Lỗi lưu tuỳ chọn uninstall: ' + ex.message);
        }
    });
}
```

### JS-2 + JS-3: Truyền flag vào 2 handler Install (dòng ~466 và ~493)

```javascript
// btn-install-selected click handler — thay dòng apiPost hiện tại:
const uninstallBeforeInstall = $id('chk-uninstall-before-install')?.checked === true;
const r = await apiPost('/api/install', {
    serials: serials,
    selectedOnly: true,
    uninstallBeforeInstall: uninstallBeforeInstall
});

// btn-install-all click handler — tương tự:
const uninstallBeforeInstall = $id('chk-uninstall-before-install')?.checked === true;
const r = await apiPost('/api/install', {
    serials: [],
    selectedOnly: false,
    uninstallBeforeInstall: uninstallBeforeInstall
});
```

## Thay đổi `InstallCoordinator.DoInstallAsync` (pseudocode)

```csharp
private async Task DoInstallAsync(string serial, string packageName, string apkPath, bool uninstallBeforeInstall)
{
    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
    var ct = timeoutCts.Token;

    try
    {
        // ── BƯỚC MỚI: Uninstall (chỉ khi flag BẬT + có packageName) ─────────────
        if (uninstallBeforeInstall && !string.IsNullOrWhiteSpace(packageName))
        {
            await _hub.Clients.All.SendAsync("InstallProgress", serial, 0,
                $"Đang gỡ cài đặt {packageName}...");

            var uninstallResult = await _adb.UninstallApkAsync(serial, packageName, ct);
            var isRealSuccess = uninstallResult.Success
                && uninstallResult.StdOut.Contains("Success", StringComparison.OrdinalIgnoreCase);

            if (isRealSuccess)
            {
                _logger.LogInformation("Uninstall {Package} trên {Serial} thành công", packageName, serial);
                await _hub.Clients.All.SendAsync("InstallProgress", serial, 15,
                    $"Đã gỡ cài đặt {packageName}");
            }
            else
            {
                // D2 + D3: LUÔN graceful. Log warning kèm StdOut/StdErr; KHÔNG abort.
                _logger.LogWarning(
                    "Uninstall {Package} trên {Serial} không thành công (vẫn tiếp tục install). StdOut={StdOut} StdErr={StdErr}",
                    packageName, serial, uninstallResult.StdOut.Trim(), uninstallResult.StdErr.Trim());
                await _hub.Clients.All.SendAsync("InstallProgress", serial, 15,
                    "Bỏ qua gỡ (chưa cài hoặc bị chặn)");
            }
        }

        // ── LUỒNG CŨ giữ nguyên logic, chỉ percent shift khi flag BẬT ───────────
        int pList     = uninstallBeforeInstall ? 25 : 0;
        int pInstall  = uninstallBeforeInstall ? 40 : 20;
        int pVerify   = uninstallBeforeInstall ? 55 : 40;
        int pVersion  = uninstallBeforeInstall ? 70 : 60;
        int pLaunch   = uninstallBeforeInstall ? 85 : 80;

        await _hub.Clients.All.SendAsync("InstallProgress", serial, pList, "Đang lấy danh sách package...");
        var before = await _adb.ListThirdPartyPackagesAsync(serial, ct);

        await _hub.Clients.All.SendAsync("InstallProgress", serial, pInstall, "Đang cài APK...");
        var installResult = await _adb.InstallApkAsync(serial, apkPath, ct);
        if (!installResult.Success)
        {
            var failMsg = $"Thất bại: {installResult.StdErr.Trim()}";
            await FinishWithStatus(serial, failMsg, null);
            return;
        }

        await _hub.Clients.All.SendAsync("InstallProgress", serial, pVerify, "Đang kiểm tra kết quả...");
        var after = await _adb.ListThirdPartyPackagesAsync(serial, ct);
        var installedPkg = after.Except(before).FirstOrDefault() ?? packageName;

        await _hub.Clients.All.SendAsync("InstallProgress", serial, pVersion, "Đang lấy version...");
        var version = await _adb.GetPackageVersionAsync(serial, installedPkg, ct);

        await _hub.Clients.All.SendAsync("InstallProgress", serial, pLaunch, "Đang mở ứng dụng...");
        var launchResult = await _adb.LaunchAppAsync(serial, installedPkg, ct);
        if (!launchResult.Success)
            _logger.LogWarning("LaunchApp {Serial}: {Msg}", serial, launchResult.StdErr.Trim());

        await FinishWithStatus(serial, "Thành công", version);
    }
    catch (OperationCanceledException) { /* unchanged */ }
    catch (Exception ex) { /* unchanged */ }
}
```

Percent tổng hợp:

| Bước | Flag TẮT | Flag BẬT |
|------|----------|----------|
| Đang gỡ cài đặt... | (không có) | 0% |
| Đã gỡ / Bỏ qua gỡ | (không có) | 15% |
| Đang lấy danh sách package... | 0% | 25% |
| Đang cài APK... | 20% | 40% |
| Đang kiểm tra kết quả... | 40% | 55% |
| Đang lấy version... | 60% | 70% |
| Đang mở ứng dụng... | 80% | 85% |
| Kết thúc (Thành công/Thất bại) | 100% | 100% |

Progress bar luôn tăng đơn điệu; UX không giật ngược.

## Thay đổi `InstallEndpoints.cs`

Chỉ đổi 1 dòng gọi `QueueInstalls`:

```csharp
var queued = coordinator.QueueInstalls(
    validSerials,
    packageName,
    apkPath,
    req.UninstallBeforeInstall);   // ← THÊM đối số cuối

// Response giữ nguyên (có thể thêm uninstallBeforeInstall để dễ debug):
return Results.Accepted("/api/install", new
{
    queued,
    serials = validSerials,
    packageName,
    apkPath,
    uninstallBeforeInstall = req.UninstallBeforeInstall
});
```

## Migration plan
Không có DB migration. Không có breaking change API — field mới có default value ở cả `InstallRequest` (default false do bool mặc định) và JS payload (nếu key thiếu, model binder tự set false). Client cũ chưa cập nhật vẫn hoạt động y như trước (uninstallBeforeInstall = false).

Deploy thứ tự bất kỳ (BE-first hoặc FE-first đều OK).

## Rủi ro & cách giảm thiểu

| Rủi ro | Ảnh hưởng | Cách giảm |
|--------|-----------|-----------|
| `adb uninstall` hang bất thường trên 1 thiết bị | Medium | Timeout 30s trong `UninstallApkAsync`; timeout tổng 10 phút của `DoInstallAsync` chặn tuyệt đối |
| User bật flag khi `packageName` rỗng | Low | `DoInstallAsync` check `!IsNullOrWhiteSpace(packageName)` — skip toàn bộ block uninstall, log không cần thiết. EC2 xử. |
| DB Settings đọc lỗi khi page load | Low | `IndexModel.OnGet` dùng `bool.TryParse` fallback false — EC5 |
| Percent shift làm confused client cache cũ | Low | Client render tuần tự message, không assume percent cố định — đã có behaviour tương thích |
| Uninstall thành công nhưng install fail (data mất) | Medium | Đây chính là mục đích tính năng (clean install). Log rõ để user biết. Docs Manual sẽ khuyến cáo backup dữ liệu app nếu cần. |
| Test coverage tăng do thêm branch | Low | Bổ sung 2 unit test (uninstall success / uninstall fail) và 1 integration test cho endpoint `/api/settings/uninstall-before-install` |

## Task breakdown

| ID | Tên task | Owner | Estimate | Phụ thuộc |
|----|----------|-------|----------|-----------|
| T-3.1.1 | Thêm `UninstallApkAsync` vào `AdbService` (KHÔNG thêm interface) | senior-developer | 0.5h | - |
| T-3.1.2 | Thêm field `UninstallBeforeInstall` vào `InstallRequest`; sửa `MapPost /api/install` truyền param mới xuống `QueueInstalls` | senior-developer | 0.5h | T-3.1.1 |
| T-3.1.3 | Sửa signature `QueueInstalls`/`InstallOneAsync`/`DoInstallAsync`; chèn block uninstall + shift percent | senior-developer | 2h | T-3.1.1 |
| T-3.1.4 | Thêm endpoint `POST /api/settings/uninstall-before-install` + DTO | senior-developer | 0.5h | - |
| T-3.1.5 | Sửa `IndexModel.OnGet` load setting; thêm checkbox vào `Index.cshtml` hàng 3 | senior-developer | 0.5h | T-3.1.4 |
| T-3.1.6 | Sửa `dashboard.js`: change handler lưu setting + truyền flag trong 2 install handler | senior-developer | 0.5h | T-3.1.4, T-3.1.5 |
| T-3.1.7 | Unit test `AdbService.UninstallApkAsync` mock RunAsync; test 2 case classify (real success / any fail); integration test endpoint mới | senior-developer | 2h | T-3.1.1, T-3.1.3, T-3.1.4 |
| T-3.1.8 | Cập nhật CODE-GRAPH (§17.2 — thêm method AdbService.UninstallApkAsync, thêm endpoint, đổi signature InstallCoordinator) | senior-developer | 0.5h | tất cả T-3.1.* |

**Tổng estimate: 7h** — nằm trong range 10–13h EM ước tính (bao gồm buffer cho review/test/deploy).

## Code Review Checklist (Tech Lead — Bước 3.2)
- [ ] Chạy đúng AC1–AC5? Đặc biệt AC3 (graceful skip khi package chưa cài — verify log WARNING, KHÔNG error nghiêm trọng)?
- [ ] `UninstallApkAsync` KHÔNG bị thêm vào interface `IAdbService`?
- [ ] Percent SignalR đúng bảng ở §Thay đổi `InstallCoordinator.DoInstallAsync`? Kiểm tra flag TẮT giữ nguyên 0/20/40/60/80/100 chính xác?
- [ ] `InstallRequest.UninstallBeforeInstall` default false; client cũ (không gửi field) vẫn chạy đúng hành vi cũ?
- [ ] Endpoint mới `/api/settings/uninstall-before-install` có handle body malformed (missing enabled)?
- [ ] `IndexModel.OnGet` fallback false khi DB lỗi/parse fail (EC5)?
- [ ] Unit test cover cả 2 branch classify (real success / fail-any-reason)? Test có meaningful assertion (log call verified, hub SendAsync verified)?
- [ ] Không có PII / secret leak trong log warning (StdOut/StdErr chỉ chứa message adb — OK)?
- [ ] CODE-GRAPH đã cập nhật? Confidence labels đúng?
- [ ] Không có control .NET UI gốc — đây là Web nên §20 KztekComponent không áp dụng (FYI: N/A).

## Checklist tài liệu đồng bộ (§15.3)
- [x] PRD cập nhật: không cần — TDD chốt Q1/Q3 từ PRD, PRD giữ nguyên như tài liệu yêu cầu
- [x] User Story: không cần cập nhật — US-005 giả định CÓ lưu đã khớp D1
- [x] TDD: file này
- [x] DESIGN: không có (checkbox đơn theo pattern có sẵn — WF-FEATURE Bước 3 UI/UX Designer đã skip)
- [ ] Test case: sẽ được QA Engineer viết ở Bước 4.1 dựa trên US-* + TDD này
- [x] CODE-GRAPH impact:
  - Depth-1 (WILL BREAK): `InstallCoordinator` (signature `QueueInstalls`/`DoInstallAsync` đổi), `InstallEndpoints` (dùng `QueueInstalls`), `AdbService` (thêm method mới — additive, không break)
  - Depth-2 (LIKELY AFFECTED): `Index.cshtml`/`IndexModel` (thêm property), `dashboard.js` (thêm handler + payload); `DeviceEndpoints` (thêm endpoint — additive)
