namespace Lab08.DAL;

public sealed record StudentRecord(
    string MaSV,
    string HoTen,
    DateTime NgaySinh,
    bool GioiTinh,
    string MaKhoa,
    string? TenKhoa,
    decimal DiemTB,
    string? Email);
