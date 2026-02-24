## 1. Quy định Chung (General)

### Git & Branching

- **Main branch**: `main` (Production), `develop` (Staging/Development).
- **Branch naming**:
  - `feat/feature-name`: Tính năng mới.
  - `fix/bug-name`: Sửa lỗi.
  - `refactor/name`: Tối ưu code.
- **Conventional Commits**:
  - `feat: ...`, `fix: ...`, `docs: ...`, `style: ...`, `refactor: ...`, `chore: ...`.

### General Naming

- Tên biến/hàm phải có ý nghĩa (Self-documenting). Tránh đặt `a`, `b`, `c`.

---

## 2. Tiêu chuẩn Backend (C# / .NET)

### Kiến trúc (Clean Architecture)

- **Domain**: Entities, Interfaces repository (No dependencies).
- **Application**: Logic nghiệp vụ, Services, DTOs, Validators.
- **Infrastructure**: Implement repositories, DbContext, Third-party services.
- **Presentation (API)**: Controllers, Middlewares.

### Result & Exception Pattern

- **Result Pattern**: Sử dụng `Result<T>` để trả về từ Service. Không dùng exception cho lỗi logic (ví dụ: `UserNotFound`).
- **Exception**: Chỉ dùng cho lỗi bất khả kháng (DB sập, lỗi code). Bắt tập trung tại Middleware.

### Giao dịch & Dữ liệu (Transaction & EF Core)

- **Transaction**: Mọi hoạt động ghi (Insert/Update/Delete) liên quan nhiều hơn 1 bảng phải nằm trong Transaction.
- **Unit of Work**: Chỉ gọi `SaveChangesAsync()` một lần duy nhất tại cuối Service/Action.
- **EF Core**:
  - Ưu tiên `.AsNoTracking()` cho truy vấn Read-only.
  - Tách Configuration ra file riêng (`IEntityTypeConfiguration`).

---

## 3. Tiêu chuẩn Frontend (Next.js / TypeScript)

### Naming Conventions

- **Components**: PascalCase (ví dụ: `ButtonSubmit.tsx`).
- **Hooks**: Bắt đầu bằng `use` (ví dụ: `useAuth.ts`).
- **Folders/Files**: kebab-case (ví dụ: `user-profile/`).
- **Variables/Methods**: camelCase.

### Cấu trúc Folder (App Router)

- `src/app`: Định nghĩa routes.
- `src/components`: Các shared components (UI, Common).
- `src/hooks`: Custom hooks dùng chung.
- `src/lib`: Cấu hình thư viện (Axios, Auth).
- `src/services`: Gọi API (mapping với BE).
- `src/types`: Định nghĩa interfaces/types cho TypeScript.

### Nguyên tắc Component

- **Atomic Design**: Cố gắng tách nhỏ component để tái sử dụng.
- **Props**: Luôn định nghĩa Type cho Props. Trành dùng `any`.
- **Client vs Server Component**:
  - Tận dụng Server Component cho SEO và Load dữ liệu.
  - Chỉ dùng Client Component khi cần tương tác (onClick, useState, useEffect).

---

## 4. Giao tiếp & Bảo mật (API & Security)

### RESTful API

- URL: Số nhiều, chữ thường (`/api/products`).
- Method: `GET` (Read), `POST` (Create), `PUT` (Update All), `PATCH` (Partial Update), `DELETE`.

### Security

- **Authentication**: JWT Bearer lưu tại HttpOnly Cookie hoặc State (tùy yêu cầu bảo mật).
- **Validation**:
  - BE: Kiểm tra bằng FluentValidation.
  - FE: Kiểm tra bằng Zod/Yup trước khi gửi request.
- **Secrets**: Không push `.env` hoặc `appsettings.json` chứa Key lên Git.

---

## 5. Quy trình Review & Testing

- **Pull Request (PR)**: Mỗi PR phải được ít nhất 1 thành viên khác review trước khi merge.
- **Unit Test**: Viết test cho các Logic nghiệp vụ phức tạp ở Service (BE) và Hooks quan trọng (FE).

---

## 6. Tiêu chuẩn Cơ sở dữ liệu (Database)

### 6.1. Quy tắc đặt tên (Naming Convention)

| Loại bảng                      | Quy tắc                    | Ví dụ                                  |
| ------------------------------ | -------------------------- | -------------------------------------- |
| Bảng chính                     | `snake_case`, **số nhiều** | `employees`, `orders`, `products`      |
| Bảng trung gian (many-to-many) | `entity1_entity2`          | `user_roles`, `order_products`         |
| Bảng log / history             | hậu tố `_logs`, `_history` | `payment_logs`, `order_status_history` |

**Column Naming:**

- **General**: `snake_case` (ví dụ: `full_name`, `created_at`).
- **Primary Key**: `table_singular_id` (ví dụ: `employee_id`, `order_id`).
- **Foreign Key**: `referenced_table_singular_id` (ví dụ: `role_id`).
- **Boolean**: Bắt đầu bằng `is_`, `has_`, `can_` (ví dụ: `is_active`).
- **Enum**: Lưu dạng `int` và map trong code.

### 6.2. Primary & Foreign Keys

- **Primary Key**: LUÔN sử dụng kiểu `UUID`, tên cột `table_singular_id`, sinh giá trị ở Backend.
- **Foreign Key**: Bắt buộc có constraint, đặt tên theo quy tắc `fk_<table>_<column>`.

### 6.3. Audit Fields & Soft Delete (BẮT BUỘC)

Mọi bảng nghiệp vụ chính phải bao gồm các trường:

- `created_at` (timestamp), `created_by` (uuid)
- `updated_at` (timestamp), `updated_by` (uuid)
- `deleted_at` (timestamp NULL) -> Hệ thống sử dụng **Soft Delete**.

### 6.4. Tiêu chuẩn dữ liệu & Index

- **Data Types**: ID dùng `UUID`, Text ngắn dùng `varchar(255)`, Tiền dùng `numeric(12,2)`, Ngày giờ dùng `timestamp`.
- **Index**: LUÔN tạo index cho Foreign Key. Sử dụng `UNIQUE INDEX` cho các trường cần duy nhất (email, code).

### 6.5. Nguyên tắc Migration & Security

- Mọi thay đổi schema phải qua Migration, không chỉnh sửa trực tiếp DB.
- Không chỉnh sửa Migration đã chạy trên Production.
- **Security**: Không lưu mật khẩu/token dạng plain text, không log dữ liệu nhạy cảm.
- **Hiệu năng**: Tránh `SELECT *`, chỉ lấy các cột cần thiết.

---

## 7. Nguyên tắc Comment Code (Code Commenting)

Mục tiêu chính: Code nên tự giải thích (Self-documenting). Comment chỉ dùng để giải thích **TẠI SAO (WHY)**, không phải **CÁI GÌ (WHAT)**.

### 7.1. Ngôn ngữ (Language)

- **Mặc định**: Ưu tiên sử dụng **Tiếng Anh** cho code comments (biến, hàm, class) để đảm bảo tính chuyên nghiệp và khả năng hội nhập.
- **Ngoại lệ**: Có thể sử dụng **Tiếng Việt** (có dấu) cho các logic nghiệp vụ cực kỳ phức tạp hoặc đặc thù dự án mà tiếng Anh khó diễn đạt hết ý nghĩa cho team.

### 7.2. Documentation Comments (Mô tả API/Function)

Bắt buộc đối với các **Public Class, Interface, Method, API Endpoint**.

- **Backend (C#)**: Sử dụng XML Documentation (`/// <summary>`).
  ```csharp
  /// <summary>
  /// Tính toán điểm trung bình của sinh viên dựa trên trọng số tín chỉ.
  /// </summary>
  /// <param name="studentId">ID của sinh viên cần tính.</param>
  /// <returns>Kết quả điểm trung bình (thang 10).</returns>
  /// <exception cref="StudentNotFoundException">Ném lỗi nếu không tìm thấy sinh viên.</exception>
  public async Task<double> CalculateGPA(Guid studentId);
  ```

- **Frontend (JS/TS)**: Sử dụng JSDoc (`/** ... */`).
  ```typescript
  /**
   * Chuyển đổi định dạng ngày tháng sang chuỗi hiển thị VN (dd/MM/yyyy).
   * @param {Date} date - Đối tượng Date cần format.
   * @returns {string} Chuỗi ngày tháng dạng "dd/MM/yyyy".
   */
  export const formatDateVN = (date: Date): string => { ... }
  ```

### 7.3. Inline Comments (Comment trong dòng)

- Chỉ dùng khi logic quá phức tạp, thuật toán khó hiểu, hoặc sử dụng "magic number/string".
- Đặt comment **trên** dòng code cần giải thích, không đặt cùng dòng (trừ khi biến ngắn/định nghĩa biến).
- **Tránh**: Comment giải thích code hiển nhiên (VD: `i++ // Tăng i` -> Lãng phí).
  
  **Ví dụ Tốt:**
  ```csharp
  // Tăng timeout lên 5s do service bên thứ 3 phản hồi chậm vào giờ cao điểm
  httpClient.Timeout = TimeSpan.FromSeconds(5);
  ```

### 7.4. TODO & FIXME

Sử dụng các từ khóa chuẩn để đánh dấu các việc cần làm hoặc nợ kỹ thuật. IDE sẽ highlight các từ khóa này.

- `// TODO: [Người phụ trách] Nội dung`: Tính năng chưa làm hoặc cần cải thiện sau.
- `// FIXME: [Người phụ trách] Nội dung`: Code đang lỗi hoặc chạy chưa đúng, cần sửa gấp.
- `// NOTE: Nội dung`: Lưu ý quan trọng về đoạn code này (ví dụ: tại sao phải hack/workaround).

### 7.5. Code đã cũ (Dead Code)

- **KHÔNG** comment lại code cũ "để dành". Hãy xóa thẳng tay.
- Code cũ đã có Git lưu lại lịch sử, không cần giữ lại trong source file làm rác code và gây rối mắt.

