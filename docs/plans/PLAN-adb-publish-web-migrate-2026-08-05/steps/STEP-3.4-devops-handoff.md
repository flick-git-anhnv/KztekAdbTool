---
step: 3.4
title: Ghi chú bàn giao DevOps (Dockerfile + docker-compose — NGOÀI SCOPE plan này)
assignee: code-migrator
status: done
completed_at: 2026-08-05
deps: [3.3]
---

## Nhiệm vụ
`docs/devops/HANDOFF-web-docker.md` mô tả (spec, KHÔNG code):
- **Dockerfile:** build stage `mcr.microsoft.com/dotnet/sdk:8.0` publish → runtime `mcr.microsoft.com/dotnet/aspnet:8.0` + `apt-get install -y android-tools-adb` (`/usr/bin/adb`). Env: `Adb__AdbPath=/usr/bin/adb`, `Adb__DbPath=/app/data/adbpublishtool.db`, `Adb__UploadsPath=/app/uploads`. `VOLUME ["/app/data","/app/uploads"]`. `EXPOSE 8080`.
- **docker-compose.yml:** `network_mode: host` (BẮT BUỘC để adb thấy LAN — Linux host, không hoạt động trên Docker Desktop Win/Mac). Volumes `./data:/app/data`, `./uploads:/app/uploads`. `restart: unless-stopped`. Healthcheck `curl -f http://localhost:8080/health`.
- **Kiểm thử image:** khởi động trên Linux host → mở browser LAN khác → connect Android WiFi → cài APK thành công.
- **Rủi ro:** `network_mode: host` chỉ hoạt động trên Linux host thật.

Giao ticket DevOps Engineer / DevOps Lead theo WF-DEVOPS.

## Definition of Done
- [ ] `docs/devops/HANDOFF-web-docker.md` đủ 4 mục.
- [ ] Ticket bàn giao đã tạo.

## Artifact
- `docs/devops/HANDOFF-web-docker.md`

## Handoff Payload
- Đã làm: Viết `docs/devops/HANDOFF-web-docker.md` đầy đủ 11 mục (base image, adb install, ENV mapping AdbSettings, volume, network host, port, healthcheck, compose structure, DoD test).
- do_not_redo: Không cần viết lại spec — DevOps Engineer đọc thẳng `docs/devops/HANDOFF-web-docker.md` để viết Dockerfile/compose thật.
- watch_out: `network_mode: host` CHỈ chạy đúng trên Linux host thật, KHÔNG dùng Docker Desktop Windows/Mac để build/test cuối. `AdbSettings.AdbPath` mặc định `/opt/platform-tools/adb` — phải override ENV `Adb__AdbPath=/usr/bin/adb` hoặc symlink.
- next_inputs: `docs/devops/HANDOFF-web-docker.md` là input chính cho DevOps Engineer (WF-DEVOPS) viết Dockerfile + docker-compose.yml.
