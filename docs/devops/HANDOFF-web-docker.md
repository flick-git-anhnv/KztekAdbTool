---
id: HANDOFF-web-docker
title: Bàn giao DevOps — Đóng gói Docker cho KztekAdbPublishTool.Web
created: 2026-08-05
owner: code-migrator → devops-lead / devops-engineer
status: ready-for-devops
related:
  - docs/plans/PLAN-adb-publish-web-migrate-2026-08-05/PLAN-MASTER.md
  - docs/architecture/adb-publish-web-migrate/ADR-001-inventory-and-mapping.md
  - src/KztekAdbPublishTool.Web/Configuration/AdbSettings.cs
---

# Bàn giao DevOps — Đóng gói Docker

> Tài liệu này CHỈ là **spec bàn giao** (không phải code). DevOps Engineer sẽ viết Dockerfile + docker-compose.yml theo WF-DEVOPS dựa trên các yêu cầu bên dưới.

## 1. Bối cảnh
- Project code migrate WinForms → **ASP.NET Core Razor Pages (.NET 8)** đã hoàn tất, QA sign-off PASS (xem PLAN-MASTER §3, dòng 3.3).
- Source: `src/KztekAdbPublishTool.Web/`. Solution build sạch, 19/19 test pass.
- Mục tiêu bước này: build 1 Docker image Linux chạy được trên **Linux host thật** (không phải Docker Desktop Windows/Mac).
- **Quy mô:** single-tenant, 1 container, không cần Kubernetes/orchestration phức tạp — 1 file `docker-compose.yml` đơn giản là đủ.

## 2. Base image gợi ý (multi-stage build)

| Stage | Image | Mục đích |
|---|---|---|
| `build` | `mcr.microsoft.com/dotnet/sdk:8.0` | `dotnet restore` + `dotnet publish -c Release -o /app/publish` |
| `runtime` | `mcr.microsoft.com/dotnet/aspnet:8.0` | Chạy app, cài thêm `adb`, expose port |

## 3. Cài `adb` trong image runtime

Trong stage runtime, cài `adb` bằng `apt-get`:
```
apt-get update && apt-get install -y --no-install-recommends android-tools-adb && rm -rf /var/lib/apt/lists/*
```
- Gói `android-tools-adb` (Debian bookworm/Ubuntu) — cài adb vào `/usr/bin/adb`.
- **Chú ý:** `AdbSettings.AdbPath` mặc định là `/opt/platform-tools/adb` (theo `appsettings.json` + `AdbSettings.cs`). DevOps có 2 lựa chọn:
  - (a) Override ENV `Adb__AdbPath=/usr/bin/adb` trong compose (khuyến nghị — đơn giản nhất).
  - (b) Symlink `ln -s /usr/bin/adb /opt/platform-tools/adb` trong Dockerfile để giữ default không cần override.

## 4. Cấu hình override qua ENV

App đọc section `"Adb"` (xem `Configuration/AdbSettings.cs`, `SectionName = "Adb"`). ASP.NET Core map ENV theo quy ước `Section__Key` (2 dấu gạch dưới):

| ENV var | Giá trị gợi ý | Ý nghĩa |
|---|---|---|
| `Adb__AdbPath` | `/usr/bin/adb` | Đường dẫn adb binary |
| `Adb__PollIntervalMs` | `3000` | Chu kỳ poll device (mặc định 3s) |
| `Adb__DbPath` | `/app/data/adbpublishtool.db` | Đường SQLite DB |
| `Adb__UploadsPath` | `/app/uploads` | Thư mục lưu APK upload |
| `Adb__MaxUploadBytes` | `500000000` | Giới hạn upload (500MB) |
| `ASPNETCORE_URLS` | `http://+:8080` | Port Kestrel bind |
| `ASPNETCORE_ENVIRONMENT` | `Production` | |

## 5. Volume mount (persistent data)

| Host path | Container path | Lý do |
|---|---|---|
| `./data/` | `/app/data/` | Chứa SQLite DB — PHẢI persist qua các lần restart |
| `./uploads/` | `/app/uploads/` | APK người dùng upload — persist để không mất khi restart |

Dockerfile khai báo:
```
VOLUME ["/app/data", "/app/uploads"]
```

## 6. Network: BẮT BUỘC `--network host`

**Vì sao:** App cần `adb connect <ip>:5555` tới thiết bị Android trên cùng LAN qua WiFi. Nếu container chạy trong network bridge mặc định:
- Docker NAT che các thiết bị LAN — `adb connect` sẽ timeout hoặc fail.
- Broadcast/scan mạng (`ScanCoordinator` quét 512 IP LAN) không nhìn thấy host thật.

**Yêu cầu:** compose config `network_mode: host` (hoặc `docker run --network host`).

**Rủi ro / cảnh báo:**
- `network_mode: host` **CHỈ hoạt động trên Linux host thật**. Docker Desktop (Windows/Mac) chạy Docker trong VM → `host` = network của VM, không phải máy vật lý → adb vẫn không thấy LAN. **KHÔNG deploy trên Docker Desktop cho production.**
- Không cần khai báo `ports:` trong compose khi dùng host network — app bind trực tiếp cổng host.
- Bảo mật: app chia sẻ toàn bộ network stack với host — cân nhắc firewall host để hạn chế cổng 8080 nếu không muốn public.

## 7. Port

- Kestrel default: **8080** (khi set `ASPNETCORE_URLS=http://+:8080`).
- Với `network_mode: host` → truy cập trực tiếp `http://<host-ip>:8080`.
- KHÔNG cần map `ports:` trong compose khi dùng host network (map cổng sẽ bị Docker cảnh báo/bỏ qua).

## 8. Healthcheck

Endpoint `/health` đã có sẵn (do STEP-2.5 tạo — trả `200 OK` khi app up).

Docker `HEALTHCHECK` gợi ý:
```
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1
```
- Cần cài `curl` trong runtime image (`apt-get install -y curl`) — hoặc dùng `wget` sẵn có.

## 9. docker-compose.yml — cấu trúc tối thiểu (chỉ mô tả, DevOps viết)

Compose file 1 service duy nhất:
- `build:` → context root repo, dockerfile `src/KztekAdbPublishTool.Web/Dockerfile` (hoặc root, tuỳ DevOps chọn).
- `network_mode: host`.
- `volumes:` mount `./data`, `./uploads`.
- `environment:` block set các ENV `Adb__*` + `ASPNETCORE_URLS`.
- `restart: unless-stopped`.
- `healthcheck:` như §8.

## 10. Kiểm thử image sau khi build (Definition of Done cho DevOps)

- [ ] `docker compose up -d` chạy trên Linux host thật.
- [ ] `docker ps` → container status `healthy` sau ~30s.
- [ ] Mở trình duyệt máy khác trong cùng LAN → `http://<host-ip>:8080/` → dashboard load OK.
- [ ] Cắm 1 điện thoại Android (đã bật ADB over WiFi) cùng LAN → nhập IP → Connect → device xuất hiện Online.
- [ ] Upload 1 APK → Install → APK cài thành công lên device.
- [ ] Restart container → data (device list, upload) còn nguyên.

## 11. Ghi chú chuyển sang WF-DEVOPS

- Tạo ticket DevOps mở workflow **WF-DEVOPS**: DevOps Engineer viết `Dockerfile` + `docker-compose.yml` → DevOps Lead review + approve.
- Sau khi image build thành công + healthcheck OK → mở ticket deploy production riêng (WF-DEVOPS deploy).
