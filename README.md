# 🐳 IOCv2 - Internship Management Platform

Dự án **IOCv2 (Internship OneConnect)** hỗ trợ chạy toàn bộ hệ thống (Database, Redis, Backend) thông qua Docker Compose, tuân thủ kiến trúc Clean Architecture và .NET 9.

## 📋 Yêu cầu
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) đã được cài đặt và đang chạy.

## 🚀 Quick Start
Chạy lệnh sau tại thư mục gốc của dự án để khởi động toàn bộ dịch vụ:

```bash
docker compose up -d --build
```

---

## 🛠️ Thông tin Dịch vụ (Services)

| Service | Container Name | Port (Public) | URL / Connection | Mô tả |
| :--- | :--- | :--- | :--- | :--- |
| **Backend** | `iocv2_backend` | `8080`, `5000` | [http://localhost:8080/swagger](http://localhost:8080/swagger) | .NET 9 API & Swagger UI |
| **Database** | `iocv2_db` | `5432` | `PostgreSQL` | Lưu trữ dữ liệu chính (PostgreSQL) |
| **Cache** | `iocv2_redis` | `6379` | `Redis` | Quản lý Cache & Rate Limiting |

---

## 🔐 Tài khoản mặc định (Seeded Data)
Dữ liệu mẫu sẽ tự động được khởi tạo (Seed) khi hệ thống khởi chạy lần đầu:

| Role | Email | Password |
| :--- | :--- | :--- |
| 🛡️ **SuperAdmin** | `admin@iocv2.com` | `Admin@123` |

---

## 🏗️ Kiến trúc & Công nghệ
- **Backend Core**: .NET 9 (ASP.NET Core Web API)
- **Architecture**: Clean Architecture (Domain, Application, Infrastructure, API)
- **Patterns**: CQRS với MediatR, Repository & Unit of Work
- **Database**: PostgreSQL với EF Core (Npgsql)
- **Caching & Security**: Redis (Lưu trữ cache và thực hiện Rate Limiting bằng Middleware tự định nghĩa)
- **Validation**: FluentValidation
- **Mapping**: AutoMapper

---

## ❓ Xử lý sự cố (Troubleshooting)
- **Database không sẵn sàng**: Backend đã có cơ chế Retry (5 lần) để đợi Database khởi động xong. Nếu vẫn lỗi, hãy kiểm tra log bằng `docker logs iocv2_backend`.
- **Cập nhật mã nguồn**: Mỗi khi thay đổi code ở Backend hoặc các project lớp dưới, hãy chạy lại `docker compose up -d --build` để đóng gói lại Image mới.
- **Lỗi cổng (Port Conflict)**: Đảm bảo các cổng `8080`, `5000`, `5432`, `6379` không bị chiếm dụng bởi ứng dụng khác trên máy host.
