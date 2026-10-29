using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Lab08.DAL;

namespace Lab08_ADONET_Commands;

public sealed class MainForm : Form
{
    private readonly StudentRepository _repository = new();
    private readonly SqlScriptRunner _scriptRunner = new();
    private readonly Label _status = new();
    private readonly DataGridView _grid = new();
    private readonly Label _countResult = new();
    private readonly TextBox _updateId = new();
    private readonly NumericUpDown _updateScore = new();
    private readonly TextBox _findId = new();
    private readonly TextBox _deleteId = new();
    private readonly ComboBox _facultyProcedure = new();
    private readonly ComboBox _facultyMax = new();
    private readonly RichTextBox _log = new();
    private List<FacultyOption> _faculties = [];

    public MainForm()
    {
        Text = "Lab 08 - SqlCommand, SqlParameter và phương thức thực thi";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1060, 760);
        Size = new Size(1240, 900);
        WindowState = FormWindowState.Maximized;
        Font = new Font("Segoe UI", 9.5F);
        BackColor = Color.FromArgb(246, 248, 252);
        BuildInterface();
        Shown += async (_, _) => await CheckConnectionAsync(showDialog: false);
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = BackColor };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 136));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
        var title = new Label
        {
            Text = "LAB 08  |  SqlCommand, SqlParameter và các phương thức thực thi",
            Location = new Point(14, 8),
            Size = new Size(1100, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 63, 110),
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(title);
        _status.Location = new Point(14, 43);
        _status.Size = new Size(1100, 26);
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _status.ForeColor = Color.DimGray;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        header.Controls.Add(_status);
        var prepare = MakeButton("Tạo CSDL + SP", Color.FromArgb(16, 129, 102), 170);
        prepare.Click += async (_, _) => await PrepareDatabaseAsync();
        var check = MakeButton("Kiểm tra kết nối", Color.FromArgb(42, 102, 184), 160);
        check.Click += async (_, _) => await CheckConnectionAsync(showDialog: true);
        prepare.Location = new Point(14, 78);
        check.Location = new Point(194, 78);
        header.Controls.Add(prepare);
        header.Controls.Add(check);

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        tabs.TabPages.Add(BuildGuidedTab());
        tabs.TabPages.Add(BuildIndependentTab());

        _log.ReadOnly = true;
        _log.BackColor = Color.White;
        _log.Font = new Font("Consolas", 9.5F);
        _log.Text = "Nhật ký thao tác SQL sẽ hiển thị ở đây. Mỗi thao tác dùng tham số có kiểu dữ liệu rõ ràng.\n";
        var logPage = new TabPage("Nhật ký thao tác") { BackColor = Color.White, Padding = new Padding(8) };
        logPage.Controls.Add(_log);
        tabs.TabPages.Add(logPage);
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(tabs, 0, 1);
    }

    private TabPage BuildGuidedTab()
    {
        var page = new TabPage("Bài 1-5: Có hướng dẫn") { BackColor = BackColor, Padding = new Padding(10) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(4) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(3), WrapContents = true, BackColor = Color.White };
        var countButton = MakeButton("1. Scalar", Color.FromArgb(42, 102, 184), 115);
        countButton.Click += async (_, _) => await RunUiAsync(async () =>
        {
            var count = await _repository.CountStudentsAsync();
            _countResult.Text = $"Kết quả: {count} sinh viên";
            Log($"[Bài 1 / ExecuteScalar] COUNT(*) = {count}");
        });
        _countResult.Text = "Kết quả: -";
        _countResult.AutoSize = true;
        _countResult.Padding = new Padding(8, 11, 0, 0);
        var reload = MakeButton("2. Reader", Color.FromArgb(42, 102, 184), 120);
        reload.Click += async (_, _) => await LoadStudentsAsync();
        ConfigureCombo(_facultyProcedure, 140);
        var procButton = MakeButton("5. Stored Proc", Color.FromArgb(16, 129, 102), 150);
        procButton.Click += async (_, _) => await RunUiAsync(async () =>
        {
            var faculty = SelectedFaculty(_facultyProcedure);
            if (faculty is null) throw new InvalidOperationException("Hãy chọn khoa.");
            ShowStudents(await _repository.GetStudentsByFacultyAsync(faculty.MaKhoa));
            Log($"[Bài 5 / StoredProcedure] sp_SinhVien_TheoKhoa @MaKhoa='{faculty.MaKhoa}'");
        });
        toolbar.Controls.Add(countButton);
        toolbar.Controls.Add(_countResult);
        toolbar.Controls.Add(reload);
        toolbar.Controls.Add(_facultyProcedure);
        toolbar.Controls.Add(procButton);
        layout.Controls.Add(toolbar, 0, 0);

        ConfigureGrid();
        layout.Controls.Add(_grid, 0, 1);

        var mutations = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(3), WrapContents = true, BackColor = Color.White };
        var addButton = MakeButton("3. Thêm sinh viên", Color.FromArgb(16, 129, 102), 175);
        addButton.Click += async (_, _) =>
        {
            var student = await PromptStudentAsync();
            if (student is null) return;
            await RunUiAsync(async () =>
            {
                var rows = await _repository.AddStudentAsync(student);
                Log($"[Bài 3 / ExecuteNonQuery] Đã thêm {rows} dòng: {student.MaSV}");
                await LoadStudentsAsync();
            });
        };
        _updateId.Width = 100;
        _updateId.PlaceholderText = "Mã SV";
        _updateScore.DecimalPlaces = 2;
        _updateScore.Minimum = 0;
        _updateScore.Maximum = 10;
        _updateScore.Value = 8;
        _updateScore.Width = 92;
        var updateButton = MakeButton("4. Cập nhật điểm", Color.FromArgb(42, 102, 184), 165);
        updateButton.Click += async (_, _) => await RunUiAsync(async () =>
        {
            var id = _updateId.Text.Trim();
            if (id.Length == 0) throw new InvalidOperationException("Nhập mã sinh viên cần cập nhật.");
            var rows = await _repository.UpdateScoreAsync(id, _updateScore.Value);
            Log(rows == 1 ? $"[Bài 4 / UPDATE] Đã cập nhật điểm {id} -> {_updateScore.Value:N2}" : $"[Bài 4 / UPDATE] Không tìm thấy mã {id}");
            await LoadStudentsAsync();
        });
        mutations.Controls.Add(addButton);
        mutations.Controls.Add(new Label { Text = "4. Cập nhật điểm:", AutoSize = true, Padding = new Padding(4, 10, 0, 0) });
        mutations.Controls.Add(_updateId);
        mutations.Controls.Add(_updateScore);
        mutations.Controls.Add(updateButton);
        layout.Controls.Add(mutations, 0, 2);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildIndependentTab()
    {
        var page = new TabPage("Bài 6-10: Tự thực hiện") { BackColor = BackColor, Padding = new Padding(10) };
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(4) };

        var find = TaskCard(page, "6. Tìm chi tiết theo mã - ExecuteReader + SqlParameter");
        var findFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), WrapContents = false };
        _findId.Width = 120;
        _findId.PlaceholderText = "Nhập mã SV";
        var findButton = MakeButton("Tìm sinh viên", Color.FromArgb(42, 102, 184), 150);
        findButton.Click += async (_, _) => await RunUiAsync(async () =>
        {
            var id = _findId.Text.Trim();
            if (id.Length == 0) throw new InvalidOperationException("Nhập mã sinh viên cần tìm.");
            var student = await _repository.FindStudentAsync(id);
            if (student is null)
            {
                Log($"[Bài 6 / ExecuteReader] Không tìm thấy mã {id}");
                MessageBox.Show(this, "Không tìm thấy sinh viên.", "Kết quả tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ShowStudents([student]);
            Log($"[Bài 6 / ExecuteReader] Tìm thấy {student.MaSV} - {student.HoTen}");
        });
        findFlow.Controls.Add(_findId);
        findFlow.Controls.Add(findButton);
        find.Controls.Add(findFlow);
        layout.Controls.Add(find);

        var max = TaskCard(page, "7. Điểm cao nhất theo khoa - ExecuteScalar");
        var maxFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), WrapContents = false };
        ConfigureCombo(_facultyMax, 160);
        var maxButton = MakeButton("Tính MAX(DiemTB)", Color.FromArgb(42, 102, 184), 165);
        maxButton.Click += async (_, _) => await RunUiAsync(async () =>
        {
            var faculty = SelectedFaculty(_facultyMax);
            if (faculty is null) throw new InvalidOperationException("Hãy chọn khoa.");
            var maximum = await _repository.GetMaximumScoreAsync(faculty.MaKhoa);
            var message = maximum is null ? "Khoa chưa có sinh viên (NULL)" : $"Điểm cao nhất: {maximum.Value:N2}";
            Log($"[Bài 7 / ExecuteScalar] {faculty.TenKhoa}: {message}");
            MessageBox.Show(this, message, "Thống kê điểm", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
        maxFlow.Controls.Add(_facultyMax);
        maxFlow.Controls.Add(maxButton);
        max.Controls.Add(maxFlow);
        layout.Controls.Add(max);

        var delete = TaskCard(page, "8. Xóa theo mã - xác nhận và khóa ngoại");
        var deleteFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), WrapContents = false };
        _deleteId.Width = 120;
        _deleteId.PlaceholderText = "Nhập mã SV";
        var deleteButton = MakeButton("Xóa sinh viên", Color.FromArgb(180, 60, 60), 150);
        deleteButton.Click += async (_, _) =>
        {
            var id = _deleteId.Text.Trim();
            if (id.Length == 0) { MessageBox.Show(this, "Nhập mã sinh viên."); return; }
            if (MessageBox.Show(this, $"Bạn chắc chắn muốn xóa {id}?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            await RunUiAsync(async () =>
            {
                try
                {
                    var rows = await _repository.DeleteStudentAsync(id);
                    Log(rows == 1 ? $"[Bài 8 / ExecuteNonQuery] Đã xóa {id}" : $"[Bài 8 / ExecuteNonQuery] Không tìm thấy {id}");
                }
                catch (SqlException ex) when (ex.Number == 547)
                {
                    Log($"[Bài 8 / FK] Không thể xóa {id} vì sinh viên đang có đăng ký học phần.");
                    MessageBox.Show(this, "Không thể xóa vì sinh viên đang được tham chiếu trong bảng Đăng ký học phần.", "Vi phạm khóa ngoại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                await LoadStudentsAsync();
            });
        };
        deleteFlow.Controls.Add(_deleteId);
        deleteFlow.Controls.Add(deleteButton);
        delete.Controls.Add(deleteFlow);
        layout.Controls.Add(delete);

        var output = TaskCard(page, "9. Stored Procedure thêm sinh viên + OUTPUT");
        var outputFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), WrapContents = false };
        var outputButton = MakeButton("Nhập thông tin và gọi SP", Color.FromArgb(16, 129, 102), 205);
        outputButton.Click += async (_, _) =>
        {
            var student = await PromptStudentAsync();
            if (student is null) return;
            await RunUiAsync(async () =>
            {
                var (rows, total) = await _repository.AddStudentWithOutputAsync(student);
                Log($"[Bài 9 / OUTPUT] Stored Procedure thêm {rows} dòng; @TongSinhVien={total}");
                MessageBox.Show(this, $"Đã thêm sinh viên. Tổng hiện có: {total}.", "Tham số OUTPUT", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadStudentsAsync();
            });
        };
        outputFlow.Controls.Add(outputButton);
        output.Controls.Add(outputFlow);
        layout.Controls.Add(output);

        var helper = TaskCard(page, "10. SqlHelper tối giản - Scalar / NonQuery / Reader, tự giải phóng tài nguyên", 94);
        var helperLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(8) };
        helperLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        helperLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var helperButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        var helperRun = MakeButton("Chạy cả 3 phương thức", Color.FromArgb(42, 102, 184), 210);
        helperRun.Click += async (_, _) => await RunUiAsync(async () =>
        {
            var count = await SqlHelper.ExecuteScalarAsync("SELECT COUNT(*) FROM dbo.SinhVien;");
            var changed = await SqlHelper.ExecuteNonQueryAsync(
                "SET NOCOUNT OFF; UPDATE dbo.SinhVien SET DiemTB=DiemTB WHERE MaSV=@MaSV;",
                p => p.Add("@MaSV", SqlDbType.VarChar, 15).Value = "SV001");
            var rows = await SqlHelper.ExecuteReaderAsync(
                "SELECT sv.MaSV, sv.HoTen, sv.NgaySinh, sv.GioiTinh, sv.MaKhoa, k.TenKhoa, sv.DiemTB, sv.Email " +
                "FROM dbo.SinhVien sv INNER JOIN dbo.Khoa k ON k.MaKhoa=sv.MaKhoa ORDER BY sv.MaSV;",
                MapStudent);
            Log($"[Bài 10 / SqlHelper] ExecuteScalar={count}; ExecuteNonQuery={changed}; ExecuteReader={rows.Count} dòng. Connection/Command/Reader đã được dispose trong helper.");
            ShowStudents(rows);
        });
        var helperNote = new Label
        {
            Text = "Mỗi hàm helper tự tạo connection/command; hàm Reader đọc và ánh xạ dữ liệu trước khi đóng reader, command và connection.",
            AutoSize = true,
            MaximumSize = new Size(850, 0),
            ForeColor = Color.DimGray,
            Padding = new Padding(4, 8, 0, 0)
        };
        helperButtons.Controls.Add(helperRun);
        helperLayout.Controls.Add(helperButtons, 0, 0);
        helperLayout.Controls.Add(helperNote, 0, 1);
        helper.Controls.Add(helperLayout);
        layout.Controls.Add(helper);

        page.Controls.Add(layout);
        return page;
    }

    private async Task PrepareDatabaseAsync()
    {
        await RunUiAsync(async () =>
        {
            var baseConnection = new SqlConnectionStringBuilder(DbConnectionFactory.GetConnectionString());
            var masterConnection = new SqlConnectionStringBuilder(baseConnection.ConnectionString) { InitialCatalog = "master" };
            var scripts = Path.Combine(AppContext.BaseDirectory, "Database");
            var setupBatches = await _scriptRunner.ExecuteAsync(masterConnection.ConnectionString, Path.Combine(scripts, "01_QLSinhVien.sql"));
            var procedureBatches = await _scriptRunner.ExecuteAsync(baseConnection.ConnectionString, Path.Combine(scripts, "02_StoredProcedures.sql"));
            Log($"[Chuẩn bị dữ liệu] Đã chạy {setupBatches} batch tạo/cập nhật database và {procedureBatches} batch stored procedure.");
            await LoadFacultiesAsync();
            await LoadStudentsAsync();
            _status.Text = "Đã chuẩn bị QLSinhVien và hai stored procedure.";
            _status.ForeColor = Color.FromArgb(16, 129, 102);
            MessageBox.Show(this, "Đã tạo/kiểm tra QLSinhVien, dữ liệu mẫu và stored procedure cho bài 5, 9.", "Lab08 sẵn sàng", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private async Task CheckConnectionAsync(bool showDialog)
    {
        await RunUiAsync(async () =>
        {
            await using var connection = DbConnectionFactory.Create();
            var timer = System.Diagnostics.Stopwatch.StartNew();
            await connection.OpenAsync();
            timer.Stop();
            var count = await _repository.CountStudentsAsync();
            _countResult.Text = $"{count} sinh viên";
            _status.Text = $"Kết nối OK | {connection.DataSource} / {connection.Database} | SQL {connection.ServerVersion} | {timer.ElapsedMilliseconds} ms";
            _status.ForeColor = Color.FromArgb(16, 129, 102);
            Log($"[Kết nối] State={connection.State}; DataSource={connection.DataSource}; Database={connection.Database}; ServerVersion={connection.ServerVersion}; {timer.ElapsedMilliseconds} ms; {count} sinh viên.");
            await LoadFacultiesAsync();
            await LoadStudentsAsync();
            if (showDialog)
                MessageBox.Show(this, $"Kết nối thành công. Database có {count} sinh viên.", "Kiểm tra kết nối", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private async Task LoadStudentsAsync()
    {
        ShowStudents(await _repository.GetStudentsAsync());
    }

    private async Task LoadFacultiesAsync()
    {
        _faculties = await SqlHelper.ExecuteReaderAsync(
            "SELECT MaKhoa, TenKhoa FROM dbo.Khoa ORDER BY TenKhoa;",
            reader => new FacultyOption(reader.GetString(0), reader.GetString(1)));
        BindFaculties(_facultyProcedure);
        BindFaculties(_facultyMax);
    }

    private void BindFaculties(ComboBox combo)
    {
        combo.DataSource = null;
        combo.DataSource = _faculties.ToList();
        combo.DisplayMember = nameof(FacultyOption.TenKhoa);
        combo.ValueMember = nameof(FacultyOption.MaKhoa);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    private static FacultyOption? SelectedFaculty(ComboBox combo) => combo.SelectedItem as FacultyOption;

    private async Task<StudentRecord?> PromptStudentAsync()
    {
        if (_faculties.Count == 0)
        {
            MessageBox.Show(this, "Hãy bấm 'Tạo CSDL + Stored Procedure' để tải danh sách khoa trước.", "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }
        using var dialog = new StudentDialog(_faculties);
        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Student : null;
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.BackgroundColor = Color.White;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.Columns.Add(TextColumn(nameof(StudentRecord.MaSV), "Mã SV", 20));
        _grid.Columns.Add(TextColumn(nameof(StudentRecord.HoTen), "Họ tên", 40));
        _grid.Columns.Add(TextColumn(nameof(StudentRecord.DiemTB), "Điểm TB", 18, "N2"));
        _grid.Columns.Add(TextColumn(nameof(StudentRecord.TenKhoa), "Khoa", 30));
    }

    private static DataGridViewTextBoxColumn TextColumn(string property, string title, float weight, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn { DataPropertyName = property, HeaderText = title, FillWeight = weight };
        if (format is not null) column.DefaultCellStyle.Format = format;
        return column;
    }

    private void ShowStudents(IEnumerable<StudentRecord> students)
    {
        _grid.DataSource = students.ToList();
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

    private async Task RunUiAsync(Func<Task> action)
    {
        UseWaitCursor = true;
        try { await action(); }
        catch (SqlException ex)
        {
            Log($"[SQL ERROR {ex.Number}] {ex.Message}");
            var friendly = ex.Number is 2601 or 2627
                ? "Mã sinh viên hoặc Email đã tồn tại. Hãy nhập giá trị khác."
                : ex.Number == 547
                    ? "Thao tác vi phạm ràng buộc khóa ngoại hoặc CHECK trong database."
                    : ex.Message;
            MessageBox.Show(this, friendly, "Lỗi SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            Log($"[ERROR] {ex.Message}");
            MessageBox.Show(this, ex.Message, "Không thể thực hiện thao tác", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { UseWaitCursor = false; }
    }

    private void Log(string message)
    {
        _log.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    private static GroupBox TaskCard(TabPage page, string title, int height = 80)
    {
        var card = new GroupBox
        {
            Text = title,
            Height = height,
            Width = Math.Max(760, page.ClientSize.Width - 48),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(35, 55, 82),
            Padding = new Padding(10),
            Margin = new Padding(6)
        };
        page.Resize += (_, _) => card.Width = Math.Max(760, page.ClientSize.Width - 48);
        return card;
    }

    private static Button MakeButton(string text, Color color, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 34,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(4)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static void ConfigureCombo(ComboBox combo, int width)
    {
        combo.Width = width;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.Margin = new Padding(5, 5, 8, 5);
    }

    private sealed record FacultyOption(string MaKhoa, string TenKhoa)
    {
        public override string ToString() => TenKhoa;
    }

    private sealed class StudentDialog : Form
    {
        private readonly TextBox _id = new() { Text = $"SV{DateTime.Now:MMddHHmm}" };
        private readonly TextBox _name = new() { Text = "Sinh viên demo Lab08" };
        private readonly DateTimePicker _birth = new() { Value = new DateTime(2005, 1, 1), Format = DateTimePickerFormat.Short };
        private readonly ComboBox _gender = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _faculty = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown _score = new() { Minimum = 0, Maximum = 10, DecimalPlaces = 2, Value = 8.25M, Increment = 0.25M };
        private readonly TextBox _email = new();

        public StudentRecord? Student { get; private set; }

        public StudentDialog(IReadOnlyList<FacultyOption> faculties)
        {
            Text = "Nhập thông tin sinh viên";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(470, 360);
            Font = new Font("Segoe UI", 9.5F);
            _gender.Items.AddRange(["Nam", "Nữ"]);
            _gender.SelectedIndex = 0;
            _faculty.DataSource = faculties.ToList();
            _faculty.DisplayMember = nameof(FacultyOption.TenKhoa);
            _faculty.ValueMember = nameof(FacultyOption.MaKhoa);
            _score.Width = 180;
            _email.PlaceholderText = "Email (có thể để trống)";

            var fields = new (string Caption, Control Input)[]
            {
                ("Mã sinh viên", _id), ("Họ tên", _name), ("Ngày sinh", _birth),
                ("Giới tính", _gender), ("Khoa", _faculty), ("Điểm trung bình", _score), ("Email", _email)
            };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = fields.Length + 1 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var row = 0;
            foreach (var field in fields)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
                layout.Controls.Add(new Label { Text = field.Caption, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
                field.Input.Dock = DockStyle.Fill;
                layout.Controls.Add(field.Input, 1, row);
                row++;
            }
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var ok = new Button { Text = "Lưu", Width = 85 };
            ok.Click += (_, _) =>
            {
                var id = _id.Text.Trim();
                var name = _name.Text.Trim();
                if (id.Length is 0 or > 15 || name.Length is 0 or > 100)
                {
                    MessageBox.Show(this, "Mã SV cần 1-15 ký tự, họ tên cần 1-100 ký tự.", "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (_email.Text.Trim().Length > 100)
                {
                    MessageBox.Show(this, "Email tối đa 100 ký tự.", "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var selectedFaculty = _faculty.SelectedItem as FacultyOption;
                if (selectedFaculty is null) return;
                Student = new StudentRecord(id, name, _birth.Value.Date, _gender.SelectedIndex == 0,
                    selectedFaculty.MaKhoa, selectedFaculty.TenKhoa, _score.Value,
                    string.IsNullOrWhiteSpace(_email.Text) ? null : _email.Text.Trim());
                DialogResult = DialogResult.OK;
                Close();
            };
            var cancel = new Button { Text = "Hủy", Width = 85, DialogResult = DialogResult.Cancel };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.Controls.Add(buttons, 0, fields.Length);
            layout.SetColumnSpan(buttons, 2);
            Controls.Add(layout);
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
