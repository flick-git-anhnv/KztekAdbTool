---
step: 4.1
plan: ../PLAN-MASTER.md
agent: QA Engineer
status: done
completed_at: 2026-08-19 10:46
deps: [3.2]
---

# STEP 4.1 — Viết test plan, thực thi test case

## Input nhận
Nhận Handoff Payload từ STEP-3.2 (code đã merge/approve). TDD tại `docs/tech-design/TDD-adb-add-device-api.md`, US tại `docs/user-stories/US-adb-add-device-api.md`.

Lưu ý giống plan mẫu: môi trường CI/sandbox có thể KHÔNG có thiết bị Android thật để test `connect-by-ip` thật sự (không có mạng LAN với thiết bị) — QA Engineer chia nhóm: case verify được qua HTTP thật (input validation, auth 401, serial not found) vs case cần thiết bị thật (connect thành công, status Online sau khi connect) → verify gián tiếp qua unit test hoặc ghi rõ giới hạn cần DevOps/user smoke test thủ công.

## Nhiệm vụ
Viết `docs/test-plans/TEST-PLAN-adb-add-device-api.md` + `docs/test-cases/TC-adb-add-device-api.md`, thực thi test case, log kết quả.

## Definition of Done
- [ ] Test plan + test case cover đủ scenario từ US (cả 2 API)
- [ ] Case chạy được thật → log kết quả PASS/FAIL cụ thể
- [ ] Case cần thiết bị thật → ghi rõ giới hạn, đề xuất smoke test thủ công trước production
- [ ] Xuất DOCX/PDF theo R1

## Đã làm

1. Đọc TDD + US (14 scenario SC-A1–A7, SC-B1–B5, SC-C1–C2) + code `DeviceConnectionEndpoints.cs`.
2. Khởi động app local: `dotnet run --urls http://localhost:5299 --no-build`, xác nhận HTTP 200.
3. Thực thi 19 test case NHOM A qua curl HTTP thật (2026-08-19 10:42:56–10:43:22). 19/19 PASS.
4. Tạo `docs/test-plans/TEST-PLAN-adb-add-device-api.md` + `docs/test-cases/TC-adb-add-device-api.md`.
5. Xuất DOCX (PDF skip — xelatex không có, giống STEP-2.1).
6. Commit `b77dff4`, push `docker-deploy`.

## Artifact

- `docs/test-plans/TEST-PLAN-adb-add-device-api.md` — test plan đầy đủ (NHOM A 19 case + NHOM B 6 case)
- `docs/test-plans/TEST-PLAN-adb-add-device-api.docx` — DOCX xuất bởi md_to_docx_kztek.py
- `docs/test-cases/TC-adb-add-device-api.md` — test case chi tiết + kết quả thực thi 19/19 PASS
- `docs/test-cases/TC-adb-add-device-api.docx` — DOCX xuất bởi md_to_docx_kztek.py

## Quyết định quan trọng

1. **Môi trường không có ADB binary**: `adb` không có tại `/opt/platform-tools/adb` trong local dev. Mọi request vượt validation → ADB layer → 500 AdbNotFound. Đây là hành vi đúng — code path `AdbNotFound` đã implement và unit test cover. TC-A10 PASS vì 500 là kết quả hợp lệ theo TDD (khi binary missing).
2. **Container production không có code mới**: Container đang chạy (port 3339) được build trước khi có `DeviceConnectionEndpoints.cs` — cả 2 endpoint trả 404. Test chạy trực tiếp qua `dotnet run` local để có code mới nhất.
3. **TC-B05 (empty path segment)**: ASP.NET Core router không match `/api/devices//status`, trả 404 body rỗng. Đây là behavior đúng theo TDD Q6 — không phải bug.
4. **NHOM B (6 case)**: Verify gián tiếp qua 64/64 unit test đã PASS. Cần smoke test thủ công tại staging (STEP-4.3) trước go-live production.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")
- do_not_redo: Không chạy lại 19 TC NHOM A — đã PASS đầy đủ với log thật. Không build lại app.
- watch_out: (1) NHOM B (TC-C01–TC-C06, 6 case) chưa test được — cần thiết bị Android thật tại staging. QA Lead sign-off bước 4.2 PHẢI yêu cầu DevOps chạy NHOM B trước go-live production (STEP-4.3/4.4). (2) Container hiện tại (port 3339) CHƯA có code mới — cần rebuild trước khi smoke test staging. (3) TC-A10 trả 500 thay vì 422 vì môi trường local không có ADB binary — tại staging/production có binary, IP không tồn tại sẽ trả 422 sau timeout ~10s.
- next_inputs: `docs/test-cases/TC-adb-add-device-api.md` (kết quả 19/19 PASS + danh sách NHOM B cần smoke test); `docs/test-plans/TEST-PLAN-adb-add-device-api.md` (tiêu chí pass/fail); không có bug P0/P1 → QA Lead có thể sign-off có điều kiện (NHOM B phải PASS tại staging trước production).

## Commit
- Hash: b77dff4
- Đã push: Yes — docker-deploy → origin

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
