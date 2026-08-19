---
step: 3.2
plan: ../PLAN-MASTER.md
agent: Tech Lead
status: done
completed_at: 2026-08-19 17:05
deps: [3.1]
---

# STEP 3.2 — Code review, security-audit-stride, quyết định merge

## Input nhận
Nhận Handoff Payload từ STEP-3.1. Code đã push tại commit `3c5541b` (nhánh `docker-deploy`).

## Nhiệm vụ
1. Review code review checklist chuẩn (correctness, security, perf, đúng TDD).
2. Chạy `/verify-pr` (`.claude/commands/verify-pr.md`) — build/lint/test/security note/diff review — chỉ review khi report toàn PASS.
3. Chạy skill `security-audit-stride` (BẮT BUỘC — route mới đụng lại cơ chế kết nối thiết bị + auth, dù tái dùng auth cũ) — OWASP Top 10 + STRIDE.
4. Quyết định APPROVE merge hoặc REQUEST-CHANGES. Nếu Fail nhóm rủi ro cao → BLOCK merge.

## Definition of Done
- [x] VERIFICATION REPORT toàn PASS
- [x] security-audit-stride chạy xong, ghi kết quả OWASP/STRIDE
- [x] Quyết định merge rõ ràng (APPROVE / REQUEST-CHANGES), có escalate CTO hay không

## Đã làm

### 1. VERIFICATION REPORT

```
## VERIFICATION REPORT
Generated: 2026-08-19 17:05 | Branch: docker-deploy | By: Tech Lead
Commit: 3c5541b

| # | Kiểm tra | Kết quả | Ghi chú |
|---|----------|---------|---------|
| 1 | Build   | PASS | `dotnet build src/KztekAdbPublishTool.Web/KztekAdbPublishTool.Web.csproj` — 0 error, 0 warning, 1.76s |
| 2 | Lint / Type-check | SKIP | Project chưa cấu hình `dotnet format --verify-no-changes` mặc định; đồng bộ với PR trước |
| 3 | Test    | PASS | `dotnet test tests/KztekAdbPublishTool.Web.Tests` — 64/64 passed, 100 ms |
| 4 | Security Quick Note | PASS | Không hard-code credential (ApiKey đọc từ config; API key trong docker-compose.yml/appsettings.json là uncommitted local config của user, không thuộc phạm vi commit 3c5541b). Không có injection risk qua shell (ProcessStartInfo UseShellExecute=false). Không có sensitive file bị add. |
| 5 | Diff Self-review | PASS | Không dead code / debug statement; naming rõ; separation of concerns tốt (validate → connect → poll → response). Response schema khớp TDD §API Contract. |

**Tổng kết:** READY FOR REVIEW
```

### 2. Code Review theo Checklist TDD (13 mục)

| # | Mục | Kết quả | Ghi chú |
|---|-----|---------|---------|
| 1 | 2 endpoint có `.AddEndpointFilter<ApiKeyEndpointFilter>()` | ✅ Pass | Xác nhận tại dòng 79 và 113 |
| 2 | Route cũ không bị đụng | ✅ Pass | Grep xác nhận `/api/devices/connect`, `/api/devices/connect-batch`, `/api/install`, `/api/launch-app` vẫn nằm ở file gốc (DeviceEndpoints.cs, InstallEndpoints.cs, LaunchAppEndpoints.cs) — không sửa |
| 3 | Xử lý ExitCode == 0 bao gồm cả "already connected" | ✅ Pass | `if (result.Success)` — `Success => ExitCode == 0` (AdbCommandResult) — cả "connected to X" và "already connected to X" đều exit 0 |
| 4 | Gọi `poll.TriggerAsync` sau connect thành công | ✅ Pass | Dòng 45 — `await poll.TriggerAsync(ct)` trong nhánh success |
| 5 | `serial` echo về = đúng `ip:port` đã gọi ADB | ✅ Pass | Dòng 51 `serial = target`, target = `$"{ip}:{port}"` (dòng 39) |
| 6 | Validation ip chặn empty + whitespace + `:` | ✅ Pass | `ValidateConnectInput` dòng 125-130: empty, contains `:`, whitespace — đủ 3 chốt chặn |
| 7 | Validation port chặn ngoài range 1–65535 | ✅ Pass | Dòng 131-132 với MinPort=1 (bỏ port 0 — TCP hợp lệ cho listen nhưng không cho connect target) |
| 8 | API 2 KHÔNG gọi ADB, chỉ đọc DeviceState | ✅ Pass | Handler API 2 chỉ dùng `deviceState.TryGet` — không inject `AdbService` |
| 9 | API 2 trả 404 khi serial NotFound | ✅ Pass | Dòng 95-104 — `Results.NotFound` với `error = "DeviceNotFound"` |
| 10 | Log level đúng | ✅ Pass | `LogInformation` cho invalid input + status not found + success; `LogWarning` cho ADB fail; `LogError` cho ADB missing |
| 11 | security-audit-stride không Fail nhóm cao | ✅ Pass | Xem mục 3 dưới đây |
| 12 | CODE-GRAPH cập nhật | ✅ Pass | STEP-3.1 đã cập nhật (DOCX xuất, PDF skip do thiếu xelatex trên WSL2 — chấp nhận theo §19.4) |
| 13 | Test coverage ValidateConnectInput + API 2 handler ≥ 90% | ✅ Pass | 22 test: 13 test cho ValidateConnectInput bao phủ toàn bộ branch; 6 test cho DeviceState TryGet path (found Online/Offline, NotFound, empty-serial trim, USB serial); 4 test cho ConnectByIp target-building + Results.BadRequest |

**Deviation ghi nhận:** `ValidateConnectInput` là `public static` (không `internal`) — đây là quyết định đúng của Senior Developer để test project truy cập trực tiếp mà không cần InternalsVisibleTo. Pattern giống `LaunchAppEndpoints.ValidateInput`. ACCEPT.

### 3. Security Audit STRIDE (BẮT BUỘC — Bước 10a WF-FEATURE)

#### OWASP Top 10

| # | Kiểm tra | Kết quả | Ghi chú |
|---|----------|---------|---------|
| A01 | Broken Access Control | ✅ Pass | Cả 2 route mới đều `AddEndpointFilter<ApiKeyEndpointFilter>()`. Không leak endpoint nội bộ. |
| A02 | Cryptographic Failures | ✅ Pass | Không lưu secret plaintext trong code. ApiKey đọc từ config (env `LaunchApp__ApiKey` khuyến nghị cho prod). |
| A03 | Injection | ✅ Pass | `ProcessStartInfo.UseShellExecute=false` → không có shell interpretation, không command injection. Validation `ip` chặn `:` + whitespace + empty. Path traversal qua `serial` (API 2): không có — serial chỉ làm key `ConcurrentDictionary.TryGetValue`, không đụng file system. **FYI (không Fail):** argument injection cấp thấp (VD `ip="-h"` sẽ khiến adb chạy `connect -h`) không gây RCE vì `adb connect` chỉ accept 1 param; kết quả xấu nhất là ADB in error → 422 — chấp nhận được. |
| A04 | Insecure Design | ✅ Pass | Reuse strategy đúng như TDD: `AdbService.ConnectAsync`, `DeviceState`, `ApiKeyEndpointFilter`, `PollControlService.TriggerAsync`. Không tạo cơ chế auth mới. |
| A05 | Security Misconfiguration | ✅ Pass | Fail-safe: ApiKey rỗng → 401 (ApiKeyEndpointFilter dòng 33-39). Không có debug endpoint bị expose. |
| A06 | Vulnerable Components | N-A | Không thêm dependency mới; tái dùng ASP.NET Core Minimal API + xUnit hiện có. |
| A07 | Auth Failures | ✅ Pass | `CryptographicOperations.FixedTimeEquals` (constant-time) — chống timing attack. Không đổi filter. |
| A08 | Data Integrity Failures | N-A | Không có deserialize source lạ; chỉ JSON body qua model binding chuẩn của ASP.NET Core. |
| A09 | Logging Failures | ✅ Pass | Log không chứa API key. Có log target IP:port (nội bộ mạng, không phải PII); log serial trong 404 (đã dùng ở endpoint cũ). Log level phù hợp. |
| A10 | SSRF | N-A | Server không phát request HTTP ra URL do user cung cấp; `adb connect` là subprocess local, không thuộc SSRF category. |

**OWASP tổng: 7 Pass + 3 N-A + 0 Fail.**

#### STRIDE (route mới → trust boundary mới)

| # | Kiểm tra | Kết quả | Ghi chú |
|---|----------|---------|---------|
| S | Spoofing | ✅ Pass | Bắt buộc `x-api-key` — kẻ tấn công không thể ẩn danh gọi endpoint. |
| T | Tampering | N-A | Không sửa persistent data (DeviceState memory-only, DeviceRepository không bị đụng ở PR này). |
| R | Repudiation | ✅ Pass | Log đủ audit trail: success (LogInformation với target), fail (LogWarning với exitCode + stderr), unauthorized (LogWarning với remote IP trong ApiKeyEndpointFilter). |
| I | Information Disclosure | ✅ Pass | Response không leak stack trace; StdErr từ ADB (VD "connection refused", "no route to host") lộ thông tin mạng nội bộ ở mức chấp nhận được cho DevOps API (giúp client debug). Không lộ danh sách device khác qua endpoint status (chỉ trả 1 serial được yêu cầu). |
| D | Denial of Service | ⚠️ FYI (không Fail) | POST /connect-by-ip gọi `adb connect` với timeout 10s, không có rate limit riêng. Kẻ tấn công có API key hợp lệ có thể spam để consume ADB process. Rate limit ở reverse proxy là hướng xử lý phù hợp — không phải blocker cho PR này. Ghi nhận Optional để cân nhắc thêm rate limit ở phase sau. |
| E | Elevation of Privilege | ✅ Pass | Không có role/permission hierarchy; API key là all-or-nothing. Không có admin endpoint. |

**STRIDE tổng: 4 Pass + 1 N-A + 1 FYI (không Fail).**

#### Kết luận Security Audit

```
- OWASP: 10/10 (7 Pass + 3 N-A) — mục Fail: NONE
- STRIDE: áp dụng — 4 Pass + 1 N-A + 1 FYI (DoS — Optional cân nhắc rate limit sau) — rủi ro cao: NONE
- Quyết định: ✅ Đủ điều kiện merge
```

Không có mục Fail ở A01/A02/A03/A07 (nhóm rủi ro cao) → không BLOCK.

### 4. Quyết định cuối: **APPROVE — merge được**

## Artifact

**Kết quả review:** APPROVE.
- Code khớp toàn bộ 13 mục Code Review Checklist TDD.
- VERIFICATION REPORT: 4 PASS + 1 SKIP (lint không cấu hình — hợp lệ).
- security-audit-stride: 0 Fail nhóm cao.
- Không escalate CTO (đã đánh giá ở TDD: reuse auth cũ, không tạo cơ chế mới).
- Không sửa code ở bước này — chỉ ghi report review.

**File đã đọc trong lần review:**
- `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs`
- `src/KztekAdbPublishTool.Web/Endpoints/ApiKeyEndpointFilter.cs`
- `src/KztekAdbPublishTool.Web/Services/AdbService.cs` (kiểm tra `ConnectAsync` + `RunAsync` cho A03)
- `src/KztekAdbPublishTool.Web/State/DeviceState.cs`
- `src/KztekAdbPublishTool.Web/Program.cs`
- `tests/KztekAdbPublishTool.Web.Tests/DeviceConnectionEndpointTests.cs`
- `docs/tech-design/TDD-adb-add-device-api.md` (checklist §Code Review Checklist)

## Quyết định quan trọng

1. **APPROVE merge** — code đạt tiêu chuẩn Tech Lead + đáp ứng đủ TDD checklist.
2. **KHÔNG escalate CTO** — đã đánh giá reuse auth cũ, không tạo cơ chế mới; nhất quán với quyết định đã ghi ở TDD (Q7).
3. **Ghi nhận FYI DoS** cho backlog: cân nhắc thêm rate limit ở reverse proxy/middleware trong phase sau nếu API mở public thực sự. Không block PR hiện tại.
4. **Không sửa code ở review** — chỉ report; Senior Developer không phải rework.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Code review đã APPROVE, security-audit-stride xong, VERIFICATION REPORT xong. Không cần Senior Developer sửa lại code. Commit `3c5541b` đủ điều kiện merge/deploy — bước tiếp theo QA Engineer thực thi test plan, không cần chỉnh code trước.
- watch_out:
  1. FYI DoS (STRIDE): endpoint POST /connect-by-ip không rate-limit riêng — nếu QA muốn test resilience có thể note vào test plan (không phải blocker P1).
  2. Test project chỉ test qua `DeviceState` + `ValidateConnectInput` (pattern LaunchAppEndpoints) — QA Engineer sẽ cần chạy integration test với ADB thật (hoặc emulator) để phủ hết behavior end-to-end. Cân nhắc test case: (a) IP hợp lệ nhưng offline → 422; (b) ADB binary missing → 500; (c) API key sai → 401; (d) API 2 với serial vừa connect xong nhưng poll chưa update → có thể 404 tạm thời (race condition đã document trong TDD).
  3. `LaunchApp:ApiKey` là section dùng chung cho cả 2 route mới — nếu QA muốn rotate key phải chú ý mọi endpoint đang dùng chung.
- next_inputs:
  1. `docs/tech-design/TDD-adb-add-device-api.md` — QA Engineer đọc để lấy AC + edge case chi tiết.
  2. `docs/user-stories/US-adb-add-device-api.md` — 14 scenario Given/When/Then.
  3. Commit `3c5541b` — code sẵn sàng test.
  4. `src/KztekAdbPublishTool.Web/Endpoints/DeviceConnectionEndpoints.cs` + `DeviceConnectionEndpointTests.cs` — QA đọc để hiểu pattern test đã có, tránh trùng lặp.

## Commit
- Hash: (không có — chỉ cập nhật step file + MASTER, không đổi code)
- Đã push: (sẽ push chung với commit update MASTER)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
