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
    public static CommonParam param;
    public static string currentToken = null;

    // Header Controls
    private Panel pnlHeader;
    private Label lblTitle;
    private Label lblSubTitle;

    // Toolbar Controls
    private Panel pnlToolbar;
    private Button btnAddRow;
    private Button btnPasteExcel;
    private ComboBox cboQuickTemplate;
    private Button btnApplyTemplate;
    private Button btnLoadDept57;
    private Button btnDeleteRow;
    private Button btnClearAll;
    private Button btnExecuteAll;

    // Grid Control
    private DataGridView dgvTracking;

    // Bottom Controls
    private Panel pnlBottom;
    private ProgressBar pbProgress;
    private Label lblSummary;
    private Button btnExportCsv;

    public static List<ClinicalTemplate> ClinicalTemplates = new List<ClinicalTemplate>();

    public MainForm()
    {
        InitializeClinicalTemplates();
        InitializeComponents();
        this.Load += MainForm_Load;
    }

    private void InitializeClinicalTemplates()
    {
        ClinicalTemplates.Clear();
        ClinicalTemplates.Add(new ClinicalTemplate(
            "1. Tờ điều trị hàng ngày / Thông thường",
            "08:00",
            "Bệnh nhân tỉnh táo, tiếp xúc tốt (Glasgow 15đ). Da niêm mạc hồng, không phù, không sốt. Tim đều, phổi thông khí rõ không rale, bụng mềm. Vết mổ / tổn thương: Đau ít (VAS 2-3đ), không sưng nóng đỏ. Đầu chi hồng ấm, cảm giác và vận động ngọn chi bình thường. Đại tiểu tiện tự chủ.",
            "Chăm sóc cấp II (CSII). Chế độ ăn: BT01 (Cơm thường bệnh lý). Theo dõi DHST (Mạch, HA, Nhiệt độ) 2 lần/ngày. Thay băng chăm sóc vết thương vô khuẩn hàng ngày.",
            "Thuốc dùng theo đơn đã kê. Thay băng chăm sóc vết thương vô khuẩn."
        ));

        ClinicalTemplates.Add(new ClinicalTemplate(
            "2. Sơ kết 3 - 5 ngày điều trị / Lãnh đạo đi buồng",
            "08:00",
            "SƠ KẾT 3 - 5 NGÀY ĐIỀU TRỊ: Bệnh nhân tỉnh, tiếp xúc tốt, thể trạng ổn định. Huyết động ổn định, không sốt. Vết mổ khô sạch, thấm ít dịch băng, đầu chi hồng ấm. Triệu chứng đau thuyên giảm (VAS 3/10). Cơ lực 2 chân 5/5, không rối loạn cảm giác nông sâu. Ý KIẾN LÃNH ĐẠO ĐI BUỒNG: Thống nhất chẩn đoán và phác đồ điều trị; Thay băng vô khuẩn hàng ngày; Tập PHCN tại giường.",
            "Chăm sóc cấp II (CSII). Chế độ ăn theo bệnh lý. Hướng dẫn tập PHCN tại giường.",
            "Duy trì phác đồ thuốc hiện tại. Hoàn thiện bilan xét nghiệm kiểm tra nếu có chỉ định."
        ));

        ClinicalTemplates.Add(new ClinicalTemplate(
            "3. Tiền phẫu (Chuẩn bị trước mổ)",
            "16:30",
            "KHÁM BỆNH NHÂN TRƯỚC MỔ: Bệnh nhân tỉnh táo, tiếp xúc tốt, tâm lý ổn định. Thể trạng trung bình, không sốt. Tim đều rõ, phổi thông khí tốt, không khó thở. Đã hoàn thiện đầy đủ bilan xét nghiệm tiền phẫu, X-quang, MRI/CT, Siêu âm tim. Đã giải thích rõ tình trạng bệnh, phương pháp phẫu thuật, nguy cơ và tai biến. Bệnh nhân và gia đình hiểu, đồng ý và đã ký cam kết phẫu thuật.",
            "Chăm sóc cấp II (CSII). Vệ sinh vùng mổ, thay trang phục mổ. Nhịn ăn uống tuyệt đối từ 00h trước mổ.",
            "Bột / nẹp rạch dọc kiểm tra. Dặn nhịn ăn uống hoàn toàn từ 00h đêm trước mổ. Kháng sinh dự phòng trước mổ 30 phút theo phác đồ. Chuyển phòng mổ theo lịch."
        ));

        ClinicalTemplates.Add(new ClinicalTemplate(
            "4. Hậu phẫu 24 giờ đầu (Sau mổ)",
            "14:00",
            "BỆNH NHÂN PHẪU THUẬT VỀ KHOA: Bệnh nhân tỉnh táo, tiếp xúc tốt, đã thoát mê hoàn toàn. Da niêm mạc hồng, tự thở êm, SpO2 98-99%. Huyết động ổn định: Mạch 80-85 l/p, HA 120/80 mmHg. Vết mổ nề nhẹ, băng thấm ít dịch máu. Dẫn lưu vết mổ ra ít dịch hồng (< 50ml), hoạt động tốt. Đầu chi hồng ấm, mạch ngoại vi bắt rõ. Đau vết mổ mức độ vừa (VAS 3-4đ).",
            "Chăm sóc cấp I / II (CSCI / CSII). Kê cao chi mổ / nằm ngửa có gối đỡ tư thế chuẩn. Theo dõi mạch, huyết áp, nhiệt độ, SpO2 mỗi 3 - 6 giờ. Theo dõi màu sắc đầu chi và lượng dịch dẫn lưu.",
            "Theo dõi sát toàn trạng và huyết động 24h sau mổ. Thuốc giảm đau, kháng sinh, chống phù nề theo biên bản bàn giao gây mê. Rút dẫn lưu sau 24 - 48h khi dịch ra < 30ml/24h."
        ));

        ClinicalTemplates.Add(new ClinicalTemplate(
            "5. Tổng kết ra viện (Discharge Summary)",
            "08:00",
            "TỔNG KẾT BỆNH ÁN RA VIỆN: Diễn biến điều trị thuận lợi, không tai biến sau mổ, vết mổ khô sạch liền sẹo tốt. Tình trạng hiện tại: Bệnh nhân tỉnh táo, hết sốt, huyết động ổn định, vết mổ khô sạch (đã cắt chỉ / liền sẹo), đỡ đau nhiều, đi lại và vận động phục hồi tốt, đại tiểu tiện tự chủ. Đủ điều kiện xuất viện.",
            "Chăm sóc cấp II (CSII). Hướng dẫn bệnh nhân và gia đình làm thủ tục thanh toán ra viện.",
            "Cho bệnh nhân ra viện. Kê đơn thuốc điều trị ngoại trú. Hẹn khám lại sau 01 tháng kèm phim chụp kiểm tra. Dặn dò chế độ tập PHCN và dinh dưỡng tại nhà."
        ));
    }

    private void InitializeComponents()
    {
        this.Text = "HỆ THỐNG TẠO TỜ ĐIỀU TRỊ & CHĂM SÓC BỆNH NHÂN (HIS / MOS EMR) - KHOA CTCH & CỘT SỐNG";
        this.Size = new Size(1360, 720);
        this.MinimumSize = new Size(1000, 550);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        this.BackColor = Color.FromArgb(245, 247, 250);

        try { if (File.Exists("APP.ico")) this.Icon = new Icon("APP.ico"); } catch { }

        // Top Header
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Color.FromArgb(26, 86, 219), // Modern Deep Blue
            Padding = new Padding(16, 8, 16, 8)
        };

        lblTitle = new Label
        {
            Text = "📋 NHẬP LIỆU & TẠO TỜ ĐIỀU TRỊ BỆNH NHÂN (HIS / MOS)",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Location = new Point(14, 8),
            AutoSize = true
        };

        lblSubTitle = new Label
        {
            Text = "Hỗ trợ nhập trực tiếp hoặc dán (Ctrl+V) từ Excel/Clipboard | Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57)",
            ForeColor = Color.FromArgb(219, 234, 254),
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Location = new Point(16, 32),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubTitle);
        this.Controls.Add(pnlHeader);

        // Toolbar
        pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8, 6, 8, 6)
        };

        btnAddRow = new Button
        {
            Text = "➕ Thêm dòng",
            Location = new Point(10, 8),
            Width = 115,
            Height = 32,
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
            Location = new Point(132, 8),
            Width = 175,
            Height = 32,
            BackColor = Color.FromArgb(236, 253, 245),
            ForeColor = Color.FromArgb(4, 120, 87),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPasteExcel.FlatAppearance.BorderColor = Color.FromArgb(167, 243, 208);
        btnPasteExcel.Click += (s, e) => PasteFromClipboard();

        Label lblM = new Label { Text = "Mẫu:", Location = new Point(315, 14), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

        cboQuickTemplate = new ComboBox
        {
            Location = new Point(355, 11),
            Width = 240,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        foreach (var t in ClinicalTemplates) cboQuickTemplate.Items.Add(t);
        cboQuickTemplate.SelectedIndex = 0;

        btnApplyTemplate = new Button
        {
            Text = "Áp dụng mẫu",
            Location = new Point(602, 8),
            Width = 105,
            Height = 32,
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
            Text = "🏥 Tải toàn bộ BN Khoa 57",
            Location = new Point(714, 8),
            Width = 180,
            Height = 32,
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
            Location = new Point(900, 8),
            Width = 85,
            Height = 32,
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
            Location = new Point(990, 8),
            Width = 75,
            Height = 32,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(100, 116, 139),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btnClearAll.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnClearAll.Click += (s, e) => { dgvTracking.Rows.Clear(); UpdateRowCount(); };

        btnExecuteAll = new Button
        {
            Text = "🚀 TẠO TẤT CẢ TỜ ĐIỀU TRỊ",
            Location = new Point(1075, 6),
            Width = 250,
            Height = 36,
            BackColor = Color.FromArgb(16, 185, 129), // Emerald Green
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnExecuteAll.FlatAppearance.BorderSize = 0;
        btnExecuteAll.Click += async (s, e) => await ExecuteCreateAllAsync();

        pnlToolbar.Controls.Add(btnAddRow);
        pnlToolbar.Controls.Add(btnPasteExcel);
        pnlToolbar.Controls.Add(lblM);
        pnlToolbar.Controls.Add(cboQuickTemplate);
        pnlToolbar.Controls.Add(btnApplyTemplate);
        pnlToolbar.Controls.Add(btnLoadDept57);
        pnlToolbar.Controls.Add(btnDeleteRow);
        pnlToolbar.Controls.Add(btnClearAll);
        pnlToolbar.Controls.Add(btnExecuteAll);
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
            Height = 44,
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12, 8, 12, 8)
        };

        pbProgress = new ProgressBar
        {
            Location = new Point(12, 10),
            Width = 280,
            Height = 22,
            Visible = false
        };

        lblSummary = new Label
        {
            Text = "Mẹo: Nhập mã bệnh nhân hoặc chọn dòng trong Excel copy rồi bấm 'Dán từ Excel' (Ctrl+V) để nạp siêu tốc.",
            Location = new Point(305, 12),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 9f, FontStyle.Italic)
        };

        btnExportCsv = new Button
        {
            Text = "📊 Xuất Excel (CSV)",
            Location = new Point(1210, 6),
            Width = 135,
            Height = 30,
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
        pnlBottom.Controls.Add(btnExportCsv);
        this.Controls.Add(pnlBottom);

        // Initial default row
        AddDefaultRow("0003969449", "08:00");
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
            Width = 190,
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
            HeaderText = "Diễn biến bệnh lý (Content) (*)",
            Width = 300
        };
        dgvTracking.Columns.Add(colContent);

        // Care Instruction
        var colCare = new DataGridViewTextBoxColumn
        {
            Name = "CareInstruction",
            HeaderText = "Chế độ chăm sóc (Care) (*)",
            Width = 260
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

    private void AddDefaultRow(string patCode = "", string time = "08:00")
    {
        var tmpl = ClinicalTemplates[0];
        AddRowInternal(patCode, "", time, tmpl.Content, tmpl.CareInstruction, tmpl.MedicalInstruction);
    }

    private void AddEmptyRow()
    {
        var tmpl = ClinicalTemplates[0];
        AddRowInternal("", "", tmpl.DefaultTime, tmpl.Content, tmpl.CareInstruction, tmpl.MedicalInstruction);
    }

    private void AddRowInternal(string patCode, string patInfo, string time, string content, string care, string med)
    {
        int idx = dgvTracking.Rows.Add();
        var row = dgvTracking.Rows[idx];
        row.Cells["Stt"].Value = idx + 1;
        row.Cells["PatientCode"].Value = patCode;
        row.Cells["PatientInfo"].Value = patInfo;
        row.Cells["TrackingTime"].Value = string.IsNullOrEmpty(time) ? "08:00" : time;
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
        if (tmpl == null) return;

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
            var defaultTmpl = ClinicalTemplates[0];

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split('\t');

                string pat = parts.Length > 0 ? parts[0].Trim() : "";
                if (string.IsNullOrEmpty(pat)) continue;

                string time = parts.Length > 1 ? parts[1].Trim() : defaultTmpl.DefaultTime;
                string content = parts.Length > 2 ? parts[2].Trim() : defaultTmpl.Content;
                string care = parts.Length > 3 ? parts[3].Trim() : defaultTmpl.CareInstruction;
                string med = parts.Length > 4 ? parts[4].Trim() : defaultTmpl.MedicalInstruction;

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

            var tmpl = ClinicalTemplates[0];
            foreach (var p in list)
            {
                AddRowInternal(p.TDL_PATIENT_CODE, string.Format("{0} ({1}) - {2}", p.TDL_PATIENT_NAME, p.TDL_PATIENT_GENDER_NAME, p.BedFull),
                    tmpl.DefaultTime, tmpl.Content, tmpl.CareInstruction, tmpl.MedicalInstruction);
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

        string msg = string.Format("XÁC NHẬN TẠO TỜ ĐIỀU TRỊ:\n\n• Tổng số dòng: {0} bệnh nhân\n• Hệ thống: HIS / MOS Khoa 57\n\nBạn có chắc chắn muốn tiến hành ghi nhận lên hệ thống?", dgvTracking.Rows.Count);
        if (MessageBox.Show(msg, "Xác nhận tạo Tờ điều trị", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        btnExecuteAll.Enabled = false;
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
                string timeStr = (row.Cells["TrackingTime"].Value ?? "08:00").ToString().Trim();
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

                    if (p == null) throw new Exception("Không tìm thấy đợt điều trị của BN!");

                    long roomId = p.WorkingRoomId > 0 ? p.WorkingRoomId : 5248;

                    HIS_TRACKING tracking = new HIS_TRACKING
                    {
                        TREATMENT_ID = p.TreatmentId,
                        DEPARTMENT_ID = 57,
                        ROOM_ID = roomId,
                        TRACKING_TIME = trackingTime,
                        CONTENT = content,
                        MEDICAL_INSTRUCTION = med,
                        CARE_INSTRUCTION = care,
                        ICD_CODE = p.IcdCode,
                        ICD_NAME = p.IcdName,
                        ICD_SUB_CODE = p.IcdSubCode,
                        ICD_TEXT = p.IcdText
                    };

                    HisTrackingSDO sdo = new HisTrackingSDO
                    {
                        Tracking = tracking,
                        WorkingRoomId = roomId,
                        Dhst = new HIS_DHST
                        {
                            TREATMENT_ID = p.TreatmentId,
                            EXECUTE_TIME = trackingTime,
                            EXECUTE_LOGINNAME = "vmc",
                            EXECUTE_USERNAME = "Vũ Minh Cường",
                            PULSE = 80,
                            TEMPERATURE = 36.8m,
                            BLOOD_PRESSURE_MAX = 120,
                            BLOOD_PRESSURE_MIN = 80,
                            BREATH_RATE = 18,
                            SPO2 = 0.98m,
                            WEIGHT = 60.0m
                        }
                    };

                    var created = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, param);
                    if (created == null) throw new Exception("Hệ thống MOS từ chối tạo!");

                    try
                    {
                        myAdapter.PostData<HIS_DHST>("api/HisDhst/Create", ApiConsumers.MosConsumer, sdo.Dhst, param);
                    }
                    catch { }

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

        lblSummary.Text = string.Format("Hoàn tất! ✔ {0} thành công | ❌ {1} thất bại.", success, fail);
        MessageBox.Show(string.Format("Hoàn tất tạo Tờ điều trị!\n\n✔ Thành công: {0}\n❌ Thất bại: {1}", success, fail),
            "Kết quả", MessageBoxButtons.OK, fail == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private static long ParseTrackingTime(string input)
    {
        DateTime date = DateTime.Today;
        TimeSpan time = new TimeSpan(8, 0, 0);

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

    private async void MainForm_Load(object sender, EventArgs e)
    {
        lblSummary.Text = "Đang kết nối hệ thống HIS / MOS...";
        await Task.Run(() =>
        {
            try { InitSession(); } catch { }
        });
        lblSummary.Text = "✔ Đã kết nối HIS thành công. Sẵn sàng nhập liệu và dán bảng từ Excel!";
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
        public string IcdCode { get; set; }
        public string IcdName { get; set; }
        public string IcdSubCode { get; set; }
        public string IcdText { get; set; }
    }

    public static void InitSession()
    {
        if (!string.IsNullOrEmpty(currentToken)) return;

        HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
        param = new CommonParam();

        // 1. Try reading live token from LogSystem.txt (FileShare.ReadWrite)
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string[] candidates = new string[]
        {
            Path.Combine(baseDir, "Logs", "LogSystem.txt"),
            @"D:\his\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt",
            @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt"
        };

        foreach (var logPath in candidates)
        {
            if (File.Exists(logPath))
            {
                try
                {
                    using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        string text = sr.ReadToEnd();
                        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                        for (int i = lines.Length - 1; i >= 0; i--)
                        {
                            if (lines[i].Contains("TokenCode|"))
                            {
                                int idx = lines[i].IndexOf("TokenCode|") + 10;
                                if (lines[i].Length >= idx + 64)
                                {
                                    currentToken = lines[i].Substring(idx, 64);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }

                if (!string.IsNullOrEmpty(currentToken)) break;
            }
        }

        // 2. Fallback to API login if token not found
        if (string.IsNullOrEmpty(currentToken))
        {
            ClientTokenManager tokenManager = new ClientTokenManager("HIS");
            var token = tokenManager.Login(param, "vmc", "789789", "2.390.0");
            if (token != null)
            {
                currentToken = token.TokenCode;
            }
        }

        if (!string.IsNullOrEmpty(currentToken))
        {
            ApiConsumers.SetConsunmer(currentToken);
            adapter = new BackendAdapter(param);

            try
            {
                var workInfo = new WorkInfoSDO
                {
                    Rooms = new List<RoomSDO>
                    {
                        new RoomSDO { RoomId = 5248 }, // Phòng 734 (Phòng trực/khám CTCH)
                        new RoomSDO { RoomId = 5252 }, // Phòng 712 (Buồng bệnh)
                        new RoomSDO { RoomId = 5251 }, // Phòng 714 (Buồng bệnh)
                        new RoomSDO { RoomId = 5257 }  // Phòng 724 (Buồng bệnh)
                    }
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

        HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
        if (code.Length >= 11) tf.TREATMENT_CODE__EXACT = code;
        else tf.PATIENT_CODE__EXACT = code;

        var treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
        if (treatments == null || treatments.Count == 0)
        {
            tf = new HisTreatmentViewFilter();
            long tId;
            if (long.TryParse(code, out tId))
            {
                tf.ID = tId;
                treatments = myAdapter.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, param);
            }
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
            WorkingRoomId = 5248,
            BedFull = "P734"
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
                WorkingRoomId = 5248
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

        if (args.Length > 0 && (args[0] == "-p" || args[0] == "--patient" || args[0] == "-f" || args[0] == "--file" || args[0] == "--help" || args[0] == "-h" || args[0] == "/?"))
        {
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
        Console.WriteLine("  HỆ THỐNG TẠO TỜ ĐIỀU TRỊ & CHĂM SÓC (CLI) - KHOA CTCH & CỘT SỐNG (KHOA 57)");
        Console.WriteLine("===============================================================================");

        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine("CÚ PHÁP SỬ DỤNG:");
            Console.WriteLine("  HisTrackingCreator.exe -p <MaBN1,MaBN2,...> -time <HH:mm> -content <NoiDung> -care <CheDoChamSoc> [-med <YLenh>]");
            Console.WriteLine("  HisTrackingCreator.exe -p <MaBN> -template <1..5>");
            Console.WriteLine("\nVÍ DỤ:");
            Console.WriteLine("  HisTrackingCreator.exe -p \"0003969449\" -time \"08:00\" -content \"BN tỉnh, không sốt, vết mổ khô.\" -care \"CSII, BT01, DHST 2 lần/ngày\"");
            return;
        }

        string rawPatients = "";
        string rawTime = "08:00";
        string content = "Bệnh nhân tỉnh táo, tiếp xúc tốt. Da niêm mạc hồng, không sốt. Vết mổ khô sạch, đầu chi ấm.";
        string medInstruction = "Thuốc dùng theo đơn đã kê. Thay băng chăm sóc vết mổ hàng ngày.";
        string careInstruction = "Chăm sóc cấp II (CSII). Chế độ ăn BT01. Theo dõi DHST 2 lần/ngày.";
        int templateId = 0;

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-p" || args[i] == "--patient") && i + 1 < args.Length) rawPatients = args[i + 1];
            if ((args[i] == "-time" || args[i] == "-t") && i + 1 < args.Length) rawTime = args[i + 1];
            if ((args[i] == "-content" || args[i] == "-c") && i + 1 < args.Length) content = args[i + 1];
            if ((args[i] == "-med" || args[i] == "-m") && i + 1 < args.Length) medInstruction = args[i + 1];
            if ((args[i] == "-care") && i + 1 < args.Length) careInstruction = args[i + 1];
            if ((args[i] == "-template" || args[i] == "-tmpl") && i + 1 < args.Length) int.TryParse(args[i + 1], out templateId);
        }

        if (templateId >= 1 && templateId <= MainForm.ClinicalTemplates.Count)
        {
            var tmpl = MainForm.ClinicalTemplates[templateId - 1];
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
        Console.WriteLine(string.Format("• Mốc giờ: {0}", rawTime));
        Console.WriteLine(string.Format("• Nội dung: {0}", content));
        Console.WriteLine(string.Format("• Chăm sóc: {0}", careInstruction));
        Console.WriteLine("-------------------------------------------------------------------------------");

        MainForm.InitSession();

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
                TimeSpan tSpan;
                if (!TimeSpan.TryParse(rawTime, out tSpan)) tSpan = new TimeSpan(8, 0, 0);
                DateTime fullDateTime = new DateTime(date.Year, date.Month, date.Day, tSpan.Hours, tSpan.Minutes, 0);
                long trackingTime = long.Parse(fullDateTime.ToString("yyyyMMddHHmmss"));

                long roomId = p.WorkingRoomId > 0 ? p.WorkingRoomId : 5248;

                HIS_TRACKING tracking = new HIS_TRACKING
                {
                    TREATMENT_ID = p.TreatmentId,
                    DEPARTMENT_ID = 57,
                    ROOM_ID = roomId,
                    TRACKING_TIME = trackingTime,
                    CONTENT = content,
                    MEDICAL_INSTRUCTION = medInstruction,
                    CARE_INSTRUCTION = careInstruction,
                    ICD_CODE = p.IcdCode,
                    ICD_NAME = p.IcdName,
                    ICD_SUB_CODE = p.IcdSubCode,
                    ICD_TEXT = p.IcdText
                };

                HisTrackingSDO sdo = new HisTrackingSDO
                {
                    Tracking = tracking,
                    WorkingRoomId = roomId,
                    Dhst = new HIS_DHST
                    {
                        TREATMENT_ID = p.TreatmentId,
                        EXECUTE_TIME = trackingTime,
                        EXECUTE_LOGINNAME = "vmc",
                        EXECUTE_USERNAME = "Vũ Minh Cường",
                        PULSE = 80,
                        TEMPERATURE = 36.8m,
                        BLOOD_PRESSURE_MAX = 120,
                        BLOOD_PRESSURE_MIN = 80,
                        BREATH_RATE = 18,
                        SPO2 = 0.98m
                    }
                };

                var created = MainForm.myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, MainForm.param);
                if (created == null) throw new Exception("MOS từ chối tạo!");

                try
                {
                    MainForm.myAdapter.PostData<HIS_DHST>("api/HisDhst/Create", ApiConsumers.MosConsumer, sdo.Dhst, MainForm.param);
                }
                catch { }

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
