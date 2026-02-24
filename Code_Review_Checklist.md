# Quy Trình & Checklist Review Code (Code Review Guidelines)

Tài liệu này hướng dẫn chi tiết các bước cần thực hiện trước khi merge code vào nhánh `main` (Production) hoặc `develop` (Staging). Mục tiêu là đảm bảo chất lượng code, tính ổn định và tuân thủ kiến trúc hệ thống.

---

## PHẦN 1: SELF-REVIEW CHECKLIST (DÀNH CHO NGƯỜI LÀM TASK)

**Trước khi tạo Pull Request (PR), lập trình viên (Author) BẮT BUỘC phải tự kiểm tra các mục sau:**

### 1. Build & Run
- [ ] Code build thành công (không có lỗi biên dịch).
- [ ] project chạy lên bình thường, không crash khi khởi động.
- [ ] Đã chạy thử các lệnh clean/rebuild để đảm bảo không dính cache cũ.

### 2. Kiến Trúc & Logic (Architecture)
- [ ] Logic nghiệp vụ (Business Logic) được đặt trong tầng **Application**, KHÔNG viết trong Controller.
- [ ] Controller chỉ đóng vai trò điều hướng, gọi MediatR và trả về kết quả.
- [ ] Sử dụng đúng **Result Pattern** (`Result<T>`) thay vì throw Exception cho lỗi logic (ví dụ: `UserNotFound` trả về `Result.NotFound()` thay vì throw).
- [ ] Sử dụng **DTO** cho Input/Output, KHÔNG trả về Entity của Domain trực tiếp ra API.
- [ ] Đã áp dụng **Dependency Injection (DI)** đúng cách (Inject Interface, không new Class).

### 3. Cơ Sở Dữ Liệu (Database & EF Core)
- [ ] Nếu có sửa đổi DB Schema, đã tạo file **Migration** (`dotnet ef migrations add ...`).
- [ ] Không sử dụng câu lệnh `Select *` (hoặc load thừa dữ liệu không cần thiết).
- [ ] Đã dùng `.AsNoTracking()` cho các truy vấn chỉ đọc (Query).
- [ ] Kiểm tra **N+1 Query**: Dùng `.Include()` để load dữ liệu liên quan thay vì lazy loading trong vòng lặp.
- [ ] Tên bảng và tên cột tuân thủ chuẩn `snake_case` (trong DB) và map đúng với Entity.

### 4. Quy tắc Code & Comment
- [ ] Tên biến, hàm, class đặt tên tiếng Anh, rõ nghĩa (PascalCase cho Class/Method, camelCase cho biến).
- [ ] Đã xóa hết các code thừa (**Dead Code**), các `Console.WriteLine` hoặc comment tạm thời.
- [ ] Đã viết **Documentation Comments** (`/// summary`) cho các API/Method public quan trọng.
- [ ] Đã format code (Ctrl+K, D) để đảm bảo chuẩn indentation.

### 5. Git & Commit
- [ ] Tên nhánh đúng chuẩn: `feat/...`, `fix/...`, `refactor/...`.
- [ ] Commit message rõ ràng, tuân thủ Conventional Commits (VD: `feat: add student list api`).
- [ ] Đã `git pull origin main` (hoặc rebase) để xử lý conflict trước khi push.

---

## PHẦN 2: CODE REVIEWER CHECKLIST (DÀNH CHO NGƯỜI REVIEW)

**Người review có trách nhiệm kiểm tra kỹ các tiêu chí sau trước khi Approve và Merge:**

### 1. Tính Đúng Đắn (Correctness)
- [ ] Code có thực hiện đúng yêu cầu của task/ticket không?
- [ ] Logic có xử lý các trường hợp biên (Edge cases) chưa? (Ví dụ: danh sách rỗng, null, số âm...).
- [ ] Có lỗi tiềm ẩn nào gây crash hệ thống không? (Ví dụ: chưa check null cho biến trước khi dùng).

### 2. Kiến Trúc & Design Pattern
- [ ] Code có tuân thủ **Clean Architecture** không? (Domain không phụ thuộc Application, Application không phụ thuộc Web...).
- [ ] Có vi phạm nguyên tắc **SOLID** không? (Ví dụ: 1 hàm làm quá nhiều việc).
- [ ] Có trùng lặp code (DRY violation) không? Nếu có, yêu cầu tách thành hàm chung/Ultilities.
- [ ] CQRS: Command (Ghi) và Query (Đọc) có được tách biệt hợp lý không?

### 3. Hiệu Năng & Bảo Mật (Performance & Security)
- [ ] **Performance**: Có vòng lặp nào gọi DB liên tục không? Có query nào lấy quá nhiều dữ liệu không?
- [ ] **Security**: 
    - Có check quyền (Authorization) cho API chưa?
    - Dữ liệu input có được validate (FluentValidation) chưa?
    - Có lộ thông tin nhạy cảm (password, key) không?
- [ ] **Resource**: Connection, Stream có được giải phóng (using statement) không?

### 4. Code Style & Readability
- [ ] Code có dễ đọc, dễ hiểu không? 
- [ ] Tên biến có gây nhầm lẫn không?
- [ ] Các `Magic Number` (số cứng 0, 1, 100...) có được đưa ra hằng số (`const`) không?
- [ ] Comment có giải thích đúng "Tại sao" thay vì "Cái gì" không?

### 5. Migration Review (Nếu có DB change)
- [ ] Kiểm tra file Migration `Up()` và `Down()`:
    - Có tạo bảng/cột thừa không?
    - Có đặt Index cho Foreign Key hoặc trường cần search không?
    - Có nguy cơ mất dữ liệu khi chạy migration trên production không? (VD: đổi kiểu dữ liệu, xóa cột đang có data).

---

## PHẦN 3: ACTIONS SAU KHI REVIEW

1.  **Request Changes**: Nếu phát hiện lỗi Logic, lỗi Bảo mật, hoặc vi phạm Kiến trúc nghiêm trọng.
2.  **Comment**: Nếu code chạy được nhưng cần tối ưu, hoặc chưa đúng Convention (đặt tên, format).
3.  **Approve**: Khi code đạt đủ các tiêu chí trên.

> **Lưu ý**: Hãy review với thái độ xây dựng (Constructive Feedback). Tập trung vào code, không công kích cá nhân.
