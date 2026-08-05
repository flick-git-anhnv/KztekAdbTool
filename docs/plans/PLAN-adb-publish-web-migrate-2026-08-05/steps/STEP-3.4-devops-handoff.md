---
step: 3.4
title: Ghi chú bàn giao DevOps (Dockerfile + docker-compose — NGOÀI SCOPE plan này)
assignee: code-migrator
status: todo
completed_at: —
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
- Đã làm: —
- do_not_redo: —
- watch_out: —
- next_inputs: Ticket bàn giao DevOps Lead mở WF-DEVOPS
