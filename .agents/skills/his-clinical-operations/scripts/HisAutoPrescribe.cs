using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Inventec.Core;
using Inventec.Token.ClientSystem;
using Inventec.Common.Adapter;
using HIS.Desktop.LocalStorage.ConfigSystem;
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

public class MedicineStockInfo
{
    public long MediStockId { get; set; }
    public string MediStockCode { get; set; }
    public string MediStockName { get; set; }

    public MedicineStockInfo(long id, string code, string name)
    {
        MediStockId = id;
        MediStockCode = code;
        MediStockName = name;
    }

    public override string ToString()
    {
        return string.Format("{0} ({1})", MediStockName, MediStockCode);
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
        this.Text = "ĐĂNG NHẬP BÁC SĨ - HỆ THỐNG HIS AUTO PRESCRIBE";
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
            Text = "HIS / MOS AUTO PRESCRIBE",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            Location = new Point(20, 14),
            AutoSize = true
        };

        Label lblSubTitle = new Label
        {
            Text = "Đăng nhập tài khoản Bác sĩ để quét thuốc & thực hiện y lệnh tự động",
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

public class MainForm : Form
{
    private static MyAdapter adapter = new MyAdapter();
    private static CommonParam param = new CommonParam();
    private static string currentToken = null;

    // Doctor info
    public string CurrentLoginName { get; set; }
    public string CurrentUserName { get; set; }
    public long CurrentDepartmentId { get; set; }
    public string CurrentDepartmentName { get; set; }
    public long CurrentRoomId { get; set; }
    public string CurrentRoomName { get; set; }

    // Predefined Stocks
    public static readonly List<MedicineStockInfo> CommonStocks = new List<MedicineStockInfo>
    {
        new MedicineStockInfo(810, "TT_KCTCHCS", "Tủ trực Khoa CTCH & Cột sống"),
        new MedicineStockInfo(4210, "KT_KD15", "Kho thuốc viên"),
        new MedicineStockInfo(4209, "KT_KD14", "Kho thuốc ống"),
        new MedicineStockInfo(4208, "KT_KD13", "Kho thuốc Hướng thần"),
        new MedicineStockInfo(4207, "KT_KD12", "Kho thuốc Gây nghiện"),
        new MedicineStockInfo(753, "LA_TTDDLS", "Kho SP Dinh dưỡng điều trị"),
        new MedicineStockInfo(7787, "TTSPDD_9", "Tủ trực SP Dinh dưỡng Khoa 57"),
        new MedicineStockInfo(796, "KVT_KCTCGCS", "Kho Vật tư Khoa CTCH & Cột sống"),
        new MedicineStockInfo(4168, "KT_KD10", "Kho Vắc xin")
    };

    // UI Tab Control
    private TabControl tabMain;
    private TabPage tabPrescribe;
    private TabPage tabMedicineScan;
    private TabPage tabPatientInspect;

    // Doctor Session Bar
    private Panel pnlDoctorHeader;
    private Label lblDoctorName;
    private Label lblDepartmentRoom;
    private Button btnSwitchUser;

    // Prescribe UI
    private DataGridView dgvPrescribe;
    private Button btnAddRow;
    private Button btnPasteExcel;
    private Button btnClearRows;
    private Button btnExecuteAll;
    private ProgressBar pbPrescribe;
    private Label lblPrescribeSummary;

    // Medicine Scan UI
    private TextBox txtMedSearch;
    private ComboBox cboMedStock;
    private Button btnMedSearch;
    private DataGridView dgvMedResults;
    private Label lblMedCount;

    // Patient Scan UI
    private TextBox txtPatSearch;
    private Button btnPatSearch;
    private Label lblPatSummary;
    private DataGridView dgvPatOrders;

    public MainForm(string loginName, string tokenCode)
    {
        CurrentLoginName = loginName;
        currentToken = tokenCode;
        ApiConsumers.SetConsunmer(currentToken);

        InitializeComponents();
        LoadDoctorProfile();
    }

    private void LoadDoctorProfile()
    {
        try
        {
            CurrentDepartmentId = 57;
            CurrentDepartmentName = "Khoa Chấn thương Chỉnh hình & Cột sống";
            CurrentRoomId = 5248;
            CurrentRoomName = "Phòng 734";

            HisEmployeeFilter ef = new HisEmployeeFilter();
            ef.LOGINNAME__EXACT = CurrentLoginName;
            var emps = adapter.FetchList<HIS_EMPLOYEE>("api/HisEmployee/Get", ApiConsumers.MosConsumer, ef, param);
            if (emps != null && emps.Count > 0)
            {
                CurrentUserName = emps[0].TDL_USERNAME;
            }
            else
            {
                CurrentUserName = CurrentLoginName.ToUpper();
            }

            lblDoctorName.Text = string.Format("👨‍⚕️ Bác sĩ: {0} ({1})", CurrentUserName, CurrentLoginName);
            lblDepartmentRoom.Text = string.Format("🏥 {0} | {1}", CurrentDepartmentName, CurrentRoomName);
        }
        catch
        {
            lblDoctorName.Text = string.Format("👨‍⚕️ Bác sĩ: {0}", CurrentLoginName);
        }
    }

    private void InitializeComponents()
    {
        this.Text = "HIS AUTO PRESCRIBE & MEDICINE SCANNER - PHẦN MỀM KÊ ĐƠN TỰ ĐỘNG & QUÉT THUỐC";
        this.Size = new Size(1300, 750);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        this.BackColor = Color.FromArgb(243, 244, 246);

        try { if (File.Exists("APP.ico")) this.Icon = new Icon("APP.ico"); } catch { }

        // Top Doctor Header Bar
        pnlDoctorHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(15, 23, 42)
        };

        lblDoctorName = new Label
        {
            Text = "👨‍⚕️ Bác sĩ: Đang tải...",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Location = new Point(20, 12),
            AutoSize = true
        };

        lblDepartmentRoom = new Label
        {
            Text = "🏥 Khoa Chấn thương Chỉnh hình & Cột sống",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Location = new Point(22, 38),
            AutoSize = true
        };

        btnSwitchUser = new Button
        {
            Text = "Đổi Bác sĩ 🔄",
            Location = new Point(1130, 15),
            Width = 135,
            Height = 36,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.FromArgb(226, 232, 240),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnSwitchUser.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
        btnSwitchUser.Click += BtnSwitchUser_Click;

        pnlDoctorHeader.Controls.Add(lblDoctorName);
        pnlDoctorHeader.Controls.Add(lblDepartmentRoom);
        pnlDoctorHeader.Controls.Add(btnSwitchUser);
        this.Controls.Add(pnlDoctorHeader);

        // Tab Control
        tabMain = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };

        tabPrescribe = new TabPage { Text = "  ⚡ KÊ ĐƠN TỰ ĐỘNG (AUTO PRESCRIBE)  ", BackColor = Color.White };
        SetupTabPrescribe();
        tabMain.TabPages.Add(tabPrescribe);

        tabMedicineScan = new TabPage { Text = "  🔍 QUÉT THUỐC & TỒN KHO (MEDICINE SCANNER)  ", BackColor = Color.White };
        SetupTabMedicineScan();
        tabMain.TabPages.Add(tabMedicineScan);

        tabPatientInspect = new TabPage { Text = "  📋 TRA CỨU Y LỆNH BỆNH NHÂN  ", BackColor = Color.White };
        SetupTabPatientInspect();
        tabMain.TabPages.Add(tabPatientInspect);

        this.Controls.Add(tabMain);
        tabMain.BringToFront();
    }

    private void SetupTabPrescribe()
    {
        Panel pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.FromArgb(248, 250, 252)
        };

        btnAddRow = new Button
        {
            Text = "+ Thêm dòng",
            Location = new Point(15, 10),
            Width = 115,
            Height = 35,
            BackColor = Color.FromArgb(238, 242, 255),
            ForeColor = Color.FromArgb(79, 70, 229),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnAddRow.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
        btnAddRow.Click += (s, e) => AddPrescribeRow();

        btnPasteExcel = new Button
        {
            Text = "📋 Dán từ Excel (Ctrl+V)",
            Location = new Point(140, 10),
            Width = 180,
            Height = 35,
            BackColor = Color.FromArgb(236, 253, 245),
            ForeColor = Color.FromArgb(5, 150, 105),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPasteExcel.FlatAppearance.BorderColor = Color.FromArgb(167, 243, 208);
        btnPasteExcel.Click += (s, e) => PasteFromClipboard();

        btnClearRows = new Button
        {
            Text = "Xóa hết",
            Location = new Point(330, 10),
            Width = 90,
            Height = 35,
            BackColor = Color.FromArgb(254, 242, 242),
            ForeColor = Color.FromArgb(220, 38, 38),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btnClearRows.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
        btnClearRows.Click += (s, e) => dgvPrescribe.Rows.Clear();

        btnExecuteAll = new Button
        {
            Text = "⚡ THỰC HIỆN KÊ ĐƠN TẤT CẢ",
            Location = new Point(940, 10),
            Width = 240,
            Height = 35,
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnExecuteAll.FlatAppearance.BorderSize = 0;
        btnExecuteAll.Click += BtnExecuteAll_Click;

        pnlToolbar.Controls.Add(btnAddRow);
        pnlToolbar.Controls.Add(btnPasteExcel);
        pnlToolbar.Controls.Add(btnClearRows);
        pnlToolbar.Controls.Add(btnExecuteAll);

        dgvPrescribe = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 32 },
            Font = new Font("Segoe UI", 9.5f)
        };

        dgvPrescribe.Columns.Add("Stt", "STT");
        dgvPrescribe.Columns["Stt"].FillWeight = 30;
        dgvPrescribe.Columns["Stt"].ReadOnly = true;

        dgvPrescribe.Columns.Add("PatientKey", "Mã BN / Mã ĐT (*)");
        dgvPrescribe.Columns["PatientKey"].FillWeight = 100;

        dgvPrescribe.Columns.Add("PatientName", "Tên BN & Buồng Giường");
        dgvPrescribe.Columns["PatientName"].FillWeight = 160;
        dgvPrescribe.Columns["PatientName"].ReadOnly = true;

        dgvPrescribe.Columns.Add("MedicineKeyword", "Thuốc / Hoạt chất (*)");
        dgvPrescribe.Columns["MedicineKeyword"].FillWeight = 140;

        dgvPrescribe.Columns.Add("DosageAmount", "Số lượng / Liều (*)");
        dgvPrescribe.Columns["DosageAmount"].FillWeight = 70;

        var colStock = new DataGridViewComboBoxColumn
        {
            Name = "StockId",
            HeaderText = "Kho xuất thuốc (*)",
            FillWeight = 150
        };
        foreach (var s in CommonStocks) colStock.Items.Add(s);
        dgvPrescribe.Columns.Add(colStock);

        dgvPrescribe.Columns.Add("Tutorial", "Thời gian / Cách dùng");
        dgvPrescribe.Columns["Tutorial"].FillWeight = 140;

        dgvPrescribe.Columns.Add("Status", "Trạng thái");
        dgvPrescribe.Columns["Status"].FillWeight = 80;
        dgvPrescribe.Columns["Status"].ReadOnly = true;

        dgvPrescribe.Columns.Add("Result", "Kết quả / Mã Y lệnh");
        dgvPrescribe.Columns["Result"].FillWeight = 160;
        dgvPrescribe.Columns["Result"].ReadOnly = true;

        Panel pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            BackColor = Color.FromArgb(248, 250, 252)
        };

        pbPrescribe = new ProgressBar
        {
            Location = new Point(15, 12),
            Width = 350,
            Height = 20,
            Visible = false
        };

        lblPrescribeSummary = new Label
        {
            Text = "Mẹo: Bạn có thể chọn và Copy nhiều dòng từ Excel rồi nhấn nút 'Dán từ Excel' hoặc Ctrl+V để nhập nhanh!",
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            Location = new Point(380, 14),
            AutoSize = true
        };

        pnlBottom.Controls.Add(pbPrescribe);
        pnlBottom.Controls.Add(lblPrescribeSummary);

        tabPrescribe.Controls.Add(dgvPrescribe);
        tabPrescribe.Controls.Add(pnlToolbar);
        tabPrescribe.Controls.Add(pnlBottom);

        AddPrescribeRow("0003951644", "Lantus", 10, CommonStocks[0], "Tối 21h tiêm dưới da");
    }

    private void AddPrescribeRow(string pat = "", string med = "", decimal amount = 1, MedicineStockInfo stock = null, string tut = "")
    {
        int idx = dgvPrescribe.Rows.Add();
        var row = dgvPrescribe.Rows[idx];
        row.Cells["Stt"].Value = idx + 1;
        row.Cells["PatientKey"].Value = pat;
        row.Cells["PatientName"].Value = "";
        row.Cells["MedicineKeyword"].Value = med;
        row.Cells["DosageAmount"].Value = amount;
        row.Cells["StockId"].Value = stock ?? CommonStocks[0];
        row.Cells["Tutorial"].Value = tut;
        row.Cells["Status"].Value = "Chờ kê";
        row.Cells["Result"].Value = "";
    }

    private void PasteFromClipboard()
    {
        try
        {
            string clip = Clipboard.GetText();
            if (string.IsNullOrEmpty(clip)) return;

            string[] lines = clip.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string[] parts = line.Split('\t');
                if (parts.Length >= 2)
                {
                    string pat = parts[0].Trim();
                    string med = parts.Length > 1 ? parts[1].Trim() : "Lantus";
                    decimal amt = 1;
                    if (parts.Length > 2) decimal.TryParse(parts[2].Trim(), out amt);
                    string tut = parts.Length > 3 ? parts[3].Trim() : "";

                    AddPrescribeRow(pat, med, amt, CommonStocks[0], tut);
                }
            }
            lblPrescribeSummary.Text = string.Format("✔ Đã nạp thành công {0} dòng từ Clipboard.", lines.Length);
            lblPrescribeSummary.ForeColor = Color.FromArgb(5, 150, 105);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi dán dữ liệu: " + ex.Message, "Lỗi Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetupTabMedicineScan()
    {
        Panel pnlSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(248, 250, 252)
        };

        Label lbl1 = new Label { Text = "Tìm kiếm thuốc / hoạt chất:", Location = new Point(15, 10), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        txtMedSearch = new TextBox { Text = "Lantus", Location = new Point(15, 30), Width = 260, Font = new Font("Segoe UI", 10.5f) };

        Label lbl2 = new Label { Text = "Kho Dược / Tủ trực:", Location = new Point(290, 10), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        cboMedStock = new ComboBox { Location = new Point(290, 30), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10f) };
        
        cboMedStock.Items.Add(new MedicineStockInfo(0, "ALL", "--- Tất cả các kho dược & tủ trực ---"));
        foreach (var s in CommonStocks) cboMedStock.Items.Add(s);
        cboMedStock.SelectedIndex = 0;

        btnMedSearch = new Button
        {
            Text = "🔍 Quét Tồn Kho Ngay",
            Location = new Point(625, 27),
            Width = 175,
            Height = 32,
            BackColor = Color.FromArgb(26, 86, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnMedSearch.FlatAppearance.BorderSize = 0;
        btnMedSearch.Click += BtnMedSearch_Click;

        lblMedCount = new Label
        {
            Text = "Sẵn sàng quét tồn kho thuốc toàn viện.",
            Location = new Point(815, 34),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        pnlSearch.Controls.Add(lbl1);
        pnlSearch.Controls.Add(txtMedSearch);
        pnlSearch.Controls.Add(lbl2);
        pnlSearch.Controls.Add(cboMedStock);
        pnlSearch.Controls.Add(btnMedSearch);
        pnlSearch.Controls.Add(lblMedCount);

        dgvMedResults = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 30 }
        };

        dgvMedResults.Columns.Add("Stt", "STT");
        dgvMedResults.Columns["Stt"].FillWeight = 30;
        dgvMedResults.Columns.Add("MedicineName", "Tên Thuốc & Hàm lượng");
        dgvMedResults.Columns["MedicineName"].FillWeight = 220;
        dgvMedResults.Columns.Add("MedicineCode", "Mã Thuốc");
        dgvMedResults.Columns["MedicineCode"].FillWeight = 100;
        dgvMedResults.Columns.Add("ActiveIngr", "Hoạt chất");
        dgvMedResults.Columns["ActiveIngr"].FillWeight = 140;
        dgvMedResults.Columns.Add("StockName", "Kho / Tủ Trực");
        dgvMedResults.Columns["StockName"].FillWeight = 150;
        dgvMedResults.Columns.Add("StockAmount", "Tồn Khả Dụng");
        dgvMedResults.Columns["StockAmount"].FillWeight = 90;
        dgvMedResults.Columns.Add("UnitName", "Đơn Vị");
        dgvMedResults.Columns["UnitName"].FillWeight = 60;
        dgvMedResults.Columns.Add("ExpiredDate", "Hạn Sử Dụng");
        dgvMedResults.Columns["ExpiredDate"].FillWeight = 90;

        tabMedicineScan.Controls.Add(dgvMedResults);
        tabMedicineScan.Controls.Add(pnlSearch);
    }

    private async void BtnMedSearch_Click(object sender, EventArgs e)
    {
        string kw = txtMedSearch.Text.Trim();
        var selStock = (MedicineStockInfo)cboMedStock.SelectedItem;
        long stockId = selStock != null ? selStock.MediStockId : 0;

        lblMedCount.Text = "Đang quét dữ liệu máy chủ...";
        btnMedSearch.Enabled = false;
        dgvMedResults.Rows.Clear();

        try
        {
            var results = await Task.Run(() =>
            {
                List<V_HIS_MEDICINE_BEAN> beans = new List<V_HIS_MEDICINE_BEAN>();

                HisMedicineBeanViewFilter bf = new HisMedicineBeanViewFilter();
                if (stockId > 0) bf.MEDI_STOCK_ID = stockId;
                bf.IS_ACTIVE = 1;
                var rawBeans = adapter.FetchList<V_HIS_MEDICINE_BEAN>("api/HisMedicineBean/GetView", ApiConsumers.MosConsumer, bf, param);

                if (rawBeans != null)
                {
                    if (!string.IsNullOrEmpty(kw))
                    {
                        rawBeans = rawBeans.Where(b => (b.MEDICINE_TYPE_NAME != null && b.MEDICINE_TYPE_NAME.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                            || (b.MEDICINE_TYPE_CODE != null && b.MEDICINE_TYPE_CODE.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                            || (b.ACTIVE_INGR_BHYT_NAME != null && b.ACTIVE_INGR_BHYT_NAME.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                    }
                    beans = rawBeans;
                }
                return beans;
            });

            if (results != null && results.Count > 0)
            {
                var grouped = results.GroupBy(b => new { b.MEDICINE_TYPE_ID, b.MEDI_STOCK_ID, b.EXPIRED_DATE }).OrderBy(g => g.First().MEDICINE_TYPE_NAME);
                int idx = 1;
                foreach (var g in grouped)
                {
                    var item = g.First();
                    decimal totalAmt = g.Sum(x => x.AMOUNT);
                    string expStr = item.EXPIRED_DATE.HasValue ? item.EXPIRED_DATE.Value.ToString().Substring(0, 8) : "";
                    if (expStr.Length == 8) expStr = expStr.Substring(6, 2) + "/" + expStr.Substring(4, 2) + "/" + expStr.Substring(0, 4);

                    dgvMedResults.Rows.Add(
                        idx++,
                        item.MEDICINE_TYPE_NAME,
                        item.MEDICINE_TYPE_CODE,
                        item.ACTIVE_INGR_BHYT_NAME,
                        item.MEDI_STOCK_NAME,
                        totalAmt.ToString("N1"),
                        item.SERVICE_UNIT_NAME,
                        expStr
                    );
                }
                lblMedCount.Text = string.Format("✔ Tìm thấy {0} lô thuốc phù hợp.", grouped.Count());
                lblMedCount.ForeColor = Color.FromArgb(5, 150, 105);
            }
            else
            {
                lblMedCount.Text = "Không tìm thấy thuốc nào khớp với từ khóa trong kho đã chọn.";
                lblMedCount.ForeColor = Color.FromArgb(220, 38, 38);
            }
        }
        catch (Exception ex)
        {
            lblMedCount.Text = "Lỗi quét thuốc: " + ex.Message;
            lblMedCount.ForeColor = Color.Red;
        }
        finally
        {
            btnMedSearch.Enabled = true;
        }
    }

    private void SetupTabPatientInspect()
    {
        Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(248, 250, 252) };
        Label l = new Label { Text = "Mã Bệnh nhân hoặc Mã Điều trị:", Location = new Point(15, 8), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
        txtPatSearch = new TextBox { Text = "0003951644", Location = new Point(15, 27), Width = 250, Font = new Font("Segoe UI", 10.5f) };
        btnPatSearch = new Button
        {
            Text = "🔎 Tra Cứu Y Lệnh",
            Location = new Point(275, 25),
            Width = 150,
            Height = 32,
            BackColor = Color.FromArgb(26, 86, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPatSearch.FlatAppearance.BorderSize = 0;
        btnPatSearch.Click += BtnPatSearch_Click;

        lblPatSummary = new Label { Text = "", Location = new Point(440, 30), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };

        pnlTop.Controls.Add(l);
        pnlTop.Controls.Add(txtPatSearch);
        pnlTop.Controls.Add(btnPatSearch);
        pnlTop.Controls.Add(lblPatSummary);

        dgvPatOrders = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 30 }
        };

        dgvPatOrders.Columns.Add("Stt", "STT");
        dgvPatOrders.Columns["Stt"].FillWeight = 30;
        dgvPatOrders.Columns.Add("ReqType", "Loại Y Lệnh");
        dgvPatOrders.Columns["ReqType"].FillWeight = 90;
        dgvPatOrders.Columns.Add("ServiceName", "Tên Thuốc / Dịch Vụ / Suất Ăn");
        dgvPatOrders.Columns["ServiceName"].FillWeight = 220;
        dgvPatOrders.Columns.Add("Amount", "Số Lượng");
        dgvPatOrders.Columns["Amount"].FillWeight = 60;
        dgvPatOrders.Columns.Add("InstructionTime", "Thời Gian Dùng");
        dgvPatOrders.Columns["InstructionTime"].FillWeight = 110;
        dgvPatOrders.Columns.Add("Status", "Trạng Thái");
        dgvPatOrders.Columns["Status"].FillWeight = 80;

        tabPatientInspect.Controls.Add(dgvPatOrders);
        tabPatientInspect.Controls.Add(pnlTop);
    }

    private async void BtnPatSearch_Click(object sender, EventArgs e)
    {
        string kw = txtPatSearch.Text.Trim();
        if (string.IsNullOrEmpty(kw)) return;

        btnPatSearch.Enabled = false;
        lblPatSummary.Text = "Đang tra cứu...";
        dgvPatOrders.Rows.Clear();

        try
        {
            var res = await Task.Run(() =>
            {
                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.KEY_WORD = kw;
                var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                if (trs == null || trs.Count == 0) return null;

                var tr = trs[0];

                HisServiceReqViewFilter srf = new HisServiceReqViewFilter();
                srf.TREATMENT_ID = tr.ID;
                srf.ORDER_FIELD = "INTRUCTION_TIME";
                srf.ORDER_DIRECTION = "DESC";
                var reqs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srf, param);

                return new { Treatment = tr, Reqs = reqs };
            });

            if (res != null && res.Treatment != null)
            {
                lblPatSummary.Text = string.Format("BN: {0} ({1}t) | Mã BN: {2} | Khoa: {3} | Chẩn đoán: {4}",
                    res.Treatment.TDL_PATIENT_NAME,
                    (res.Treatment.TDL_PATIENT_DOB > 0 ? (2026 - (int)(res.Treatment.TDL_PATIENT_DOB / 10000000000L)) : 0),
                    res.Treatment.TDL_PATIENT_CODE,
                    (res.Treatment.END_DEPARTMENT_NAME ?? "Khoa 57"),
                    res.Treatment.ICD_NAME);

                if (res.Reqs != null)
                {
                    int idx = 1;
                    foreach (var r in res.Reqs.Take(40))
                    {
                        string timeStr = r.INTRUCTION_TIME.ToString();
                        if (timeStr.Length == 14) timeStr = timeStr.Substring(6, 2) + "/" + timeStr.Substring(4, 2) + " " + timeStr.Substring(8, 2) + ":" + timeStr.Substring(10, 2);

                        dgvPatOrders.Rows.Add(
                            idx++,
                            r.SERVICE_REQ_TYPE_NAME,
                            r.SERVICE_REQ_CODE + " - " + r.REQUEST_DEPARTMENT_NAME,
                            "1",
                            timeStr,
                            r.SERVICE_REQ_STT_NAME
                        );
                    }
                }
            }
            else
            {
                lblPatSummary.Text = "Không tìm thấy hồ sơ bệnh án!";
            }
        }
        catch (Exception ex)
        {
            lblPatSummary.Text = "Lỗi: " + ex.Message;
        }
        finally
        {
            btnPatSearch.Enabled = true;
        }
    }

    private async void BtnExecuteAll_Click(object sender, EventArgs e)
    {
        if (dgvPrescribe.Rows.Count == 0)
        {
            MessageBox.Show("Chưa có dòng y lệnh nào để kê!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(string.Format("Bạn có chắc chắn muốn Bác sĩ {0} thực hiện kê {1} y lệnh này không?", CurrentUserName, dgvPrescribe.Rows.Count),
            "Xác nhận Kê đơn", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        btnExecuteAll.Enabled = false;
        pbPrescribe.Visible = true;
        pbPrescribe.Maximum = dgvPrescribe.Rows.Count;
        pbPrescribe.Value = 0;

        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < dgvPrescribe.Rows.Count; i++)
        {
            var row = dgvPrescribe.Rows[i];
            string patKey = row.Cells["PatientKey"].Value != null ? row.Cells["PatientKey"].Value.ToString().Trim() : "";
            string medKw = row.Cells["MedicineKeyword"].Value != null ? row.Cells["MedicineKeyword"].Value.ToString().Trim() : "";
            decimal amount = 1;
            if (row.Cells["DosageAmount"].Value != null) decimal.TryParse(row.Cells["DosageAmount"].Value.ToString(), out amount);
            var stock = row.Cells["StockId"].Value as MedicineStockInfo ?? CommonStocks[0];
            string tutorial = row.Cells["Tutorial"].Value != null ? row.Cells["Tutorial"].Value.ToString() : "";

            if (string.IsNullOrEmpty(patKey) || string.IsNullOrEmpty(medKw))
            {
                row.Cells["Status"].Value = "Bỏ qua";
                row.Cells["Result"].Value = "Thiếu Mã BN hoặc Tên thuốc";
                continue;
            }

            row.Cells["Status"].Value = "Đang xử lý...";

            try
            {
                var result = await Task.Run(() =>
                {
                    HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                    tf.KEY_WORD = patKey;
                    var trs = adapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
                    if (trs == null || trs.Count == 0) throw new Exception("Không tìm thấy BN / Mã ĐT: " + patKey);
                    var tr = trs[0];

                    HisTrackingViewFilter tkf = new HisTrackingViewFilter();
                    tkf.TREATMENT_ID = tr.ID;
                    tkf.ORDER_FIELD = "TRACKING_TIME";
                    tkf.ORDER_DIRECTION = "DESC";
                    var tks = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, tkf, param);

                    long trackingId = 0;
                    long trackingTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

                    if (tks != null && tks.Count > 0)
                    {
                        trackingId = tks[0].ID;
                        trackingTime = tks[0].TRACKING_TIME;
                    }
                    else
                    {
                        HIS_TRACKING tk = new HIS_TRACKING
                        {
                            TREATMENT_ID = tr.ID,
                            DEPARTMENT_ID = CurrentDepartmentId,
                            TRACKING_TIME = trackingTime,
                            CONTENT = "Theo dõi và thực hiện thuốc theo y lệnh.",
                            ICD_CODE = tr.ICD_CODE,
                            ICD_NAME = tr.ICD_NAME,
                            ICD_SUB_CODE = tr.ICD_SUB_CODE
                        };
                        HisTrackingSDO sdo = new HisTrackingSDO { Tracking = tk };
                        var cr = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, param);
                        if (cr != null) trackingId = cr.ID;
                    }

                    HisMedicineTypeViewFilter mtf = new HisMedicineTypeViewFilter();
                    mtf.KEY_WORD = medKw;
                    mtf.IS_ACTIVE = 1;
                    var meds = adapter.FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", ApiConsumers.MosConsumer, mtf, param);
                    if (meds == null || meds.Count == 0) throw new Exception("Không tìm thấy thuốc khớp từ khóa: " + medKw);
                    var targetMed = meds[0];

                    InPatientPresSDO presSDO = new InPatientPresSDO
                    {
                        TreatmentId = tr.ID,
                        InstructionTimes = new List<long> { trackingTime },
                        UseTimes = new List<long> { trackingTime },
                        TrackingId = trackingId,
                        TrackingInfos = new List<TrackingInfoSDO>
                        {
                            new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = trackingTime }
                        },
                        RequestRoomId = CurrentRoomId,
                        RequestLoginName = CurrentLoginName,
                        RequestUserName = CurrentUserName,
                        IcdCode = tr.ICD_CODE,
                        IcdName = tr.ICD_NAME,
                        IcdSubCode = tr.ICD_SUB_CODE,
                        IcdText = tr.ICD_TEXT,
                        Medicines = new List<PresMedicineSDO>
                        {
                            new PresMedicineSDO
                            {
                                MedicineTypeId = targetMed.ID,
                                MediStockId = stock.MediStockId,
                                Amount = amount,
                                PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1,
                                Tutorial = !string.IsNullOrEmpty(tutorial) ? tutorial : "Theo chỉ dẫn của bác sĩ"
                            }
                        }
                    };

                    var res = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", ApiConsumers.MosConsumer, presSDO, param);
                    string code = (res != null && res.ExpMests != null && res.ExpMests.Count > 0) ? res.ExpMests[0].EXP_MEST_CODE : (res != null && res.ServiceReqs != null && res.ServiceReqs.Count > 0 ? res.ServiceReqs[0].SERVICE_REQ_CODE : "OK");
                    return new { Success = true, Code = code, PatientName = tr.TDL_PATIENT_NAME };
                });

                row.Cells["PatientName"].Value = result.PatientName;
                row.Cells["Status"].Value = "Thành công ✔";
                row.Cells["Status"].Style.ForeColor = Color.FromArgb(5, 150, 105);
                row.Cells["Result"].Value = "Mã xuất: " + result.Code;
                successCount++;
            }
            catch (Exception ex)
            {
                row.Cells["Status"].Value = "Lỗi ❌";
                row.Cells["Status"].Style.ForeColor = Color.Red;
                row.Cells["Result"].Value = ex.Message;
                failCount++;
            }

            pbPrescribe.Value = i + 1;
        }

        lblPrescribeSummary.Text = string.Format("✔ Hoàn tất kê đơn: {0} thành công, {1} lỗi.", successCount, failCount);
        lblPrescribeSummary.ForeColor = failCount == 0 ? Color.FromArgb(5, 150, 105) : Color.FromArgb(220, 38, 38);

        btnExecuteAll.Enabled = true;
        pbPrescribe.Visible = false;

        MessageBox.Show(string.Format("Đã hoàn tất kê đơn!\n- Thành công: {0} y lệnh\n- Thất bại: {1} y lệnh", successCount, failCount),
            "Kết quả Kê đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnSwitchUser_Click(object sender, EventArgs e)
    {
        LoginForm login = new LoginForm();
        if (login.ShowDialog() == DialogResult.OK)
        {
            CurrentLoginName = login.Username;
            currentToken = login.TokenCode;
            ApiConsumers.SetConsunmer(currentToken);
            LoadDoctorProfile();
            MessageBox.Show("Đã chuyển sang phiên làm việc của Bác sĩ: " + CurrentUserName, "Đổi Bác sĩ thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

class Program
{
    // Parse một dòng CSV hỗ trợ field có dấu phẩy trong ngoặc kép
    static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();
        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes) { fields.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }
        fields.Add(current.ToString());
        return fields;
    }

    [STAThread]
    static void Main(string[] args)
    {
        // 1. Hook AssemblyResolve to auto-load DLLs from ReferencedAssemblies, Lib, Bin or current dir
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            try
            {
                string assemblyName = new System.Reflection.AssemblyName(resolveArgs.Name).Name + ".dll";
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] searchPaths = new string[]
                {
                    baseDir,
                    Path.Combine(baseDir, "ReferencedAssemblies"),
                    Path.Combine(baseDir, "Lib"),
                    Path.Combine(baseDir, "Bin")
                };

                foreach (var dir in searchPaths)
                {
                    if (Directory.Exists(dir))
                    {
                        string targetPath = Path.Combine(dir, assemblyName);
                        if (File.Exists(targetPath))
                        {
                            return System.Reflection.Assembly.LoadFrom(targetPath);
                        }
                    }
                }
            }
            catch { }
            return null;
        };

        // 2. Global Exception Handling
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            MessageBox.Show("Lỗi thực thi (UI Thread): " + e.Exception.Message + "\n\nChi tiết:\n" + e.Exception.StackTrace,
                "Thông Báo Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            MessageBox.Show("Lỗi hệ thống (Fatal Exception):\n" + (e.ExceptionObject != null ? e.ExceptionObject.ToString() : "Unknown error"),
                "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }
        catch { }

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch { }

        // ──────────────────────────────────────────────────────────
        // CHẾ ĐỘ --batch: Đọc file CSV hàng loạt, kê đơn Insulin
        // CSV format: patient_code,dose,medicine,tutorial,time,date
        // VD: HisAutoPrescribe.exe --batch insulin_orders.csv
        // ──────────────────────────────────────────────────────────
        if (args != null && args.Length >= 2 && (args[0] == "--batch" || args[0] == "-b"))
        {
            string csvPath = args[1];
            string batchUser = args.Length > 2 ? args[2] : "vmc";
            string batchPass = args.Length > 3 ? args[3] : "789789";

            Console.WriteLine("===============================================================================");
            Console.WriteLine("  HIS AUTO PRESCRIBE - CHẾ ĐỘ KÊ ĐƠN HÀNG LOẠT (BATCH MODE)");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("• File CSV: " + csvPath);
            Console.WriteLine("• Bác sĩ: " + batchUser);

            if (!File.Exists(csvPath))
            {
                Console.WriteLine("❌ Không tìm thấy file CSV: " + csvPath);
                return;
            }

            // Đọc & parse CSV
            var csvLines = File.ReadAllLines(csvPath, Encoding.UTF8)
                               .Skip(1) // bỏ header
                               .Where(l => !string.IsNullOrWhiteSpace(l))
                               .ToList();

            Console.WriteLine(string.Format("• Số lệnh: {0}", csvLines.Count));
            Console.WriteLine("-------------------------------------------------------------------------------");

            if (csvLines.Count == 0) { Console.WriteLine("⚠ File CSV không có dữ liệu!"); return; }

            try
            {
                HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
                ClientTokenManager btm = new ClientTokenManager("HIS");
                CommonParam bp = new CommonParam();
                var btok = btm.Login(bp, batchUser, batchPass, "2.390.0");
                if (btok == null) { Console.WriteLine("❌ Đăng nhập thất bại!"); return; }
                ApiConsumers.SetConsunmer(btok.TokenCode);
                MyAdapter bad = new MyAdapter();

                // Cache: tra cứu BN và thuốc chỉ 1 lần
                var treatmentCache = new Dictionary<string, V_HIS_TREATMENT>();
                var medicineCache  = new Dictionary<string, V_HIS_MEDICINE_TYPE>();

                int bSucc = 0, bFail = 0;

                foreach (var line in csvLines)
                {
                    // Parse CSV: hỗ trợ field có dấu phẩy trong ngoặc kép
                    var fields = ParseCsvLine(line);
                    if (fields.Count < 3)
                    {
                        Console.WriteLine("  ⚠ Bỏ qua dòng không hợp lệ: " + line);
                        bFail++;
                        continue;
                    }

                    string bPatKey  = fields[0].Trim().Trim('"');
                    string bDoseStr = fields.Count > 1 ? fields[1].Trim().Trim('"') : "1";
                    string bMedKw   = fields.Count > 2 ? fields[2].Trim().Trim('"') : "";
                    string bTut     = fields.Count > 3 ? fields[3].Trim().Trim('"') : "Tiêm dưới da theo chỉ dẫn bác sĩ";
                    string bTime    = fields.Count > 4 ? fields[4].Trim().Trim('"') : "";
                    string bDate    = fields.Count > 5 ? fields[5].Trim().Trim('"') : DateTime.Today.ToString("yyyy-MM-dd");

                    decimal bAmount = 1;
                    decimal.TryParse(bDoseStr, out bAmount);

                    if (string.IsNullOrEmpty(bPatKey) || string.IsNullOrEmpty(bMedKw))
                    {
                        Console.WriteLine("  ⚠ Bỏ qua: thiếu Mã BN hoặc Tên thuốc");
                        bFail++;
                        continue;
                    }

                    // Tính instructionTime từ bTime + bDate
                    DateTime targetDate = DateTime.Today;
                    DateTime.TryParse(bDate, out targetDate);
                    int bHour = 17, bMin = 0;
                    if (!string.IsNullOrEmpty(bTime))
                    {
                        var tp = bTime.Split(':');
                        int.TryParse(tp[0], out bHour);
                        if (tp.Length > 1) int.TryParse(tp[1], out bMin);
                    }
                    DateTime instructionDt = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, bHour, bMin, 0);
                    long instructionTime   = long.Parse(instructionDt.ToString("yyyyMMddHHmmss"));

                    try
                    {
                        // 1. Tra cứu bệnh nhân (cache)
                        if (!treatmentCache.ContainsKey(bPatKey))
                        {
                            HisTreatmentViewFilter btf = new HisTreatmentViewFilter();
                            btf.KEY_WORD = bPatKey;
                            var btrs = bad.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, btf, bp);
                            if (btrs == null || btrs.Count == 0) throw new Exception("Không tìm thấy BN: " + bPatKey);
                            treatmentCache[bPatKey] = btrs[0];
                        }
                        var btr = treatmentCache[bPatKey];

                        // 2. Tìm tờ điều trị gần nhất TRÙNG giờ chỉ định (trong ±30 phút)
                        HisTrackingViewFilter btkf = new HisTrackingViewFilter();
                        btkf.TREATMENT_ID    = btr.ID;
                        btkf.ORDER_FIELD     = "TRACKING_TIME";
                        btkf.ORDER_DIRECTION = "DESC";
                        var btks = bad.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, btkf, bp);

                        long bTkId   = 0;
                        long bTkTime = instructionTime;

                        if (btks != null && btks.Count > 0)
                        {
                            // Tìm tờ điều trị có TRACKING_TIME gần nhất với instructionTime (±30 phút)
                            long tolerance = 3000; // 30 phút = 3000 (đơn vị HHMMSS)
                            var matched = btks.Where(tk =>
                            {
                                long diff = Math.Abs(tk.TRACKING_TIME - instructionTime);
                                return diff <= tolerance;
                            }).OrderBy(tk => Math.Abs(tk.TRACKING_TIME - instructionTime)).FirstOrDefault();

                            if (matched != null)
                            {
                                bTkId   = matched.ID;
                                bTkTime = matched.TRACKING_TIME;
                            }
                            else
                            {
                                // Dùng tờ điều trị mới nhất nếu không có tờ trùng giờ
                                bTkId   = btks[0].ID;
                                bTkTime = instructionTime; // Dùng đúng giờ chỉ định
                            }
                        }

                        // 3. Tra cứu thuốc (cache theo tên)
                        if (!medicineCache.ContainsKey(bMedKw))
                        {
                            HisMedicineTypeViewFilter bmtf = new HisMedicineTypeViewFilter();
                            bmtf.KEY_WORD  = bMedKw;
                            bmtf.IS_ACTIVE = 1;
                            var bmeds = bad.FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", ApiConsumers.MosConsumer, bmtf, bp);
                            if (bmeds == null || bmeds.Count == 0) throw new Exception("Không tìm thấy thuốc: " + bMedKw);
                            medicineCache[bMedKw] = bmeds[0];
                        }
                        var bMed = medicineCache[bMedKw];

                        // Xác định kho thuốc: Insulin thường lấy từ kho tủ trực (810)
                        // hoặc kho thuốc ống (4209) tùy tên thuốc
                        long bStockId = 810; // Tủ trực Khoa 57 (mặc định cho Insulin)

                        // 4. Tạo đơn thuốc nội trú
                        InPatientPresSDO bPresSDO = new InPatientPresSDO
                        {
                            TreatmentId       = btr.ID,
                            InstructionTimes  = new List<long> { bTkTime },
                            UseTimes          = new List<long> { bTkTime },
                            TrackingId        = bTkId,
                            TrackingInfos     = new List<TrackingInfoSDO>
                            {
                                new TrackingInfoSDO { TrackingId = bTkId, IntructionTime = bTkTime }
                            },
                            RequestRoomId     = 5248,
                            RequestLoginName  = batchUser,
                            RequestUserName   = batchUser.ToUpper(),
                            IcdCode           = btr.ICD_CODE,
                            IcdName           = btr.ICD_NAME,
                            IcdSubCode        = btr.ICD_SUB_CODE,
                            IcdText           = btr.ICD_TEXT,
                            Medicines = new List<PresMedicineSDO>
                            {
                                new PresMedicineSDO
                                {
                                    MedicineTypeId = bMed.ID,
                                    MediStockId    = bStockId,
                                    Amount         = bAmount,
                                    PatientTypeId  = btr.TDL_PATIENT_TYPE_ID ?? 1,
                                    Tutorial       = bTut
                                }
                            }
                        };

                        var bRes = bad.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", ApiConsumers.MosConsumer, bPresSDO, bp);
                        string bCode = (bRes != null && bRes.ServiceReqs != null && bRes.ServiceReqs.Count > 0)
                            ? bRes.ServiceReqs[0].SERVICE_REQ_CODE : "OK";

                        Console.WriteLine(string.Format("  ✔ [{0}] {1} | {2} {3} đv | {4} | Mã: {5}",
                            bPatKey, btr.TDL_PATIENT_NAME, bMed.MEDICINE_TYPE_NAME, bAmount, bTime, bCode));
                        bSucc++;
                    }
                    catch (Exception bex)
                    {
                        Console.WriteLine(string.Format("  ❌ [{0}] Lỗi: {1}", bPatKey, bex.Message));
                        bFail++;
                    }

                    System.Threading.Thread.Sleep(300); // Tránh spam API
                }

                Console.WriteLine("===============================================================================");
                Console.WriteLine(string.Format("KẾT THÚC BATCH: ✔ Thành công: {0} | ❌ Lỗi: {1}", bSucc, bFail));
                Console.WriteLine("===============================================================================");
            }
            catch (Exception batchEx)
            {
                Console.WriteLine("❌ Lỗi nghiêm trọng batch mode: " + batchEx.Message);
            }
            return;
        }

        if (args != null && args.Length >= 3)
        {
            string patKey = args[0];
            decimal amount = 1;
            decimal.TryParse(args[1], out amount);
            string medKw = args[2];
            string tut = args.Length > 3 ? args[3] : "Theo chỉ dẫn bác sĩ";
            string user = args.Length > 4 ? args[4] : "vmc";
            string pass = args.Length > 5 ? args[5] : "789789";

            Console.WriteLine(string.Format(">>> CLI AUTO PRESCRIBE: BS {0} | BN: {1} | Thuốc: {2} | Liều: {3}", user, patKey, medKw, amount));
            try
            {
                HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
                ClientTokenManager tm = new ClientTokenManager("HIS");
                CommonParam p = new CommonParam();
                var tok = tm.Login(p, user, pass, "2.390.0");
                if (tok == null) { Console.WriteLine("❌ Đăng nhập thất bại!"); return; }
                ApiConsumers.SetConsunmer(tok.TokenCode);
                MyAdapter ad = new MyAdapter();

                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                tf.KEY_WORD = patKey;
                var trs = ad.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, p);
                if (trs == null || trs.Count == 0) { Console.WriteLine("❌ Không tìm thấy bệnh nhân!"); return; }
                var tr = trs[0];

                HisTrackingViewFilter tkf = new HisTrackingViewFilter();
                tkf.TREATMENT_ID = tr.ID;
                tkf.ORDER_FIELD = "TRACKING_TIME";
                tkf.ORDER_DIRECTION = "DESC";
                var tks = ad.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, tkf, p);
                long tkId = tks != null && tks.Count > 0 ? tks[0].ID : 0;
                long tkTime = tks != null && tks.Count > 0 ? tks[0].TRACKING_TIME : long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

                HisMedicineTypeViewFilter mtf = new HisMedicineTypeViewFilter();
                mtf.KEY_WORD = medKw;
                mtf.IS_ACTIVE = 1;
                var meds = ad.FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", ApiConsumers.MosConsumer, mtf, p);
                if (meds == null || meds.Count == 0) { Console.WriteLine("❌ Không tìm thấy thuốc!"); return; }
                var med = meds[0];

                InPatientPresSDO presSDO = new InPatientPresSDO
                {
                    TreatmentId = tr.ID,
                    InstructionTimes = new List<long> { tkTime },
                    UseTimes = new List<long> { tkTime },
                    TrackingId = tkId,
                    TrackingInfos = new List<TrackingInfoSDO> { new TrackingInfoSDO { TrackingId = tkId, IntructionTime = tkTime } },
                    RequestRoomId = 5248,
                    RequestLoginName = user,
                    RequestUserName = user.ToUpper(),
                    IcdCode = tr.ICD_CODE,
                    IcdName = tr.ICD_NAME,
                    IcdSubCode = tr.ICD_SUB_CODE,
                    IcdText = tr.ICD_TEXT,
                    Medicines = new List<PresMedicineSDO>
                    {
                        new PresMedicineSDO
                        {
                            MedicineTypeId = med.ID,
                            MediStockId = 810,
                            Amount = amount,
                            PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1,
                            Tutorial = tut
                        }
                    }
                };

                var res = ad.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", ApiConsumers.MosConsumer, presSDO, p);
                Console.WriteLine("✔ Kê đơn thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi: " + ex.Message);
            }
            return;
        }

        try
        {
            LoginForm loginForm = new LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new MainForm(loginForm.Username, loginForm.TokenCode));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khởi chạy ứng dụng: " + ex.Message + "\n\n" + ex.StackTrace, "Lỗi Nghiêm Trọng", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
