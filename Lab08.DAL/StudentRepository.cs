using System.Data;
using System.Data.SqlClient;

namespace Lab08.DAL;

public sealed class StudentRepository
{
    private const string SelectStudentsSql = """
        SELECT sv.MaSV, sv.HoTen, sv.NgaySinh, sv.GioiTinh, sv.MaKhoa,
               k.TenKhoa, sv.DiemTB, sv.Email
        FROM dbo.SinhVien AS sv
        INNER JOIN dbo.Khoa AS k ON k.MaKhoa = sv.MaKhoa
        """;

    // Bài 1: ExecuteScalar
    public async Task<int> CountStudentsAsync(CancellationToken ct = default)
    {
        await using var connection = DbConnectionFactory.Create();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.SinhVien;", connection);
        await connection.OpenAsync(ct);
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    // Bài 2: ExecuteReader
    public Task<List<StudentRecord>> GetStudentsAsync(CancellationToken ct = default) =>
        ReadStudentsAsync(SelectStudentsSql + " ORDER BY sv.MaSV;", null, ct);

    // Bài 3: parameterized INSERT + ExecuteNonQuery
    public async Task<int> AddStudentAsync(StudentRecord student, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.SinhVien(MaSV, HoTen, NgaySinh, GioiTinh, MaKhoa, DiemTB, Email)
            VALUES (@MaSV, @HoTen, @NgaySinh, @GioiTinh, @MaKhoa, @DiemTB, @Email);
            """;
        await using var connection = DbConnectionFactory.Create();
        await using var command = new SqlCommand(sql, connection);
        AddStudentParameters(command, student);
        await connection.OpenAsync(ct);
        return await command.ExecuteNonQueryAsync(ct);
    }

    // Bài 4: parameterized UPDATE with precision and scale
    public async Task<int> UpdateScoreAsync(string maSv, decimal score, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.SinhVien SET DiemTB=@DiemTB WHERE MaSV=@MaSV;";
        await using var connection = DbConnectionFactory.Create();
        await using var command = new SqlCommand(sql, connection);
        var scoreParameter = command.Parameters.Add("@DiemTB", SqlDbType.Decimal);
        scoreParameter.Precision = 4;
        scoreParameter.Scale = 2;
        scoreParameter.Value = score;
        command.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = maSv;
        await connection.OpenAsync(ct);
        return await command.ExecuteNonQueryAsync(ct);
    }

    // Bài 5: Stored Procedure + ExecuteReader
    public Task<List<StudentRecord>> GetStudentsByFacultyAsync(string maKhoa, CancellationToken ct = default) =>
        ReadStudentsAsync("dbo.sp_SinhVien_TheoKhoa", parameters =>
        {
            parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = maKhoa;
        }, ct, CommandType.StoredProcedure);

    // Bài 6: find details by ID using ExecuteReader and a parameter
    public async Task<StudentRecord?> FindStudentAsync(string maSv, CancellationToken ct = default)
    {
        var results = await ReadStudentsAsync(SelectStudentsSql + " WHERE sv.MaSV=@MaSV;", parameters =>
        {
            parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = maSv;
        }, ct);
        return results.SingleOrDefault();
    }

    // Bài 7: ExecuteScalar, MAX returns DBNull for a faculty with no students
    public async Task<decimal?> GetMaximumScoreAsync(string maKhoa, CancellationToken ct = default)
    {
        await using var connection = DbConnectionFactory.Create();
        await using var command = new SqlCommand(
            "SELECT MAX(DiemTB) FROM dbo.SinhVien WHERE MaKhoa=@MaKhoa;", connection);
        command.Parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = maKhoa;
        await connection.OpenAsync(ct);
        var value = await command.ExecuteScalarAsync(ct);
        return value is null or DBNull ? null : Convert.ToDecimal(value);
    }

    // Bài 8: parameterized DELETE. SQL error 547 is handled by the UI as a FK conflict.
    public async Task<int> DeleteStudentAsync(string maSv, CancellationToken ct = default)
    {
        await using var connection = DbConnectionFactory.Create();
        await using var command = new SqlCommand("DELETE dbo.SinhVien WHERE MaSV=@MaSV;", connection);
        command.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = maSv;
        await connection.OpenAsync(ct);
        return await command.ExecuteNonQueryAsync(ct);
    }

    // Bài 9: stored procedure INSERT with an OUTPUT count parameter
    public async Task<(int Rows, int TotalStudents)> AddStudentWithOutputAsync(
        StudentRecord student, CancellationToken ct = default)
    {
        await using var connection = DbConnectionFactory.Create();
        await using var command = new SqlCommand("dbo.sp_SinhVien_ThemVaDem", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        AddStudentParameters(command, student);
        var output = command.Parameters.Add("@TongSinhVien", SqlDbType.Int);
        output.Direction = ParameterDirection.Output;
        await connection.OpenAsync(ct);
        var rows = await command.ExecuteNonQueryAsync(ct);
        return (rows, Convert.ToInt32(output.Value));
    }

    private static async Task<List<StudentRecord>> ReadStudentsAsync(
        string commandText,
        Action<SqlParameterCollection>? addParameters,
        CancellationToken ct,
        CommandType commandType = CommandType.Text)
    {
        var students = new List<StudentRecord>();
        await using var connection = DbConnectionFactory.Create();
        await using var command = new SqlCommand(commandText, connection) { CommandType = commandType };
        addParameters?.Invoke(command.Parameters);
        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            students.Add(MapStudent(reader));
        return students;
    }

    private static StudentRecord MapStudent(SqlDataReader reader) => new(
        reader.GetString(reader.GetOrdinal("MaSV")),
        reader.GetString(reader.GetOrdinal("HoTen")),
        reader.GetDateTime(reader.GetOrdinal("NgaySinh")),
        reader.GetBoolean(reader.GetOrdinal("GioiTinh")),
        reader.GetString(reader.GetOrdinal("MaKhoa")),
        reader.IsDBNull(reader.GetOrdinal("TenKhoa")) ? null : reader.GetString(reader.GetOrdinal("TenKhoa")),
        reader.GetDecimal(reader.GetOrdinal("DiemTB")),
        reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")));

    private static void AddStudentParameters(SqlCommand command, StudentRecord student)
    {
        command.Parameters.Add("@MaSV", SqlDbType.VarChar, 15).Value = student.MaSV;
        command.Parameters.Add("@HoTen", SqlDbType.NVarChar, 100).Value = student.HoTen;
        command.Parameters.Add("@NgaySinh", SqlDbType.Date).Value = student.NgaySinh.Date;
        command.Parameters.Add("@GioiTinh", SqlDbType.Bit).Value = student.GioiTinh;
        command.Parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = student.MaKhoa;
        var score = command.Parameters.Add("@DiemTB", SqlDbType.Decimal);
        score.Precision = 4;
        score.Scale = 2;
        score.Value = student.DiemTB;
        command.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value =
            string.IsNullOrWhiteSpace(student.Email) ? DBNull.Value : student.Email.Trim();
    }
}
