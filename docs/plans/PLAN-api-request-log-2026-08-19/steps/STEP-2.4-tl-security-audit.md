---
step: "2.4"
plan: ../PLAN-MASTER.md
agent: tech-lead
status: skipped
completed_at: 2026-08-19 13:55
deps: ["2.3"]
---

# STEP 2.4 — Tech Lead: Security Audit (Conditional)

## Input nhận
- Code đã merge từ STEP-2.3
- `docs/tech-design/TDD-api-request-log.md` — xem mục schema, data logged
- Handoff Payload từ 2.3: commit hash sau merge, bất kỳ deviation đã accept

## Nhiệm vụ
Tech Lead tự quyết định có cần chạy `security-audit-stride` không. Điều kiện theo CLAUDE.md §4 WF-FEATURE Bước 10a: "nếu đụng auth/payment/DB schema/dữ liệu nhạy cảm". Feature này có DB schema change (bảng `ApiRequestLog` mới) nhưng dữ liệu ghi vào KHÔNG nhạy cảm (serial số thiết bị, package name, timestamp, caller IP). Nếu quyết định KHÔNG chạy STRIDE, ghi rõ lý do. Nếu quyết định chạy STRIDE, ghi kết quả — BLOCK merge nếu có Fail nhóm rủi ro cao.

## Definition of Done
- [ ] Quyết định rõ ràng: CÓ hoặc KHÔNG chạy security-audit-stride, kèm lý do cụ thể
- [ ] Nếu KHÔNG chạy: lý do được ghi trong step file này (VD: "Dữ liệu log không nhạy cảm, không có auth/payment, schema change nhỏ không ảnh hưởng security posture")
- [ ] Nếu CÓ chạy: kết quả STRIDE được ghi (không có Fail nhóm rủi ro cao mới chuyển phase 3)
- [ ] Nếu chạy STRIDE phát hiện Fail → BLOCK, ghi rõ điểm fail, escalate lên Engineering Manager trước khi tiếp tục

## Đã làm

Đánh giá điều kiện chạy `security-audit-stride` theo CLAUDE.md §4 WF-FEATURE Bước 10a ("auth/payment/DB schema/dữ liệu nhạy cảm"):

| Trục điều kiện | Đánh giá thực tế feature api-request-log | Cần STRIDE? |
|---|---|---|
| **Auth** | KHÔNG đụng `ApiKeyEndpointFilter` (verified `git show 79ae3cf -- ApiKeyEndpointFilter.cs` — 0 dòng đổi). Filter mới đặt OUTER, không thay đổi cơ chế xác thực. | Không |
| **Payment** | Không liên quan. | Không |
| **DB schema mới** | Có (bảng `ApiRequestLog`) — NHƯNG chỉ INSERT, không có query đọc dựa trên input người dùng, không có JOIN. Query dùng parameterized `AddWithValue("$param", value)` → SQL-injection safe. Schema idempotent (`CREATE TABLE IF NOT EXISTS`) không phá dữ liệu cũ. | Cân nhắc — nhưng risk thấp |
| **Dữ liệu nhạy cảm** | Kiểm tra từng field: `serial` (đã hiển thị công khai trên dashboard), `package name` (public), `timestamp` (kỹ thuật), `callerIp` (LAN IP nội bộ), `parameters` = JSON body 2 API cụ thể (`{ip, port}` và `{serial, app}` — KHÔNG chứa credential/token/PII). `x-api-key` nằm ở HEADER và filter KHÔNG log header → không leak. | Không |

**Rủi ro bổ sung kiểm tra thủ công (không cần STRIDE formal):**

1. **Log injection qua body:** Attacker gửi body chứa `<script>alert(1)</script>` hoặc control chars → SQLite store as text OK; hiển thị qua JS `appendLog()` dùng `textContent` (verified line 80 dashboard.js) → không XSS.
2. **DoS qua body lớn:** Kestrel `MaxRequestBodySize=500MB` mặc định + filter dùng `ReadToEndAsync` không cap — có attack surface. NHƯNG `ApiKeyEndpointFilter` gate INNER **KHÔNG** chặn được ở đây vì filter logging đặt OUTER và đọc body TRƯỚC khi vào auth chain. **Đây là điểm phát hiện**: kẻ tấn công không có api-key vẫn có thể gửi body lớn 500MB, filter đọc vào memory rồi mới trả 401. Mức độ: **rủi ro trung bình** cho môi trường public; **thấp** cho triển khai LAN Docker hiện tại (không public internet, ACL mạng đủ). Ghi backlog để refactor cap 8KB ở filter khi go public.
3. **Log flooding:** Attacker gửi request 401 spam → mỗi request tạo 1 DB row → SQLite phình. Non-goal PRD (không retention/purge). Chấp nhận cho P2.
4. **Timing attack qua durationMs:** Không nhạy cảm — không có logic bí mật (comparison string) đo được.

**Quyết định căn cứ:**
- 3/4 trục điều kiện KHÔNG match; trục DB schema match nhưng risk thấp (parameterized query + không sensitive data).
- Rủi ro DoS body lớn có phát hiện nhưng thuộc trách nhiệm hạ tầng (LAN-only deploy), ghi note để refactor sau — KHÔNG cần STRIDE formal.

## Artifact

- Bảng đánh giá 4 trục ở trên (nhúng trực tiếp step file này).
- Không có file mới; không tạo `docs/security/` report vì SKIP STRIDE.
- Note bổ sung ghi vào Handoff Payload cho UXR/QAE/DOE.

## Quyết định quan trọng

**KHÔNG chạy `security-audit-stride`.** Lý do:
1. Không đụng cơ chế auth (`ApiKeyEndpointFilter` untouched).
2. Không đụng payment.
3. DB schema mới chỉ INSERT với parameterized query — SQL-injection safe.
4. Dữ liệu log tất cả là non-sensitive (serial công khai, package public, IP nội bộ LAN, không log header/credential).
5. XSS-safe qua `textContent` ở JS.
6. Rủi ro DoS body lớn được xác định nhưng thuộc hạ tầng mạng (LAN-only Docker deploy), ghi backlog thay vì STRIDE full.

Status: **⏭️ Skipped có lý do rõ.**

## Handoff Payload — bước sau đọc phần này

- Đã làm: Đánh giá 4 trục STRIDE-trigger; kết luận KHÔNG cần STRIDE formal. Phát hiện 1 rủi ro backlog: filter đọc body TRƯỚC auth → không cap size (LAN-only, chấp nhận cho P2).
- do_not_redo: Không rechạy đánh giá STRIDE lại; không tạo file security report.
- watch_out: (a) Khi feature này được đề xuất go public internet, PHẢI thêm size cap (VD 8KB) ở `TryReadBodyJsonAsync` TRƯỚC ReadToEnd — hiện tại chấp nhận vì LAN-only. (b) Nếu QAE (3.2) muốn test body lớn (>1KB), lưu ý Parameters bị truncate 1024 chars ở service (không phải filter) — DB row đủ để verify truncate; SignalR broadcast payload cũng ≤1KB.
- next_inputs: (i) SKIP STRIDE — không có security constraint mới cho UXR (3.1) hoặc QAE (3.2); (ii) sang phase 3 bình thường (UXR + QAE + QAL + Deploy).

## Commit
- Hash: [điền sau khi commit]
- Đã push: không (theo yêu cầu task)

---
**Status icons:** ⬜ Todo | 🔄 In Progress | ✅ Done | 🛑 Blocked | ⏭️ Skipped
