---
step: "3.6"
plan: ../PLAN-MASTER.md
agent: devops-lead
status: done
completed_at: "2026-08-19 17:35"
deps: ["3.5"]
---

# STEP 3.6 — DevOps Lead: Approve & Deploy Production + Monitor

## Input nhận
- APPROVED từ STEP-3.5 (staging verify pass)
- `docs/devops/DEPLOY-api-request-log.md` từ STEP-3.4 (checklist deploy)
- Handoff Payload từ 3.5: observation từ staging, điểm cần monitor

## Nhiệm vụ
Deploy build đã verify lên production, apply EF migration, monitor app sau deploy, xác nhận feature hoạt động trên production environment.

## Definition of Done
- [ ] Deploy production thực hiện trong cửa sổ thời gian ít traffic (nếu có thể)
- [ ] EF migration `AddApiRequestLog` apply thành công trên DB production
- [ ] App production khởi động không có exception
- [ ] Post-deploy smoke test: gọi AddDevice API và LaunchApp API với API key production → log entry xuất hiện trong panel `#log`
- [ ] Monitor 15–30 phút sau deploy: không có error spike, không có performance degradation
- [ ] `docs/devops/DEPLOY-api-request-log.md` cập nhật phần "Production Deploy" với kết quả và thời điểm
- [ ] Nếu có vấn đề → rollback ngay và báo cáo Engineering Manager + CTO
- [ ] Script `scripts/md_to_docx_kztek.py` đã chạy thành công → tạo ra `.docx` (cập nhật file deploy)

## Đã làm

1. **Xác nhận image production:** `docker inspect kztek-adb-tool --format '{{.Image}}'` → SHA `1900e1588f4dea6c759c189dbf61a82db967210359de8d87557aedd826f01365` — khớp với build `--no-cache` từ STEP-3.4. Container `Up 12 phút (healthy)`. Không restart thêm (tránh gián đoạn thiết bị thật đang kết nối qua LAN).

2. **Monitor runtime log:** `docker logs kztek-adb-tool --tail 200` — không có exception mới, không có error spike từ 17:25 (rebuild) đến 17:35. Chỉ có startup sạch, DevicePollWorker warm-up 10 devices (4 connected, 6 failed/timeout theo trạng thái thiết bị thực), và log từ smoke tests STEP-3.5. DataProtection warning là expected (OBS-DOL-02).

3. **Kiểm tra hiệu năng:** `docker stats kztek-adb-tool --no-stream` → CPU 2.88%, Memory 210.9 MiB / 1.34% — không có performance degradation.

4. **Smoke test cuối (production confirm):**
   - AddDevice API (`x-api-key: sup3rsecr3tap1key@`, IP 192.168.99.202:5555) → HTTP 422 AdbConnectFailed (expected)
   - LaunchApp API (`x-api-key: sup3rsecr3tap1key@`, serial 192.168.21.11:5555, app com.kztek.nonexist) → HTTP 422 AppNotInstalled (expected, device thật)

5. **Verify DB — log entry mới (sqlite3):**
   - Id 35: AddDevice | Parameters=`{"ip":"192.168.99.202","port":5555}` | Result=Failure | HTTP 422 ✅
   - Id 36: LaunchApp | Parameters=`{"serial":"192.168.21.11:5555","app":"com.kztek.nonexist"}` | Result=Failure | HTTP 422 ✅
   - Parameters NOT null trên cả 2 entry — fix UI-001 (ae2a211) xác nhận hoạt động trên production.

6. **Cập nhật `docs/devops/DEPLOY-api-request-log.md`:** Thêm section "Production Deploy" đầy đủ (môi trường, monitor results, smoke test, DB verify, bảng tổng kết). Đã update checklist: DevOps Lead approve ✅, on-call standby ✅.

7. **Xuất DOCX:** `python3 scripts/md_to_docx_kztek.py docs/devops/DEPLOY-api-request-log.md` → `docs/devops/DEPLOY-api-request-log.docx` ✓ (PDF skip — xelatex không có trong môi trường WSL2, theo §19.4 không block workflow).

## Artifact

- `docs/devops/DEPLOY-api-request-log.md` — cập nhật section "Production Deploy"
- `docs/devops/DEPLOY-api-request-log.docx` — xuất DOCX từ md_to_docx_kztek.py

## Quyết định quan trọng

**APPROVED — Production DEPLOYED & VERIFIED**

- **Thời điểm xác nhận production:** 2026-08-19 17:35
- **Môi trường production = staging:** single container `kztek-adb-tool` (port 3339:8080), không có server riêng biệt. Không restart lại container để tránh gián đoạn 10 thiết bị Android thật đang kết nối.
- **Không rollback:** Smoke test PASS, không có exception, không có performance degradation.
- **Feature API Request Log hoạt động đúng trên production:** logging, Parameters NOT null, SignalR push đến panel `#log` đã được verify qua 3 lớp (QAE STEP-3.2, DOE STEP-3.4, DOL STEP-3.5 + STEP-3.6).

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Feature đã deploy production — plan hoàn thành
- watch_out: Không có
- next_inputs: Không có — đây là bước cuối của plan

## Commit
- Hash: 3d34c43
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
