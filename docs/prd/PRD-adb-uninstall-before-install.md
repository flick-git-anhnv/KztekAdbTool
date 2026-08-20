# PRD-adb-uninstall-before-install: Tùy chọn Gỡ cài đặt App trước khi Cài đặt

## Tổng quan
- Vấn đề: Khi cài đặt APK lên thiết bị Android qua ADB, nếu phiên bản cũ trên thiết bị không tương thích (certificate khác, shared UID thay đổi...), lệnh `adb install -r` sẽ thất bại. Người vận hành phải gỡ thủ công rồi cài lại, gây gián đoạn quy trình deploy hàng loạt.
- Đối tượng người dùng: Nhân viên vận hành dùng Dashboard KztekAdbPublishTool để deploy APK lên nhiều thiết bị Android cùng lúc.
- Giá trị mang lại: Giảm thao tác thủ công; cho phép deploy sạch (clean install) khi cần, đồng thời giữ hành vi mặc định an toàn (`adb install -r`) để không ảnh hưởng dữ liệu thiết bị trong trường hợp bình thường.

## Mục tiêu (Goals)
- G1: Thêm 1 tùy chọn (checkbox) trên Dashboard cho phép người dùng chọn có chạy `adb uninstall <package>` TRƯỚC `adb install` hay không.
- G2: Hành vi mặc định KHÔNG thay đổi — checkbox tắt mặc định, vẫn dùng `adb install -r` như cũ.
- G3: Khi bật tùy chọn, nếu app chưa cài trên thiết bị, hệ thống log cảnh báo và tiếp tục install — không abort toàn bộ luồng.
- G4: Trạng thái tiến trình trên Dashboard cập nhật thêm bước "Đang gỡ cài đặt..." khi tùy chọn được bật, giúp người dùng theo dõi đúng luồng thực tế.

## Non-goals
- Không hỗ trợ gỡ cài đặt mà không cài đặt lại (feature này chỉ là bước tiền xử lý cho install).
- Không thay đổi logic xử lý lỗi install hiện tại — chỉ thêm bước uninstall trước.
- Không hỗ trợ gỡ theo danh sách package tuỳ chỉnh — package name lấy từ APK đang deploy.
- Không lưu lịch sử trạng thái uninstall vào log riêng — chỉ ghi vào log tiến trình chung hiện tại.
- Không áp dụng cho luồng auto-detect (bước auto-detect vẫn hoạt động độc lập).

## User Story (sơ lược — BA sẽ chi tiết hóa)
Là nhân viên vận hành, tôi muốn bật tùy chọn "Gỡ cài đặt trước khi cài" trước khi nhấn Install để hệ thống tự động gỡ phiên bản cũ khỏi mỗi thiết bị trước khi cài phiên bản mới, giúp tôi tránh lỗi cài đặt do xung đột certificate hoặc UID mà không cần thao tác thủ công từng máy.

## Acceptance Criteria (mức cao)
- [ ] AC1: Trên Dashboard, hiển thị checkbox "Gỡ cài đặt trước khi cài" (mặc định tắt) theo đúng pattern `form-check form-switch` như các toggle hiện có.
- [ ] AC2: Khi checkbox bật và người dùng nhấn Install, server chạy `adb uninstall <package>` TRƯỚC `adb install` cho mỗi thiết bị được chọn; tiến trình hiển thị thêm bước "Đang gỡ cài đặt...".
- [ ] AC3: Khi checkbox bật nhưng app chưa cài trên thiết bị, hệ thống ghi log cảnh báo (không phải lỗi nghiêm trọng) và tiếp tục chạy `adb install` bình thường — không dừng toàn bộ luồng install.
- [ ] AC4: Khi checkbox tắt, hành vi hoàn toàn giống hành vi hiện tại (chỉ `adb install -r`), không có tác dụng phụ nào.
- [ ] AC5: Trạng thái checkbox (bật/tắt) được lưu lại qua DB Settings để giữ nguyên giá trị sau khi tải lại trang (nhất quán với cách lưu PackageName/ApkPath).

## Metric đo lường thành công
- Tỉ lệ lỗi install do xung đột certificate/UID giảm về 0 trong các phiên deploy có bật tùy chọn.
- Không có sự cố nào trên thiết bị do hành vi mặc định (checkbox tắt) bị thay đổi sau khi release.
- Phản hồi từ nhân viên vận hành: tùy chọn dễ hiểu, không gây nhầm lẫn (đánh giá qua UX Review sau khi deploy staging).

## Rủi ro / Câu hỏi mở
- R1: Nếu `adb uninstall` thất bại vì lý do khác ngoài "package chưa cài" (VD: thiết bị bị lock, MDM chặn), cần Tech Lead xác định rõ trong TDD cách phân biệt lỗi "package not found" vs lỗi thực sự để quyết định có abort hay không.
- R2: Thời gian tổng của luồng install sẽ tăng thêm thời gian uninstall (~1–3 giây/thiết bị) — cần thông báo rõ cho người dùng qua tiến trình SignalR, tránh nhầm tưởng hệ thống bị treo.
- Q1: Vị trí chính xác của checkbox trên toolbar (trước hay sau checkbox auto-detect?) — Tech Lead quyết định trong TDD.
