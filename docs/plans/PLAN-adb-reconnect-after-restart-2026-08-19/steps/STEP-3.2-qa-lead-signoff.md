---
step: "3.2"
plan: ../PLAN-MASTER.md
agent: qa-lead
status: done
completed_at: "2026-08-19 12:17"
deps: ["3.1b"]
---

# STEP 3.2 — QA Lead: Sign-off chất lượng (P1 — bắt buộc)

## Input nhận

**Từ STEP-3.1b (QA Engineer — Handoff Payload):**
- do_not_redo: QA Engineer đã chạy app thật với 2 WiFi device offline, xác nhận cả 2 được thử warm-up (log evidence tại STEP-3.1b Bước 3). Đã chạy 73/73 unit test PASS. Không cần re-run test hay re-build.
- watch_out: (1) TC-1/TC-2 còn ENVIRONMENT LIMITATION — warm-up logic đúng (log xác nhận), nhưng không có Android device thật để verify E2E 200 OK sau restart. Đây là giới hạn đã tồn tại từ STEP-3.1, không phải lỗi mới từ fix cff893f. (2) Nit kỹ thuật (string constant lặp) là Optional — không cần fix trước sign-off. (3) Cả 2 bug: (a) warm-up không được gọi sau restart [fix 3a86825] VÀ (b) device timeout chặn device khác [fix cff893f] — đều đã có test coverage và verified.
- next_inputs: STEP-3.1b + STEP-3.1 + BUG report + unit test results (73/73 PASS + 9/9 warm-up PASS) là input đầy đủ. Commit cần sign-off: `cff893f` (bao gồm cả `3a86825`). QA Lead xác nhận ENV_LIMIT cho TC-1/TC-2 dựa trên log evidence + unit test coverage.

## Nhiệm vụ

Review toàn bộ evidence từ Bước 3.1 + 3.1b: xác nhận cả 2 bug P1 đã được fix và không có P0/P1 regression mới. Sign-off để cho phép DevOps Engineer deploy fix (hoặc veto nếu còn vấn đề).

## Definition of Done

- [x] Đọc đầy đủ STEP-3.1, STEP-3.1b, STEP-2.2, STEP-2.4 để tự đánh giá độc lập
- [x] Xác nhận TC-5 evidence (log app thật) đủ tin cậy để coi bug thứ 2 đã fix
- [x] Đánh giá risk tổng thể (sequential warm-up / ENV_LIMIT)
- [x] Quyết định rõ ràng: PASS CÓ ĐIỀU KIỆN
- [x] Ghi sign-off vào BUG report `docs/bugs/BUG-adb-reconnect-after-restart.md`

## Đã làm

### 1. Đánh giá TC-5 evidence (STEP-3.1b)

**Log app thật tại STEP-3.1b Bước 3 — đủ tin cậy không?**

QA Lead đọc trực tiếp log startup từ lần chạy 2026-08-19 12:13 (không qua self-report):

```
info: ...DevicePollWorker warm-up: reconnecting 2 persisted WiFi device(s)...
info: ...Now listening on: http://0.0.0.0:57946     ← Kestrel up TRƯỚC khi warm-up xong
info: ...Application started.
warn: ...Warm-up connect 192.0.2.1:5555 failed/timeout: ... — skipping, continuing with next device
warn: ...Warm-up connect 192.0.2.2:5555 failed/timeout: ... — skipping, continuing with next device
```

Đánh giá từng điểm:

| Điểm kiểm tra | Nhận xét của QA Lead |
|---|---|
| Cả 2 device đều được thử | LOG CONFIRM — cả `192.0.2.1:5555` lẫn `192.0.2.2:5555` đều có dòng warn. Trước fix: device 2 "im lặng" hoàn toàn. Đây là bằng chứng dứt khoát. |
| Log format "skipping, continuing with next device" | ĐÚNG với fix 2.3 — message này chỉ xuất hiện khi branch `continue` được thực thi, tức `OperationCanceledException` timeout per-device KHÔNG bị nhầm thành service cancellation. |
| Không còn "adb binary not found" cho case timeout | Xác nhận qua log — không xuất hiện. Fix log message (QA finding P2/P3 gốc) hoạt động. |
| USB device bị lọc (count=2 không phải 3) | `reconnecting 2 persisted WiFi device(s)...` — PASS. TC-4 regression clean. |
| Kestrel up TRƯỚC warm-up | `Now listening on:` xuất hiện GIỮA dòng "warm-up bắt đầu" và các dòng warn → startup không bị block. |
| App không crash sau warm-up | `Application started` + tắt sạch khi kill → PASS. |

**Kết luận về TC-5 evidence:** ĐỦ TIN CẬY. Log đầy đủ, không cắt xén, timestamp khớp với session re-verify. Hành vi trước/sau fix được mô tả rõ trong STEP-3.1b và xác nhận bởi log thật.

### 2. Đánh giá risk tổng thể

#### Risk A — Sequential warm-up (N device × 5s): CHẤP NHẬN, GHI NỢ KỸ THUẬT

Tech Lead đã note tại STEP-2.2 (optional, không blocking): nếu fleet có N WiFi device đều offline, startup warm-up chạy tuần tự, thời gian += N × 5s timeout.

Đánh giá:
- Kestrel bắt đầu lắng nghe TRƯỚC khi warm-up xong (log xác nhận) — không block HTTP server.
- Trong khoảng N×5s warm-up, API `GET /api/devices/{serial}/status` trả 404/stale. Đây là window chấp nhận được ở startup (service vừa restart, caller hợp lý không expect instant ready).
- Quy mô fleet hiện tại: không có thông tin cụ thể, nhưng worst case (10 device offline) = 50s background warm-up. Chấp nhận với workaround: caller retry sau vài giây.
- **Quyết định:** Không nâng thành điều kiện sign-off. Ghi vào tech-debt note cho Tech Lead xử lý riêng (parallel warm-up / configurable timeout).

#### Risk B — ENV_LIMIT (TC-1/TC-2 chưa test với Android device thật): CẦN ĐIỀU KIỆN

TC-1 (`GET /api/devices/{serial}/status` trả 200 sau restart với device thật) và TC-2 (`POST /api/devices/connect-by-ip` sau restart) chưa được verify E2E vì không có Android device thật trong môi trường test WSL2.

Đây là gap quan trọng:
- Warm-up logic đúng (TC-5 log xác nhận, 9/9 unit test PASS).
- Tuy nhiên, chưa confirm luồng đầy đủ: warm-up kết nối thật → `DeviceState` được populate → `GET /api/devices/{serial}/status` trả 200.
- Đây chính xác là tình huống NHÓM B của feature gốc `adb-add-device-api` — QA Lead lúc đó đã áp điều kiện smoke test thủ công với thiết bị thật trước go-live.

**Quyết định:** Áp điều kiện tương tự. DevOps Engineer phải thực hiện smoke test TC-1 và TC-2 với ít nhất 1 Android WiFi device thật trên staging TRƯỚC KHI coi deploy production là hoàn toàn an toàn.

### 3. Xác nhận không có P0/P1 bug nào còn mở

| Bug | Status | Evidence |
|---|---|---|
| Bug 1: warm-up không được gọi sau restart (commit 3a86825) | FIXED | TC-5 log: "DevicePollWorker warm-up: reconnecting 2 persisted WiFi device(s)..." — warm-up được gọi. |
| Bug 2: per-device timeout chặn device sau (commit cff893f) | FIXED | TC-5 log: cả 2 device đều có dòng warn riêng biệt. |
| Regression TC-3 (GET /api/devices) | PASS | 73/73 unit test PASS + app không crash. |
| Regression TC-4 (USB không vào warm-up) | PASS | count=2 (không phải 3), unit test confirm. |
| P0/P1 bug mới | KHÔNG PHÁT SINH | QA Engineer confirm. |

**Kết luận: Không còn P0/P1 bug nào chưa giải quyết (trong phạm vi môi trường test có sẵn).**

### 4. Quyết định Sign-off

**QUYẾT ĐỊNH: PASS CÓ ĐIỀU KIỆN**

**Phê duyệt deploy lên staging ngay.** DevOps Engineer được phép deploy fix (`cff893f`) lên staging environment.

**Điều kiện bắt buộc trước khi coi production deploy là hoàn toàn an toàn:**

> **ĐIỀU KIỆN:** DevOps Engineer phải thực hiện smoke test TC-1 và TC-2 với ít nhất 1 Android WiFi device thật trên staging:
> - **TC-1:** Restart service → KHÔNG scan UI → gọi `GET /api/devices/{serial}/status` → kỳ vọng 200 OK với status đúng (không phải 404).
> - **TC-2:** Restart service → KHÔNG scan UI → gọi `POST /api/devices/connect-by-ip` với IP device đang online → kỳ vọng 200 OK.
> - Nếu pass → ghi log bằng chứng (curl output), production deploy an toàn.
> - Nếu fail → BLOCK deploy, escalate lên Tech Lead ngay.
> - Nếu không có Android device thật trong cửa sổ deploy → DevOps Lead phải CHẤP NHẬN RỦI RO còn lại bằng văn bản trong step 3.3 trước khi deploy production.

**Tech-debt note (không blocking):** Sequential warm-up N×5s — Tech Lead cân nhắc chuyển sang parallel warm-up với giới hạn concurrency trong task riêng.

## Artifact

- Sign-off nhúng trong `docs/bugs/BUG-adb-reconnect-after-restart.md` (mục Sign-off ở cuối file)

## Quyết định quan trọng

1. **PASS CÓ ĐIỀU KIỆN** — không VETO, không FAIL. Cả 2 bug P1 đã được verify fix đúng bằng log app thật + unit test đủ mạnh.
2. **ENV_LIMIT TC-1/TC-2 không dừng sign-off** nhưng BẮT BUỘC smoke test với thiết bị thật trước production final.
3. **Sequential warm-up** là performance concern, không phải correctness bug — ghi tech-debt, không blocking deploy.
4. **QA Lead tự đọc evidence** (không tin self-report của QA Engineer) — log STEP-3.1b Bước 3 đủ tin cậy, không cần yêu cầu thêm.

## Handoff Payload — bước sau đọc phần này (chỉ phần này, không cần đọc "Đã làm")

- do_not_redo: QA Lead đã verify toàn bộ evidence. Không cần re-run test hay đọc lại step file cũ hơn 3.1b.
- watch_out: (1) ĐIỀU KIỆN BẮT BUỘC trước production final: smoke test TC-1 + TC-2 với ≥1 Android WiFi device thật trên staging. Nếu không có thiết bị → DevOps Lead phải chấp nhận rủi ro bằng văn bản trong step 3.3. (2) Deployment target: fix thuần backend, không cần migrate DB, không cần đổi appsettings — chỉ build lại Docker image và restart container. (3) Warm-up xảy ra trong background khi startup, Kestrel sẵn sàng nhận request song song — không cần wait-for-health-check đặc biệt.
- next_inputs: Commit `cff893f` là commit cần deploy (include cả fix 3a86825). BUG report `docs/bugs/BUG-adb-reconnect-after-restart.md` (đã có sign-off mục cuối) là source of truth. DevOps Engineer cần: (a) build lại Docker image từ `cff893f`, (b) deploy lên staging, (c) thực hiện smoke test TC-1/TC-2 với thiết bị thật (hoặc ghi nhận rủi ro nếu không có thiết bị), (d) ghi log bằng chứng vào STEP-3.3.

## Commit

- Hash: [điền sau khi commit]
- Đã push: [có/không]

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
