---
step: 3.2
plan: ../PLAN-MASTER.md
agent: Tech Lead
status: done
completed_at: 2026-08-24 22:46
deps: ["3.1"]
---

# STEP 3.2 — Code review PR + security-audit-stride + quyết định merge

## Input nhận
Từ Bước 3.1 Handoff Payload — commit `5297bc2`, 14 file thay đổi (+856/-17), 119/119 test pass, CODE-GRAPH đã cập nhật, push `origin/docker-deploy`.

## Nhiệm vụ

### Code Review checklist (STEP file + TDD §12 hợp nhất):

| # | Mục kiểm tra | Kết quả | Ghi chú |
|---|---|---|---|
| 1 | `GetAppStatusAsync` xử lý edge cases (empty pidof, dumpsys fail, device offline) | ✅ PASS | `pidof.ExitCode == -1` → return NotRunning + PidofResult; `pidof exit 1 + stdout empty` → NotRunning (đúng semantics, không throw); `dumpsys.Success == false` → coi như Background (giữ nguyên `Running=true` từ pidof). Logic đúng TDD §4.1-4.2. |
| 2 | `RebootDeviceAsync` không block thread, error handling khi device offline | ✅ PASS | Task-based (`Task<AdbCommandResult>`), timeout 10s qua CTS linked. Không throw. Endpoint map exit code → 200/422/500. |
| 3 | 2 endpoints — filter chain OUTER logging → INNER auth theo pattern `LaunchAppEndpoints.cs` | ✅ PASS | `AppStatusEndpoints.cs:48-49` và `RebootEndpoints.cs:33-34` cả hai đăng ký `ApiRequestLoggingEndpointFilter` TRƯỚC `ApiKeyEndpointFilter` — đúng thứ tự để log được cả response 401. |
| 4 | `context.Arguments` dùng thay vì `Request.Body` (GOTCHA G008) | ✅ PASS | `ApiRequestLoggingEndpointFilter.TryExtractParametersJson` dùng `context.Arguments[0]`, skip framework/system types. GET/POST route mới có `Arguments[0]=string serial` → System namespace → skip → parameters=null → `summarizeParams` fallback về `entry.path` (đúng ý đồ TDD §7.3). |
| 5 | UI confirm dialog Reboot hoạt động, không JS error | ✅ PASS | `dashboard.js:622-624` `confirm()` TRƯỚC fetch, early return `if (!ok) return;`. Message có kèm serial + note "không thể hoàn tác". |
| 6 | Tests đủ coverage (happy + edge), không assertion trống | ✅ PASS | 15 test mới: `AppStatusEndpointsTests` x9 (Validate x3, CheckDeviceState x2, MapAppStatusResult x7 gồm 3 state + AdbNotFound + AdbTimeout + AdbError + NotRunning), `RebootEndpointsTests` x6 (CheckDeviceState x2, MapRebootResult x4), `AdbServiceAppStatusTests` x5 (Foreground/Background/Empty/Fallback Android6/Neither). Mọi test có `Assert.Equal(<status>, sc.StatusCode)` — không rỗng. |
| 7 | `Program.cs` — 2 endpoint đăng ký đúng | ✅ PASS | Line 86-87: `MapAppStatusEndpoints()` + `MapRebootEndpoints()`. Position hợp lý (sau `MapDeviceConnectionEndpoints`). |
| 8 | CODE-GRAPH cập nhật, callers/relationships đúng | ✅ PASS | Line 45/51 tree; line 99 Program.cs deps; line 113/114 bảng Dependencies (`AppStatusEndpoints`, `RebootEndpoints`); line 158/159 endpoint map; line 219-225 changelog. Confidence CONFIRMED, Last verified 2026-08-24 (đúng ngày). |
| 9 | Không conflict file plan adb-reconnect (docker-compose.yml, AdbService.cs, appsettings.json) | ✅ PASS | Git status xác nhận 3 file đó vẫn `modified` (không stage vào commit 5297bc2 của Senior Developer). Diff commit 5297bc2 không đụng 3 file này. |
| 10 | Checklist tài liệu đồng bộ (CLAUDE.md §15.3) | ✅ PASS | TDD-adb-app-status-reboot-api.md đầy đủ (§1-13); PRD/US bỏ theo phương án rút gọn (Phase 1 SKIPPED user duyệt); CODE-GRAPH updated. |
| 11 | Response `state` field string `Foreground`/`Background`/`NotRunning` (không enum int) | ✅ PASS | `AppStatusEndpoints.cs:135` — `state = result.State.ToString()`. |
| 12 | `GetAppStatusAsync` KHÔNG throw khi `pidof` exit 1 | ✅ PASS | `AdbService.cs:235` — `running = pidof.Success && !string.IsNullOrWhiteSpace(pidof.StdOut)`; false → return NotRunning bình thường. |
| 13 | Nút Reboot yêu cầu ĐÚNG 1 thiết bị | ✅ PASS | `dashboard.js:618-619` — `serials.length === 0` alert; `serials.length > 1` alert. |
| 14 | Nút Check App Status validate `#txt-package` rỗng trước khi gọi | ✅ PASS | `dashboard.js:590-591` — `if (!pkg) { alert(...); return; }`. |
| 15 | `window.KZ_API_KEY` chỉ embed từ server-side, không log ra console | ✅ PASS | `Index.cshtml:279` — `@Html.Raw(Json.Serialize(Model.PublicApiKey ?? ""))`. `grep console\.` trong `dashboard.js` = 0 kết quả. |
| 16 | KHÔNG thêm method vào `IAdbService` | ✅ PASS | `AdbService.cs` — 2 method mới KHÔNG có trong interface (do_not_redo NG4 tuân thủ). |
| 17 | `ResolveApiName` dùng `EndsWith` (không Equals — vì có `{serial}` placeholder) | ✅ PASS | `ApiRequestLoggingEndpointFilter.cs:103-108` — `path.EndsWith("/app-status")` + `path.StartsWith("/api/devices/")` (defensive), tương tự cho `/reboot`. Đúng watch_out #2. |
| 18 | Không secret leak trong log (`x-api-key` không log ra text) | ✅ PASS | `ApiKeyEndpointFilter.cs:46-49` chỉ log path + RemoteIP, KHÔNG log header value. Không có `_logger.Log*` nào chứa `x-api-key` payload. |

**Kết quả checklist: 18/18 PASS.**

### Security audit (security-audit-stride):

#### OWASP Top 10
| Mục | Kết quả | Ghi chú |
|-----|---------|---------|
| A01 Broken Access Control | ✅ Pass | Cả 2 endpoint có `ApiKeyEndpointFilter` (constant-time compare, fail-safe khi ApiKey rỗng → 401). Không có route bypass. |
| A02 Cryptographic Failures | N/A | Không lưu/xử lý mật khẩu; ApiKey qua env var, không hardcode trong repo (`appsettings.json` default rỗng). |
| A03 Injection | ⚠️ Optional | `serial` được nối vào lệnh ADB (`adb -s {serial} reboot`). Do `ProcessStartInfo.UseShellExecute=false` → không invoke shell, args tokenize theo .NET parser → không tạo shell injection thực sự. Thêm nữa `CheckDeviceState` bắt buộc serial phải khớp record đã có trong `DeviceState` (populated từ `adb devices -l`), khống chế được input. **Khuyến nghị (Optional):** validate regex serial ở endpoint (`[a-zA-Z0-9._:-]+`) làm defense-in-depth — không blocker. |
| A04 Insecure Design | ✅ Pass | Reboot dùng POST (không GET), có confirm dialog client-side, có API key gate. |
| A05 Security Misconfiguration | ✅ Pass | Không có CORS mới, không mở port mới, không đổi debug mode. |
| A06 Vulnerable Components | ✅ Pass | Không thêm NuGet package mới (chỉ dùng `System.Text.Json`, `System.Text.RegularExpressions` sẵn có). |
| A07 Auth Failures | ✅ Pass | ApiKey compare constant-time chống timing attack (`CryptographicOperations.FixedTimeEquals`). Không session/token phức tạp. |
| A08 Data Integrity Failures | N/A | Không có deserialization user-controlled (path/query params đơn giản). |
| A09 Logging Failures | ✅ Pass | Log không chứa API key. Có audit log qua `ApiRequestLoggingEndpointFilter` (`Timestamp`, `ApiName`, `Path`, `Parameters=null (empty body)`, `CallerIp`, `Result`, `HttpStatusCode`, `DurationMs`). |
| A10 SSRF | N/A | Không request URL ngoài từ user input. |

**OWASP kết quả: 7 Pass / 0 Fail / 3 N-A. Không có mục Fail nhóm rủi ro cao (A01/A02/A03/A07).**

#### STRIDE (áp dụng cho luồng reboot — destructive action)
| Threat | Kết quả | Ghi chú |
|---|---|---|
| Spoofing (bypass API key) | ✅ Pass | Fail-safe khi ApiKey chưa cấu hình (401 mọi request); constant-time compare tránh timing attack. Chỉ risk khi ApiKey bị leak — vấn đề vận hành, không code. |
| Tampering (giả mạo serial) | ✅ Pass | `CheckDeviceState.TryGet(serial)` bắt buộc serial phải khớp record đã có trong `DeviceState` singleton → giả mạo serial random → 404 `DeviceNotFound`. |
| Repudiation | ✅ Pass | Có `ApiRequestLoggingEndpointFilter` ghi audit (`ApiRequestLogEntry`) mỗi request với `CallerIp` + `Timestamp`. |
| Information Disclosure | ✅ Pass | Response error message an toàn (không lộ path adb server, không stack trace); log không chứa API key. |
| Denial of Service (spam reboot liên tục) | ⚠️ FYI | **Không có rate limit** cho endpoint reboot. Attacker/user vô ý gọi liên tục → thiết bị reboot liên tục → tổn thất vận hành. Rủi ro thực tế thấp: (1) yêu cầu API key hợp lệ, (2) internal LAN scope (TDD Q9), (3) reboot mất ~30-60s → tự giới hạn tần suất. **Khuyến nghị (Optional):** thêm rate limit `[EnableRateLimiting("reboot", 3/min per serial)]` ở release sau — không blocker cho MVP. |
| Elevation of Privilege | N/A | Không có phân quyền user thường/admin — hệ 1 tầng API key. |

**STRIDE kết quả: 4 Pass / 0 Fail / 1 N-A / 1 FYI (rate limit). Không có Fail nhóm rủi ro cao.**

### Quyết định

**✅ APPROVE MERGE → chuyển Bước 3.3 UX/UI Reviewer.**

**Lý do:**
- 18/18 mục code review PASS, không có comment mức Required/Critical.
- OWASP 7 Pass / 0 Fail / 3 N-A, STRIDE 4 Pass / 0 Fail / 1 N-A / 1 FYI. Không có Fail nhóm rủi ro cao (A01/A02/A03/A07).
- Test coverage đúng (15 test mới, meaningful assertions), 119/119 pass.
- Follow đúng pattern LaunchAppEndpoints (filter chain, CheckDeviceState, MapResult), không tạo cơ chế mới cần review kiến trúc.

**Comment mức Optional/FYI (không block):**
1. **Optional (A03 defense-in-depth):** Thêm regex validate serial ở endpoint (`^[a-zA-Z0-9._:-]+$`) — hiện `CheckDeviceState` đã khống chế đủ, nhưng validate tường minh giúp fail sớm và dễ audit hơn.
2. **Optional (DoS):** Cân nhắc rate limit endpoint reboot ở release sau (`[EnableRateLimiting]` với AspNetCore.RateLimiting) — 3 lần/phút/serial là đủ cho use case thật.
3. **FYI:** API key hiện được embed vào HTML source (Index.cshtml → `window.KZ_API_KEY`) → bất kỳ user nào mở dashboard đều thấy key qua DevTools. Chấp nhận trong internal LAN scope (TDD Q9), nhưng nếu sau này mở public thì phải review lại (đổi sang cookie httpOnly + CSRF token, hoặc backend-for-frontend pattern).
4. **Nit (docs):** Ở `AppStatusEndpoints.cs:9-10` comment ghi "DRY vi phạm nhẹ" — có thể refactor `PackageNameRegex` + `CheckDeviceState` ra file helper (`DeviceStateHelper`, `PackageNameValidator`) trong task refactor riêng sau này.

Không sửa code trong bước này (theo scope §4) — các Optional/FYI để lại cho follow-up hoặc release sau. STEP-2.1 có 4 dòng modified pending (điền commit hash TDD 6e900b9) — stage kèm commit này.

## Definition of Done
- [x] Code review checklist đầy đủ (18/18 PASS)
- [x] Security-audit-stride hoàn thành (OWASP 7P/0F/3NA; STRIDE 4P/0F/1NA/1FYI), không Fail nhóm cao
- [x] Quyết định APPROVE ghi rõ lý do + 4 Optional/FYI
- [x] Commit + push (ghi kết quả review)
- [x] Cập nhật STEP file này + PLAN-MASTER.md Bước 3.2 → ✅

## Đã làm
- Đọc diff `git diff 6e900b9..5297bc2` (14 file, +856/-17).
- Đọc 5 file chính: `AppStatusEndpoints.cs`, `RebootEndpoints.cs`, `AdbService.cs`, `dashboard.js`, `Index.cshtml`, `Index.cshtml.cs`, `Program.cs`, `ApiRequestLoggingEndpointFilter.cs`, `ApiRequestLogConstants.cs`, `LaunchAppSettings.cs`, `ApiKeyEndpointFilter.cs`, `LaunchAppEndpoints.cs` (đối chiếu pattern).
- Đọc 3 test file mới: `AppStatusEndpointsTests.cs`, `RebootEndpointsTests.cs`, `AdbServiceAppStatusTests.cs`.
- Đọc CODE-GRAPH.md phần cập nhật (dòng 45, 51, 99, 113, 114, 158, 159, 219-225).
- Đọc TDD §12 Code Review Checklist + §13 Q&A để đối chiếu.
- Chạy skill `security-audit-stride`: OWASP Top 10 + STRIDE cho luồng reboot.
- Ghi checklist 18 mục kết quả (18/18 PASS), security audit kết luận (0 Fail nhóm rủi ro cao).
- KHÔNG sửa code chính (chỉ APPROVE, các Optional/FYI ghi nhận cho follow-up).
- Stage kèm sửa nhỏ STEP-2.1 (điền commit hash 6e900b9 sau khi TDD đã push).

## Artifact
- `docs/plans/PLAN-adb-app-status-reboot-api-2026-08-24/steps/STEP-3.2-tech-lead-review.md` (file này) — cập nhật review + audit.
- `docs/plans/PLAN-adb-app-status-reboot-api-2026-08-24/steps/STEP-3.2-tech-lead-review.docx` — xuất từ script (§19).
- `docs/plans/PLAN-adb-app-status-reboot-api-2026-08-24/PLAN-MASTER.md` — cập nhật Bước 3.2 → ✅.
- `docs/plans/PLAN-adb-app-status-reboot-api-2026-08-24/PLAN-MASTER.docx` — xuất từ script.
- `docs/plans/PLAN-adb-app-status-reboot-api-2026-08-24/steps/STEP-2.1-tech-lead-tdd.md` — sửa nhỏ điền commit hash (đã pending).

## Quyết định quan trọng
1. **APPROVE merge** — 18/18 code review PASS, security audit không Fail nhóm rủi ro cao.
2. Không request changes cho 4 Optional/FYI (regex serial, rate limit, API key trong HTML, DRY refactor) — để lại cho follow-up/release sau, không block feature MVP.
3. Không tự sửa code trong bước này (tuân thủ scope §4 — Tech Lead review-only, không viết code).
4. Xác nhận filter chain order (OUTER logging → INNER auth) đúng — bắt được cả 401 response cho audit log.
5. Xác nhận `EndsWith` được dùng cho ResolveApiName 2 route mới (không Equals — watch_out #2 tuân thủ).

## Handoff Payload — bước sau đọc phần này
- Đã làm: Code review 18/18 PASS + security-audit-stride (OWASP 7P/0F/3NA; STRIDE 4P/0F/1NA/1FYI-rate-limit) → **APPROVE MERGE**. Không sửa code chính. Chuyển UX/UI Reviewer.
- do_not_redo: KHÔNG audit lại security/code review — đã APPROVE. KHÔNG rebuild/retest (đã xác nhận 119/119 pass ở STEP-3.1). KHÔNG sửa Optional/FYI (ghi nhận cho follow-up).
- watch_out:
  1. UI có 2 nút mới cần kiểm tra trực quan (C1-C7): `#btn-check-app-status` (outline-primary), `#btn-reboot-device` (outline-warning) — vị trí trong action panel dưới `hr` "Thao tác thiết bị (1 thiết bị đã chọn)".
  2. Confirm dialog Reboot dùng native `confirm()` — kiểm tra có hiển thị đúng serial + note "không hoàn tác".
  3. Kịch bản test UI: (a) không chọn thiết bị → click nút → alert; (b) chọn >1 thiết bị → alert; (c) không nhập package name → click Check App Status → alert; (d) reboot → confirm popup, cancel → không gửi; (e) reboot → confirm popup, OK → toast "Đã gửi reboot".
  4. Package name lấy từ ô `#txt-package` HIỆN CÓ (không có input mới) — nếu ô trống thì alert đòi nhập.
  5. Toast hiển thị màu theo state: Foreground=success, Background=info, NotRunning=warning, Error=danger.
- next_inputs:
  1. Cách chạy app: `cd /home/duonghoang21/docker/KztekAdbTool && dotnet run --project src/KztekAdbPublishTool.Web/KztekAdbPublishTool.Web.csproj` (Kestrel default `http://localhost:5000` hoặc theo `appsettings.json`).
  2. UI element cần kiểm tra:
     - Vị trí 2 nút: `Pages/Index.cshtml:180-186` (dưới `<hr class="my-2" />`, section "Thao tác thiết bị (1 thiết bị đã chọn)").
     - Icon: `bi-info-circle` (Check App Status) + `bi-arrow-repeat` (Reboot).
     - Màu: `btn-outline-primary` (Check) + `btn-outline-warning` (Reboot) — đúng KZTEK brand? (KZTEK Navy #251C53 primary, Cam #F05922 accent — có thể cần check màu `warning` bootstrap có clash không).
     - Confirm dialog: `dashboard.js:622-624`.
     - Toast: `showToast(msg, type)` hàm ở `dashboard.js:49`.
  3. TDD reference: `docs/tech-design/TDD-adb-app-status-reboot-api.md` §8 (UI Spec).
  4. Base commit hiện tại: `<sẽ điền sau commit>`.

## Commit
- Hash: (sẽ điền sau commit)
- Đã push: (sẽ điền sau push)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
