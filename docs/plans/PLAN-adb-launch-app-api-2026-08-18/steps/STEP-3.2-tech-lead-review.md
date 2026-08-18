---
step: "3.2"
plan: ../PLAN-MASTER.md
agent: tech-lead
status: done
completed_at: 2026-08-18 16:47
deps: ["3.1"]
---

# STEP 3.2 — Tech Lead: Code Review + security-audit-stride + Merge Decision

## Input nhận
Output từ Bước 3.1 (Senior Developer): commit `f0581a8` đã push. VERIFICATION REPORT trong `STEP-3.1-senior-developer-code.md` toàn PASS (build 0 error, 42/42 test pass, format 3 file mới đạt, diff self-review OK).

## Nhiệm vụ
Review PR theo checklist kỹ thuật 14 mục trong TDD, tự chạy lại `dotnet build` + `dotnet test`, chạy `security-audit-stride` bắt buộc (đụng auth — cơ chế API key mới), quyết định approve/request-changes.

## Definition of Done
- [x] Đã đọc PR description + VERIFICATION REPORT — xác nhận toàn PASS trước khi mở review
- [x] Code review checklist hoàn thành: correctness, error handling, test coverage, naming, cấu hình appsettings
- [x] Đã chạy skill `security-audit-stride` (OWASP + STRIDE) cho phần auth filter + endpoint mới
- [x] Kết quả security-audit-stride: PASS (không có Fail nhóm rủi ro cao) → approve merge
- [x] Kiểm tra mục "Component đã dùng tối đa KztekComponent*?" — không áp dụng (backend thuần túy)
- [x] Quyết định merge ghi rõ: APPROVE + lý do
- [x] Không phát hiện rủi ro bảo mật cần escalate CTO

## Đã làm

### 1. Verify build + test (tự chạy lại, không chỉ tin báo cáo)

| # | Kiểm tra | Lệnh | Kết quả | Ghi chú |
|---|----------|------|---------|---------|
| 1 | Build | `dotnet build src/KztekAdbPublishTool.Web/KztekAdbPublishTool.Web.csproj` | ✅ PASS | 0 warning, 0 error, 1.73s |
| 2 | Test | `dotnet test tests/KztekAdbPublishTool.Web.Tests/KztekAdbPublishTool.Web.Tests.csproj` | ✅ PASS | 42/42 passed, 101ms |
| 3 | Text-match source | `grep "timeout sau\|Không tìm thấy adb\|No activities found"` trong `AdbService.cs` | ✅ PASS | 3 text hằng đúng vị trí: dòng 49, 90, 184 — mapping trong `MapAdbResult` khớp chính xác |

### 2. Code Review Checklist (14 mục TDD)

| # | Mục | Kết quả | Chi tiết |
|---|-----|---------|----------|
| 1 | Route `POST /api/launch-app` (flat, không nested) | ✅ | `LaunchAppEndpoints.cs:22` — `app.MapPost("/api/launch-app", ...)` |
| 2 | Filter gắn qua `.AddEndpointFilter<>()`, KHÔNG `app.Use()` toàn cục | ✅ | `LaunchAppEndpoints.cs:60` — `.AddEndpointFilter<ApiKeyEndpointFilter>()` |
| 3 | Route cũ (`/api/install`, `/api/devices`, `/api/scan`, `/api/health`, `/api/apk/*`) KHÔNG bị áp filter | ✅ | `Program.cs:64-73` — mỗi `Map*Endpoints()` gọi riêng biệt, KHÔNG có `app.Use<ApiKeyEndpointFilter>()` toàn cục. R4 auth-bypass eliminated. |
| 4 | So sánh key dùng `CryptographicOperations.FixedTimeEquals` | ✅ | `ApiKeyEndpointFilter.cs:66` — bytes UTF-8, không dùng `==`/`string.Equals` |
| 5 | Fail-safe: `ApiKey` rỗng → 401 | ✅ | `ApiKeyEndpointFilter.cs:33-39` — check `IsNullOrEmpty(expected)` trước; test `EmptyApiKeyConfig_Returns401_AndDoesNotCallNext` cover |
| 6 | Không log giá trị `x-api-key` | ✅ | Log chỉ chứa `Path`, `RemoteIP` — không có `provided`/`expected` trong log statement (dòng 35, 46-49) |
| 7 | Regex package name khớp `^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$` | ✅ | `LaunchAppEndpoints.cs:17` — `RegexOptions.Compiled`, khớp TDD |
| 8 | Phân biệt SC-03 (404) và SC-05 (422) | ✅ | `CheckDeviceState`: `TryGet` false → 404 (dòng 90-100); `Status != "Online"` (Ordinal) → 422 DeviceOffline (dòng 102-114). Test 3 case: unknown/offline/online đủ. |
| 9 | Mapping StdErr đủ 4 case (`"Không tìm thấy adb tại:"` 500, `"No activities found to run"` 422 AppNotInstalled, `"timeout sau"` 422 AdbTimeout, else 422 AdbError) | ✅ | `MapAdbResult` dòng 145-199. Text hằng khớp `AdbService.cs` dòng 49/90/184 (đã grep verify). Test cover 5 case: exit0, AppNotInstalled, Timeout, AdbNotFound, other. |
| 10 | Handler dùng `HttpContext.RequestAborted` làm CancellationToken | ✅ | `LaunchAppEndpoints.cs:55-56` — `var ct = httpContext.RequestAborted; await adbService.LaunchAppAsync(serial, packageName, ct);` |
| 11 | KHÔNG sửa `AdbService.cs` | ✅ | `git diff` chỉ hiện file mới + `Program.cs`/csproj — `AdbService.cs` không đổi |
| 12 | Structured logging placeholder `{Serial}`, `{App}` (không interpolation) | ✅ | Tất cả log statements dùng placeholder (VD: `"Launch OK — serial={Serial}, app={App}, exitCode={ExitCode}"`) |
| 13 | `security-audit-stride` PASS | ✅ | Xem mục 3 dưới đây |
| 14 | `code-graph/CODE-GRAPH.md` cập nhật | ✅ | Senior Dev đã cập nhật ở STEP-3.1 (theo báo cáo) — 3 module mới + 1 endpoint |
| — | Docker Compose env var — thao tác ở STEP-4.3 | ⏭️ | Ngoài scope PR này, giao DevOps Engineer |
| — | `appsettings.json` chưa có section `LaunchApp` | ✅ (fail-safe) | Hook `config-protection` chặn Senior Dev; user sẽ tự tay thêm. Không ảnh hưởng chức năng vì POCO default `""` → 401 fail-safe. Xác nhận đây không phải blocker cho merge. |

### 3. Security Audit (OWASP Top 10 + STRIDE)

#### 3.1. OWASP Top 10

| Mục | Kết quả | Ghi chú |
|-----|---------|---------|
| A01 Broken Access Control | ✅ PASS | Endpoint mới yêu cầu `x-api-key` qua filter gắn cứng vào 1 endpoint. Route cũ KHÔNG bị áp (verified qua đọc Program.cs). Fail-safe config rỗng → 401. |
| A02 Cryptographic Failures | ✅ PASS | Default POCO rỗng — không hardcode key trong git. Env var override. Constant-time compare bằng `FixedTimeEquals`. |
| A03 Injection | ✅ PASS | `AdbService.LaunchAppAsync` dùng `Process` với argument list (không shell string concat). Package name qua regex loại ký tự đặc biệt. Serial dù không validate strict nhưng qua `TryGet(serial)` — chỉ chấp nhận serial đã có trong `DeviceState` (được `DevicePollWorker` cập nhật từ `adb devices`); serial độc hại → 404 (không tồn tại). |
| A04 Insecure Design | ✅ PASS | Fail-safe khi config rỗng. Filter chạy trước handler, không có bypass logic. |
| A05 Security Misconfiguration | ✅ PASS | Default POCO = `""` → 401 all (không "insecure default"). Response body có `stdOut`/`stdErr` nhưng nội dung ADB không nhạy cảm (chỉ tên component + serial — client đã biết). Đã ghi chú R7 rủi ro Low. |
| A06 Vulnerable Components | N/A | Không thêm dependency mới. `FrameworkReference Microsoft.AspNetCore.App` chỉ dùng cho test project (framework có sẵn). |
| A07 Auth Failures | ✅ PASS | API key tĩnh — không session/token rotation trong scope. Constant-time compare chống timing attack. Không log key. Non-goal đã ghi rõ trong PRD (không rotation phiên bản này). |
| A08 Data Integrity Failures | N/A | Chỉ JSON body → DTO đơn giản. Không có deserialization phức tạp/update signed. |
| A09 Logging Failures | ✅ PASS | Log 401 chỉ chứa `Path` + `RemoteIP` (không key). Log 200/422 chứa `Serial`/`App`/`ExitCode` — không PII. Structured logging đầy đủ. |
| A10 SSRF | N/A | Endpoint không request URL ra ngoài. Serial là param cho `adb -s <serial>` local, không phải URL. |

**Tóm tắt OWASP:** 7 Pass, 3 N/A, 0 Fail — không có mục Fail ở nhóm rủi ro cao (A01/A02/A03/A07).

#### 3.2. STRIDE

| Mục | Kết quả | Ghi chú |
|-----|---------|---------|
| Spoofing | ✅ PASS | Xác thực bằng API key + constant-time compare. Kẻ tấn công không thể forge identity nếu không có key. |
| Tampering | ✅ PASS | JSON body parse an toàn bởi ASP.NET Core model binder. Không có persistence tampering surface. |
| Repudiation | ✅ PASS | Log warning cho mọi 401 event (Path + RemoteIP). Log info cho 200 event (Serial + App + ExitCode). Structured logging đủ để trace, dù không có audit log riêng. |
| Information Disclosure | ✅ PASS | Response 500 map về generic message `"adb binary not found on server."` — KHÔNG leak path server ra client (chỉ log server-side, chấp nhận). Response 200/422 chứa `stdOut`/`stdErr` — TDD Q2 đã quyết định để dễ debug CI/CD, không nhạy cảm. Không leak key ở bất kỳ đâu. |
| Denial of Service | ✅ PASS (với Optional) | `LaunchAppAsync` có timeout 10s cứng → không hang vô hạn. Fail-safe 401 khi config rỗng → không lãng phí resource ADB. **Optional (không block):** không có rate-limit — kẻ tấn công có key hợp lệ vẫn spam được. TDD Non-goal đã ghi rõ; khuyến nghị rate limiter cho phiên bản sau. |
| Elevation of Privilege | ✅ PASS | Không có multi-level auth. API key single tier. Filter gắn cứng scope 1 endpoint — KHÔNG lây sang route khác (verified). |

**Tóm tắt STRIDE:** 6/6 PASS — không có Fail.

#### 3.3. Kết luận Security Audit

```
Kết quả Security Audit:
- OWASP: 7/10 Pass, 3 N/A (A06, A08, A10) — 0 Fail
- STRIDE: 6/6 Pass (Optional: rate-limit cho DoS mitigation trong tương lai)
- Quyết định: ✅ Đủ điều kiện merge (không có Fail nhóm rủi ro cao A01/A02/A03/A07)
```

### 4. Nit/Optional (không block)

- **Nit:** Test 5 trong `ApiKeyEndpointFilterTests` (`InvokeAsync_KeyDifferentLength_Returns401`) chỉ assert `Assert.False(nextCalled)` — không assert status code 401. Đủ ý nghĩa nhưng có thể improve.
- **Optional:** Xem xét thêm rate-limiter cho endpoint launch-app trong phiên bản sau (DoS mitigation — kẻ tấn công có key vẫn spam được). TDD Non-goal đã ghi rõ.
- **FYI:** Response 200/422 chứa `stdOut`/`stdErr` từ ADB — TDD Q2 đã chốt để CI/CD tự debug. Nếu tương lai muốn strip khi production → thêm flag `?verbose=true`.

## Artifact

| File | Loại | Ghi chú |
|------|------|---------|
| `docs/plans/PLAN-adb-launch-app-api-2026-08-18/steps/STEP-3.2-tech-lead-review.md` | Sửa | Báo cáo review + security audit nhúng trong step file (không tạo ADR riêng vì không phát hiện lỗ hổng nghiêm trọng theo hướng dẫn skill) |
| `docs/plans/PLAN-adb-launch-app-api-2026-08-18/PLAN-MASTER.md` | Sửa | Đổi 3.2 ⬜ → ✅ |

## Quyết định quan trọng

### QUYẾT ĐỊNH: ✅ **APPROVE**

**Lý do:**
1. Build sạch (0 error, 0 warning) + 42/42 test pass (tự chạy lại).
2. 14 mục Code Review Checklist đều PASS (mục 15 "appsettings.json section" không phải blocker vì fail-safe design).
3. Security Audit OWASP + STRIDE: 0 Fail. Không có mục Fail ở nhóm rủi ro cao (A01/A02/A03/A07).
4. Text-matching `AdbService` verified qua grep — mapping StdErr trong `MapAdbResult` khớp chính xác 3 điểm hằng số ở `AdbService.cs` (dòng 49, 90, 184). Rủi ro R3 TDD được quản lý qua ghi chú, không phải blocker.
5. Filter scope hẹp cho 1 endpoint duy nhất — verified qua đọc `Program.cs` — rủi ro R4 auth-bypass eliminated.

**Coi là merge decision:** Project dùng single branch `docker-deploy`, không có PR/branch riêng. Code đã đủ điều kiện tiếp tục chuyển QA Engineer (STEP-4.1).

**Không escalate CTO:** Không phát hiện rủi ro bảo mật cần quyết định kiến trúc cấp CTO. Cơ chế API key tĩnh phù hợp với Non-goal PRD (không rotation/multi-key/rate limit trong phiên bản này).

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Code Review + Security Audit đã APPROVE. Build/test đã tự verify PASS. Không cần QA đọc lại code chi tiết — tập trung test integration/functional theo TDD 8 scenario US (SC-01..SC-08 + EC1..EC7). Filter scope đã verify không lây sang route cũ — QA không cần regression test `/api/install`.
- watch_out:
  1. Text-matching StdErr với `AdbService` là coupling ngầm (R3 TDD) — QA test SC-06/EC1/EC4 PHẢI dùng payload thực tế (không stub) để đảm bảo text match ở runtime.
  2. `appsettings.json` chưa có section `LaunchApp` (user tự thêm sau). Khi QA test 401/200 cần: (a) tự thêm section vào file dev-only hoặc (b) dùng env var `LaunchApp__ApiKey=<test-key>` khi chạy Web host. Không có bước này → mọi request sẽ 401 (fail-safe đúng theo thiết kế nhưng QA sẽ tưởng là bug).
  3. Endpoint không có rate-limit (Optional trong audit) — QA cần smoke test bao nhiêu là hợp lý (không cần load test).
  4. Response body 200/422 chứa `stdOut`/`stdErr` từ ADB — QA verify format khớp TDD Response schemas.
- next_inputs: TDD `docs/tech-design/TDD-adb-launch-app-api.md` mục "Response schemas theo status code" (bảng mapping đầy đủ 8 status code). User story `docs/user-stories/US-adb-launch-app-api.md` cho 8 scenario SC-01..SC-08 và edge case EC1..EC7. Code endpoint tại `src/KztekAdbPublishTool.Web/Endpoints/LaunchAppEndpoints.cs`. Test hiện có 22 unit test — QA bổ sung integration/functional cho end-to-end path.

## Commit
- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
