LAB 08 - SQLCOMMAND, SQLPARAMETER VÀ CÁC PHƯƠNG THỨC THỰC THI
Học phần: Lập trình .NET (IT3212)

MỤC TIÊU
- Demo đủ bài 1-10 của đề Lab08 bằng WinForms và ADO.NET.
- Dùng CSDL QLSinhVien từ Lab07; kèm script tạo lại CSDL và Stored Procedure.

CHẠY DEMO
1. Yêu cầu Windows, Visual Studio 2022 và .NET 10 SDK.
2. Cài/chạy SQL Server (đã kiểm tra với SQL Server 2022 Express).
3. Mở Lab08_ADONET_Commands.sln. Project WinForms được đặt đầu solution; nếu VS chưa chọn,
   chọn Lab08_ADONET_Commands làm Startup Project.
4. Mặc định kết nối .\SQLEXPRESS bằng Windows Authentication. Sửa Data Source trong
   Lab08_ADONET_Commands\App.config nếu máy dùng instance khác.
5. Bấm "Tạo CSDL + SP" để chạy Database\01_QLSinhVien.sql và
   Database\02_StoredProcedures.sql. Nút này tạo/kiểm tra database, thêm dữ liệu mẫu còn thiếu
   và cài hai stored procedure. Không xóa dữ liệu đang có.
6. Màn hình tự kiểm tra kết nối; dùng các nút ở tab "Bài 1-5" và "Bài 6-10" để demo.
   Tab "Nhật ký thao tác" ghi kết quả; form thêm sinh viên cho phép kiểm tra INSERT/OUTPUT.

CÁC PHẦN ĐÃ HOÀN THÀNH
1. Đếm số sinh viên với ExecuteScalar.
2. Đọc danh sách MaSV, HoTen, DiemTB với ExecuteReader.
3. Thêm sinh viên bằng INSERT tham số hóa và ExecuteNonQuery; hỗ trợ Email NULL.
4. Cập nhật DiemTB theo MaSV; có kiểm tra số dòng cập nhật và cấu hình decimal.
5. Gọi sp_SinhVien_TheoKhoa bằng CommandType.StoredProcedure.
6. Tìm chi tiết sinh viên theo mã bằng ExecuteReader và SqlParameter.
7. Lấy MAX(DiemTB) theo khoa; xử lý kết quả NULL.
8. Xóa có xác nhận; xử lý lỗi 547 do ràng buộc khóa ngoại.
9. Gọi sp_SinhVien_ThemVaDem, đọc @TongSinhVien OUTPUT.
10. SqlHelper gói ExecuteScalar, ExecuteNonQuery, ExecuteReader; tự dispose connection,
    command và reader.

THƯ MỤC QUAN TRỌNG
- Lab08_ADONET_Commands: giao diện WinForms.
- Lab08.DAL: factory kết nối, repository thao tác database, SqlHelper và script runner.
- Database\01_QLSinhVien.sql: tạo schema và tối thiểu 3 khoa, 10 sinh viên, 5 học phần.
- Database\02_StoredProcedures.sql: tạo hai stored procedure cho bài 5 và bài 9.
- Screenshots\App_Lab08.png: ảnh màn hình demo kết nối và kết quả truy vấn.

KIỂM CHỨNG TRÊN MÁY PHÁT TRIỂN
- SQL Server: .\SQLEXPRESS, database QLSinhVien.
- Dữ liệu: 3 khoa, 10 sinh viên, 5 học phần, 15 lượt đăng ký.
- Build Lab08_ADONET_Commands.sln: thành công, 0 warning, 0 error.
- Đã chạy thử các phương thức scalar/reader/nonquery, tìm kiếm, lọc theo khoa, MAX,
  INSERT/UPDATE/DELETE tham số hóa, Stored Procedure OUTPUT và trường hợp xóa vi phạm FK.
- Các dòng test thêm vào được xóa sau khi xác minh; dữ liệu mẫu gốc được giữ nguyên.
