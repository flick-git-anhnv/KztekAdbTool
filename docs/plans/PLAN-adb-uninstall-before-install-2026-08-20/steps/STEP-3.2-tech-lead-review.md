---
step: 3.2
plan: ../PLAN-MASTER.md
agent: tech-lead
status: done
completed_at: 2026-08-20 14:21
deps: [3.1]
---

# STEP 3.2 — Code Review và Quyết định Merge

## Input nhận
- Commit `c0565c3` (local, chưa push) sửa 7 file source + 1 file test mới + CODE-GRAPH cập nhật
- TDD: `docs/tech-design/TDD-adb-uninstall-before-install.md`
- Handoff Payload từ STEP-3.1: do_not_redo (KHÔNG thêm `UninstallApkAsync` vào `IAdbService`; giữ percent flag TẮT 0/20/40/60/80/100); watch_out (build phải qua Docker SDK container; classify dùng `Success && StdOut.Contains("Success")`, không dùng exit code đơn thuần); next_inputs (commit `c0565c3` với 7 file + test file `UninstallBeforeInstallTests.cs`)

## Nhiệm vụ
Review toàn bộ diff commit `c0565c3` đối chiếu TDD; verify lại build + test bằng Docker SDK container; quyết định APPROVE hoặc REQUEST CHANGES.

## Definition of Done
- [x] Review checklist đầy đủ: correctness, error handling graceful, test coverage, UI/style, JS payload
- [x] Build verify: build sạch, tất cả test pass sau review (tự chạy Docker SDK)
- [x] Quyết định APPROVE được ghi rõ trong step file
- [x] Không tạo commit merge riêng — nhánh làm việc là `docker-deploy`, không có feature branch, code đã sẵn sàng đi tiếp UXR + QA
- [x] Cập nhật step file này + PLAN-MASTER.md status → ✅

## Đã làm

### Review checklist 7 mục — kết quả từng mục

| # | Mục | Kết quả | Chi tiết |
|---|-----|---------|----------|
| 1 | Correctness — logic uninstall-before-install | ✅ PASS | `AdbService.UninstallApkAsync` chuẩn TDD (timeout 30s, không dùng `-k`/`--user 0`). `InstallCoordinator.DoInstallAsync`: block uninstall đặt TRƯỚC luồng install cũ, sau timeout setup — đúng thứ tự. Chỉ chạy khi `uninstallBeforeInstall && !IsNullOrWhiteSpace(packageName)` — đúng EC2. Graceful error handling D2/D3: cả nhánh real-success và fail đều gửi SignalR 15% + tiếp tục install, không abort. Log warning kèm nguyên StdOut/StdErr đúng format TDD §D2. Classify logic `result.Success && StdOut.Contains("Success", OrdinalIgnoreCase)` — đúng watch_out. |
| 2 | Backward-compat percent khi flag TẮT | ✅ PASS | `pList=0, pInstall=20, pVerify=40, pVersion=60, pLaunch=80` khi flag TẮT — đúng 5/5 mốc gốc. 100% cuối (FinishWithStatus) không đổi. Chuỗi 0/20/40/60/80/100 giữ NGUYÊN — không bị lệch. |
| 3 | `QueueInstalls` signature | ✅ PASS | Param `bool uninstallBeforeInstall` KHÔNG có default value (khớp TDD "bắt caller khai báo rõ"). Grep `QueueInstalls(` toàn repo: chỉ 1 định nghĩa (InstallCoordinator.cs:51) + 1 callsite (InstallEndpoints.cs:60 đã sửa) — KHÔNG sót callsite nào. |
| 4 | UI/style checkbox | ✅ PASS | Đặt ở hàng 3 toolbar (comment `<!-- Hàng 3: Network scan + Refresh + Auto-detect -->` dòng 64), ngay sau `chk-auto-detect` (dòng 80/81) → `chk-uninstall-before-install` (dòng 87+). Dùng đúng pattern `col-auto ms-2` + `form-check form-switch mb-0` khớp chk-auto-detect. Label rõ "Gỡ cài đặt app trước khi cài". `checked` attribute conditional theo `Model.UninstallBeforeInstall`. |
| 5 | JS payload | ✅ PASS | `btn-install-selected` (dòng ~492) và `btn-install-all` (dòng ~516) đều đọc `$id('chk-uninstall-before-install')?.checked === true` (an toàn với null) và truyền đúng `uninstallBeforeInstall`. Change handler `chkUninstall` (dòng ~449) gọi đúng endpoint mới `POST /api/settings/uninstall-before-install` với body `{ enabled: chkUninstall.checked }`. |
| 6 | Test coverage ≥2 case classify | ✅ PASS | 12 test trong `UninstallBeforeInstallTests.cs` — 5 test classify (real success + failure exit 0 stdout "Failure" + failure exit 1 + timeout exit -1 + case-insensitive "success"), 2 test `InstallRequest` (default false + set true), 5 test endpoint DTO + `IndexModel.OnGet` parse. Test có meaningful assertion — không giả để qua coverage. Case `Classify_ExitCode0_StdOutContainsFailure_IsNotRealSuccess` chính là watch_out mấu chốt. |
| 7 | Build + test verify lại (Docker SDK) | ✅ PASS | `docker run --rm -v $(pwd):/src -w /src mcr.microsoft.com/dotnet/sdk:8.0-jammy dotnet test tests/KztekAdbPublishTool.Web.Tests/KztekAdbPublishTool.Web.Tests.csproj` → `Passed! - Failed: 0, Passed: 97, Skipped: 0, Total: 97, Duration: 222 ms`. Build sạch (Restored 7.02s, không warning). Trùng khớp báo cáo Senior Dev. |

### Rủi ro bảo mật (đánh giá thêm)
- Endpoint `POST /api/settings/uninstall-before-install`: nhận `bool Enabled`, type-safe, không có SQL injection risk (dùng `SetSetting` param binding). Không có auth — nhưng **trùng pattern** các endpoint settings hiện có (`/api/settings/package`, `/api/settings/polling`); toàn app không có auth (tool nội bộ single-tenant Docker LAN). Không phải regression bảo mật.
- Feature không đụng auth/payment/DB schema/dữ liệu nhạy cảm → KHÔNG kích hoạt `security-audit-stride` (đúng theo CLAUDE.md §4 WF-FEATURE Bước 10a — có điều kiện).

## Artifact
- Không tạo file mới. Chỉ cập nhật step file (chính file này) và PLAN-MASTER.md.
- Không có REQUEST CHANGES → không có commit code sửa mới. Commit code giữ nguyên `c0565c3`.

## Quyết định quan trọng
- **APPROVE** commit `c0565c3`. Code sẵn sàng đi tiếp Bước 3.3 (UX/UI Reviewer) và Phase 4 (QA + DevOps).
- **Lý do:** 7/7 mục review pass; verify build/test độc lập trên Docker SDK khớp báo cáo Senior Dev (97/97 pass, 0 fail); không phát hiện regression backward-compat; không có rủi ro bảo mật ngoài baseline hiện tại.
- **Không merge branch riêng:** plan này đang làm trực tiếp trên nhánh `docker-deploy` (theo tiền lệ các bước trước), không có feature branch nên không có PR merge tách rời. Commit `c0565c3` giữ nguyên trên nhánh `docker-deploy` local.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: KHÔNG review lại logic backend/JS (đã APPROVE 7 mục ở Bước 3.2). KHÔNG chạy lại `dotnet test` (đã verify 97/97 pass qua Docker SDK). KHÔNG tạo PR merge branch riêng (plan làm trực tiếp trên `docker-deploy`, không có feature branch).
- watch_out: Bước 3.3 (UX/UI Reviewer) chạy app THẬT (Dashboard trên trình duyệt) — không phải chỉ review code. Cần khởi động app qua Docker (`docker-compose up` từ root repo) hoặc `dotnet run` từ `src/KztekAdbPublishTool.Web`; port mặc định theo `appsettings.json` (thường `8080` khi qua docker-compose, hoặc `5000/5001` khi `dotnet run` local). Kiểm tra kỹ Dashboard `/` — checkbox mới `chk-uninstall-before-install` nằm ở hàng 3 toolbar SAU `chk-auto-detect`. Đánh giá C1–C7 tập trung vào: (a) checkbox có visible/tap-target đủ 44px?; (b) label "Gỡ cài đặt app trước khi cài" có rõ nghĩa cho user không am hiểu kỹ thuật không?; (c) persist state sau F5 (test bằng cách bật → refresh page → kiểm tra `checked`); (d) khi bật + click Install, có thấy 2 progress message mới ("Đang gỡ cài đặt..." / "Đã gỡ cài đặt" hoặc "Bỏ qua gỡ") trước 6 message cũ không?; (e) khi tắt, luồng install có giữ nguyên 6 message cũ với percent 0/20/40/60/80/100 không?.
- next_inputs: Commit `c0565c3` trên nhánh `docker-deploy` (local, chưa push). File UI: `src/KztekAdbPublishTool.Web/Pages/Index.cshtml` (checkbox dòng ~82-91), `wwwroot/js/dashboard.js` (3 handler đã kiểm tra). Endpoint test: `POST /api/settings/uninstall-before-install` với body `{"enabled":true}`. Không cần thiết bị Android thật để đánh giá UI/UX cơ bản; nếu có thiết bị test thì đánh giá thêm hành vi thực tế của progress message khi flag BẬT/TẮT.

## Commit
- Hash: `c0565c3` (không đổi — review APPROVE, không sửa code)
- Đã push: không (giữ nguyên trạng thái local như Bước 3.1)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
