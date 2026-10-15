# BÁO CÁO CÔNG VIỆC BACKEND — ĐÀO ĐĂNG QUANG

- **Thành viên**: Đào Đăng Quang
- **GitHub**: `DDQuang1`
- **Email**: `daodquang295@gmail.com`
- **Vai trò**: Backend Developer
- **Nhánh phụ trách**: `backend` (kết nối về `develop`)
- **Phạm vi mã nguồn**: `backend/src/ECommerce.Api/`, `backend/tests/ECommerce.Api.Tests/`

---

## 1. Tổng quan các gói nhiệm vụ hoàn thành

Căn cứ theo [PHÂN CÔNG CÔNG VIỆC VÀ THEO DÕI TIẾN ĐỘ TRÊN GIT](../PHAN_CONG_CONG_VIEC.md), phụ trách 3 gói công việc cốt lõi của Backend:

| Mã nhiệm vụ | Tiêu đề | Phạm vi triển khai | Trạng thái |
|---|---|---|---|
| **BE-01** | Database Schema, Migrations, Seed, JWT Auth, Catalog APIs | `Data/`, `Entities/`, `Controllers/AuthController.cs`, `Controllers/ProductsController.cs`, `Controllers/CategoriesController.cs` | **Done** |
| **BE-02** | Customer Profile, Addresses, Cart, Checkout & Cancel Order | `Controllers/ProfileController.cs`, `Controllers/AddressesController.cs`, `Controllers/CartsController.cs`, `Controllers/OrdersController.cs` | **Done** |
| **BE-03** | Admin Management, Inventory Tracking, Reports & Order Lifecycle | `Controllers/AdminController.cs`, `Controllers/InventoryController.cs`, `Controllers/ReportsController.cs`, `Services/Admin/`, `Services/Inventory/`, `Services/Reports/` | **Done** |

---

## 2. Chi tiết triển khai theo từng gói nhiệm vụ

### 2.1. Gói BE-01: Schema/Migrations, Seed, JWT Auth và Catalog APIs
- **Database Architecture**: Thiết kế Code-First sử dụng Entity Framework Core với SQL Server / SQLite. Cấu hình Fluent API cho tất cả các bảng: `Users`, `Roles`, `Categories`, `Brands`, `Products`, `Carts`, `CartItems`, `Addresses`, `Orders`, `OrderDetails`, `OrderStatusHistories`, `InventoryTransactions`.
- **Migrations & Seed Data**:
  - `20260821081643_InitialCreate`: Khởi tạo toàn bộ cấu trúc bảng và quan hệ khóa ngoại.
  - `20260824082718_AddCategoryManagement`: Bổ sung danh mục sản phẩm đa cấp.
  - `20260829171309_SeedRolesAndBrands`: Dữ liệu mẫu vai trò (Admin, Customer) và các thương hiệu.
  - `20261008075327_RemoveModelManagedSeedData`: Tách biệt seed dữ liệu runtime.
- **Authentication & JWT**:
  - Đăng ký tài khoản mới kèm mã hóa mật khẩu an toàn với BCrypt.
  - Đăng nhập xác thực và cấp phát JWT token mang Claims định danh (`sub`, `email`, `role`).
  - Middleware xử lý lỗi tập trung `ExceptionHandlingMiddleware`, chuẩn hóa phản hồi ProblemDetails RFC 7807.
- **Catalog APIs**:
  - Phân trang, tìm kiếm theo từ khóa, lọc theo danh mục, lọc theo thương hiệu, khoảng giá và sắp xếp.
  - Quản lý danh mục sản phẩm (CRUD Category) phân quyền Admin.

### 2.2. Gói BE-02: Profile, Địa chỉ, Giỏ hàng, Đặt hàng và Hủy đơn
- **Customer Profile & Address Book**:
  - `GET /api/profile`, `PUT /api/profile`: Xem và cập nhật thông tin cá nhân.
  - `GET/POST/PUT/DELETE /api/addresses`: Quản lý danh sách địa chỉ giao hàng, đặt địa chỉ mặc định.
  - **Bảo mật dữ liệu**: Tuyệt đối cô lập dữ liệu người dùng, chỉ truy cập địa chỉ thuộc `User.GetRequiredUserId()`.
- **Server-backed Cart**:
  - Lưu giỏ hàng trên database, đồng bộ đa thiết bị.
  - Thêm, sửa số lượng, xóa item và xóa toàn bộ giỏ hàng khi checkout thành công.
- **Transactional Checkout & Inventory Consistency**:
  - Hỗ trợ thanh toán COD và Mock QR Transfer.
  - Sử dụng Database Transaction (`BeginTransactionAsync`) đảm bảo tính toàn vẹn: snapshot thông tin sản phẩm, tính toán tổng tiền trên server, xóa giỏ hàng và trừ kho.
  - Cơ chế Rollback tự động khi có ngoại lệ phát sinh trong quá trình lưu đơn.
- **Hủy đơn hàng (Order Cancellation)**:
  - Bổ sung `POST /api/orders/{id}/cancel` cho khách hàng.
  - Kiểm tra trạng thái đơn: chỉ cho phép hủy khi đơn còn `PENDING` (chặn khi đã hoàn thành hoặc đang giao).
  - **Chống hoàn kho lặp**: Kiểm tra chặt chẽ trạng thái đơn, không hoàn tồn kho 2 lần nếu đơn đã ở trạng thái `CANCELLED`.
  - Tự động cộng hoàn tồn kho vào `Products.StockQuantity` và ghi nhận lịch sử vào `InventoryTransactions` với loại `CANCEL_ORDER`.

### 2.3. Gói BE-03: Admin Management, Quản lý Kho, Báo cáo & Trạng thái đơn hàng
- **Admin Management APIs**:
  - `AdminController`: Thống kê dashboard, quản lý danh sách người dùng (kích hoạt / vô hiệu hóa), quản lý sản phẩm.
  - Kiểm soát phân quyền nghiêm ngặt với `[Authorize(Roles = "Admin")]`.
- **Inventory Tracking**:
  - `InventoryController`: Theo dõi biến động kho hàng theo thời gian thực.
  - Ghi nhận lịch sử giao dịch tồn kho với các loại: `IMPORT` (nhập kho), `SALE` (bán hàng khi checkout), `ADJUSTMENT` (điều chỉnh kiểm kê), `CANCEL_ORDER` (hoàn kho khi hủy đơn).
- **Reports & Analytics**:
  - `ReportsController`: Thống kê doanh thu theo khoảng thời gian, top sản phẩm bán chạy, tỷ lệ đơn hàng theo trạng thái.
- **Order Lifecycle Management**:
  - Cập nhật trạng thái đơn hàng (`PENDING` → `CONFIRMED` → `SHIPPING` → `COMPLETED` / `CANCELLED`).
  - Lưu vết lịch sử thay đổi trạng thái (`OrderStatusHistories`) có thông tin Admin thực hiện và thời gian.

---

## 3. Kiểm thử và Nghiệm thu

Các kiểm thử đơn vị và tích hợp trong `backend/tests/ECommerce.Api.Tests/`:
- `AuthControllerTests`: Xác thực đăng ký, đăng nhập và bảo vệ endpoint.
- `ProductsControllerTests`, `CategoriesControllerTests`: Tìm kiếm, phân trang và CRUD danh mục.
- `CartControllerTests`, `CartServiceTests`: Thao tác giỏ hàng, cập nhật số lượng.
- `AddressesControllerTests`, `AddressServiceTests`: Cô lập địa chỉ người dùng.
- `OrderServiceCheckoutTests`, `OrdersControllerTests`: Đặt hàng COD/QR, trừ kho, hủy đơn khôi phục kho, chặn truy cập đơn người khác.
- `AdminControllerTests`, `AdminPortalServiceTests`: Phân quyền Admin, chuyển trạng thái đơn, cập nhật kho.
- `HealthEndpointTests`, `PersistenceBoundaryTests`: Smoke test và ranh giới dữ liệu.
