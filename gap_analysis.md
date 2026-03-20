# Gap Analysis: PRD vs Code — Quản lý Sinh Viên Thực Tập của HR

## Tóm tắt

| Phân loại | Số lượng |
|-----------|----------|
| ✅ PRD + Code đã khớp (implement đầy đủ) | 9 |
| ⚠️ Code có nhưng PRD chưa mô tả / mô tả lệch | 5 |
| ❌ PRD yêu cầu nhưng Code **chưa implement** | 10 |

---

## Phần A — Những gì đã khớp giữa PRD và Code ✅

| AC | Mô tả | Ghi chú |
|----|-------|---------|
| AC-G01 | GetInternshipGroups (danh sách nhóm, phân trang, filter theo Status/EnterpriseId) | [GetInternshipGroupsHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Queries/GetInternshipGroups/GetInternshipGroupsHandler.cs#15-20) + [GetInternshipGroupsQuery](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Queries/GetInternshipGroups/GetInternshipGroupsQuery.cs#10-19) |
| AC-G02 | CreateInternshipGroup (tạo nhóm, validate SV đã Approved, gắn Mentor) | [CreateInternshipGroupHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Commands/CreateInternshipGroup/CreateInternshipGroupHandler.cs#21-34) |
| AC-G03 | GetInternshipGroupById (xem chi tiết nhóm, include Members/Mentor/Enterprise) | [GetInternshipGroupByIdHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Queries/GetInternshipGroupById/GetInternshipGroupByIdHandler.cs#20-27) |
| AC-G04 | UpdateInternshipGroup (cập nhật tên, kỳ, mentor, ngày) | [UpdateInternshipGroupHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Commands/UpdateInternshipGroup/UpdateInternshipGroupHandler.cs#12-115) |
| AC-G05 | AddStudentsToGroup (validate Approved, race-condition check, thêm SV vào nhóm) | [AddStudentsToGroupHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Commands/AddStudentsToGroup/AddStudentsToGroupHandler.cs#21-34) |
| AC-G06 | RemoveStudentsFromGroup (xóa SV khỏi nhóm, giữ nguyên trạng thái Placed) | [RemoveStudentsFromGroupHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Commands/RemoveStudentsFromGroup/RemoveStudentsFromGroupHandler.cs#19-30) |
| AC-G09 | DeleteInternshipGroup — chặn khi còn SV (Members.Any()) | [DeleteInternshipGroupHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Commands/DeleteInternshipGroup/DeleteInternshipGroupHandler.cs#19-30) |
| — | Accept/Reject Application (Pending → Approved/Rejected) | `AcceptApplicationCommandHandler`, `RejectApplicationCommandHandler` |
| — | GetEnterpriseApplications + GetApplicationDetail | `GetEnterpriseApplicationsQueryHandler`, `GetApplicationDetailQueryHandler` |

---

## Phần B — Code có nhưng PRD chưa mô tả / mô tả lệch ⚠️

### B1. `InternshipStatus` enum lệch hoàn toàn với PRD

**Code hiện tại** ([InternshipStatus.cs](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Domain/Enums/InternshipStatus.cs)):
```csharp
Registered = 1, Onboarded = 2, InProgress = 3, Completed = 4, Failed = 5
```

**PRD yêu cầu** (Group Status Definition — Tab Nhóm thực tập):

| Status | Trigger |
|--------|---------|
| `Active` | HR tạo nhóm |
| `Finished` | Term → Ended (tự động) |
| `Archived` | Term → Closed (tự động) hoặc HR archive thủ công |

> [!WARNING]
> Enum `InternshipStatus` hiện tại KHÔNG CÓ `Active`, `Finished`, `Archived`. Toàn bộ logic Group Status trong PRD chưa được phản ánh. Khi `InternshipGroup.Create()` được gọi, nó tạo nhóm với `Status = Registered` — sai với PRD yêu cầu tạo với `Active`.

### B2. `InternshipApplicationStatus.Approved` vs "Placed"

- PRD dùng thuật ngữ **"Placed"** để chỉ SV đã được HR accept.
- Code dùng `InternshipApplicationStatus.Approved`.
- Đây là khác nhau về **terminology** nhưng logic hiểu được. Tuy nhiên PRD cần được update để dùng cùng thuật ngữ với code, hoặc code rename `Approved → Placed`.

### B3. [InternshipGroup](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Domain/Entities/InternshipGroup.cs#31-32) không có field `Description`

- **AC-G02, AC-G04** cho phép HR nhập/sửa **Mô tả** nhóm.
- Entity [InternshipGroup](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Domain/Entities/InternshipGroup.cs#31-32) **không có** property `Description`.
- [Create()](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Domain/Entities/InternshipGroup.cs#33-53) và [UpdateInfo()](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Domain/Entities/InternshipGroup.cs#54-69) cũng không nhận `description`.

### B4. [GetInternshipGroupsQuery](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Queries/GetInternshipGroups/GetInternshipGroupsQuery.cs#10-19) thiếu filter theo TermId và IncludeArchived

- PRD **AC-G01**: filter theo **Tháng/Năm**, **Trạng thái** (bao gồm toggle *Include Archived*).
- [GetInternshipGroupsQuery](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Queries/GetInternshipGroups/GetInternshipGroupsQuery.cs#10-19) hiện có filter: [Status](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Domain/Entities/InternshipGroup.cs#92-96), `UniversityId`, `EnterpriseId`, `SearchTerm`.
- **Lưu ý quan trọng (Đã kiểm tra Entity)**: Code database (`InternshipGroup.cs`) hoàn toàn **không có `UniversityId`**, nghĩa là cấu trúc DB **đã hỗ trợ sinh viên đa trường** trong cùng 1 nhóm (đáp ứng đúng PRD mới).
- **Thiếu**: Gỡ bỏ tham số `UniversityId` trong query liên quan đến nhóm. Thêm filter theo `TermId` / tháng-năm, và flag `IncludeArchived` (vì Archived ẩn mặc định).

### B5. [DeleteInternshipGroup](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.API/Controllers/InternshipGroups/InternshipGroupsController.cs#105-121) thiếu check "0 SV nhưng còn data"

- **AC-G09**: quy trình 3 bước — (1) còn SV → block; (2) 0 SV nhưng còn data (logbook/project/evaluation) → block, gợi ý Archive; (3) hoàn toàn trống → confirm delete.
- Code chỉ có check bước (1): `if (entity.Members.Any())`. **Thiếu check bước (2)**.

---

## Phần C — PRD yêu cầu nhưng Code chưa implement ❌

### C1. **[THIẾU FEATURE]** Tab Sinh viên — Xem danh sách SV đang Placed (AC-S01 → AC-S07)

**Toàn bộ "Tab Sinh viên" chưa có endpoint nào:**

| AC | Mô tả | Status |
|----|-------|--------|
| AC-S01 | Xem danh sách SV Placed tại enterprise trong một term | ❌ Chưa có |
| AC-S02 | Tìm kiếm SV (họ tên, MSSV, email, trường) | ❌ Chưa có |
| AC-S03 | Lọc theo tháng/năm, trạng thái Placed, nhóm, mentor | ❌ Chưa có |
| AC-S04 | Sắp xếp theo họ tên / ngày Placed / trạng thái | ❌ Chưa có |
| AC-S05 | Xem chi tiết SV (kèm lịch sử thay đổi nhóm/Mentor) | ❌ Chưa có |
| AC-S06 | Phân trang (10/20/50/100 per page) | ❌ Chưa có |
| AC-S07 | Đổi nhóm cho SV (move SV từ nhóm này sang nhóm khác) | ❌ Chưa có |

> [!IMPORTANT]
> Cần tạo feature mới: `Features/Enterprises/Queries/GetPlacedStudents/` hoặc `Features/Students/Queries/GetPlacedStudentsForEnterprise/`

### C2. **[THIẾU FEATURE]** Archive nhóm thủ công (AC-G07)

- `PATCH /internship-groups/{id}/archive` — chưa có command/handler/endpoint.
- Logic: chỉ archive khi nhóm có 0 SV, status chuyển sang `Archived`.

### C3. **[IGNORED]** Tự động chuyển Group Status khi Term thay đổi (AC-G08)

- Khi Term → **Ended**: tất cả nhóm trong term đó → read-only (hoặc status `Finished`).
- Khi Term → **Closed**: tất cả nhóm → `Archived`.
- **Quyết định (Theo User):** Tạm bỏ qua (Out of MVP scope) vì user chọn workflow đơn giản: HR chỉ tạo và quản lý nhóm khi Term đang Active.

### C4. **[THIẾU VALIDATION]** Kiểm tra Group Status trước khi thao tác

- **AC-G04 (Edit), AC-G05 (Add SV), AC-G06 (Remove SV)**: chỉ được phép khi group `Status = Active`.
- Hiện tại [UpdateInternshipGroupHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Commands/UpdateInternshipGroup/UpdateInternshipGroupHandler.cs#12-115), [AddStudentsToGroupHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Commands/AddStudentsToGroup/AddStudentsToGroupHandler.cs#21-34), [RemoveStudentsFromGroupHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Commands/RemoveStudentsFromGroup/RemoveStudentsFromGroupHandler.cs#19-30) **không kiểm tra group status** trước khi thực hiện.

### C5. **[THIẾU VALIDATION]** Kiểm tra một SV chỉ thuộc 1 nhóm trong cùng enterprise + term

- PRD: *"Mỗi student chỉ thuộc tối đa 1 Intern Group cho 1 enterprise trong 1 term"*.
- [AddStudentsToGroupHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Commands/AddStudentsToGroup/AddStudentsToGroupHandler.cs#21-34) (step 5): chỉ check SV có `InternshipApplication.Approved` nhưng **không check** SV đã có trong nhóm nào khác của cùng enterprise + term.
- [CreateInternshipGroupHandler](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Application/Features/InternshipGroups/Commands/CreateInternshipGroup/CreateInternshipGroupHandler.cs#21-34) cũng có lỗ hổng tương tự.

### C6. **[THIẾU VALIDATION]** CreateGroup yêu cầu ít nhất 1 SV (AC-G02)

- PRD: form tạo nhóm yêu cầu *"Chọn SV (bắt buộc, ít nhất 1)"*.
- Code: [Students](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.API/Controllers/InternshipGroups/InternshipGroupsController.cs#122-138) là optional — `if (request.Students != null && request.Students.Any())` → có thể tạo nhóm trống.
- **Quyết định (Theo User):** Phải implement validation **yêu cầu bắt buộc ít nhất 1 SV** khi lưu thông tin.

### C7. **[THIẾU]** Warning khi tên nhóm trùng (AC-G02, AC-G04)

- PRD: nếu tên nhóm trùng trong cùng enterprise + term → **inline warning** nhưng vẫn cho Save.
- Code không có bất kỳ check trùng tên nào, cũng không có warning response.

### C8. **[THIẾU]** `GetMyInternshipGroups` — Endpoint cho Student xem nhóm của mình (AC-G10)

- PRD: *"Student xem thông tin nhóm của mình (tên nhóm, Mentor, danh sách bạn cùng nhóm — read-only) qua dashboard cá nhân."*
- Handler `GetMyInternshipGroupsHandler` **đã có** nhưng cần kiểm tra lại phân quyền và response trả về đủ field theo PRD.

### C9. **[THIẾU]** Lịch sử thay đổi nhóm/Mentor (AC-S05)

- PRD: *"Lịch sử thay đổi nhóm/Mentor: thời gian, người thực hiện"*.
- Không có table audit log riêng cho InternshipStudent/InternshipGroup. [AuditLog.cs](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Domain/Entities/AuditLog.cs) có tồn tại nhưng chưa rõ có được dùng cho context này không.

### C10. **[THIẾU]** Notification/Toast messages (nhiều AC)

- PRD mô tả chi tiết: notify SV khi được xếp vào nhóm, notify Mentor khi được assign, notify khi đổi nhóm, v.v.
- Không có `INotificationService` hay messaging system nào trong các handler liên quan. (Đây có thể là out-of-scope MVP và sẽ làm sau.)

---

## Tóm tắt ưu tiên implement

| Ưu tiên | Item | Effort |
|---------|------|--------|
| 🔴 Critical | **B1**: Rename/refactor `InternshipStatus` enum khớp với PRD (Active/Finished/Archived) | Cao — ảnh hưởng DB schema, migration |
| 🔴 Critical | **C5**: Validate SV chỉ thuộc 1 nhóm/enterprise/term (business rule cốt lõi) | Thấp — thêm query check |
| 🔴 Critical | **C4**: Check group status (Active) trước khi Update/Add/Remove | Thấp — thêm validation trong handler |
| 🟡 Important | **B3**: Thêm `Description` vào [InternshipGroup](file:///d:/Internship-OneConnect_IOC_v2.0_Backend/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.Domain/Entities/InternshipGroup.cs#31-32) entity | Trung bình — cần migration |
| 🟡 Important | **C1**: Tạo feature GetPlacedStudents cho HR (toàn bộ Tab Sinh viên) | Cao — feature mới hoàn toàn |
| 🟡 Important | **C2**: ArchiveInternshipGroup command | Trung bình |
| 🟡 Important | **B5**: DeleteGroup check bước 2 (0 SV nhưng còn data) | Thấp |
| 🟡 Important | **C7**: Duplicate group name warning | Thấp |
| 🔴 Critical | **C6**: Validate bắt buộc có ít nhất 1 SV khi CreateGroup | Thấp |
| ⚪ Ignored | ~~**C3**: Auto-update Group Status khi Term thay đổi (background job)~~ | (Theo yêu cầu User) |
| 🔵 Nice-to-have | **C9**: Audit log lịch sử thay đổi nhóm/Mentor | Cao |
| 🔵 Nice-to-have | **C10**: Notification system | Cao |
