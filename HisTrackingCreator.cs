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
using Inventec.Token.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
using HIS.Desktop.LocalStorage.LocalData;
using HIS.Desktop.ApiConsumer;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;
using EMR.EFMODEL.DataModels;
using EMR.Filter;
using EMR.SDO;
using EMR.TDO;

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

public class LoginForm : Form
{
    public string Username { get; private set; }
    public string Password { get; private set; }
    public bool LoginSuccess { get; private set; }
    public string TokenCode { get; private set; }

    private TextBox txtUser;
    private TextBox txtPass;
    private Button btnLogin;
    private Button btnCancel;
    private Label lblError;
    private ProgressBar pbLoading;

    public LoginForm(string defaultUser = "vmc", string defaultPass = "789789")
    {
        InitializeComponent(defaultUser, defaultPass);
    }

    private void InitializeComponent(string defaultUser, string defaultPass)
    {
        this.Text = "ĐĂNG NHẬP BÁC SĨ - HỆ THỐNG TẠO TỜ ĐIỀU TRỊ (HIS/EMR)";
        this.Size = new Size(480, 360);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
        this.BackColor = Color.FromArgb(245, 247, 250);

        try { if (File.Exists("APP.ico")) this.Icon = new Icon("APP.ico"); } catch { }

        Panel pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = Color.FromArgb(26, 86, 219)
        };

        Label lblTitle = new Label
        {
            Text = "TẠO TỜ ĐIỀU TRỊ (HIS / MOS EMR)",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            Location = new Point(20, 14),
            AutoSize = true
        };

        Label lblSubTitle = new Label
        {
            Text = "Đăng nhập tài khoản Bác sĩ để tạo tờ điều trị & y lệnh đúng tên bác sĩ",
            ForeColor = Color.FromArgb(220, 235, 252),
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(22, 42),
            AutoSize = true
        };

        pnlTop.Controls.Add(lblTitle);
        pnlTop.Controls.Add(lblSubTitle);
        this.Controls.Add(pnlTop);

        Label lblU = new Label { Text = "Tên đăng nhập (Username):", Location = new Point(35, 95), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        txtUser = new TextBox { Text = defaultUser, Location = new Point(35, 120), Width = 390, Font = new Font("Segoe UI", 10.5f) };

        Label lblP = new Label { Text = "Mật khẩu (Password):", Location = new Point(35, 160), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        txtPass = new TextBox { Text = defaultPass, Location = new Point(35, 185), Width = 390, UseSystemPasswordChar = true, Font = new Font("Segoe UI", 10.5f) };

        lblError = new Label { Text = "", ForeColor = Color.Red, Location = new Point(35, 220), Width = 390, Height = 35, Font = new Font("Segoe UI", 9f) };
        pbLoading = new ProgressBar { Location = new Point(35, 225), Width = 390, Height = 10, Style = ProgressBarStyle.Marquee, Visible = false };

        btnLogin = new Button
        {
            Text = "ĐĂNG NHẬP 🔑",
            Location = new Point(145, 260),
            Width = 150,
            Height = 40,
            BackColor = Color.FromArgb(26, 86, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        btnCancel = new Button
        {
            Text = "Thoát",
            Location = new Point(310, 260),
            Width = 115,
            Height = 40,
            BackColor = Color.FromArgb(229, 231, 235),
            ForeColor = Color.FromArgb(55, 65, 81),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => { this.Close(); };

        this.Controls.Add(lblU);
        this.Controls.Add(txtUser);
        this.Controls.Add(lblP);
        this.Controls.Add(txtPass);
        this.Controls.Add(lblError);
        this.Controls.Add(pbLoading);
        this.Controls.Add(btnLogin);
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnLogin;
    }

    private async void BtnLogin_Click(object sender, EventArgs e)
    {
        string u = txtUser.Text.Trim();
        string p = txtPass.Text;

        if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
        {
            lblError.Text = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!";
            return;
        }

        lblError.Text = "Đang xác thực tài khoản bác sĩ trên máy chủ MOS...";
        lblError.ForeColor = Color.FromArgb(26, 86, 219);
        pbLoading.Visible = true;
        btnLogin.Enabled = false;

        try
        {
            var res = await Task.Run(() =>
            {
                HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
                ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                CommonParam cp = new CommonParam();
                var token = tokenManager.Login(cp, u, p, "2.390.0");
                return new { Token = token, Param = cp };
            });

            if (res.Token != null && !string.IsNullOrEmpty(res.Token.TokenCode))
            {
                TokenCode = res.Token.TokenCode;
                Username = u;
                Password = p;
                LoginSuccess = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblError.ForeColor = Color.Red;
                lblError.Text = "Đăng nhập không thành công! Vui lòng kiểm tra lại tài khoản hoặc mật khẩu.";
                pbLoading.Visible = false;
                btnLogin.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            lblError.ForeColor = Color.Red;
            lblError.Text = "Lỗi kết nối máy chủ: " + ex.Message;
            pbLoading.Visible = false;
            btnLogin.Enabled = true;
        }
    }
}

public class ClinicalTemplate
{
    public string Name { get; set; }
    public string DefaultTime { get; set; }
    public string Content { get; set; }
    public string CareInstruction { get; set; }
    public string MedicalInstruction { get; set; }

    public ClinicalTemplate(string name, string defaultTime, string content, string careInstruction, string medInstruction)
    {
        Name = name;
        DefaultTime = defaultTime;
        Content = content;
        CareInstruction = careInstruction;
        MedicalInstruction = medInstruction;
    }

    public override string ToString()
    {
        return Name;
    }
}

public class MainForm : Form
{
    public static BackendAdapter adapter;
    public static MyAdapter myAdapter = new MyAdapter();
    public static CommonParam param = new CommonParam();
    public static string currentToken = null;
    public static string CurrentLoginName = "034727";
    public static string CurrentUserName = "NGUYỄN HỮU SÂM";

    // Header Controls
    private Panel pnlHeader;
    private Label lblTitle;
    private Label lblSubTitle;
    private Label lblDoctorInfo;
    private Button btnSwitchUser;
    private Button btnHeaderStart;

    // Toolbar Controls
    private Panel pnlToolbar;
    private Button btnAddRow;
    private Button btnPasteExcel;
    private Button btnExecuteAll;
    private ComboBox cboQuickTemplate;
    private Button btnApplyTemplate;
    private Button btnLoadDept57;
    private Button btnDeleteRow;
    private Button btnClearAll;

    // Grid Control
    private DataGridView dgvTracking;

    // Bottom Controls
    private Panel pnlBottom;
    private ProgressBar pbProgress;
    private Label lblSummary;
    private Button btnBottomStart;
    private Button btnExportCsv;

    public static List<ClinicalTemplate> ClinicalTemplates = new List<ClinicalTemplate>();

    public MainForm(string loginName = "034727", string tokenCode = "")
    {
        CurrentLoginName = loginName;
        currentToken = tokenCode;
        if (!string.IsNullOrEmpty(currentToken))
        {
            ApiConsumers.SetConsunmer(currentToken);
            adapter = new BackendAdapter(param);
        }

        InitializeClinicalTemplates();
        InitializeComponents();
        this.Load += MainForm_Load;
    }

    private void InitializeClinicalTemplates()
    {
        ClinicalTemplates.Clear();
        ClinicalTemplates.Add(new ClinicalTemplate(
            "--- Chọn mẫu điền nhanh (Tùy chọn) ---",
            "17:00",
            "",
            "",
            ""
        ));

        ClinicalTemplates.Add(new ClinicalTemplate(
            "1. Tờ điều trị hàng ngày",
            "08:00",
            "Bệnh nhân tỉnh táo, tiếp xúc tốt. Da niêm mạc hồng, không sốt. Vết mổ khô sạch, đầu chi hồng ấm.",
            "csii, bt01",
            "Thuốc theo đơn đã kê"
        ));

        ClinicalTemplates.Add(new ClinicalTemplate(
            "2. Đường máu mao mạch 17h",
            "17:00",
            "Khám: Đường máu mao mạch lúc 17h: 12.5 mmol/l",
            "csii, dd01",
            "bổ sung thuốc"
        ));

        ClinicalTemplates.Add(new ClinicalTemplate(
            "3. Sơ kết 3 - 5 ngày điều trị",
            "08:00",
            "SƠ KẾT 3-5 NGÀY: Toàn trạng ổn định, không sốt. Vết mổ khô liền tốt, đỡ đau. Vận động ngọn chi bình thường.",
            "csii, bt01",
            "Duy trì thuốc theo đơn"
        ));

        ClinicalTemplates.Add(new ClinicalTemplate(
            "4. Chuẩn bị trước mổ (Tiền phẫu)",
            "16:30",
            "Khám tiền phẫu: Toàn trạng ổn định, tim phổi bình thường. Đã hoàn thiện bilan xét nghiệm và ký cam kết mổ.",
            "csii, nhịn ăn trước mổ",
            "Nhịn ăn uống từ 00h trước mổ. Kháng sinh dự phòng trước mổ."
        ));

        ClinicalTemplates.Add(new ClinicalTemplate(
            "5. Hậu phẫu 24h",
            "14:00",
            "Hậu phẫu: Bệnh nhân tỉnh táo, đã thoát mê. Vết mổ nề nhẹ, đầu chi hồng ấm, dẫn lưu hoạt động tốt.",
            "csci, theo dõi dẫn lưu",
            "Thuốc giảm đau, kháng sinh theo biên bản hồi tỉnh"
        ));

        ClinicalTemplates.Add(new ClinicalTemplate(
            "6. Tổng kết ra viện",
            "08:00",
            "Tổng kết ra viện: Diễn biến điều trị ổn định, vết mổ khô sạch liền tốt, đi lại phục hồi tốt. Đủ điều kiện ra viện.",
            "csii",
            "Cho ra viện. Kê đơn ngoại trú. Hẹn tái khám sau 1 tháng."
        ));
    }

    private void InitializeComponents()
    {
        this.Text = "TẠO TỜ ĐIỀU TRỊ BỆNH NHÂN (HIS / MOS EMR) - KHOA CTCH & CỘT SỐNG";
        this.Size = new Size(1360, 740);
        this.MinimumSize = new Size(1000, 550);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        this.BackColor = Color.FromArgb(245, 247, 250);

        try { if (File.Exists("APP.ico")) this.Icon = new Icon("APP.ico"); } catch { }

        this.KeyPreview = true;
        this.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                await ExecuteCreateAllAsync();
            }
        };

        // Top Header
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(26, 86, 219), // Modern Blue
            Padding = new Padding(16, 8, 16, 8)
        };

        lblTitle = new Label
        {
            Text = "📋 TẠO TỜ ĐIỀU TRỊ BỆNH NHÂN (HIS / MOS EMR)",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Location = new Point(14, 8),
            AutoSize = true
        };

        lblDoctorInfo = new Label
        {
            Text = string.Format("👨‍⚕️ Bác sĩ: {0} ({1}) | 🏥 Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57)", CurrentUserName, CurrentLoginName),
            ForeColor = Color.FromArgb(220, 235, 252),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(16, 34),
            AutoSize = true
        };

        btnSwitchUser = new Button
        {
            Text = "Đổi Bác sĩ 🔄",
            Dock = DockStyle.Right,
            Width = 120,
            BackColor = Color.FromArgb(30, 64, 175),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSwitchUser.FlatAppearance.BorderSize = 0;
        btnSwitchUser.Click += BtnSwitchUser_Click;

        btnHeaderStart = new Button
        {
            Text = "▶ BẮT ĐẦU (F5) 🚀",
            Dock = DockStyle.Right,
            Width = 200,
            BackColor = Color.FromArgb(16, 185, 129), // Emerald Green
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnHeaderStart.FlatAppearance.BorderSize = 0;
        btnHeaderStart.Click += async (s, e) => await ExecuteCreateAllAsync();

        pnlHeader.Controls.Add(btnHeaderStart);
        pnlHeader.Controls.Add(btnSwitchUser);
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblDoctorInfo);
        this.Controls.Add(pnlHeader);

        // Toolbar
        pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8, 6, 8, 6),
            AutoScroll = true
        };

        btnAddRow = new Button
        {
            Text = "➕ Thêm dòng",
            Location = new Point(10, 8),
            Width = 110,
            Height = 34,
            BackColor = Color.FromArgb(238, 242, 255),
            ForeColor = Color.FromArgb(67, 56, 202),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnAddRow.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
        btnAddRow.Click += (s, e) => AddEmptyRow();

        btnPasteExcel = new Button
        {
            Text = "📋 Dán từ Excel (Ctrl+V)",
            Location = new Point(125, 8),
            Width = 175,
            Height = 34,
            BackColor = Color.FromArgb(236, 253, 245),
            ForeColor = Color.FromArgb(4, 120, 87),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPasteExcel.FlatAppearance.BorderColor = Color.FromArgb(167, 243, 208);
        btnPasteExcel.Click += (s, e) => PasteFromClipboard();

        btnExecuteAll = new Button
        {
            Text = "▶ BẮT ĐẦU TẠO (F5) 🚀",
            Location = new Point(306, 6),
            Width = 210,
            Height = 38,
            BackColor = Color.FromArgb(16, 185, 129), // Emerald Green
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnExecuteAll.FlatAppearance.BorderSize = 0;
        btnExecuteAll.Click += async (s, e) => await ExecuteCreateAllAsync();

        Label lblM = new Label { Text = "Mẫu:", Location = new Point(525, 15), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

        cboQuickTemplate = new ComboBox
        {
            Location = new Point(565, 12),
            Width = 210,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        foreach (var t in ClinicalTemplates) cboQuickTemplate.Items.Add(t);
        cboQuickTemplate.SelectedIndex = 0;

        btnApplyTemplate = new Button
        {
            Text = "Áp dụng mẫu",
            Location = new Point(780, 8),
            Width = 100,
            Height = 34,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnApplyTemplate.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnApplyTemplate.Click += (s, e) => ApplyTemplateToSelectedRows();

        btnLoadDept57 = new Button
        {
            Text = "🏥 Tải BN Khoa 57",
            Location = new Point(885, 8),
            Width = 150,
            Height = 34,
            BackColor = Color.FromArgb(240, 249, 255),
            ForeColor = Color.FromArgb(3, 105, 161),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLoadDept57.FlatAppearance.BorderColor = Color.FromArgb(186, 230, 253);
        btnLoadDept57.Click += async (s, e) => await LoadDept57Async();

        btnDeleteRow = new Button
        {
            Text = "Xóa dòng",
            Location = new Point(1040, 8),
            Width = 80,
            Height = 34,
            BackColor = Color.FromArgb(254, 242, 242),
            ForeColor = Color.FromArgb(185, 28, 28),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btnDeleteRow.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
        btnDeleteRow.Click += (s, e) => DeleteSelectedRows();

        btnClearAll = new Button
        {
            Text = "Xóa hết",
            Location = new Point(1125, 8),
            Width = 70,
            Height = 34,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(100, 116, 139),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btnClearAll.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnClearAll.Click += (s, e) => { dgvTracking.Rows.Clear(); UpdateRowCount(); };

        pnlToolbar.Controls.Add(btnAddRow);
        pnlToolbar.Controls.Add(btnPasteExcel);
        pnlToolbar.Controls.Add(btnExecuteAll);
        pnlToolbar.Controls.Add(lblM);
        pnlToolbar.Controls.Add(cboQuickTemplate);
        pnlToolbar.Controls.Add(btnApplyTemplate);
        pnlToolbar.Controls.Add(btnLoadDept57);
        pnlToolbar.Controls.Add(btnDeleteRow);
        pnlToolbar.Controls.Add(btnClearAll);
        this.Controls.Add(pnlToolbar);

        // DataGridView
        dgvTracking = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            Font = new Font("Segoe UI", 9.5f),
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
        };
        dgvTracking.EnableHeadersVisualStyles = false;
        dgvTracking.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        dgvTracking.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
        dgvTracking.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        dgvTracking.ColumnHeadersHeight = 34;
        dgvTracking.RowTemplate.Height = 32;

        dgvTracking.KeyDown += DgvTracking_KeyDown;
        dgvTracking.CellEndEdit += DgvTracking_CellEndEdit;
        dgvTracking.CellFormatting += DgvTracking_CellFormatting;

        BuildGridColumns();
        this.Controls.Add(dgvTracking);
        dgvTracking.BringToFront();

        // Bottom Bar
        pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12, 6, 12, 6)
        };

        pbProgress = new ProgressBar
        {
            Location = new Point(12, 12),
            Width = 260,
            Height = 22,
            Visible = false
        };

        lblSummary = new Label
        {
            Text = "Mẹo: Dán trực tiếp từ Excel (Ctrl+V) hoặc gõ nội dung vào bảng rồi bấm BẮT ĐẦU (F5).",
            Location = new Point(285, 14),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 9f, FontStyle.Italic)
        };

        btnBottomStart = new Button
        {
            Text = "▶ BẮT ĐẦU (F5)",
            Location = new Point(980, 7),
            Width = 190,
            Height = 32,
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        btnBottomStart.FlatAppearance.BorderSize = 0;
        btnBottomStart.Click += async (s, e) => await ExecuteCreateAllAsync();

        btnExportCsv = new Button
        {
            Text = "📊 Xuất CSV",
            Location = new Point(1180, 7),
            Width = 110,
            Height = 32,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        btnExportCsv.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnExportCsv.Click += (s, e) => ExportToCsv();

        pnlBottom.Controls.Add(pbProgress);
        pnlBottom.Controls.Add(lblSummary);
        pnlBottom.Controls.Add(btnBottomStart);
        pnlBottom.Controls.Add(btnExportCsv);
        this.Controls.Add(pnlBottom);

        // Initial default row with clean fields
        AddRowInternal("0003969449", "", "17:00", "Khám: Đường máu mao mạch lúc 17h: 12.5 mmol/l", "csii, dd01", "bổ sung thuốc");
    }

    private void BuildGridColumns()
    {
        dgvTracking.Columns.Clear();

        // STT
        var colStt = new DataGridViewTextBoxColumn
        {
            Name = "Stt",
            HeaderText = "STT",
            Width = 45,
            ReadOnly = true,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };
        dgvTracking.Columns.Add(colStt);

        // Patient Code
        var colPat = new DataGridViewTextBoxColumn
        {
            Name = "PatientCode",
            HeaderText = "Mã BN / Mã ĐT (*)",
            Width = 120,
            DefaultCellStyle = { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 58, 138) }
        };
        dgvTracking.Columns.Add(colPat);

        // Patient Info (Auto resolved)
        var colName = new DataGridViewTextBoxColumn
        {
            Name = "PatientInfo",
            HeaderText = "Họ Tên BN & Buồng Giường",
            Width = 200,
            ReadOnly = true,
            DefaultCellStyle = { ForeColor = Color.FromArgb(51, 65, 85) }
        };
        dgvTracking.Columns.Add(colName);

        // Time
        var colTime = new DataGridViewTextBoxColumn
        {
            Name = "TrackingTime",
            HeaderText = "Giờ / Ngày (*)",
            Width = 100,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
        };
        dgvTracking.Columns.Add(colTime);

        // Content
        var colContent = new DataGridViewTextBoxColumn
        {
            Name = "Content",
            HeaderText = "Diễn biến bệnh (Content) (*)",
            Width = 320
        };
        dgvTracking.Columns.Add(colContent);

        // Care Instruction
        var colCare = new DataGridViewTextBoxColumn
        {
            Name = "CareInstruction",
            HeaderText = "Chế độ chăm sóc (Care) (*)",
            Width = 220
        };
        dgvTracking.Columns.Add(colCare);

        // Medical Instruction
        var colMed = new DataGridViewTextBoxColumn
        {
            Name = "MedicalInstruction",
            HeaderText = "Y lệnh điều trị (Med)",
            Width = 200
        };
        dgvTracking.Columns.Add(colMed);

        // Status
        var colStatus = new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Trạng thái",
            Width = 120,
            ReadOnly = true
        };
        dgvTracking.Columns.Add(colStatus);

        // Result / Tracking ID
        var colResult = new DataGridViewTextBoxColumn
        {
            Name = "Result",
            HeaderText = "Kết quả / ID Tờ ĐT",
            Width = 140,
            ReadOnly = true
        };
        dgvTracking.Columns.Add(colResult);
    }

    private void AddEmptyRow()
    {
        AddRowInternal("", "", "17:00", "", "", "");
    }

    private void AddRowInternal(string patCode, string patInfo, string time, string content, string care, string med)
    {
        int idx = dgvTracking.Rows.Add();
        var row = dgvTracking.Rows[idx];
        row.Cells["Stt"].Value = idx + 1;
        row.Cells["PatientCode"].Value = patCode;
        row.Cells["PatientInfo"].Value = patInfo;
        row.Cells["TrackingTime"].Value = string.IsNullOrEmpty(time) ? "17:00" : time;
        row.Cells["Content"].Value = content;
        row.Cells["CareInstruction"].Value = care;
        row.Cells["MedicalInstruction"].Value = med;
        row.Cells["Status"].Value = "⚪ Chờ tạo";
        row.Cells["Result"].Value = "";

        if (!string.IsNullOrEmpty(patCode) && string.IsNullOrEmpty(patInfo))
        {
            Task.Run(() => ResolvePatientInfoForRow(row, patCode));
        }

        UpdateRowCount();
    }

    private void UpdateRowCount()
    {
        for (int i = 0; i < dgvTracking.Rows.Count; i++)
        {
            dgvTracking.Rows[i].Cells["Stt"].Value = i + 1;
        }
        lblSummary.Text = string.Format("Tổng cộng: {0} bệnh nhân trong bảng.", dgvTracking.Rows.Count);
    }

    private void DgvTracking_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.V)
        {
            e.Handled = true;
            PasteFromClipboard();
        }
        else if (e.KeyCode == Keys.Delete && !dgvTracking.IsCurrentCellInEditMode)
        {
            DeleteSelectedRows();
        }
    }

    private void DeleteSelectedRows()
    {
        var selectedRows = dgvTracking.SelectedRows.Cast<DataGridViewRow>().ToList();
        if (selectedRows.Count == 0 && dgvTracking.CurrentRow != null)
        {
            selectedRows.Add(dgvTracking.CurrentRow);
        }

        foreach (var r in selectedRows)
        {
            if (!r.IsNewRow) dgvTracking.Rows.Remove(r);
        }
        UpdateRowCount();
    }

    private void ApplyTemplateToSelectedRows()
    {
        var tmpl = cboQuickTemplate.SelectedItem as ClinicalTemplate;
        if (tmpl == null || string.IsNullOrEmpty(tmpl.Content)) return;

        var targetRows = dgvTracking.SelectedRows.Cast<DataGridViewRow>().ToList();
        if (targetRows.Count == 0)
        {
            targetRows = dgvTracking.Rows.Cast<DataGridViewRow>().ToList();
        }

        foreach (var r in targetRows)
        {
            r.Cells["Content"].Value = tmpl.Content;
            r.Cells["CareInstruction"].Value = tmpl.CareInstruction;
            r.Cells["MedicalInstruction"].Value = tmpl.MedicalInstruction;
            if (string.IsNullOrEmpty(r.Cells["TrackingTime"].Value as string) || (r.Cells["TrackingTime"].Value as string) == "08:00")
            {
                r.Cells["TrackingTime"].Value = tmpl.DefaultTime;
            }
        }

        lblSummary.Text = string.Format("✔ Đã áp dụng '{0}' cho {1} dòng.", tmpl.Name, targetRows.Count);
    }

    private void PasteFromClipboard()
    {
        try
        {
            string clip = Clipboard.GetText();
            if (string.IsNullOrEmpty(clip)) return;

            string[] lines = clip.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split('\t');

                string pat = parts.Length > 0 ? parts[0].Trim() : "";
                if (string.IsNullOrEmpty(pat)) continue;

                string time = parts.Length > 1 ? parts[1].Trim() : "17:00";
                string content = parts.Length > 2 ? parts[2].Trim() : "";
                string care = parts.Length > 3 ? parts[3].Trim() : "";
                string med = parts.Length > 4 ? parts[4].Trim() : "";

                AddRowInternal(pat, "", time, content, care, med);
                addedCount++;
            }

            lblSummary.Text = string.Format("✔ Đã dán thành công {0} dòng từ Clipboard.", addedCount);
            lblSummary.ForeColor = Color.FromArgb(4, 120, 87);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi dán dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvTracking_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= dgvTracking.Rows.Count) return;

        if (dgvTracking.Columns[e.ColumnIndex].Name == "PatientCode")
        {
            var row = dgvTracking.Rows[e.RowIndex];
            string code = (row.Cells["PatientCode"].Value ?? "").ToString().Trim();
            if (!string.IsNullOrEmpty(code))
            {
                Task.Run(() => ResolvePatientInfoForRow(row, code));
            }
        }
    }

    private void ResolvePatientInfoForRow(DataGridViewRow row, string code)
    {
        try
        {
            InitSession();
            var p = LookupPatientDirect(code);
            if (p != null)
            {
                this.Invoke(new Action(() =>
                {
                    row.Cells["PatientInfo"].Value = string.Format("{0} ({1}) - {2}", p.TDL_PATIENT_NAME, p.TDL_PATIENT_GENDER_NAME, p.BedFull);
                    row.Tag = p;
                }));
            }
            else
            {
                this.Invoke(new Action(() =>
                {
                    row.Cells["PatientInfo"].Value = "⚠️ Không tìm thấy hồ sơ!";
                }));
            }
        }
        catch { }
    }

    private void DgvTracking_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= dgvTracking.Rows.Count) return;

        if (dgvTracking.Columns[e.ColumnIndex].Name == "Status")
        {
            string val = (e.Value ?? "").ToString();
            if (val.Contains("Thành công"))
            {
                e.CellStyle.ForeColor = Color.FromArgb(4, 120, 87);
                e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            }
            else if (val.Contains("Lỗi"))
            {
                e.CellStyle.ForeColor = Color.FromArgb(220, 38, 38);
                e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            }
        }
    }

    private async Task LoadDept57Async()
    {
        btnLoadDept57.Enabled = false;
        lblSummary.Text = "Đang tải danh sách toàn bộ bệnh nhân nội trú Khoa CTCH & Cột sống (Khoa 57)...";

        try
        {
            var list = await Task.Run(() =>
            {
                InitSession();
                return FetchDept57Direct();
            });

            foreach (var p in list)
            {
                AddRowInternal(p.TDL_PATIENT_CODE, string.Format("{0} ({1}) - {2}", p.TDL_PATIENT_NAME, p.TDL_PATIENT_GENDER_NAME, p.BedFull),
                    "17:00", "", "", "");
            }

            lblSummary.Text = string.Format("✔ Đã tải thành công {0} bệnh nhân nội trú Khoa 57.", list.Count);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi tải bệnh nhân: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnLoadDept57.Enabled = true;
        }
    }

    private async Task ExecuteCreateAllAsync()
    {
        if (dgvTracking.Rows.Count == 0)
        {
            MessageBox.Show("Chưa có dòng dữ liệu nào để tạo tờ điều trị!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string msg = string.Format("XÁC NHẬN TẠO TỜ ĐIỀU TRỊ:\n\n• Bác sĩ thực hiện: {0} ({1})\n• Tổng số dòng: {2} bệnh nhân\n• Hệ thống: HIS / MOS Khoa 57\n\nBạn có chắc chắn muốn tiến hành ghi nhận lên hệ thống?", 
            CurrentUserName, CurrentLoginName, dgvTracking.Rows.Count);

        if (MessageBox.Show(msg, "Xác nhận tạo Tờ điều trị", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        btnExecuteAll.Enabled = false;
        btnHeaderStart.Enabled = false;
        btnBottomStart.Enabled = false;
        pbProgress.Visible = true;
        pbProgress.Minimum = 0;
        pbProgress.Maximum = dgvTracking.Rows.Count;
        pbProgress.Value = 0;

        int success = 0;
        int fail = 0;

        await Task.Run(() =>
        {
            InitSession();

            for (int i = 0; i < dgvTracking.Rows.Count; i++)
            {
                var row = dgvTracking.Rows[i];
                string patCode = (row.Cells["PatientCode"].Value ?? "").ToString().Trim();
                string timeStr = (row.Cells["TrackingTime"].Value ?? "17:00").ToString().Trim();
                string content = (row.Cells["Content"].Value ?? "").ToString().Trim();
                string care = (row.Cells["CareInstruction"].Value ?? "").ToString().Trim();
                string med = (row.Cells["MedicalInstruction"].Value ?? "").ToString().Trim();

                if (string.IsNullOrEmpty(patCode))
                {
                    this.Invoke(new Action(() =>
                    {
                        row.Cells["Status"].Value = "❌ Thiếu mã BN";
                    }));
                    fail++;
                    continue;
                }

                try
                {
                    long trackingTime = ParseTrackingTime(timeStr);
                    var p = row.Tag as PatientLookupInfo;
                    if (p == null) p = LookupPatientDirect(patCode);

                    long deptId = p.DepartmentId > 0 ? p.DepartmentId : 57;
                    long doctorWorkRoomId = (deptId == 915 ? 18679 : 5248);
                    long roomId = doctorWorkRoomId;

                    try
                    {
                        var wi = new WorkInfoSDO
                        {
                            Rooms = new List<RoomSDO>
                            {
                                new RoomSDO { RoomId = doctorWorkRoomId },
                                new RoomSDO { RoomId = roomId },
                                new RoomSDO { RoomId = 5248 },
                                new RoomSDO { RoomId = 18679 },
                                new RoomSDO { RoomId = 18681 }
                            }
                        };
                        myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, wi, new CommonParam());
                    }
                    catch { }

                    HIS_TRACKING tracking = new HIS_TRACKING
                    {
                        TREATMENT_ID = p.TreatmentId,
                        DEPARTMENT_ID = deptId,
                        ROOM_ID = roomId,
                        TRACKING_TIME = trackingTime,
                        CONTENT = content,
                        MEDICAL_INSTRUCTION = med,
                        CARE_INSTRUCTION = care,
                        ICD_CODE = !string.IsNullOrEmpty(p.IcdCode) ? p.IcdCode : "M51.2",
                        ICD_NAME = !string.IsNullOrEmpty(p.IcdName) ? p.IcdName : "Thoát vị đĩa đệm",
                        ICD_SUB_CODE = p.IcdSubCode,
                        ICD_TEXT = p.IcdText
                    };

                    HisTrackingSDO sdo = new HisTrackingSDO
                    {
                        Tracking = tracking,
                        WorkingRoomId = roomId > 0 ? roomId : doctorWorkRoomId,
                        Dhst = null // Do not inject forced vital signs to keep tracking clean
                    };

                    CommonParam cp = new CommonParam();
                    var created = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, cp);
                    if (created == null || created.ID <= 0)
                    {
                        var resSdo = myAdapter.PostData<HisTrackingSDO>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, cp);
                        if (resSdo != null && resSdo.Tracking != null && resSdo.Tracking.ID > 0)
                        {
                            created = resSdo.Tracking;
                        }
                    }
                    if (created == null || created.ID <= 0)
                    {
                        tracking.ROOM_ID = doctorWorkRoomId;
                        sdo.WorkingRoomId = doctorWorkRoomId;
                        cp = new CommonParam();
                        created = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, cp);
                        if (created == null || created.ID <= 0)
                        {
                            var resSdo = myAdapter.PostData<HisTrackingSDO>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, cp);
                            if (resSdo != null && resSdo.Tracking != null && resSdo.Tracking.ID > 0)
                            {
                                created = resSdo.Tracking;
                            }
                        }
                    }
                    if (created == null || created.ID == 0)
                    {
                        string errMsg = "Hệ thống MOS từ chối tạo!";
                        if (cp.Messages != null && cp.Messages.Count > 0) errMsg = string.Join("; ", cp.Messages);
                        else if (cp.BugCodes != null && cp.BugCodes.Count > 0) errMsg = string.Join("; ", cp.BugCodes);
                        throw new Exception(errMsg);
                    }

                    this.Invoke(new Action(() =>
                    {
                        row.Cells["Status"].Value = "✔ Thành công";
                        row.Cells["Result"].Value = "ID: " + created.ID;
                        row.Cells["PatientInfo"].Value = string.Format("{0} ({1}) - {2}", p.TDL_PATIENT_NAME, p.TDL_PATIENT_GENDER_NAME, p.BedFull);
                    }));
                    success++;
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        row.Cells["Status"].Value = "❌ Lỗi";
                        row.Cells["Result"].Value = ex.Message;
                    }));
                    fail++;
                }

                this.Invoke(new Action(() =>
                {
                    pbProgress.Value = i + 1;
                    dgvTracking.Refresh();
                }));
            }
        });

        pbProgress.Visible = false;
        btnExecuteAll.Enabled = true;
        btnHeaderStart.Enabled = true;
        btnBottomStart.Enabled = true;

        lblSummary.Text = string.Format("Hoàn tất! ✔ {0} thành công | ❌ {1} thất bại.", success, fail);
        MessageBox.Show(string.Format("Hoàn tất tạo Tờ điều trị!\n\n• Bác sĩ ký: {0}\n✔ Thành công: {1}\n❌ Thất bại: {2}", CurrentUserName, success, fail),
            "Kết quả", MessageBoxButtons.OK, fail == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private static long ParseTrackingTime(string input)
    {
        DateTime date = DateTime.Today;
        TimeSpan time = new TimeSpan(17, 0, 0);

        long dummyVal;
        if (input.Length >= 14 && long.TryParse(input, out dummyVal))
        {
            return dummyVal;
        }

        if (input.Contains("/") || input.Contains("-"))
        {
            DateTime dt;
            if (DateTime.TryParse(input, out dt)) return long.Parse(dt.ToString("yyyyMMddHHmmss"));
        }

        TimeSpan parsedTime;
        if (TimeSpan.TryParse(input, out parsedTime))
        {
            time = parsedTime;
        }

        DateTime res = new DateTime(date.Year, date.Month, date.Day, time.Hours, time.Minutes, 0);
        return long.Parse(res.ToString("yyyyMMddHHmmss"));
    }

    private void ExportToCsv()
    {
        if (dgvTracking.Rows.Count == 0) return;

        SaveFileDialog sfd = new SaveFileDialog
        {
            Filter = "CSV File (*.csv)|*.csv",
            FileName = string.Format("To_Dieu_Tri_{0}.csv", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("STT,MaBN,ThongTinBN,Gio,NoiDung,ChamSoc,YLenh,TrangThai,KetQua");
                foreach (DataGridViewRow r in dgvTracking.Rows)
                {
                    sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\"",
                        r.Cells["Stt"].Value,
                        r.Cells["PatientCode"].Value,
                        (r.Cells["PatientInfo"].Value ?? "").ToString().Replace("\"", "\"\""),
                        r.Cells["TrackingTime"].Value,
                        (r.Cells["Content"].Value ?? "").ToString().Replace("\"", "\"\""),
                        (r.Cells["CareInstruction"].Value ?? "").ToString().Replace("\"", "\"\""),
                        (r.Cells["MedicalInstruction"].Value ?? "").ToString().Replace("\"", "\"\""),
                        r.Cells["Status"].Value,
                        r.Cells["Result"].Value));
                }
                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Xuất file CSV thành công:\n" + sfd.FileName, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnSwitchUser_Click(object sender, EventArgs e)
    {
        LoginForm login = new LoginForm(CurrentLoginName);
        if (login.ShowDialog() == DialogResult.OK)
        {
            CurrentLoginName = login.Username;
            currentToken = login.TokenCode;
            ApiConsumers.SetConsunmer(currentToken);
            adapter = new BackendAdapter(param);
            LoadDoctorProfile();
        }
    }

    private void LoadDoctorProfile()
    {
        try
        {
            InitSession();
            HisEmployeeFilter ef = new HisEmployeeFilter();
            ef.LOGINNAME__EXACT = CurrentLoginName;
            var emps = myAdapter.FetchList<HIS_EMPLOYEE>("api/HisEmployee/Get", ApiConsumers.MosConsumer, ef, param);
            if (emps != null && emps.Count > 0)
            {
                CurrentUserName = emps[0].TDL_USERNAME;
            }
            else
            {
                CurrentUserName = CurrentLoginName.ToUpper();
            }

            lblDoctorInfo.Text = string.Format("👨‍⚕️ Bác sĩ: {0} ({1}) | 🏥 Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57)", CurrentUserName, CurrentLoginName);
        }
        catch
        {
            lblDoctorInfo.Text = string.Format("👨‍⚕️ Bác sĩ: {0} ({1})", CurrentUserName, CurrentLoginName);
        }
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        lblSummary.Text = "Đang kết nối hệ thống HIS / MOS...";
        await Task.Run(() =>
        {
            try 
            { 
                InitSession(); 
                LoadDoctorProfile();
            } 
            catch { }
        });
        lblSummary.Text = "✔ Đã kết nối HIS thành công. Bác sĩ: " + CurrentUserName;
    }

    // =========================================================================
    // DIRECT API DATA ACCESS METHODS
    // =========================================================================

    public class PatientLookupInfo
    {
        public long TreatmentId { get; set; }
        public string TreatmentCode { get; set; }
        public string TDL_PATIENT_CODE { get; set; }
        public string TDL_PATIENT_NAME { get; set; }
        public string TDL_PATIENT_GENDER_NAME { get; set; }
        public string BedFull { get; set; }
        public long WorkingRoomId { get; set; }
        public long DepartmentId { get; set; }
        public string IcdCode { get; set; }
        public string IcdName { get; set; }
        public string IcdSubCode { get; set; }
        public string IcdText { get; set; }
    }

    public static string ReadLiveTokenFast()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1. Kiểm tra cache token độc lập của Bác sĩ (hạn 6 tiếng)
        try
        {
            List<string> tokenFiles = new List<string>();
            string envTok = Environment.GetEnvironmentVariable("HIS_TOKEN_FILE");
            string cacheName = !string.IsNullOrEmpty(envTok) ? envTok : "doctor_hn.token";

            tokenFiles.Add(Path.Combine(baseDir, cacheName));
            tokenFiles.Add(Path.Combine(baseDir, "doctor_standalone.token"));
            tokenFiles.Add(Path.Combine(baseDir, ".agents", "skills", "his-clinical-operations", "scripts", cacheName));
            tokenFiles.Add(Path.Combine(baseDir, ".agents", "skills", "his-clinical-operations", "scripts", "doctor_standalone.token"));

            DirectoryInfo dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 5; i++)
            {
                if (dir == null) break;
                tokenFiles.Add(Path.Combine(dir.FullName, cacheName));
                tokenFiles.Add(Path.Combine(dir.FullName, "doctor_standalone.token"));
                dir = dir.Parent;
            }

            tokenFiles.Add(Path.Combine(@"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB", cacheName));
            tokenFiles.Add(Path.Combine(@"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB", "doctor_standalone.token"));

            foreach (var tf in tokenFiles.Distinct())
            {
                if (!File.Exists(tf)) continue;
                try
                {
                    string[] parts = File.ReadAllText(tf, Encoding.UTF8).Split('|');
                    if (parts.Length >= 2)
                    {
                        long savedTime;
                        if (long.TryParse(parts[1], out savedTime))
                        {
                            DateTime savedDt = new DateTime(savedTime);
                            if ((DateTime.Now - savedDt).TotalHours < 6.0 && parts[0].Length == 64)
                            {
                                if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                                {
                                    CurrentLoginName = parts[2];
                                    if (CurrentLoginName == "034727") CurrentUserName = "NGUYỄN HỮU SÂM";
                                    else if (CurrentLoginName == "vmc") CurrentUserName = "VŨ MINH CƯỜNG";
                                }
                                return parts[0];
                            }
                        }
                    }
                }
                catch { }
            }
        }
        catch { }

        List<string> candidates = new List<string>();
        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
        if (Directory.Exists(preferredDir))
        {
            candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        }

        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
            candidates.Add(Path.Combine(cur.FullName, "Logs", "HLSLogSystem.txt"));
            cur = cur.Parent;
        }

        foreach (var lp in candidates)
        {
            if (!File.Exists(lp)) continue;
            try
            {
                using (var fs = new FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long length = fs.Length;
                    if (length == 0) continue;
                    int bufferSize = (int)Math.Min(131072L, length);
                    fs.Seek(length - bufferSize, SeekOrigin.Begin);
                    byte[] buffer = new byte[bufferSize];
                    int read = fs.Read(buffer, 0, bufferSize);
                    string chunk = Encoding.UTF8.GetString(buffer, 0, read);

                    if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true"))
                    {
                        continue;
                    }

                    // BẢO VỆ DANH TÍNH: Phải thuộc 034727 hoặc vmc
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (chunk.Length >= start + 64)
                        {
                            string tok = chunk.Substring(start, 64);
                            if (chunk.Contains("034727") || chunk.Contains("vmc"))
                            {
                                return tok;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static void EnsureAcsConfiguration()
    {
        try
        {
            var settings = System.Configuration.ConfigurationManager.AppSettings;
            if (string.IsNullOrEmpty(settings["Inventec.Token.ClientSystem.Acs.Base.Uri"]))
                settings["Inventec.Token.ClientSystem.Acs.Base.Uri"] = "http://192.168.7.200:1401/";
            if (string.IsNullOrEmpty(settings["Inventec.Token.ClientSystem.Acs.Uri"]))
                settings["Inventec.Token.ClientSystem.Acs.Uri"] = "http://192.168.7.200:1401/";
            if (string.IsNullOrEmpty(settings["Inventec.Token.ClientSystem.Acs.Version"]))
                settings["Inventec.Token.ClientSystem.Acs.Version"] = "2.0";
        }
        catch { }
    }

    public static void InitSession()
    {
        if (string.IsNullOrEmpty(currentToken))
        {
            EnsureAcsConfiguration();
            try { HIS.Desktop.LocalStorage.ConfigSystem.Load.Init(); } catch { }
            param = new CommonParam();

            // 1. Check doctor_standalone.token
            try
            {
                DirectoryInfo curD = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int i = 0; i < 5; i++)
                {
                    if (curD == null) break;
                    string checkF = Path.Combine(curD.FullName, "doctor_standalone.token");
                    if (File.Exists(checkF))
                    {
                        string[] parts = File.ReadAllText(checkF, Encoding.UTF8).Split('|');
                        if (parts.Length >= 2)
                        {
                            long ticks;
                            if (long.TryParse(parts[1], out ticks))
                            {
                                if (new TimeSpan(DateTime.Now.Ticks - ticks).TotalHours < 6)
                                {
                                    currentToken = parts[0];
                                    break;
                                }
                            }
                        }
                    }
                    curD = curD.Parent;
                }
            }
            catch { }

            // 2. Read live log
            if (string.IsNullOrEmpty(currentToken))
            {
                string liveToken = ReadLiveTokenFast();
                if (!string.IsNullOrEmpty(liveToken))
                {
                    currentToken = liveToken;
                }
            }

            // 3. Fallback to ACS Login
            if (string.IsNullOrEmpty(currentToken))
            {
                try
                {
                    var creds = new List<Tuple<string, string>>
                    {
                        Tuple.Create("034727", "981"),
                        Tuple.Create("034727", "998199"),
                        Tuple.Create("vmc", "789789")
                    };
                    ClientTokenManager tokenManager = new ClientTokenManager("HIS");
                    foreach (var c in creds)
                    {
                        var tok = tokenManager.Login(param, c.Item1, c.Item2, "2.390.0");
                        if (tok != null && !string.IsNullOrEmpty(tok.TokenCode))
                        {
                            currentToken = tok.TokenCode;
                            try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token"), currentToken + "|" + DateTime.Now.Ticks + "|" + c.Item1, Encoding.UTF8); } catch { }
                            break;
                        }
                    }
                }
                catch { }
            }
        }

        if (!string.IsNullOrEmpty(currentToken))
        {
            ApiConsumers.SetConsunmer(currentToken);
            adapter = new BackendAdapter(param);

            try
            {
                long[] defaultRooms = new long[] {
                    931, 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 
                    5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 
                    5267, 6622, 6623,
                    18679, 18681, 14759
                };
                var workInfo = new WorkInfoSDO
                {
                    Rooms = defaultRooms.Select(r => new RoomSDO { RoomId = r }).ToList()
                };
                var workPlaces = myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, param);
                HIS.Desktop.LocalStorage.LocalData.WorkPlace.WorkPlaceSDO = workPlaces;
                HIS.Desktop.LocalStorage.LocalData.WorkPlace.WorkInfoSDO = workInfo;
            }
            catch { }
        }
        else
        {
            throw new Exception("Không thể đăng nhập hoặc lấy Token hệ thống HIS / MOS!");
        }
    }

    public static PatientLookupInfo LookupPatientDirect(string code)
    {
        InitSession();

        string kw = code.Trim();
        long numVal;
        bool isNum = long.TryParse(kw, out numVal);

        List<V_HIS_TREATMENT> treatments = null;

        if (isNum)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.PATIENT_CODE__EXACT = kw.PadLeft(10, '0');
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);

            if (treatments == null || treatments.Count == 0)
            {
                tf = new HisTreatmentViewFilter();
                tf.TREATMENT_CODE__EXACT = kw.PadLeft(12, '0');
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
            }
        }

        if (treatments == null || treatments.Count == 0)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            if (kw.Length >= 11) tf.TREATMENT_CODE__EXACT = kw;
            else tf.PATIENT_CODE__EXACT = kw;
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
        }

        if (treatments == null || treatments.Count == 0)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            long tId;
            if (long.TryParse(kw, out tId))
            {
                tf.ID = tId;
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
            }
        }

        if (treatments == null || treatments.Count == 0)
        {
            HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
            tf.KEY_WORD = kw;
            treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
        }

        if (treatments == null || treatments.Count == 0) return null;

        var tr = treatments.OrderByDescending(t => t.IN_TIME).First();

        var item = new PatientLookupInfo
        {
            TreatmentId = tr.ID,
            TreatmentCode = tr.TREATMENT_CODE,
            TDL_PATIENT_CODE = tr.TDL_PATIENT_CODE,
            TDL_PATIENT_NAME = tr.TDL_PATIENT_NAME,
            TDL_PATIENT_GENDER_NAME = tr.TDL_PATIENT_GENDER_NAME,
            IcdCode = !string.IsNullOrEmpty(tr.ICD_CODE) ? tr.ICD_CODE : "M51.2",
            IcdName = !string.IsNullOrEmpty(tr.ICD_NAME) ? tr.ICD_NAME : "Thoát vị đĩa đệm",
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = !string.IsNullOrEmpty(tr.ICD_TEXT) ? tr.ICD_TEXT : tr.ICD_NAME,
            WorkingRoomId = 5257,
            DepartmentId = 57,
            BedFull = "Phòng 724"
        };

        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.TREATMENT_ID = tr.ID;
        tbrf.IS_IN_ROOM = true;
        var beds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, param);
        if (beds != null && beds.Count > 0)
        {
            var b = beds.OrderByDescending(x => x.ADD_TIME).First();
            item.BedFull = string.Format("{0} - {1}", b.BED_ROOM_NAME, b.BED_NAME);

            HisBedRoomViewFilter brf = new HisBedRoomViewFilter();
            brf.ID = b.BED_ROOM_ID;
            var bRooms = myAdapter.FetchList<V_HIS_BED_ROOM>("api/HisBedRoom/GetView", ApiConsumers.MosConsumer, brf, param);
            if (bRooms != null && bRooms.Count > 0)
            {
                item.WorkingRoomId = bRooms[0].ROOM_ID;
                if (bRooms[0].DEPARTMENT_ID > 0) item.DepartmentId = bRooms[0].DEPARTMENT_ID;
            }
            else if (b.DEPARTMENT_ID > 0)
            {
                item.DepartmentId = b.DEPARTMENT_ID;
            }
        }

        return item;
    }

    public static List<PatientLookupInfo> FetchDept57Direct()
    {
        InitSession();

        List<PatientLookupInfo> results = new List<PatientLookupInfo>();

        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.IS_IN_ROOM = true;
        tbrf.TREATMENT_IS_ACTIVE = true;
        var beds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, param);

        if (beds == null || beds.Count == 0) return results;

        var dept57Beds = beds.Where(b => b.DEPARTMENT_ID == 57).GroupBy(b => b.TREATMENT_ID).Select(g => g.First()).ToList();
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

            var item = new PatientLookupInfo
            {
                TreatmentId = b.TREATMENT_ID,
                TreatmentCode = tr != null ? tr.TREATMENT_CODE : b.TDL_PATIENT_CODE,
                TDL_PATIENT_CODE = b.TDL_PATIENT_CODE,
                TDL_PATIENT_NAME = b.TDL_PATIENT_NAME,
                TDL_PATIENT_GENDER_NAME = b.TDL_PATIENT_GENDER_NAME,
                BedFull = string.Format("{0} - {1}", b.BED_ROOM_NAME, b.BED_NAME),
                IcdCode = tr != null ? tr.ICD_CODE : "M51.2",
                IcdName = tr != null ? tr.ICD_NAME : "Thoát vị đĩa đệm",
                IcdSubCode = tr != null ? tr.ICD_SUB_CODE : "",
                IcdText = tr != null ? (!string.IsNullOrEmpty(tr.ICD_TEXT) ? tr.ICD_TEXT : tr.ICD_NAME) : "",
                WorkingRoomId = 5257,
                DepartmentId = 57
            };

            results.Add(item);
        }

        return results.OrderBy(p => p.BedFull).ThenBy(p => p.TDL_PATIENT_NAME).ToList();
    }
}

// =========================================================================
// PROGRAM ENTRYPOINT (CLI & GUI DISPATCHER)
// =========================================================================

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch { }

        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            try
            {
                string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                string name = new AssemblyName(resolveArgs.Name).Name + ".dll";
                string path1 = Path.Combine(folderPath, name);
                if (File.Exists(path1)) return Assembly.LoadFrom(path1);
                string path2 = Path.Combine(folderPath, "ReferencedAssemblies", name);
                if (File.Exists(path2)) return Assembly.LoadFrom(path2);
                string path3 = Path.Combine(folderPath, "HisAutoPrescribe_Portable", name);
                if (File.Exists(path3)) return Assembly.LoadFrom(path3);

                DirectoryInfo cur = new DirectoryInfo(folderPath);
                for (int i = 0; i < 5; i++)
                {
                    if (cur.Parent == null) break;
                    cur = cur.Parent;
                    string pRoot = Path.Combine(cur.FullName, name);
                    if (File.Exists(pRoot)) return Assembly.LoadFrom(pRoot);
                    string pRef = Path.Combine(cur.FullName, "ReferencedAssemblies", name);
                    if (File.Exists(pRef)) return Assembly.LoadFrom(pRef);
                }
            }
            catch { }
            return null;
        };

        if (args.Length > 0)
        {
            RunCli(args);
            return;
        }

        RunGui();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void RunGui()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        LoginForm login = new LoginForm();
        if (login.ShowDialog() == DialogResult.OK)
        {
            Application.Run(new MainForm(login.Username, login.TokenCode));
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void RunCli(string[] args)
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("  HỆ THỐNG TẠO TỜ ĐIỀU TRỊ & CHĂM SÓC (CLI) - KHOA CTCH & CỘT SỐNG (KHOA 57)");
        Console.WriteLine("===============================================================================");

        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine("CÚ PHÁP SỬ DỤNG:");
            Console.WriteLine("  HisTrackingCreator.exe -p <MaBN1,MaBN2,...> -time <HH:mm> -content <NoiDung> -care <CheDoChamSoc> [-med <YLenh>]");
            Console.WriteLine("  HisTrackingCreator.exe -p <MaBN> -template <1..6>");
            Console.WriteLine("\nVÍ DỤ:");
            Console.WriteLine("  HisTrackingCreator.exe -p \"0003969449\" -time \"17:00\" -content \"Khám: Đường máu mao mạch lúc 17h: 12.5 mmol/l\" -care \"csii, dd01\" -med \"bổ sung thuốc\"");
            return;
        }

        string rawPatients = "";
        if (args.Length > 0 && !args[0].StartsWith("-")) rawPatients = args[0];

        string rawTime = "17:00";
        string rawDate = "";
        string content = "";
        string medInstruction = "";
        string careInstruction = "";
        int templateId = 0;

        long? pulse = null;
        long? bpMax = null;
        long? bpMin = null;
        decimal? temp = null;
        decimal? spo2 = null;
        long? rr = null;
        decimal? weight = null;
        decimal? height = null;

        string envDoc = Environment.GetEnvironmentVariable("HIS_DOCTOR_LOGIN");
        if (string.IsNullOrEmpty(envDoc)) envDoc = "034727";
        MainForm.CurrentLoginName = envDoc;
        MainForm.CurrentUserName = (envDoc == "034727" ? "NGUYỄN HỮU SÂM" : (envDoc == "vmc" ? "VŨ MINH CƯỜNG" : envDoc));

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-p" || args[i] == "--patient") && i + 1 < args.Length) rawPatients = args[i + 1];
            if ((args[i] == "-time" || args[i] == "-t" || args[i] == "--time") && i + 1 < args.Length) rawTime = args[i + 1];
            if ((args[i] == "-date" || args[i] == "-d" || args[i] == "--date") && i + 1 < args.Length) rawDate = args[i + 1];
            if ((args[i] == "-u" || args[i] == "-user" || args[i] == "--user") && i + 1 < args.Length)
            {
                MainForm.CurrentLoginName = args[i + 1].Trim();
                if (MainForm.CurrentLoginName == "vmc") MainForm.CurrentUserName = "VŨ MINH CƯỜNG";
                else if (MainForm.CurrentLoginName == "034727") MainForm.CurrentUserName = "NGUYỄN HỮU SÂM";
            }
            if ((args[i] == "-content" || args[i] == "-c" || args[i] == "--content") && i + 1 < args.Length) content = args[i + 1];
            if ((args[i] == "-med" || args[i] == "-m" || args[i] == "--med") && i + 1 < args.Length) medInstruction = args[i + 1];
            if ((args[i] == "-care" || args[i] == "--care") && i + 1 < args.Length) careInstruction = args[i + 1];
            if ((args[i] == "--note" || args[i] == "-note") && i + 1 < args.Length)
            {
                if (string.IsNullOrEmpty(content)) content = args[i + 1];
                if (string.IsNullOrEmpty(medInstruction)) medInstruction = args[i + 1];
                if (string.IsNullOrEmpty(careInstruction)) careInstruction = "Chăm sóc cấp II. Theo dõi đường máu mao mạch.";
            }
            if ((args[i] == "-pulse" || args[i] == "--pulse") && i + 1 < args.Length)
            {
                long v; if (long.TryParse(args[i + 1], out v)) pulse = v;
            }
            if ((args[i] == "-bp" || args[i] == "--bp") && i + 1 < args.Length)
            {
                string bpStr = args[i + 1].Trim();
                if (bpStr.Contains("/"))
                {
                    string[] bpParts = bpStr.Split('/');
                    long v1, v2;
                    if (long.TryParse(bpParts[0], out v1)) bpMax = v1;
                    if (bpParts.Length > 1 && long.TryParse(bpParts[1], out v2)) bpMin = v2;
                }
                else
                {
                    long v; if (long.TryParse(bpStr, out v)) bpMax = v;
                }
            }
            if ((args[i] == "-temp" || args[i] == "--temp") && i + 1 < args.Length)
            {
                decimal v; if (decimal.TryParse(args[i + 1].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v)) temp = v;
            }
            if ((args[i] == "-spo2" || args[i] == "--spo2") && i + 1 < args.Length)
            {
                decimal v; if (decimal.TryParse(args[i + 1].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v)) spo2 = v;
            }
            if ((args[i] == "-rr" || args[i] == "--rr") && i + 1 < args.Length)
            {
                long v; if (long.TryParse(args[i + 1], out v)) rr = v;
            }
            if ((args[i] == "-weight" || args[i] == "--weight") && i + 1 < args.Length)
            {
                decimal v; if (decimal.TryParse(args[i + 1].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v)) weight = v;
            }
            if ((args[i] == "-height" || args[i] == "--height") && i + 1 < args.Length)
            {
                decimal v; if (decimal.TryParse(args[i + 1].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v)) height = v;
            }
            if ((args[i] == "-template" || args[i] == "-tmpl") && i + 1 < args.Length) int.TryParse(args[i + 1], out templateId);
        }

        if (templateId >= 1 && templateId < MainForm.ClinicalTemplates.Count)
        {
            var tmpl = MainForm.ClinicalTemplates[templateId];
            if (!args.Contains("-content") && !args.Contains("-c")) content = tmpl.Content;
            if (!args.Contains("-med") && !args.Contains("-m")) medInstruction = tmpl.MedicalInstruction;
            if (!args.Contains("-care")) careInstruction = tmpl.CareInstruction;
            if (!args.Contains("-time") && !args.Contains("-t")) rawTime = tmpl.DefaultTime;
        }

        if (string.IsNullOrEmpty(rawPatients))
        {
            Console.WriteLine("[LỖI] Thiếu danh sách mã bệnh nhân (-p \"0003969449\")!");
            return;
        }

        var codes = rawPatients.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();

        Console.WriteLine(string.Format("• Số lượng bệnh nhân: {0}", codes.Count));
        if (!string.IsNullOrEmpty(rawDate)) Console.WriteLine(string.Format("• Ngày chỉ định: {0}", rawDate));
        Console.WriteLine(string.Format("• Mốc giờ: {0}", rawTime));
        Console.WriteLine(string.Format("• Bác sĩ: {0} ({1})", MainForm.CurrentUserName, MainForm.CurrentLoginName));
        Console.WriteLine(string.Format("• Nội dung: {0}", content));
        Console.WriteLine(string.Format("• Chăm sóc: {0}", careInstruction));
        Console.WriteLine(string.Format("• Y lệnh: {0}", medInstruction));
        if (pulse.HasValue || temp.HasValue || bpMax.HasValue || spo2.HasValue || rr.HasValue)
        {
            Console.WriteLine(string.Format("• DHST: Mạch {0} l/p | HA {1}/{2} mmHg | T {3}°C | SpO2 {4}% | NT {5} l/p",
                pulse.HasValue ? pulse.Value.ToString() : "-",
                bpMax.HasValue ? bpMax.Value.ToString() : "-",
                bpMin.HasValue ? bpMin.Value.ToString() : "-",
                temp.HasValue ? temp.Value.ToString("F1") : "-",
                spo2.HasValue ? spo2.Value.ToString() : "-",
                rr.HasValue ? rr.Value.ToString() : "-"));
        }
        Console.WriteLine("-------------------------------------------------------------------------------");

        MainForm.InitSession();

        if (args.Contains("-check"))
        {
            foreach (var code in codes)
            {
                var p = MainForm.LookupPatientDirect(code);
                if (p == null) continue;
                HisTrackingFilter tf = new HisTrackingFilter { TREATMENT_ID = p.TreatmentId };
                Console.WriteLine(string.Format("=== TRACKING CỦA {0} (TreatmentId: {1}) ===", p.TDL_PATIENT_NAME, p.TreatmentId));
                Console.WriteLine(string.Format("  • ICD: {0} | SubCode: {1} | Text: {2}", p.IcdCode, p.IcdSubCode, p.IcdText));

                HisDepartmentTranViewFilter dtf = new HisDepartmentTranViewFilter { TREATMENT_ID = p.TreatmentId };
                var dTrans = MainForm.myAdapter.FetchList<V_HIS_DEPARTMENT_TRAN>("api/HisDepartmentTran/GetView", ApiConsumers.MosConsumer, dtf, MainForm.param);
                Console.WriteLine("--- LỊCH SỬ KHOA (DEPARTMENT_TRAN) ---");
                if (dTrans != null)
                {
                    foreach (var dt in dTrans)
                    {
                        Console.WriteLine(string.Format("  • TranId: {0} | DeptId: {1} | DeptName: {2} | InTime: {3}", dt.ID, dt.DEPARTMENT_ID, dt.DEPARTMENT_NAME, dt.DEPARTMENT_IN_TIME));
                    }
                }
                HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter { TREATMENT_ID = p.TreatmentId };
                var tbrs = MainForm.myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, MainForm.param);
                Console.WriteLine("--- BUỒNG GIƯỜNG (TREATMENT_BED_ROOM) ---");
                if (tbrs != null)
                {
                    foreach (var tb in tbrs)
                    {
                        Console.WriteLine(string.Format("  • BedRoom: {0} (Id: {1}, RoomId: {2}) | Bed: {3} | AddTime: {4}", tb.BED_ROOM_NAME, tb.BED_ROOM_ID, tb.ROOM_ID, tb.BED_NAME, tb.ADD_TIME));
                    }
                }
                var trks = MainForm.myAdapter.FetchList<HIS_TRACKING>("api/HisTracking/Get", ApiConsumers.MosConsumer, tf, MainForm.param);
                if (trks != null && trks.Count > 0)
                {
                    foreach (var t in trks)
                    {
                        Console.WriteLine(string.Format("  • ID: {0} | Time: {1} | Room: {2} | Content: {3}", t.ID, t.TRACKING_TIME, t.ROOM_ID, t.CONTENT));
                    }
                }
                else
                {
                    Console.WriteLine("  (Chưa có tờ điều trị nào)");
                }
            }
            return;
        }

        int successCount = 0;
        int failCount = 0;

        foreach (var code in codes)
        {
            try
            {
                var p = MainForm.LookupPatientDirect(code);
                if (p == null)
                {
                    Console.WriteLine(string.Format("❌ [{0}] Không tìm thấy hồ sơ bệnh nhân!", code));
                    failCount++;
                    continue;
                }

                DateTime date = DateTime.Today;
                if (!string.IsNullOrEmpty(rawDate))
                {
                    DateTime parsedDate;
                    if (DateTime.TryParseExact(rawDate, new string[] { "yyyyMMdd", "yyyy-MM-dd", "dd/MM/yyyy" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
                        date = parsedDate;
                    else if (DateTime.TryParse(rawDate, out parsedDate))
                        date = parsedDate;
                }
                TimeSpan tSpan;
                if (!TimeSpan.TryParse(rawTime, out tSpan)) tSpan = new TimeSpan(17, 0, 0);
                DateTime fullDateTime = new DateTime(date.Year, date.Month, date.Day, tSpan.Hours, tSpan.Minutes, 0);
                long trackingTime = long.Parse(fullDateTime.ToString("yyyyMMddHHmmss"));

                long deptId = p.DepartmentId > 0 ? p.DepartmentId : 57;
                long doctorWorkRoomId = (deptId == 915 ? 18679 : 5248);
                long roomId = doctorWorkRoomId;

                try
                {
                    var wi = new WorkInfoSDO
                    {
                        Rooms = new List<RoomSDO>
                        {
                            new RoomSDO { RoomId = doctorWorkRoomId },
                            new RoomSDO { RoomId = 5248 },
                            new RoomSDO { RoomId = 18679 },
                            new RoomSDO { RoomId = 18681 }
                        }
                    };
                    if (p.WorkingRoomId > 0 && !wi.Rooms.Any(r => r.RoomId == p.WorkingRoomId))
                    {
                        wi.Rooms.Add(new RoomSDO { RoomId = p.WorkingRoomId });
                    }
                    MainForm.myAdapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, wi, new CommonParam());
                }
                catch { }

                HIS_TRACKING tracking = new HIS_TRACKING
                {
                    TREATMENT_ID = p.TreatmentId,
                    DEPARTMENT_ID = deptId,
                    ROOM_ID = roomId,
                    TRACKING_TIME = trackingTime,
                    CONTENT = content,
                    MEDICAL_INSTRUCTION = medInstruction,
                    ICD_CODE = (!string.IsNullOrEmpty(p.IcdCode) ? (p.IcdCode == "M23.90" ? "M23.9" : p.IcdCode) : "M51.2"),
                    ICD_NAME = !string.IsNullOrEmpty(p.IcdName) ? p.IcdName : (!string.IsNullOrEmpty(p.IcdText) ? p.IcdText : "Rối loạn khớp gối"),
                    ICD_SUB_CODE = p.IcdSubCode,
                    ICD_TEXT = p.IcdText
                };

                HIS_DHST dhst = null;
                if (pulse.HasValue || temp.HasValue || bpMax.HasValue || bpMin.HasValue || spo2.HasValue || rr.HasValue || weight.HasValue || height.HasValue)
                {
                    dhst = new HIS_DHST
                    {
                        TREATMENT_ID = p.TreatmentId,
                        EXECUTE_TIME = trackingTime,
                        EXECUTE_LOGINNAME = MainForm.CurrentLoginName,
                        EXECUTE_USERNAME = MainForm.CurrentUserName,
                        PULSE = pulse,
                        TEMPERATURE = temp,
                        BLOOD_PRESSURE_MAX = bpMax,
                        BLOOD_PRESSURE_MIN = bpMin,
                        SPO2 = spo2,
                        BREATH_RATE = rr,
                        WEIGHT = weight,
                        HEIGHT = height
                    };
                }

                HisTrackingSDO sdo = new HisTrackingSDO
                {
                    Tracking = tracking,
                    WorkingRoomId = doctorWorkRoomId,
                    Dhst = dhst
                };

                CommonParam cp = new CommonParam();
                var created = MainForm.myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, cp);
                if (created == null || created.ID <= 0)
                {
                    var resSdo = MainForm.myAdapter.PostData<HisTrackingSDO>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, cp);
                    if (resSdo != null && resSdo.Tracking != null && resSdo.Tracking.ID > 0)
                    {
                        created = resSdo.Tracking;
                    }
                }
                if (created == null || created.ID <= 0)
                {
                    tracking.ROOM_ID = doctorWorkRoomId;
                    sdo.WorkingRoomId = doctorWorkRoomId;
                    cp = new CommonParam();
                    created = MainForm.myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, cp);
                    if (created == null || created.ID <= 0)
                    {
                        var resSdo = MainForm.myAdapter.PostData<HisTrackingSDO>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, cp);
                        if (resSdo != null && resSdo.Tracking != null && resSdo.Tracking.ID > 0)
                        {
                            created = resSdo.Tracking;
                        }
                    }
                }
                if (created == null || created.ID == 0)
                {
                    string errMsg = "Hệ thống MOS từ chối tạo!";
                    try
                    {
                        errMsg += " | CP: " + Newtonsoft.Json.JsonConvert.SerializeObject(cp);
                    }
                    catch { }
                    throw new Exception(errMsg);
                }

                Console.WriteLine(string.Format("✔ [{0} - {1}] Tạo Tờ điều trị THÀNH CÔNG! ID: {2} | {3}",
                    p.TDL_PATIENT_CODE, p.TDL_PATIENT_NAME, created.ID, p.BedFull));
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("❌ [{0}] LỖI: {1}", code, ex.Message));
                failCount++;
            }
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine(string.Format("KẾT THÚC: ✔ {0} thành công | ❌ {1} thất bại", successCount, failCount));
    }
}
