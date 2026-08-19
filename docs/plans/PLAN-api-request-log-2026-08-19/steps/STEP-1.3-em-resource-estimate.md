---
step: "1.3"
plan: ../PLAN-MASTER.md
agent: engineering-manager
status: done
completed_at: "2026-08-19 13:26"
deps: ["1.1", "1.2"]
---

# STEP 1.3 — Engineering Manager: Estimate Resource & Confirm Priority

## Input nhận
- `docs/prd/PRD-api-request-log.md` từ STEP-1.1
- `docs/user-stories/US-api-request-log.md` từ STEP-1.2

## Nhiệm vụ
Ước tính thời gian/effort cho feature, phân bổ task giữa Senior Developer (backend) và Junior Developer (frontend JS), xác nhận priority P2 và không có xung đột resource với các task đang chạy.

## Definition of Done
- [ ] `docs/planning/RESOURCE-api-request-log.md` đã được tạo
- [ ] Estimate thời gian cho từng nhóm task (backend/frontend) được ghi rõ (đơn vị: giờ)
- [ ] Phân bổ rõ: phần nào Senior Dev phụ trách, phần nào Junior Dev phụ trách
- [ ] Priority P2 được xác nhận chính thức (hoặc điều chỉnh nếu EM thấy cần)
- [ ] Không có resource conflict với task đang chạy (adb-reconnect-after-restart là P1 đang active — cần xác nhận không block)
- [ ] Script `scripts/md_to_docx_kztek.py` đã chạy thành công → tạo ra `.docx`

## Đã làm
- Đọc PRD (STEP-1.1) và US (STEP-1.2) để hiểu scope đầy đủ.
- Phân tích P1 `adb-reconnect-after-restart`: chỉ còn STEP-3.3 DevOps deploy, code area hoàn toàn khác (DevicePollWorker vs. ApiRequestLog/service/endpoint logging) — xác nhận không conflict.
- Viết `docs/planning/RESOURCE-api-request-log.md`: phân bổ Senior Dev (backend 5–7h) + Junior Dev (frontend JS 2–3h), estimate tổng ~15–22h (~2–3 ngày).
- Xác nhận priority P2 chính thức — không nâng P1.
- Chạy `md_to_docx_kztek.py` → `.docx` thành công (PDF thất bại do thiếu xelatex — chấp nhận được).
- Commit `2ac134b`.

## Artifact
- `docs/planning/RESOURCE-api-request-log.md` — tạo mới
- `docs/planning/RESOURCE-api-request-log.docx` — xuất thành công

## Quyết định quan trọng
1. **P2 xác nhận** — observability tốt nhưng không block production; giữ thấp hơn P1.
2. **Không conflict với P1** — code area hoàn toàn tách biệt; tech team (Senior Dev, Junior Dev, Tech Lead) free ngay trong khi P1 chỉ còn DevOps deploy.
3. **P2 deploy staging** chờ P1 deploy (STEP-3.3) hoàn thành trước — DevOps Lead xác nhận thời điểm.
4. **Junior Developer được giao frontend JS** (signalr-client.js + appendLog rendering) — phù hợp cấp, scope rõ sau khi có TDD.
5. **Senior Developer** phụ trách toàn bộ backend C# — bao gồm 401 inject point cần kinh nghiệp kiến trúc.
6. **Bước TDD (STEP-1.5)** là prerequisite quan trọng nhất trước khi bắt đầu Phase 2 — phải chốt: inject point 401, tên SignalR event, async strategy, CallerIp source.

## Handoff Payload — bước sau đọc phần này
- do_not_redo: Priority P2 đã được xác nhận — không đánh giá lại. Conflict analysis với P1 đã done — P2 được phép chạy song song.
- watch_out: P2 deploy staging (STEP-3.4) phải chờ P1 deploy (STEP-3.3) xong — DevOps Lead quyết định thời điểm. `DeviceEndpoints.cs` sẽ có code P1 merged — Senior Dev phải pull code P1 trước khi code P2 inject logging vào file đó.
- next_inputs: `docs/planning/RESOURCE-api-request-log.md` — PJM (STEP-1.4) dùng để lên sprint plan lightweight; estimate tổng ~15–22h, Senior Dev 5–7h, Junior Dev 2–3h.

## Commit
- Hash: 2ac134b
- Đã push: không

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
