/* Lab 08 - Stored Procedure cho bài 5 và bài 9. Chạy trong database QLSinhVien. */
CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_TheoKhoa
    @MaKhoa VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sv.MaSV, sv.HoTen, sv.NgaySinh, sv.GioiTinh, sv.MaKhoa,
           k.TenKhoa, sv.DiemTB, sv.Email
    FROM dbo.SinhVien AS sv
    INNER JOIN dbo.Khoa AS k ON k.MaKhoa = sv.MaKhoa
    WHERE sv.MaKhoa = @MaKhoa
    ORDER BY sv.HoTen;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_ThemVaDem
    @MaSV VARCHAR(15),
    @HoTen NVARCHAR(100),
    @NgaySinh DATE,
    @GioiTinh BIT,
    @MaKhoa VARCHAR(10),
    @DiemTB DECIMAL(4,2),
    @Email VARCHAR(100) = NULL,
    @TongSinhVien INT OUTPUT
AS
BEGIN
    SET NOCOUNT OFF;
    INSERT INTO dbo.SinhVien(MaSV, HoTen, NgaySinh, GioiTinh, MaKhoa, DiemTB, Email)
    VALUES (@MaSV, @HoTen, @NgaySinh, @GioiTinh, @MaKhoa, @DiemTB, @Email);

    SELECT @TongSinhVien = COUNT(*) FROM dbo.SinhVien;
END;
GO
