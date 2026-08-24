using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.LocalStorage.LocalData;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

public class MyAdapter : AdapterBase
{
    public List<T> FetchList<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }

    public T PostData<T>(string uri, Inventec.Common.WebApiClient.ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }
}

public class PatientItemDto
{
    public bool Selected { get; set; }
    public int Stt { get; set; }
    public string PatientCode { get; set; }
    public string TreatmentCode { get; set; }
    public long TreatmentId { get; set; }
    public string PatientName { get; set; }
    public string GenderName { get; set; }
    public string AgeStr { get; set; }
    public string BedRoomName { get; set; }
    public string BedName { get; set; }
    public string BedFull { get; set; }
    public long WorkingRoomId { get; set; }
    public long PatientTypeId { get; set; }
    public string PatientTypeName { get; set; }
    public string IcdCode { get; set; }
    public string IcdName { get; set; }
    public string IcdSubCode { get; set; }
    public string IcdText { get; set; }
    public string AssignedTimes { get; set; }
    public string ServiceReqCodes { get; set; }
    public string ExecutionStatus { get; set; }
    public int StatusCode { get; set; } // 0: Pending, 1: Success, 2: Error

    public PatientItemDto()
    {
        Selected = true;
        StatusCode = 0;
        ExecutionStatus = "⚪ Sẵn sàng";
        PatientTypeId = 1;
        PatientTypeName = "BHYT";
    }
}

public class RoomOption
{
    public long RoomId { get; set; }
    public string RoomName { get; set; }
    public string DisplayText { get; set; }

    public RoomOption(long id, string name, string display)
    {
        RoomId = id;
        RoomName = name;
        DisplayText = display;
    }

    public override string ToString()
    {
        return DisplayText;
    }
}

public class MainForm : Form
{
    public static BackendAdapter adapter;
    public static MyAdapter myAdapter = new MyAdapter();
    public static CommonParam param;
    public static string currentToken = null;

    public const long SERVICE_ID_BM02426 = 6217;
    public const string SERVICE_CODE_BM02426 = "BM02426";
    public const string SERVICE_NAME_BM02426 = "Xét nghiệm đường máu mao mạch tại giường (một lần)";

    // Data lists
    private List<PatientItemDto> allPatients = new List<PatientItemDto>();
    private List<PatientItemDto> filteredPatients = new List<PatientItemDto>();

    // UI Layout Controls
    private Panel pnlHeader;
    private Panel pnlLeftSidebar;
    private Panel pnlRightMain;
    private Panel pnlToolbar;
    private Panel pnlBottomActions;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatusText;
    private ToolStripStatusLabel lblCountInfo;
    private DataGridView dgvPatients;

    // Left Sidebar Controls (Configuration)
    private DateTimePicker dtpOrderDate;
    private CheckBox chkTime06;
    private CheckBox chkTime11;
    private CheckBox chkTime17;
    private CheckBox chkTime21;
    private CheckBox chkTimeNow;
    private CheckBox chkTimeCustom;
    private DateTimePicker dtpCustomTime;
    private ComboBox cboExecuteRoom;
    private TextBox txtOrderNote;
    private CheckBox chkAutoTracking;
    private Button btnExecuteAssign;
    private Button btnClearAll;

    // Top Toolbar Controls (Patient Input & Filters)
    private TextBox txtInputCodes;
    private Button btnLookupCodes;
    private Button btnLoadDept57;
    private Button btnSelectAll;
    private Button btnUnselectAll;
    private TextBox txtSearchFilter;
    private ComboBox cboFilterRoom;

    // Bottom Action Buttons
    private Button btnExportCsv;
    private Button btnCopyReport;

    public MainForm()
    {
        InitializeCustomComponents();
        this.Load += MainForm_Load;
    }

    private void InitializeCustomComponents()
    {
        this.Text = "HỆ THỐNG CHỈ ĐỊNH XÉT NGHIỆM ĐƯỜNG MÁU MAO MẠCH TẠI GIƯỜNG (BM02426) - KHOA CTCH & CỘT SỐNG";
        this.Size = new Size(1380, 800);
        this.MinimumSize = new Size(1100, 650);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        this.BackColor = Color.FromArgb(243, 244, 246);

        try
        {
            if (File.Exists("APP.ico")) this.Icon = new Icon("APP.ico");
        }
        catch { }

        BuildHeaderPanel();
        BuildLeftSidebar();
        BuildRightMainPanel();
        BuildStatusStrip();

        this.Controls.Add(pnlRightMain);
        this.Controls.Add(pnlLeftSidebar);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(statusStrip);
    }

    private void BuildHeaderPanel()
    {
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.FromArgb(30, 58, 138), // Deep Blue Navy
            Padding = new Padding(16, 8, 16, 8)
        };

        Label lblTitle = new Label
        {
            Text = "🩸 CHỈ ĐỊNH ĐƯỜNG MÁU MAO MẠCH TẠI GIƯỜNG (BM02426) - KHOA CTCH & CỘT SỐNG (KHOA 57)",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Label lblBadge = new Label
        {
            Text = "BS. NGUYỄN HỮU SÂM / BS. VŨ MINH CƯỜNG | MOS 2.390",
            ForeColor = Color.FromArgb(191, 219, 254),
            Font = new Font("Segoe UI", 9.0f, FontStyle.Italic),
            Dock = DockStyle.Right,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblBadge);
    }

    private void BuildLeftSidebar()
    {
        pnlLeftSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 360,
            BackColor = Color.White,
            Padding = new Padding(12),
            BorderStyle = BorderStyle.FixedSingle
        };

        Label lblSidebarTitle = new Label
        {
            Text = "⚙️ THIẾT LẬP Y LỆNH BM02426",
            Font = new Font("Segoe UI", 11.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 138),
            Dock = DockStyle.Top,
            Height = 30
        };

        // Panel for scrollable sidebar content
        Panel pnlScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        int curY = 6;

        // Service Info Box
        Panel pnlServiceInfo = new Panel
        {
            Location = new Point(4, curY),
            Size = new Size(328, 54),
            BackColor = Color.FromArgb(239, 246, 255),
            BorderStyle = BorderStyle.FixedSingle
        };
        Label lblServiceCode = new Label
        {
            Text = "Mã DV: BM02426 (ID: 6217) - Xét nghiệm",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(29, 78, 216),
            Location = new Point(6, 4),
            AutoSize = true
        };
        Label lblServiceName = new Label
        {
            Text = "Đường máu mao mạch tại giường (1 lần)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(31, 41, 55),
            Location = new Point(6, 26),
            AutoSize = true
        };
        pnlServiceInfo.Controls.Add(lblServiceCode);
        pnlServiceInfo.Controls.Add(lblServiceName);
        pnlScroll.Controls.Add(pnlServiceInfo);
        curY += 62;

        // Order Date
        Label lblDateTitle = new Label { Text = "📅 Ngày chỉ định y lệnh:", Location = new Point(4, curY), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        pnlScroll.Controls.Add(lblDateTitle);
        curY += 24;

        dtpOrderDate = new DateTimePicker
        {
            Location = new Point(4, curY),
            Size = new Size(328, 26),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy (dddd)",
            Value = DateTime.Today,
            Font = new Font("Segoe UI", 9.5f)
        };
        pnlScroll.Controls.Add(dtpOrderDate);
        curY += 34;

        // Time Selection Group
        GroupBox grpTime = new GroupBox
        {
            Text = "🕒 Chọn mốc giờ thực hiện (Multi-Slots):",
            Location = new Point(4, curY),
            Size = new Size(328, 175),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39)
        };

        chkTime06 = new CheckBox { Text = "🌅 06:00 (Sáng trước ăn)", Location = new Point(12, 24), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
        chkTime11 = new CheckBox { Text = "☀️ 11:00 (Trưa trước ăn)", Location = new Point(12, 48), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
        chkTime17 = new CheckBox { Text = "🌇 17:00 (Chiều trước ăn)", Location = new Point(12, 72), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
        chkTime21 = new CheckBox { Text = "🌙 21:00 (Tối trước ngủ)", Location = new Point(12, 96), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
        chkTimeNow = new CheckBox { Text = "⚡ Ngay bây giờ (" + DateTime.Now.ToString("HH:mm") + ")", Location = new Point(12, 120), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };

        chkTimeCustom = new CheckBox { Text = "🕒 Giờ tùy chỉnh:", Location = new Point(12, 144), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Regular) };
        dtpCustomTime = new DateTimePicker
        {
            Location = new Point(160, 142),
            Size = new Size(90, 24),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Value = DateTime.Now,
            Font = new Font("Segoe UI", 9.0f)
        };

        chkTimeCustom.CheckedChanged += (s, e) => { dtpCustomTime.Enabled = chkTimeCustom.Checked; };
        dtpCustomTime.Enabled = false;

        grpTime.Controls.Add(chkTime06);
        grpTime.Controls.Add(chkTime11);
        grpTime.Controls.Add(chkTime17);
        grpTime.Controls.Add(chkTime21);
        grpTime.Controls.Add(chkTimeNow);
        grpTime.Controls.Add(chkTimeCustom);
        grpTime.Controls.Add(dtpCustomTime);
        pnlScroll.Controls.Add(grpTime);
        curY += 184;

        // Execution Room Dropdown
        Label lblRoomTitle = new Label { Text = "🏥 Phòng thực hiện chỉ định:", Location = new Point(4, curY), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        pnlScroll.Controls.Add(lblRoomTitle);
        curY += 24;

        cboExecuteRoom = new ComboBox
        {
            Location = new Point(4, curY),
            Size = new Size(328, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.0f)
        };
        cboExecuteRoom.Items.Add(new RoomOption(931, "Q.T7TP", "Phòng Tiểu Phẫu Tầng 7 Nhà Q (Khoa CTCH & CS)"));
        cboExecuteRoom.Items.Add(new RoomOption(531, "P289", "P289 - Khoa CTCH & Cột Sống"));
        cboExecuteRoom.Items.Add(new RoomOption(533, "P291", "P291 - Khoa CTCH & Cột Sống"));
        cboExecuteRoom.Items.Add(new RoomOption(410, "P168", "Phòng Xét Nghiệm Sinh Hóa (P168)"));
        cboExecuteRoom.SelectedIndex = 0;
        pnlScroll.Controls.Add(cboExecuteRoom);
        curY += 34;

        // Order Note
        Label lblNoteTitle = new Label { Text = "📝 Ghi chú y lệnh / Lời dặn:", Location = new Point(4, curY), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        pnlScroll.Controls.Add(lblNoteTitle);
        curY += 24;

        txtOrderNote = new TextBox
        {
            Location = new Point(4, curY),
            Size = new Size(328, 24),
            Text = "Đo ĐMMM theo dõi đường huyết",
            Font = new Font("Segoe UI", 9.5f)
        };
        pnlScroll.Controls.Add(txtOrderNote);
        curY += 34;

        // Tracking Integration Checkbox
        chkAutoTracking = new CheckBox
        {
            Text = "🔗 Tự động liên kết / Tạo Tờ điều trị (EMR)",
            Location = new Point(4, curY),
            Size = new Size(328, 24),
            Checked = true,
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 185, 129) // Emerald Green
        };
        pnlScroll.Controls.Add(chkAutoTracking);
        curY += 34;

        // Action Buttons
        btnExecuteAssign = new Button
        {
            Text = "🚀 THỰC HIỆN CHỈ ĐỊNH BM02426",
            Location = new Point(4, curY),
            Size = new Size(328, 44),
            BackColor = Color.FromArgb(2, 132, 199), // Medical Bright Blue
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnExecuteAssign.FlatAppearance.BorderSize = 0;
        btnExecuteAssign.Click += async (s, e) => await ExecuteAssignAsync();
        pnlScroll.Controls.Add(btnExecuteAssign);
        curY += 50;

        btnClearAll = new Button
        {
            Text = "🧹 Xóa trắng danh sách",
            Location = new Point(4, curY),
            Size = new Size(328, 30),
            BackColor = Color.FromArgb(229, 231, 235),
            ForeColor = Color.FromArgb(55, 65, 81),
            Font = new Font("Segoe UI", 9.0f),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClearAll.FlatAppearance.BorderSize = 0;
        btnClearAll.Click += (s, e) => ClearPatientList();
        pnlScroll.Controls.Add(btnClearAll);

        pnlLeftSidebar.Controls.Add(pnlScroll);
        pnlLeftSidebar.Controls.Add(lblSidebarTitle);
    }

    private void BuildRightMainPanel()
    {
        pnlRightMain = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(243, 244, 246)
        };

        // 1. Top Toolbar (Patient Input & Filters)
        pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Color.White,
            Padding = new Padding(12, 10, 12, 10),
            BorderStyle = BorderStyle.FixedSingle
        };

        // Row 1: Fast Input Codes
        Label lblInputPrompt = new Label
        {
            Text = "📋 Nhập / Dán Mã Bệnh Nhân (hoặc Mã Điều Trị) [Ctrl+V từ Excel]:",
            Location = new Point(12, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 138)
        };

        txtInputCodes = new TextBox
        {
            Location = new Point(12, 34),
            Width = 460,
            Height = 26,
            Font = new Font("Segoe UI", 10.0f)
        };
        txtInputCodes.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnLookupCodes.PerformClick();
            }
        };

        btnLookupCodes = new Button
        {
            Text = "🔍 Tra Cứu & Thêm BN",
            Location = new Point(480, 32),
            Width = 160,
            Height = 30,
            BackColor = Color.FromArgb(16, 185, 129), // Green
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLookupCodes.FlatAppearance.BorderSize = 0;
        btnLookupCodes.Click += async (s, e) => await LookupAndAddPatientsAsync();

        btnLoadDept57 = new Button
        {
            Text = "🏥 Tải Toàn Bộ BN Nội Trú Khoa 57",
            Location = new Point(650, 32),
            Width = 240,
            Height = 30,
            BackColor = Color.FromArgb(79, 70, 229), // Indigo
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLoadDept57.FlatAppearance.BorderSize = 0;
        btnLoadDept57.Click += async (s, e) => await LoadAllDept57PatientsAsync();

        // Row 2: Search & Filter Controls
        Label lblSearch = new Label { Text = "🔍 Lọc BN:", Location = new Point(12, 74), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
        txtSearchFilter = new TextBox
        {
            Location = new Point(80, 71),
            Width = 220,
            Font = new Font("Segoe UI", 9.0f)
        };
        txtSearchFilter.TextChanged += (s, e) => ApplyFilter();

        Label lblRoomFilter = new Label { Text = "Buồng:", Location = new Point(315, 74), AutoSize = true, Font = new Font("Segoe UI", 9.0f) };
        cboFilterRoom = new ComboBox
        {
            Location = new Point(365, 71),
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.0f)
        };
        cboFilterRoom.Items.Add("-- Tất cả buồng --");
        cboFilterRoom.SelectedIndex = 0;
        cboFilterRoom.SelectedIndexChanged += (s, e) => ApplyFilter();

        btnSelectAll = new Button
        {
            Text = "☑ Chọn tất cả",
            Location = new Point(520, 70),
            Width = 100,
            Height = 26,
            BackColor = Color.FromArgb(243, 244, 246),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btnSelectAll.FlatAppearance.BorderSize = 0;
        btnSelectAll.Click += (s, e) => SetSelectionAll(true);

        btnUnselectAll = new Button
        {
            Text = "☐ Bỏ chọn tất cả",
            Location = new Point(626, 70),
            Width = 110,
            Height = 26,
            BackColor = Color.FromArgb(243, 244, 246),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btnUnselectAll.FlatAppearance.BorderSize = 0;
        btnUnselectAll.Click += (s, e) => SetSelectionAll(false);

        pnlToolbar.Controls.Add(lblInputPrompt);
        pnlToolbar.Controls.Add(txtInputCodes);
        pnlToolbar.Controls.Add(btnLookupCodes);
        pnlToolbar.Controls.Add(btnLoadDept57);
        pnlToolbar.Controls.Add(lblSearch);
        pnlToolbar.Controls.Add(txtSearchFilter);
        pnlToolbar.Controls.Add(lblRoomFilter);
        pnlToolbar.Controls.Add(cboFilterRoom);
        pnlToolbar.Controls.Add(btnSelectAll);
        pnlToolbar.Controls.Add(btnUnselectAll);

        // 2. DataGridView for Patients
        dgvPatients = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            AutoGenerateColumns = false,
            Font = new Font("Segoe UI", 9.0f)
        };
        dgvPatients.EnableHeadersVisualStyles = false;
        dgvPatients.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        dgvPatients.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
        dgvPatients.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
        dgvPatients.ColumnHeadersHeight = 32;
        dgvPatients.RowTemplate.Height = 28;
        dgvPatients.CellFormatting += DgvPatients_CellFormatting;

        BuildGridColumns();

        // 3. Bottom Actions Panel
        pnlBottomActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = Color.White,
            Padding = new Padding(12, 6, 12, 6),
            BorderStyle = BorderStyle.FixedSingle
        };

        btnExportCsv = new Button
        {
            Text = "📊 Xuất Excel (CSV)",
            Location = new Point(12, 6),
            Width = 140,
            Height = 30,
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.0f),
            Cursor = Cursors.Hand
        };
        btnExportCsv.FlatAppearance.BorderSize = 0;
        btnExportCsv.Click += (s, e) => ExportToCsv();

        btnCopyReport = new Button
        {
            Text = "📋 Copy Báo Cáo Kết Quả",
            Location = new Point(160, 6),
            Width = 180,
            Height = 30,
            BackColor = Color.FromArgb(75, 85, 99),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.0f),
            Cursor = Cursors.Hand
        };
        btnCopyReport.FlatAppearance.BorderSize = 0;
        btnCopyReport.Click += (s, e) => CopyReportToClipboard();

        pnlBottomActions.Controls.Add(btnExportCsv);
        pnlBottomActions.Controls.Add(btnCopyReport);

        pnlRightMain.Controls.Add(dgvPatients);
        pnlRightMain.Controls.Add(pnlBottomActions);
        pnlRightMain.Controls.Add(pnlToolbar);
    }

    private void BuildGridColumns()
    {
        dgvPatients.Columns.Clear();

        DataGridViewCheckBoxColumn colSel = new DataGridViewCheckBoxColumn
        {
            DataPropertyName = "Selected",
            HeaderText = "Chọn",
            Width = 45
        };
        dgvPatients.Columns.Add(colSel);

        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Stt", HeaderText = "STT", Width = 40, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BedFull", HeaderText = "Buồng / Giường", Width = 135, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PatientCode", HeaderText = "Mã BN", Width = 95, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PatientName", HeaderText = "Họ và Tên Bệnh Nhân", Width = 160, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GenderName", HeaderText = "Giới", Width = 45, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AgeStr", HeaderText = "Tuổi", Width = 45, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PatientTypeName", HeaderText = "Đối Tượng", Width = 80, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IcdText", HeaderText = "Chẩn Đoán Bệnh Án", Width = 200, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AssignedTimes", HeaderText = "Mốc Giờ Đã Chọn", Width = 130, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ServiceReqCodes", HeaderText = "Mã Y Lệnh BM02426", Width = 130, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ExecutionStatus", HeaderText = "Trạng Thái Chỉ Định", Width = 160, ReadOnly = true });
    }

    private void DgvPatients_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= filteredPatients.Count) return;
        var p = filteredPatients[e.RowIndex];

        if (dgvPatients.Columns[e.ColumnIndex].DataPropertyName == "ExecutionStatus")
        {
            if (p.StatusCode == 1) // Success
            {
                e.CellStyle.ForeColor = Color.FromArgb(22, 163, 74); // Green
                e.CellStyle.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            }
            else if (p.StatusCode == 2) // Error
            {
                e.CellStyle.ForeColor = Color.FromArgb(220, 38, 38); // Red
                e.CellStyle.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            }
        }
        else if (dgvPatients.Columns[e.ColumnIndex].DataPropertyName == "PatientName")
        {
            e.CellStyle.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
        }
    }

    private void BuildStatusStrip()
    {
        statusStrip = new StatusStrip();
        lblStatusText = new ToolStripStatusLabel
        {
            Text = "Sẵn sàng nhập mã bệnh nhân và chọn mốc giờ.",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        lblCountInfo = new ToolStripStatusLabel
        {
            Text = "0 bệnh nhân (0 đã chọn)",
            TextAlign = ContentAlignment.MiddleRight
        };

        statusStrip.Items.Add(lblStatusText);
        statusStrip.Items.Add(lblCountInfo);
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        lblStatusText.Text = "Đang kiểm tra kết nối hệ thống HIS / MOS...";
        await Task.Run(() =>
        {
            try
            {
                InitSession();
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    lblStatusText.Text = "Lỗi kết nối: " + ex.Message;
                }));
            }
        });
        lblStatusText.Text = "✔ Kết nối HIS thành công! Sẵn sàng chỉ định BM02426.";
    }

    private List<string> GetSelectedTimeSlots()
    {
        List<string> slots = new List<string>();
        if (chkTime06.Checked) slots.Add("06:00");
        if (chkTime11.Checked) slots.Add("11:00");
        if (chkTime17.Checked) slots.Add("17:00");
        if (chkTime21.Checked) slots.Add("21:00");
        if (chkTimeNow.Checked) slots.Add(DateTime.Now.ToString("HH:mm"));
        if (chkTimeCustom.Checked) slots.Add(dtpCustomTime.Value.ToString("HH:mm"));

        return slots.Distinct().OrderBy(t => t).ToList();
    }

    private void SetSelectionAll(bool select)
    {
        foreach (var p in filteredPatients)
        {
            p.Selected = select;
        }
        dgvPatients.Refresh();
        UpdateKpiCounts();
    }

    private void ClearPatientList()
    {
        allPatients.Clear();
        filteredPatients.Clear();
        dgvPatients.DataSource = null;
        cboFilterRoom.Items.Clear();
        cboFilterRoom.Items.Add("-- Tất cả buồng --");
        cboFilterRoom.SelectedIndex = 0;
        UpdateKpiCounts();
        lblStatusText.Text = "Đã xóa trắng danh sách bệnh nhân.";
    }

    private void ApplyFilter()
    {
        string query = (txtSearchFilter.Text ?? "").Trim().ToLower();
        string roomFilter = cboFilterRoom.SelectedItem != null ? cboFilterRoom.SelectedItem.ToString() : "";

        var q = allPatients.AsEnumerable();

        if (!string.IsNullOrEmpty(query))
        {
            q = q.Where(p =>
                (!string.IsNullOrEmpty(p.PatientName) && p.PatientName.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(p.PatientCode) && p.PatientCode.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(p.TreatmentCode) && p.TreatmentCode.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(p.BedFull) && p.BedFull.ToLower().Contains(query)));
        }

        if (!string.IsNullOrEmpty(roomFilter) && !roomFilter.Contains("Tất cả"))
        {
            q = q.Where(p => p.BedRoomName == roomFilter);
        }

        filteredPatients = q.OrderBy(p => p.BedFull).ThenBy(p => p.PatientName).ToList();
        for (int i = 0; i < filteredPatients.Count; i++)
        {
            filteredPatients[i].Stt = i + 1;
        }

        dgvPatients.DataSource = null;
        dgvPatients.DataSource = filteredPatients;
        UpdateKpiCounts();
    }

    private void UpdateKpiCounts()
    {
        int total = allPatients.Count;
        int selected = allPatients.Count(p => p.Selected);
        lblCountInfo.Text = string.Format("{0} bệnh nhân ({1} đã chọn)", total, selected);
    }

    private async Task LookupAndAddPatientsAsync()
    {
        string raw = (txtInputCodes.Text ?? "").Trim();
        if (string.IsNullOrEmpty(raw))
        {
            MessageBox.Show("Vui lòng nhập hoặc dán ít nhất 1 mã bệnh nhân hoặc mã điều trị!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var codes = raw.Split(new char[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(c => c.Trim())
                       .Where(c => c.Length >= 6)
                       .Distinct()
                       .ToList();

        if (codes.Count == 0)
        {
            MessageBox.Show("Không tìm thấy mã bệnh nhân hợp lệ!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnLookupCodes.Enabled = false;
        lblStatusText.Text = string.Format("Đang tra cứu {0} bệnh nhân từ hệ thống HIS...", codes.Count);

        try
        {
            var addedList = await Task.Run(() =>
            {
                InitSession();
                List<PatientItemDto> results = new List<PatientItemDto>();

                foreach (var c in codes)
                {
                    var p = LookupSinglePatient(c);
                    if (p != null)
                    {
                        results.Add(p);
                    }
                }
                return results;
            });

            foreach (var item in addedList)
            {
                if (!allPatients.Any(p => p.PatientCode == item.PatientCode || p.TreatmentId == item.TreatmentId))
                {
                    allPatients.Add(item);
                }
            }

            // Update room dropdown
            UpdateRoomFilterDropdown();
            ApplyFilter();
            txtInputCodes.Clear();
            lblStatusText.Text = string.Format("✔ Đã nạp thành công {0} bệnh nhân.", addedList.Count);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi tra cứu bệnh nhân:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatusText.Text = "Lỗi: " + ex.Message;
        }
        finally
        {
            btnLookupCodes.Enabled = true;
        }
    }

    private async Task LoadAllDept57PatientsAsync()
    {
        btnLoadDept57.Enabled = false;
        lblStatusText.Text = "Đang tải danh sách toàn bộ bệnh nhân nội trú Khoa CTCH & Cột Sống (Khoa 57)...";

        try
        {
            var inPatients = await Task.Run(() =>
            {
                InitSession();
                return FetchInPatientsKhoa57();
            });

            foreach (var item in inPatients)
            {
                if (!allPatients.Any(p => p.TreatmentId == item.TreatmentId))
                {
                    allPatients.Add(item);
                }
            }

            UpdateRoomFilterDropdown();
            ApplyFilter();
            lblStatusText.Text = string.Format("✔ Đã tải {0} bệnh nhân nội trú Khoa 57.", inPatients.Count);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi tải bệnh nhân nội trú Khoa 57:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatusText.Text = "Lỗi: " + ex.Message;
        }
        finally
        {
            btnLoadDept57.Enabled = true;
        }
    }

    private void UpdateRoomFilterDropdown()
    {
        var rooms = allPatients.Select(p => p.BedRoomName).Where(r => !string.IsNullOrEmpty(r)).Distinct().OrderBy(r => r).ToList();
        string current = cboFilterRoom.SelectedItem != null ? cboFilterRoom.SelectedItem.ToString() : "-- Tất cả buồng --";
        cboFilterRoom.Items.Clear();
        cboFilterRoom.Items.Add("-- Tất cả buồng --");
        foreach (var r in rooms) cboFilterRoom.Items.Add(r);
        if (cboFilterRoom.Items.Contains(current)) cboFilterRoom.SelectedItem = current;
        else cboFilterRoom.SelectedIndex = 0;
    }

    private async Task ExecuteAssignAsync()
    {
        var selectedPatients = allPatients.Where(p => p.Selected).ToList();
        if (selectedPatients.Count == 0)
        {
            MessageBox.Show("Vui lòng tích chọn ít nhất 1 bệnh nhân trong danh sách!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var timeSlots = GetSelectedTimeSlots();
        if (timeSlots.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn ít nhất 1 mốc giờ thực hiện (VD: 06:00, 11:00, 17:00, 21:00 hoặc Tùy chỉnh)!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RoomOption roomOpt = cboExecuteRoom.SelectedItem as RoomOption;
        long executeRoomId = roomOpt != null ? roomOpt.RoomId : 931;
        DateTime orderDate = dtpOrderDate.Value.Date;
        string note = (txtOrderNote.Text ?? "").Trim();
        bool autoTracking = chkAutoTracking.Checked;

        string confirmMsg = string.Format(
            "XÁC NHẬN CHỈ ĐỊNH BM02426:\n\n" +
            "• Số bệnh nhân: {0} BN\n" +
            "• Ngày chỉ định: {1}\n" +
            "• Các mốc giờ: {2} ({3} y lệnh / BN)\n" +
            "• Tổng số y lệnh sẽ tạo: {4}\n" +
            "• Phòng thực hiện: {5}\n\n" +
            "Bạn có chắc chắn muốn tiến hành chỉ định trên HIS/MOS?",
            selectedPatients.Count,
            orderDate.ToString("dd/MM/yyyy"),
            string.Join(", ", timeSlots),
            timeSlots.Count,
            selectedPatients.Count * timeSlots.Count,
            roomOpt != null ? roomOpt.DisplayText : "Phòng Tiểu Phẫu Nhà Q");

        if (MessageBox.Show(confirmMsg, "Xác nhận chỉ định cận lâm sàng", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        btnExecuteAssign.Enabled = false;
        lblStatusText.Text = "Đang tiến hành chỉ định BM02426 trên máy chủ HIS...";

        try
        {
            int totalSuccess = 0;
            int totalFail = 0;

            await Task.Run(() =>
            {
                InitSession();

                foreach (var p in selectedPatients)
                {
                    List<string> successCodes = new List<string>();
                    List<string> errorMsgs = new List<string>();

                    foreach (var slot in timeSlots)
                    {
                        string[] parts = slot.Split(':');
                        int hour = int.Parse(parts[0]);
                        int minute = int.Parse(parts[1]);
                        DateTime fullTime = new DateTime(orderDate.Year, orderDate.Month, orderDate.Day, hour, minute, 0);
                        long instructionTimeNum = long.Parse(fullTime.ToString("yyyyMMddHHmmss"));

                        try
                        {
                            string reqCode = AssignSinglePatientService(p, instructionTimeNum, slot, executeRoomId, note, autoTracking);
                            successCodes.Add(string.Format("{0} ({1})", reqCode, slot));
                            totalSuccess++;
                        }
                        catch (Exception ex)
                        {
                            errorMsgs.Add(string.Format("{0}: {1}", slot, ex.Message));
                            totalFail++;
                        }
                    }

                    this.Invoke(new Action(() =>
                    {
                        p.AssignedTimes = string.Join(", ", timeSlots);
                        if (errorMsgs.Count == 0)
                        {
                            p.StatusCode = 1;
                            p.ServiceReqCodes = string.Join(", ", successCodes);
                            p.ExecutionStatus = string.Format("🟢 Thành công ({0} YL)", successCodes.Count);
                        }
                        else if (successCodes.Count > 0)
                        {
                            p.StatusCode = 1;
                            p.ServiceReqCodes = string.Join(", ", successCodes);
                            p.ExecutionStatus = string.Format("🟡 Xong {0} YL, Lỗi {1}", successCodes.Count, errorMsgs.Count);
                        }
                        else
                        {
                            p.StatusCode = 2;
                            p.ExecutionStatus = "🔴 Lỗi: " + string.Join("; ", errorMsgs);
                        }
                        dgvPatients.Refresh();
                    }));
                }
            });

            lblStatusText.Text = string.Format("✔ Hoàn tất: {0} thành công, {1} lỗi.", totalSuccess, totalFail);
            MessageBox.Show(string.Format("Đã hoàn tất chỉ định BM02426!\n\n• Thành công: {0} y lệnh\n• Lỗi: {1} y lệnh\n\nKiểm tra chi tiết trên bảng kết quả.", totalSuccess, totalFail), "Kết quả chỉ định", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi trong quá trình chỉ định:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatusText.Text = "Lỗi: " + ex.Message;
        }
        finally
        {
            btnExecuteAssign.Enabled = true;
        }
    }

    private void ExportToCsv()
    {
        if (filteredPatients.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SaveFileDialog sfd = new SaveFileDialog
        {
            Filter = "File Excel CSV (*.csv)|*.csv",
            FileName = string.Format("ChiDinh_BM02426_KhoaCTCH_{0}.csv", DateTime.Now.ToString("yyyyMMdd_HHmm"))
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("STT,Buồng Giường,Mã BN,Mã ĐT,Họ và Tên Bệnh Nhân,Giới tính,Tuổi,Đối Tượng,Chẩn Đoán,Mốc Giờ Chỉ Định,Mã Y Lệnh BM02426,Trạng Thái Thực Thi");

                foreach (var p in filteredPatients)
                {
                    sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\"",
                        p.Stt,
                        EscapeCsv(p.BedFull),
                        EscapeCsv(p.PatientCode),
                        EscapeCsv(p.TreatmentCode),
                        EscapeCsv(p.PatientName),
                        EscapeCsv(p.GenderName),
                        EscapeCsv(p.AgeStr),
                        EscapeCsv(p.PatientTypeName),
                        EscapeCsv(p.IcdText),
                        EscapeCsv(p.AssignedTimes),
                        EscapeCsv(p.ServiceReqCodes),
                        EscapeCsv(p.ExecutionStatus)));
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Đã xuất file thành công tại:\n" + sfd.FileName, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void CopyReportToClipboard()
    {
        if (filteredPatients.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("📋 BÁO CÁO CHỈ ĐỊNH ĐƯỜNG MÁU MAO MẠCH BM02426 - KHOA CTCH (LÚC {0})", DateTime.Now.ToString("HH:mm dd/MM/yyyy")));
            sb.AppendLine(string.Format("Ngày chỉ định: {0} | Mốc giờ: {1}", dtpOrderDate.Value.ToString("dd/MM/yyyy"), string.Join(", ", GetSelectedTimeSlots())));
            sb.AppendLine("----------------------------------------------------------------------------------");

            var groupedByRoom = filteredPatients.Where(p => p.Selected).GroupBy(p => p.BedRoomName).OrderBy(g => g.Key);
            foreach (var rg in groupedByRoom)
            {
                string rName = string.IsNullOrEmpty(rg.Key) ? "Chưa xếp buồng" : rg.Key;
                sb.AppendLine(string.Format("\n🏢 [{0}]:", rName));
                foreach (var p in rg)
                {
                    string icon = p.StatusCode == 1 ? "✔" : (p.StatusCode == 2 ? "❌" : "⚪");
                    sb.AppendLine(string.Format("  {0} {1} ({2}) - {3} | Y lệnh: {4}",
                        icon, p.PatientName, p.PatientCode, p.BedName, p.ServiceReqCodes ?? p.ExecutionStatus));
                }
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show("Đã sao chép báo cáo vào Clipboard!\nBạn có thể dán (Ctrl+V) vào Zalo / Viber / Word để bàn giao điều dưỡng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string EscapeCsv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\"", "\"\"");
    }

    // =========================================================================
    // BACKEND INTEGRATION METHODS
    // =========================================================================

    public static void InitSession()
    {
        if (!string.IsNullOrEmpty(currentToken)) return;

        HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
        ClientTokenManager tokenManager = new ClientTokenManager("HIS");
        param = new CommonParam();
        var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
        if (token != null)
        {
            currentToken = token.TokenCode;
            ApiConsumers.SetConsunmer(currentToken);
            adapter = new BackendAdapter(param);

            // Bind token session to working rooms on MOS backend
            var workInfo = new WorkInfoSDO
            {
                Rooms = new List<RoomSDO>
                {
                    new RoomSDO { RoomId = 5248 }, // Phòng 734 (Phòng trực CTCH)
                    new RoomSDO { RoomId = 5252 }, // Phòng 712
                    new RoomSDO { RoomId = 5251 }, // Phòng 714
                    new RoomSDO { RoomId = 5257 }, // Phòng 724
                    new RoomSDO { RoomId = 5255 }, // Phòng 722
                    new RoomSDO { RoomId = 5253 }, // Phòng 711
                    new RoomSDO { RoomId = 5259 }, // Phòng 740
                    new RoomSDO { RoomId = 5265 }  // Phòng 731
                }
            };
            var workPlaces = myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, param);
            HIS.Desktop.LocalStorage.LocalData.WorkPlace.WorkPlaceSDO = workPlaces;
            HIS.Desktop.LocalStorage.LocalData.WorkPlace.WorkInfoSDO = workInfo;
        }
        else
        {
            throw new Exception("Không thể xác thực tài khoản BS 'vmc' trên hệ thống HIS!");
        }
    }

    public static PatientItemDto LookupSinglePatient(string code)
    {
        InitSession();

        // 1. Try finding by PATIENT_CODE
        HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
        tf.PATIENT_CODE__EXACT = code;
        var trs = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);

        // 2. Fallback to TREATMENT_CODE
        if (trs == null || trs.Count == 0)
        {
            HisTreatmentViewFilter tf2 = new HisTreatmentViewFilter();
            tf2.TREATMENT_CODE__EXACT = code;
            trs = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf2, param);
        }

        if (trs == null || trs.Count == 0) return null;

        var tr = trs.OrderByDescending(t => t.ID).First();

        PatientItemDto item = new PatientItemDto
        {
            TreatmentId = tr.ID,
            TreatmentCode = tr.TREATMENT_CODE,
            PatientCode = tr.TDL_PATIENT_CODE,
            PatientName = tr.TDL_PATIENT_NAME,
            GenderName = tr.TDL_PATIENT_GENDER_NAME,
            AgeStr = tr.TDL_PATIENT_DOB > 0 ? (DateTime.Now.Year - int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4))).ToString() : "",
            PatientTypeId = tr.TDL_PATIENT_TYPE_ID.HasValue ? tr.TDL_PATIENT_TYPE_ID.Value : 1,
            PatientTypeName = (tr.TDL_PATIENT_TYPE_ID.HasValue && tr.TDL_PATIENT_TYPE_ID.Value == 1) ? "BHYT" : "Viện phí",
            IcdCode = tr.ICD_CODE,
            IcdName = tr.ICD_NAME,
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = !string.IsNullOrEmpty(tr.ICD_TEXT) ? tr.ICD_TEXT : tr.ICD_NAME,
            WorkingRoomId = 5248,
            BedRoomName = "Khoa CTCH",
            BedName = "Chưa rõ",
            BedFull = "Nội trú Khoa CTCH"
        };

        // Query bed room info
        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.TREATMENT_IDs = new List<long> { tr.ID };
        tbrf.IS_IN_ROOM = true;
        var beds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, param);
        if (beds != null && beds.Count > 0)
        {
            var b = beds[0];
            item.BedRoomName = b.BED_ROOM_NAME;
            item.BedName = b.BED_NAME;
            item.BedFull = string.Format("{0} - {1}", b.BED_ROOM_NAME, b.BED_NAME);

            // Lookup room id from BedRoom
            HisBedRoomViewFilter brf = new HisBedRoomViewFilter();
            brf.ID = b.BED_ROOM_ID;
            var bRooms = myAdapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, brf, param);
            if (bRooms != null && bRooms.Count > 0)
            {
                item.WorkingRoomId = bRooms[0].ROOM_ID;
            }
        }

        return item;
    }

    public static List<PatientItemDto> FetchInPatientsKhoa57()
    {
        InitSession();

        List<PatientItemDto> results = new List<PatientItemDto>();

        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.IS_IN_ROOM = true;
        tbrf.TREATMENT_IS_ACTIVE = true;
        var beds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, param);

        if (beds == null || beds.Count == 0) return results;

        var dept57Beds = beds.Where(b => b.DEPARTMENT_ID == 57).GroupBy(b => b.TREATMENT_ID).Select(g => g.First()).ToList();

        // Get treatments
        List<long> treatmentIds = dept57Beds.Select(b => b.TREATMENT_ID).ToList();
        Dictionary<long, V_HIS_TREATMENT> treatMap = new Dictionary<long, V_HIS_TREATMENT>();

        int batchSize = 100;
        for (int i = 0; i < treatmentIds.Count; i += batchSize)
        {
            var batch = treatmentIds.Skip(i).Take(batchSize).ToList();
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.IDs = batch;
            var trList = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
            if (trList != null)
            {
                foreach (var t in trList)
                {
                    if (!treatMap.ContainsKey(t.ID)) treatMap.Add(t.ID, t);
                }
            }
        }

        foreach (var b in dept57Beds)
        {
            V_HIS_TREATMENT tr = null;
            treatMap.TryGetValue(b.TREATMENT_ID, out tr);

            PatientItemDto item = new PatientItemDto
            {
                TreatmentId = b.TREATMENT_ID,
                TreatmentCode = tr != null ? tr.TREATMENT_CODE : b.TDL_PATIENT_CODE,
                PatientCode = b.TDL_PATIENT_CODE,
                PatientName = b.TDL_PATIENT_NAME,
                GenderName = b.TDL_PATIENT_GENDER_NAME,
                AgeStr = b.TDL_PATIENT_DOB > 0 ? (DateTime.Now.Year - int.Parse(b.TDL_PATIENT_DOB.ToString().Substring(0, 4))).ToString() : "",
                BedRoomName = b.BED_ROOM_NAME,
                BedName = b.BED_NAME,
                BedFull = string.Format("{0} - {1}", b.BED_ROOM_NAME, b.BED_NAME),
                PatientTypeId = (tr != null && tr.TDL_PATIENT_TYPE_ID.HasValue) ? tr.TDL_PATIENT_TYPE_ID.Value : 1,
                PatientTypeName = (tr != null && tr.TDL_PATIENT_TYPE_ID.HasValue && tr.TDL_PATIENT_TYPE_ID.Value == 1) ? "BHYT" : "Viện phí",
                IcdCode = tr != null ? tr.ICD_CODE : "M51.2",
                IcdName = tr != null ? tr.ICD_NAME : "Thoát vị đĩa đệm",
                IcdSubCode = tr != null ? tr.ICD_SUB_CODE : "",
                IcdText = tr != null ? (!string.IsNullOrEmpty(tr.ICD_TEXT) ? tr.ICD_TEXT : tr.ICD_NAME) : "",
                WorkingRoomId = 5248
            };

            results.Add(item);
        }

        return results.OrderBy(p => p.BedFull).ThenBy(p => p.PatientName).ToList();
    }

    public static string AssignSinglePatientService(PatientItemDto patient, long instructionTime, string slotTimeStr, long executeRoomId, string note, bool autoCreateTracking)
    {
        InitSession();

        long trackingId = 0;
        long trackingTime = instructionTime;
        HIS_TRACKING targetTracking = null;

        // 1. Find existing Tracking for patient
        HisTrackingFilter tf = new HisTrackingFilter();
        tf.TREATMENT_ID = patient.TreatmentId;
        var trackings = myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", ApiConsumers.MosConsumer, tf, param);

        if (trackings != null && trackings.Count > 0)
        {
            // Find tracking on the same day
            string dateStr = instructionTime.ToString().Substring(0, 8);
            var sameDay = trackings.Where(t => t.TRACKING_TIME.ToString().StartsWith(dateStr)).OrderByDescending(t => t.TRACKING_TIME).FirstOrDefault();
            if (sameDay != null)
            {
                targetTracking = sameDay;
                trackingId = sameDay.ID;
                trackingTime = sameDay.TRACKING_TIME;
            }
            else
            {
                var latest = trackings.OrderByDescending(t => t.TRACKING_TIME).First();
                targetTracking = latest;
                trackingId = latest.ID;
                trackingTime = latest.TRACKING_TIME;
            }
        }

        // If no tracking exists at all and autoCreate is enabled, create one
        if (trackingId == 0 && autoCreateTracking)
        {
            long nowNum = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
            long createTrackingTime = instructionTime >= nowNum ? instructionTime : nowNum;

            var newTracking = new HIS_TRACKING
            {
                TREATMENT_ID = patient.TreatmentId,
                DEPARTMENT_ID = 57,
                ROOM_ID = patient.WorkingRoomId > 0 ? patient.WorkingRoomId : 5248,
                TRACKING_TIME = createTrackingTime,
                CONTENT = "Bệnh nhân tỉnh táo, tiếp xúc tốt. Theo dõi chỉ số đường máu mao mạch.",
                MEDICAL_INSTRUCTION = "Xét nghiệm đường máu mao mạch tại giường (BM02426)",
                CARE_INSTRUCTION = "Chăm sóc cấp II. Theo dõi đường huyết.",
                ICD_CODE = patient.IcdCode,
                ICD_NAME = patient.IcdName,
                ICD_SUB_CODE = patient.IcdSubCode,
                ICD_TEXT = patient.IcdText
            };

            var sdo = new HisTrackingSDO
            {
                Tracking = newTracking,
                WorkingRoomId = patient.WorkingRoomId > 0 ? patient.WorkingRoomId : 5248
            };

            var created = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, param);
            if (created != null && created.ID > 0)
            {
                targetTracking = created;
                trackingId = created.ID;
                trackingTime = created.TRACKING_TIME;
            }
        }

        if (trackingId == 0)
        {
            throw new Exception("Không tìm thấy Tờ điều trị hợp lệ của bệnh nhân trong ngày!");
        }

        // 2. Prepare AssignServiceSDO
        long requestRoomId = patient.WorkingRoomId > 0 ? patient.WorkingRoomId : ((targetTracking != null && targetTracking.ROOM_ID.HasValue && targetTracking.ROOM_ID.Value > 0) ? targetTracking.ROOM_ID.Value : 5257);
        string instructionNote = string.Format("Đo ĐMMM lúc {0}{1}", slotTimeStr, !string.IsNullOrEmpty(note) ? " - " + note : "").Trim();

        AssignServiceSDO assignSDO = new AssignServiceSDO
        {
            TreatmentId = patient.TreatmentId,
            RequestRoomId = requestRoomId,
            RequestLoginName = "vmc",
            RequestUserName = "VŨ MINH CƯỜNG",
            InstructionTime = trackingTime,
            InstructionTimes = new List<long> { trackingTime },
            UseTimes = new List<long> { trackingTime },
            TrackingId = trackingId,
            TrackingInfos = new List<TrackingInfoSDO>
            {
                new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = trackingTime }
            },
            IcdCode = (targetTracking != null && !string.IsNullOrEmpty(targetTracking.ICD_CODE)) ? targetTracking.ICD_CODE : patient.IcdCode,
            IcdName = (targetTracking != null && !string.IsNullOrEmpty(targetTracking.ICD_NAME)) ? targetTracking.ICD_NAME : patient.IcdName,
            IcdSubCode = (targetTracking != null && !string.IsNullOrEmpty(targetTracking.ICD_SUB_CODE)) ? targetTracking.ICD_SUB_CODE : patient.IcdSubCode,
            IcdText = (targetTracking != null && !string.IsNullOrEmpty(targetTracking.ICD_TEXT)) ? targetTracking.ICD_TEXT : patient.IcdText,
            SessionCode = null,
            ServiceReqDetails = new List<ServiceReqDetailSDO>
            {
                new ServiceReqDetailSDO
                {
                    ServiceId = SERVICE_ID_BM02426,
                    Amount = 1.0m,
                    PatientTypeId = patient.PatientTypeId > 0 ? patient.PatientTypeId : 1,
                    PrimaryPatientTypeId = (patient.PatientTypeId == 1 ? (long?)null : patient.PatientTypeId),
                    RoomId = executeRoomId,
                    SampleTypeCode = "BP0042", // Bắt buộc cho dịch vụ xét nghiệm BM02426
                    InstructionNote = instructionNote,
                    MultipleExecute = 1,
                    IsNotUseBhyt = false,
                    IsNoHeinDifference = false,
                    IsGuaranteed = false,
                    EkipInfos = new List<EkipSDO>()
                }
            }
        };

        CommonParam postParam = new CommonParam();
        var res = myAdapter.PostData<HisServiceReqListResultSDO>("api/HisServiceReq/AssignServiceByInstructionTimes", ApiConsumers.MosConsumer, assignSDO, postParam);

        if (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0)
        {
            return res.ServiceReqs[0].SERVICE_REQ_CODE;
        }
        else
        {
            string errMsg = "Hệ thống MOS từ chối tạo chỉ định.";
            if (postParam.Messages != null && postParam.Messages.Count > 0)
            {
                errMsg += " " + string.Join("; ", postParam.Messages);
            }
            if (postParam.BugCodes != null && postParam.BugCodes.Count > 0)
            {
                errMsg += " [Mã lỗi: " + string.Join(", ", postParam.BugCodes) + "]";
            }
            throw new Exception(errMsg);
        }
    }
}

// =========================================================================
// PROGRAM ENTRYPOINT (CLI & GUI DISPATCHER)
// =========================================================================

class Program
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    [STAThread]
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
            string path1 = Path.Combine(folderPath, name);
            if (File.Exists(path1)) return Assembly.LoadFrom(path1);
            string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
            if (File.Exists(path2)) return Assembly.LoadFrom(path2);
            string path3 = Path.Combine(folderPath, "HisAutoPrescribe_Portable", name);
            if (File.Exists(path3)) return Assembly.LoadFrom(path3);
            return null;
        };

        if (args.Length > 0 && (args[0] == "-p" || args[0] == "--patient" || args[0] == "-f" || args[0] == "--file" || args[0] == "--help" || args[0] == "-h"))
        {
            AttachConsole(-1);
            RunCli(args);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    static void RunCli(string[] args)
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("  HỆ THỐNG CHỈ ĐỊNH ĐƯỜNG MÁU MAO MẠCH BM02426 (CLI) - KHOA CTCH & CỘT SỐNG");
        Console.WriteLine("===============================================================================");

        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine("CÚ PHÁP SỬ DỤNG:");
            Console.WriteLine("  HisGlucoseBedsideAssigner.exe -p <MaBN1,MaBN2,...> -time <06:00,11:00,17:00,21:00> [-date <yyyy-MM-dd>] [-note <GhiChu>]");
            Console.WriteLine("  HisGlucoseBedsideAssigner.exe -f <danh_sach.txt> -time <06:00>");
            Console.WriteLine("\nVÍ DỤ:");
            Console.WriteLine("  HisGlucoseBedsideAssigner.exe -p \"0003969449,0003298895\" -time \"06:00\"");
            Console.WriteLine("  HisGlucoseBedsideAssigner.exe -p \"0003969449\" -time \"06:00,11:00,17:00,21:00\" -date \"2026-08-24\"");
            return;
        }

        string rawPatients = "";
        string rawTimes = "06:00";
        string rawDate = DateTime.Today.ToString("yyyy-MM-dd");
        string note = "Đo ĐMMM theo dõi đường huyết";
        long executeRoomId = 931; // Phòng Tiểu Phẫu Nhà Q

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-p" || args[i] == "--patient") && i + 1 < args.Length) rawPatients = args[i + 1];
            if ((args[i] == "-time" || args[i] == "-t") && i + 1 < args.Length) rawTimes = args[i + 1];
            if ((args[i] == "-date" || args[i] == "-d") && i + 1 < args.Length) rawDate = args[i + 1];
            if ((args[i] == "-note" || args[i] == "-n") && i + 1 < args.Length) note = args[i + 1];
            if (args[i] == "-f" || args[i] == "--file")
            {
                if (i + 1 < args.Length && File.Exists(args[i + 1]))
                {
                    rawPatients = File.ReadAllText(args[i + 1]);
                }
            }
        }

        var codes = rawPatients.Split(new char[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        var times = rawTimes.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
        DateTime targetDate = DateTime.Today;
        DateTime.TryParse(rawDate, out targetDate);

        if (codes.Count == 0)
        {
            Console.WriteLine("❌ Lỗi: Chưa cung cấp mã bệnh nhân (-p <MaBN>)!");
            return;
        }

        Console.WriteLine(string.Format("• Số lượng BN: {0} | Ngày: {1} | Mốc giờ: {2}", codes.Count, targetDate.ToString("dd/MM/yyyy"), string.Join(", ", times)));

        try
        {
            MainForm.InitSession();
            int totalSuccess = 0;
            int totalFail = 0;

            foreach (var code in codes)
            {
                var patient = MainForm.LookupSinglePatient(code);
                if (patient == null)
                {
                    Console.WriteLine(string.Format("❌ [{0}] Không tìm thấy bệnh nhân hoặc đợt điều trị!", code));
                    totalFail++;
                    continue;
                }

                Console.WriteLine(string.Format("\n👤 [{0}] {1} ({2}) - {3}:", patient.PatientCode, patient.PatientName, patient.GenderName, patient.BedFull));

                foreach (var t in times)
                {
                    string[] parts = t.Split(':');
                    int h = int.Parse(parts[0]);
                    int m = parts.Length > 1 ? int.Parse(parts[1]) : 0;
                    DateTime dt = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, h, m, 0);
                    long instructionTime = long.Parse(dt.ToString("yyyyMMddHHmmss"));

                    try
                    {
                        string reqCode = MainForm.AssignSinglePatientService(patient, instructionTime, t, executeRoomId, note, true);
                        Console.WriteLine(string.Format("   ✔ [{0}] Chỉ định thành công! Mã Y Lệnh: {1}", t, reqCode));
                        totalSuccess++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("   ❌ [{0}] Thất bại: {1}", t, ex.Message));
                        totalFail++;
                    }
                }
            }

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine(string.Format("KẾT THÚC: ✔ Thành công: {0} | ❌ Lỗi: {1}", totalSuccess, totalFail));
            Console.WriteLine("===============================================================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi nghiêm trọng: " + ex.Message);
        }
    }
}
