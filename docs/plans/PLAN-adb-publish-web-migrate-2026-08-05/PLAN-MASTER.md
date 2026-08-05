---
id: PLAN-adb-publish-web-migrate
title: Migrate KztekAdbPublishTool WinForms → ASP.NET Core Razor Pages (.NET 8) cho Docker Linux
created: 2026-08-05
updated: 2026-08-05 16:00
status: approved
owner: code-migrator (planning) → senior-developer + junior-developer (implementation)
related:
  - docs/architecture/adb-publish-web-migrate/ADR-001-inventory-and-mapping.md
---

# PLAN-MASTER — Migrate WinForms → ASP.NET Core (.NET 8) + Docker Linux

> Chi tiết inventory + 3 bảng mapping (Control / Event / Service): xem ADR-001 (link bên trên).
> Nguyên tắc §1A: project nguồn `src/KztekAdbPublishTool` KHÔNG SỬA — code mới vào `src/KztekAdbPublishTool.Web/`.

---

## 1. Mục tiêu & phạm vi

- Chuyển 100% tính năng của WinForms sang web (không cắt tính năng nào).
- Đích: ASP.NET Core **Razor Pages** (.NET 8) + SignalR + Bootstrap 5 + SQLite.
- Build được Docker image Linux; bước viết Dockerfile do DevOps làm SAU khi migrate code xong.
- ADB chỉ dùng qua WiFi (`adb connect ip:5555`).

---

## 2. Assumptions cần user xác nhận trước khi bắt đầu

1. Đích là **Razor Pages** (không MVC controllers, không Blazor, không SPA React/Vue).
2. Container chạy với `--network host` trên Linux → adb thấy được LAN thiết bị.
3. UI dùng **Bootstrap 5** (bỏ hoàn toàn KztekComponent + Guna.UI2).
4. State toàn cục single-tenant (tất cả tab/user thấy cùng danh sách device).
5. Real-time cập nhật qua **SignalR**.

→ Nếu 1 trong 5 giả định sai → BLOCK, sửa plan trước khi thực thi.

---

## 3. Phases & Steps

| # | Phase / Step | Loại | Assignee | Nhóm ∥ | Phụ thuộc | Effort | Status | Hoàn thành lúc | Step file |
|---|---|---|---|---|---|---|---|---|---|
| **Phase 1 — Foundation** | (Senior tuần tự) | | | | | | | | |
| 1.1 | Tạo project `src/KztekAdbPublishTool.Web` (Razor Pages .NET 8), Program.cs khung, DI, appsettings (adbPath, dbPath, uploads) | Foundation | Senior Dev | A | — | 3h | ✅ | 2026-08-05 16:00 | [STEP-1.1](steps/STEP-1.1-bootstrap-web-project.md) |
| 1.2 | Copy `AdbService`, `DeviceRepository`, `ApkManifestReader` sang project mới; sửa constructor nhận path từ `IOptions<>`; đăng ký DI (Singleton) | Foundation | Senior Dev | A | 1.1 | 2h | ✅ | 2026-08-05 16:00 | [STEP-1.2](steps/STEP-1.2-port-services.md) |
| 1.3 | Tạo `Hubs/DeviceHub` (SignalR) + `Workers/DevicePollWorker` khung (chưa logic, chỉ heartbeat) + Bootstrap 5 layout (`_Layout.cshtml`) | Foundation | Senior Dev | A | 1.2 | 3h | ✅ | 2026-08-05 16:00 | [STEP-1.3](steps/STEP-1.3-signalr-worker-layout.md) |
| **Phase 2 — Features song song** | | | | | | | | | |
| 2.1 | `DevicePollWorker` logic đầy đủ (3s poll → Upsert → RefreshVersion Online → push SignalR `DevicesUpdated`); toggle bật/tắt qua flag global | Backend | Senior Dev | B | 1.3 | 4h | ⬜ | — | [STEP-2.1](steps/STEP-2.1-device-poll-worker.md) |
| 2.2 | Install workflow: endpoint POST `/api/install`, `InstallCoordinator` (queue per-device, SemaphoreSlim(4) global), push progress `InstallProgress`/`DeviceInstalled` qua SignalR, giữ logic beforePackages/afterPackages + LaunchApp | Backend | Senior Dev | B | 1.3 | 5h | ⬜ | — | [STEP-2.2](steps/STEP-2.2-install-workflow.md) |
| 2.3 | Network scan: endpoint POST `/api/scan/start` + `/api/scan/cancel`, `ScanCoordinator` (SemaphoreSlim(24), max 512 IP, timeout 1200ms), push `ScanProgress`/`ScanFound` qua SignalR | Backend | Senior Dev | B | 1.3 | 4h | ⬜ | — | [STEP-2.3](steps/STEP-2.3-network-scan.md) |
| 2.4 | APK upload + auto-detect package: POST `/api/apk/upload` (multipart, cấu hình Kestrel 500MB), lưu `uploads/`, gọi `ApkManifestReader`, trả `{path, packageName}`; DELETE `/api/apk` để xóa | Backend | Senior Dev | B | 1.2 | 3h | ⬜ | — | [STEP-2.4](steps/STEP-2.4-apk-upload.md) |
| 2.5 | Endpoints device management: POST `/api/devices/connect`, `/api/devices/connect-batch`, `/api/devices/remove`, `/api/devices/poll` (trigger thủ công), GET `/api/devices` (fallback nếu SignalR mất), POST `/api/settings/package`, `/api/polling/toggle` | Backend | Junior Dev | B | 1.2 | 3h | ⬜ | — | [STEP-2.5](steps/STEP-2.5-device-endpoints.md) |
| 2.6 | Dashboard `Pages/Index.cshtml`: toolbar (3 hàng như MainForm), grid device 9 cột, action panel (Chọn tất cả / Cài selected / Cài all / Xóa), progress bar + status, log area | Frontend | Junior Dev | B | 1.3 | 5h | ⬜ | — | [STEP-2.6](steps/STEP-2.6-dashboard-view.md) |
| 2.7 | Network scan modal `#networkScanModal` + JS gọi API scan/cancel/add, progress bar realtime | Frontend | Junior Dev | B | 1.3 | 3h | ⬜ | — | [STEP-2.7](steps/STEP-2.7-scan-modal.md) |
| 2.8 | Client-side JS: SignalR client (auto-reconnect + fallback polling), toggle select-all theo filter, filter IP/version, checkbox row toggle, log append, toast/modal cho MessageBox tương đương | Frontend | Junior Dev | B | 1.3 | 4h | ⬜ | — | [STEP-2.8](steps/STEP-2.8-client-js.md) |
| **Phase 3 — Integration & Verify** | | | | | | | | | |
| 3.1 | Ghép, chạy local (`dotnet run`), test 12 luồng chính (poll, install, scan, connect, remove, filter, select-all, upload APK, auto-detect package, launch app sau install, cancel scan, toggle auto-detect) | Integration | Senior Dev | C | 2.1–2.8 | 4h | ⬜ | — | [STEP-3.1](steps/STEP-3.1-integration.md) |
| 3.2 | Code Migrator review artifact (Opus): correctness, behavior parity vs WinForms, security, style — request-changes nếu lệch | Review | Code Migrator | C | 3.1 | 2h | ⬜ | — | [STEP-3.2](steps/STEP-3.2-code-review.md) |
| 3.3 | QA Engineer smoke test 12 luồng + verify behavior parity đối chiếu bản WinForms | QA | QA Engineer | D | 3.2 | 4h | ⬜ | — | [STEP-3.3](steps/STEP-3.3-qa-smoke.md) |
| 3.4 | Ghi chú bàn giao DevOps: yêu cầu Dockerfile (mcr .NET 8 SDK+aspnet, apt install android-tools-adb, volume /app/data /app/uploads, network host), docker-compose | Handoff | Code Migrator | D | 3.3 | 1h | ⬜ | — | [STEP-3.4](steps/STEP-3.4-devops-handoff.md) |

**Tổng effort ước tính:** ~50 giờ = **~6–7 ngày công** (Senior ~30h, Junior ~15h, Code Migrator review ~3h, QA ~4h).
**Đường tới hạn (critical path):** 1.1 → 1.2 → 1.3 → (2.1 ∥ 2.2 ∥ 2.3) → 3.1 → 3.2 → 3.3 → 3.4.

---

## 4. Phân bổ song song

- **Nhóm A (tuần tự, Senior):** 1.1 → 1.2 → 1.3 (~1 ngày).
- **Nhóm B (song song sau A):**
  - Backend Senior: 2.1, 2.2, 2.3, 2.4 (~2 ngày).
  - Backend Junior: 2.5 (~0.5 ngày).
  - Frontend Junior: 2.6, 2.7, 2.8 (~1.5 ngày, cần thoả thuận contract JSON với Senior trước).
- **Nhóm C:** 3.1, 3.2 (Senior + Code Migrator).
- **Nhóm D:** 3.3, 3.4 (QA + Code Migrator handoff).

---

## 5. Rủi ro chính (chi tiết trong ADR-001 §6)

| # | Rủi ro | Mitigation ngắn |
|---|---|---|
| R1 | Multi-user race cùng install 1 device | `InstallCoordinator` queue per-serial |
| R2 | SignalR mất kết nối → UI cũ | Auto-reconnect + fallback GET `/api/devices` 5s |
| R3 | APK > 28MB fail upload | Kestrel MaxRequestBodySize = 500MB |
| R4 | Container không thấy LAN | Yêu cầu `--network host` (ghi rõ trong handoff DevOps) |
| R5 | Behavior parity filter/select-all | Test đối chiếu WinForms bản gốc ở step 3.3 |

---

## 6. Blockers

_(Không có tại thời điểm lập plan. Cập nhật khi phát sinh.)_

---

## 7. Lịch sử cập nhật

| Ngày | Người | Thay đổi |
|---|---|---|
| 2026-08-05 | Code Migrator | Tạo plan + ADR-001. Chờ user duyệt. |
| 2026-08-05 | User | Duyệt plan — xác nhận cả 5 assumption (Razor Pages, network host trên Linux server thật, Bootstrap 5, single-tenant, SignalR). Bắt đầu Phase 1. |
| 2026-08-05 16:00 | Senior Developer | Phase 1 hoàn thành (STEP 1.1+1.2+1.3 → ✅). Build 0 lỗi, 5/5 test pass. Commit: 0689a7e. |
