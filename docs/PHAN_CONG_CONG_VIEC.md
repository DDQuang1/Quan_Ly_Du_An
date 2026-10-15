# PHÂN CÔNG CÔNG VIỆC VÀ THEO DÕI TIẾN ĐỘ TRÊN GIT

## 1. Căn cứ và phạm vi

Dự án: Website bán hàng trực tuyến, kế thừa từ môn Quản lý dự án. Nguồn đã đối chiếu: `../QLDA/docs/work_distribution.md`, `docs/git-workflow.md`, `.github/workflows/ci.yml`, mã nguồn `backend/` và `frontend/` trong QLDA.

Trong workspace hiện tại, QLDA nằm cùng cấp với ChuyenDeTongHop (`../QLDA`), không phải `./QLDA`. Repo Chuyên đề Tổng hợp nằm trong `.repo-sync`, remote hiện có: https://github.com/nbn146/Chuyen-de-tong-hop.

QLDA đã có React/TypeScript/Vite, ASP.NET Core/EF Core/SQL Server, chatbot và workflow CI. Công việc dưới đây là tiếp nhận, kiểm tra, sửa lỗi và hoàn thiện; không coi việc sao chép mã nguồn cũ là chức năng mới hoàn thành. Phân công thành viên theo thống nhất của nhóm: Toản — Team leader; Anh Quang — Frontend; Đăng Quang — Backend; Bình — Test CI/CD; Bá Nam — Backend AI. Tài khoản GitHub sẽ được bổ sung khi có thông tin.

## 2. Phân công theo vai trò

| Vai trò | Thành viên / GitHub | Phạm vi chịu trách nhiệm | Sản phẩm bàn giao và tiêu chí nghiệm thu |
|---|---|---|---|
| Team leader | Toản / GitHub: chưa bổ sung | Backlog, yêu cầu, kiến trúc, tích hợp, review, tài liệu và demo | Danh sách issue có người nhận và tiêu chí hoàn thành; hướng dẫn chạy; báo cáo tuần có link bằng chứng; bản tích hợp chạy được kịch bản mua hàng |
| Frontend | Anh Quang / GitHub: chưa bổ sung | `frontend/src/`: trang Guest/Customer/Admin, router, API client, trạng thái đăng nhập; giao diện `components/ChatBot.tsx` | Chạy được đăng nhập → tìm sản phẩm → giỏ → COD → lịch sử đơn; màn hình quản trị hoạt động theo quyền; có loading/empty/error; ảnh/video và test giao diện liên quan |
| Backend | Đăng Quang / GitHub: `DDQuang1` | `backend/src/ECommerce.Api/`: Auth, Products, Categories, Carts, Orders, Profile, Addresses, Admin, Inventory, Reports; Entities, Data, migrations | API/DTO khớp FE; JWT và phân quyền được kiểm tra; đặt/hủy đơn cập nhật kho đúng và nhất quán; migration và dữ liệu demo tái lập được; test nghiệp vụ liên quan |
| Test CI/CD | Bình / GitHub: chưa bổ sung | Kế hoạch kiểm thử, hồi quy, `.github/workflows/ci.yml`, kết quả chạy test, đóng gói và triển khai demo | Checklist pass/fail có bằng chứng; CI build/test FE và BE chạy trên PR; lỗi có bước tái hiện; hướng dẫn deploy/rollback và smoke test bản demo |
| Backend AI | Bá Nam / GitHub: chưa bổ sung | `Controllers/ChatController.cs`, `Services/Chat/`, `DTOs/Chat/`, cấu hình provider và dữ liệu sản phẩm cho chatbot | Hợp đồng `POST /api/chat`; tư vấn dựa trên catalog; test thiếu API key/lỗi provider/timeout; bộ câu hỏi đánh giá và kết quả; hướng dẫn cấu hình key ngoài Git |

Mỗi thành viên viết/sửa test cho phần mình. Test CI/CD thiết kế kiểm thử tích hợp và kiểm tra độc lập; Team leader không phải người duy nhất commit mã nguồn.

## 3. Backlog có thể chuyển thành GitHub Issues

Trạng thái ban đầu của tất cả nhiệm vụ: **Todo — chưa nghiệm thu**. Mỗi dòng tương ứng một issue; sau khi tạo, bổ sung URL issue vào cột mã nhiệm vụ hoặc báo cáo tuần.

| Mã | Chủ trì | Công việc cụ thể | Phụ thuộc | Điều kiện hoàn thành |
|---|---|---|---|---|
| TL-01 | Toản (Team leader) | Ghi nhận nguồn kế thừa, chọn phạm vi và đưa baseline sạch lên repo môn học | — | Có commit baseline ghi nguồn/commit QLDA, README chạy dự án; loại file build, dependency, key và dữ liệu cá nhân |
| TL-02 | Toản (Team leader) | Tạo backlog, milestone, bảng tiến độ và thống nhất API/DTO | TL-01 | Mỗi issue có owner, tiêu chí nghiệm thu, mốc dự kiến; FE/BE/AI xác nhận hợp đồng |
| TL-03 | Toản (Team leader) | Review, tích hợp, tổng hợp báo cáo và kịch bản demo | Các gói chức năng | Bản demo có tag, PR và kết quả kiểm thử; tài liệu khớp hành vi thực tế |
| FE-01 | Anh Quang (Frontend) | Tiếp nhận giao diện, auth và danh sách/chi tiết/tìm lọc sản phẩm | TL-01, TL-02 | Chạy với API thật; hiển thị lỗi và kết quả rỗng; test liên quan đạt |
| FE-02 | Anh Quang (Frontend) | Hoàn thiện profile, địa chỉ, giỏ hàng, COD, lịch sử/hủy đơn | BE-02 | Demo trọn luồng; xử lý hết hàng và request thất bại; có bằng chứng |
| FE-03 | Anh Quang (Frontend) | Hoàn thiện màn hình Admin và tích hợp chatbot | BE-03, AI-01 | Phân quyền UI; cập nhật đơn/kho đúng; chat hiển thị phản hồi và sản phẩm |
| BE-01 | Đăng Quang (Backend) | Tiếp nhận schema/migrations, seed, JWT, catalog API | TL-01, TL-02 | DB mới dựng được; test đăng nhập/phân quyền/tìm lọc đạt; có tài liệu API |
| BE-02 | Đăng Quang (Backend) | Kiểm tra/sửa profile, địa chỉ, cart, checkout và hủy đơn | BE-01 | Không truy cập đơn/địa chỉ người khác; không âm kho; rollback khi đặt thất bại; hủy không hoàn kho lặp |
| BE-03 | Đăng Quang (Backend) | Kiểm tra/sửa Admin, inventory, báo cáo và trạng thái đơn | BE-02 | Chặn sai quyền/chuyển trạng thái sai; có lịch sử kho/đơn; số liệu đối chiếu dữ liệu demo |
| QA-01 | Bình (Test CI/CD) | Chạy baseline, lập test plan và bug list | TL-01 | Ghi lệnh, môi trường, kết quả thực tế; mỗi lỗi có expected/actual và bước tái hiện |
| QA-02 | Bình (Test CI/CD) | Tích hợp workflow CI kế thừa, báo cáo test và kiểm tra ruleset | TL-01, QA-01 | PR có job backend/frontend đạt; lưu test report; ghi nhận branch protection đã cấu hình hay còn thiếu |
| QA-03 | Bình (Test CI/CD) | Hồi quy E2E, đóng gói/deploy demo, smoke test và rollback | FE-02, FE-03, BE-03, AI-02 | Có kết quả E2E; URL hoặc cách chạy demo; artifact/tag; hướng dẫn khôi phục |
| AI-01 | Bá Nam (Backend AI) | Tiếp nhận chatbot, chốt request/response, cấu hình và truy xuất catalog | BE-01, TL-02 | Contract và ví dụ request/response; không lộ key; trả sản phẩm tồn tại trong dữ liệu |
| AI-02 | Bá Nam (Backend AI) | Kiểm tra/sửa fallback, timeout, lỗi provider và chất lượng tư vấn | AI-01 | Bộ tối thiểu 20 câu hỏi: tìm SP, so sánh, ngân sách, không tìm thấy, ngoài phạm vi; báo cáo kết quả và hạn chế; test dùng mock provider |

FE có thể dùng dữ liệu mock theo DTO đã thống nhất trong khi chờ BE. QA-01/QA-02 chạy song song với các phần phát triển. Lỗi tìm thấy được tạo issue mới, gán lại cho chủ phần mã nguồn.

## 4. Ranh giới phối hợp và review

| Phần dùng chung | Người chủ trì | Người phối hợp/review |
|---|---|---|
| API/DTO nghiệp vụ, schema, migrations | Backend | Frontend, Backend AI và Team leader |
| API chat và logic AI | Backend AI | Backend review truy cập dữ liệu; Frontend review contract |
| Giao diện và API client của chatbot | Frontend | Backend AI |
| `Program.cs`, cấu hình backend và đăng ký dependency | Backend | Backend AI gửi thay đổi qua PR, tránh sửa chồng chéo |
| Workflow, đóng gói, cấu hình triển khai | Test CI/CD | Backend, Frontend và Team leader |
| Kế hoạch, báo cáo, release | Team leader | Tất cả thành viên cung cấp bằng chứng; PR của leader có thành viên khác review |

## 5. Lộ trình đề xuất: 3 mốc, mỗi mốc 2 tuần

Đây là lịch đề xuất từ ngày nhóm thống nhất bắt đầu, chưa phải deadline do giáo viên giao.

| Mốc | Phạm vi | Kết quả giáo viên có thể kiểm tra |
|---|---|---|
| M1 — Tuần 1–2 | TL-01/02, FE-01, BE-01, QA-01/02, AI-01 | Baseline có ghi nguồn; backlog; catalog/auth chạy được; CI trên PR; contract chatbot |
| M2 — Tuần 3–4 | FE-02, BE-02, BE-03; bắt đầu FE-03 và AI-02 | Demo COD và tồn kho; kết quả kiểm thử lỗi nghiệp vụ; PR sửa lỗi |
| M3 — Tuần 5–6 | Hoàn tất FE-03, AI-02, QA-03, TL-03 | Demo Customer/Admin/AI, báo cáo hồi quy, hướng dẫn chạy, tag phiên bản |

## 6. Đưa dự án lên Git và ghi nhận đóng góp

1. Team leader nhập baseline từ QLDA vào repo môn học theo cấu trúc gốc `backend/`, `frontend/`, `docs/`, `.github/`; giữ nguyên biểu đồ và báo cáo hiện có. Chỉ đưa file nguồn cần thiết; giữ lockfile và migration, loại `.git`, `.worktrees`, `node_modules`, `bin`, `obj`, `.env.local` và secret. Bổ sung `.gitignore` và cấu hình mẫu không chứa key trước khi push.
2. Baseline ghi rõ repo/commit nguồn, ngày tiếp nhận và kết quả chạy thực tế. Không chia mã nguồn cũ thành commit giả danh công việc mới của từng người.
3. Thiết lập `main` là bản ổn định, `develop` là bản tích hợp. Mỗi nhiệm vụ tạo nhánh riêng từ `develop`, ví dụ `feature/FE-02-cart-checkout`, `fix/BE-02-stock`, `docs/TL-02-plan`. Khởi tạo `develop` một lần từ baseline nếu chưa có.
4. Mỗi người dùng tài khoản GitHub và Git author của mình; commit theo phần công việc thực tế, push sau mỗi phần có thể review và tối thiểu mỗi buổi có thay đổi. Ví dụ `feat: FE-02 handle checkout errors`, `test: QA-01 add order regression cases`.
5. Mở PR về `develop`, liên kết issue bằng `Closes #<số-issue>`, mô tả thay đổi, phần kế thừa, cách kiểm tra và bằng chứng. Có ít nhất một người khác review; CI đạt trước khi merge. Không push trực tiếp vào `main`.
6. Cuối mốc, mở PR `develop` → `main`, tạo tag `m1`, `m2`, `m3` sau nghiệm thu và ghi commit phiên bản trong báo cáo.

Ví dụ thao tác của thành viên, chạy tại thư mục clone repo môn học sau khi có baseline và nhánh `develop`:

```powershell
git switch develop
git pull --ff-only origin develop
git switch -c feature/FE-02-cart-checkout
# Thực hiện và kiểm tra công việc; chọn đúng file thay đổi để stage.
git add frontend/src/pages/CartPage.tsx
git commit -m "fix: FE-02 handle cart errors"
git push -u origin feature/FE-02-cart-checkout
```

Tên file trong ví dụ cần khớp thay đổi thực tế. Dùng `git status` và `git diff --cached` để kiểm tra nội dung trước commit.

## 7. Giáo viên giám sát tiến độ

- Team leader cung cấp link repo; nếu private, cấp quyền đọc cho tài khoản giáo viên.
- Dùng GitHub Issues/Projects với cột **Todo → In progress → In review → Done**, nhãn `leader`, `frontend`, `backend`, `qa-cicd`, `ai`; milestone `M1`, `M2`, `M3`.
- Một nhiệm vụ có một owner chính; chỉ chuyển Done khi đáp ứng tiêu chí, có PR đã merge và bằng chứng kiểm tra. Việc kế thừa chưa kiểm tra ghi rõ “Đã có mã nguồn — chưa xác minh”.
- Cuối mỗi tuần, mỗi thành viên bổ sung kết quả vào `docs/tien-do/TUAN_XX.md` theo [mẫu](docs/tien-do/MAU_BAO_CAO_TUAN.md). Team leader tổng hợp và liên kết file từ README.
- Giáo viên xem issue, PR/review, Actions, báo cáo và tag demo để đối chiếu. Tỷ lệ hoàn thành = số nhiệm vụ Done / tổng nhiệm vụ của mốc; không đánh giá chỉ theo số commit hoặc số dòng code.

CI kế thừa hiện có job `backend` (restore/build/test .NET) và `frontend` (npm ci/test/build), kích hoạt PR vào `main`/`develop` và push vào `develop`. Chưa có job triển khai CD trong workflow đã đọc. QA-02 cần tích hợp và xác minh trên repo môn học; QA-03 phụ trách phần triển khai. Ruleset phải được cấu hình trên GitHub, không được coi là đã bật chỉ vì có workflow.

## 8. Checklist hoàn thành chung

- [ ] Issue có owner, phạm vi, tiêu chí và mốc dự kiến.
- [ ] Code/tài liệu khớp yêu cầu; ghi rõ thay đổi so với baseline.
- [ ] Kiểm tra phù hợp đã chạy; có kết quả và hạn chế nếu có.
- [ ] PR liên kết issue, được review và CI đạt.
- [ ] Thay đổi đã merge; báo cáo tuần có link commit/PR/test/demo.
- [ ] Không còn lỗi chặn kịch bản demo của mốc.

Tài liệu này là kế hoạch phân công. Chưa thực hiện nhập baseline, tạo issue, cấu hình GitHub hay push mã nguồn trong lần lập kế hoạch này.
