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

public class PatientTrackingDto
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
    public string TrackingTimeDisplay { get; set; }
    public long CreatedTrackingId { get; set; }
    public string ExecutionStatus { get; set; }
    public int StatusCode { get; set; } // 0: Pending, 1: Success, 2: Error

    public PatientTrackingDto()
    {
        Selected = true;
        StatusCode = 0;
        ExecutionStatus = "⚪ Sẵn sàng";
        PatientTypeId = 1;
        PatientTypeName = "BHYT";
        WorkingRoomId = 5248;
    }
}

public class ClinicalTemplate
{
    public string Name { get; set; }
    public string Content { get; set; }
    public string MedicalInstruction { get; set; }
    public string CareInstruction { get; set; }
    public string CareLevel { get; set; }
    public string DietMode { get; set; }

    public ClinicalTemplate(string name, string content, string medInstruction, string careInstruction, string careLevel, string dietMode)
    {
        Name = name;
        Content = content;
        MedicalInstruction = medInstruction;
        CareInstruction = careInstruction;
        CareLevel = careLevel;
        DietMode = dietMode;
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

    // Data lists
    private List<PatientTrackingDto> allPatients = new List<PatientTrackingDto>();
    private List<PatientTrackingDto> filteredPatients = new List<PatientTrackingDto>();

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

    // Sidebar - Clinical inputs
    private DateTimePicker dtpOrderDate;
    private DateTimePicker dtpOrderTime;
    private ComboBox cboPresetTimes;
    private ComboBox cboTemplates;
    private TextBox txtContent;
    private TextBox txtMedicalInstruction;
    private TextBox txtCareInstruction;
    private ComboBox cboCareLevel;
    private ComboBox cboDietMode;

    // Care action checkboxes
    private CheckBox chkCareDhst2Times;
    private CheckBox chkCareDhstEvery3h;
    private CheckBox chkCareDressing;
    private CheckBox chkCareGlucose;
    private CheckBox chkCarePhysio;
    private CheckBox chkCareFasting;

    // Vital signs inputs (DHST)
    private CheckBox chkIncludeDhst;
    private NumericUpDown nudPulse;
    private NumericUpDown nudBpMax;
    private NumericUpDown nudBpMin;
    private NumericUpDown nudTemp;
    private NumericUpDown nudBreathRate;
    private NumericUpDown nudSpo2;
    private NumericUpDown nudWeight;

    // Action button
    private Button btnCreateTracking;
    private Button btnClearInputs;

    // Patient lookup controls
    private TextBox txtInputCodes;
    private Button btnLookupCodes;
    private Button btnLoadDept57;
    private Button btnSelectAll;
    private Button btnUnselectAll;
    private TextBox txtSearchFilter;
    private ComboBox cboFilterRoom;

    // Bottom export buttons
    private Button btnExportCsv;
    private Button btnCopyReport;

    // Selected Patient Card Panel
    private Panel pnlPatientCard;
    private Label lblCardPatientName;
    private Label lblCardPatientInfo;
    private Label lblCardDiagnosis;
    private Label lblCardLocation;

    private List<ClinicalTemplate> clinicalTemplates = new List<ClinicalTemplate>();

    public MainForm()
    {
        InitializeClinicalTemplates();
        InitializeCustomComponents();
        this.Load += MainForm_Load;
    }

    public static List<ClinicalTemplate> GetDefaultClinicalTemplates()
    {
        var list = new List<ClinicalTemplate>();
        list.Add(new ClinicalTemplate(
            "1. Tờ điều trị hàng ngày / Thông thường",
            "Bệnh nhân tỉnh táo, tiếp xúc tốt (Glasgow 15đ).\r\nDa niêm mạc hồng, không phù, không sốt.\r\nTim đều, phổi thông khí rõ không rale, bụng mềm.\r\nVết mổ / tổn thương: Đau ít (VAS 2-3đ), không sưng nóng đỏ.\r\nĐầu chi hồng ấm, cảm giác và vận động ngọn chi bình thường.\r\nĐại tiểu tiện tự chủ.",
            "Thuốc dùng theo đơn đã kê.\r\nThay băng chăm sóc vết thương vô khuẩn hàng ngày.",
            "Chăm sóc cấp II (CSII).\r\nChế độ ăn: BT01 (Cơm thường bệnh lý).\r\nTheo dõi DHST (Mạch, HA, Nhiệt độ) 2 lần/ngày.",
            "Chăm sóc cấp II (CSII)",
            "BT01"
        ));

        list.Add(new ClinicalTemplate(
            "2. Sơ kết 3 - 5 ngày điều trị / Lãnh đạo khoa đi buồng",
            "SƠ KẾT 3 - 5 NGÀY ĐIỀU TRỊ:\r\nBệnh nhân tỉnh, tiếp xúc tốt, thể trạng ổn định.\r\nHuyết động ổn định, không sốt.\r\nVết mổ khô sạch, thấm ít dịch băng, đầu chi hồng ấm.\r\nTriệu chứng đau thuyên giảm (VAS 3/10).\r\nCơ lực 2 chân 5/5, không rối loạn cảm giác nông sâu.\r\n\r\nÝ KIẾN LÃNH ĐẠO KHOA ĐI BUỒNG:\r\n- Thống nhất chẩn đoán và phác đồ điều trị hiện tại.\r\n- Thay băng chăm sóc vết thương vô khuẩn hàng ngày.\r\n- Tập phục hồi chức năng vận động theo hướng dẫn.",
            "Duy trì phác đồ thuốc hiện tại.\r\nHoàn thiện bilan xét nghiệm kiểm tra nếu có chỉ định.",
            "Chăm sóc cấp II (CSII).\r\nChế độ ăn theo bệnh lý.\r\nHướng dẫn tập PHCN tại giường.",
            "Chăm sóc cấp II (CSII)",
            "BT01"
        ));

        list.Add(new ClinicalTemplate(
            "3. Tiền phẫu (Chuẩn bị trước mổ)",
            "KHÁM BỆNH NHÂN TRƯỚC MỔ:\r\nBệnh nhân tỉnh táo, tiếp xúc tốt, tâm lý ổn định.\r\nThể trạng trung bình, không sốt.\r\nTim đều rõ, phổi thông khí tốt, không khó thở.\r\nĐã hoàn thiện đầy đủ bilan xét nghiệm tiền phẫu, X-quang, MRI/CT, Siêu âm tim.\r\nĐã giải thích rõ tình trạng bệnh, phương pháp phẫu thuật, nguy cơ và tai biến có thể xảy ra trong và sau mổ cho bệnh nhân và gia đình. Bệnh nhân và đại diện gia đình hiểu, đồng ý và đã ký cam kết phẫu thuật.",
            "Bột / nẹp rạch dọc kiểm tra.\r\nDặn nhịn ăn uống hoàn toàn từ 00h đêm trước mổ.\r\nKháng sinh dự phòng trước mổ 30 phút theo phác đồ.\r\nChuyển phòng mổ theo lịch.",
            "Chăm sóc cấp II (CSII).\r\nVệ sinh vùng mổ, thay trang phục mổ.\r\nNhịn ăn uống tuyệt đối trước mổ.",
            "Chăm sóc cấp II (CSII)",
            "Nhịn ăn trước mổ"
        ));

        list.Add(new ClinicalTemplate(
            "4. Hậu phẫu 24 giờ đầu (Sau mổ)",
            "BỆNH NHÂN PHẪU THUẬT VỀ KHOA (Bàn giao từ phòng Hồi tỉnh/GMHS):\r\nBệnh nhân tỉnh táo, tiếp xúc tốt, đã thoát mê / thoát tê hoàn toàn.\r\nDa niêm mạc hồng, tự thở êm, SpO2 98-99%.\r\nHuyết động ổn định: Mạch 80-85 l/p, HA 120/80 mmHg.\r\nVết mổ nề nhẹ, băng thấm ít dịch máu.\r\nDẫn lưu vết mổ ra ít dịch hồng (< 50ml), hoạt động tốt.\r\nĐầu chi hồng ấm, mạch ngoại vi bắt rõ, không tê liệt ngọn chi.\r\nĐau vết mổ mức độ vừa (VAS 3-4 điểm).",
            "Theo dõi sát toàn trạng và huyết động 24h sau mổ.\r\nThuốc giảm đau, kháng sinh, chống phù nề theo biên bản bàn giao gây mê.\r\nRút dẫn lưu sau 24 - 48h khi dịch ra < 30ml/24h.",
            "Chăm sóc cấp I / II (CSCI / CSII).\r\nKê cao chi mổ / nằm ngửa có gối đỡ tư thế chuẩn.\r\nTheo dõi mạch, huyết áp, nhiệt độ, SpO2 mỗi 3 - 6 giờ.\r\nTheo dõi màu sắc đầu chi và lượng dịch dẫn lưu.",
            "Chăm sóc cấp I (CSCI)",
            "Cháo / Súp dinh dưỡng"
        ));

        list.Add(new ClinicalTemplate(
            "5. Tổng kết ra viện (Discharge Summary)",
            "TỔNG KẾT BỆNH ÁN RA VIỆN:\r\n- Diễn biến điều trị: Diễn biến thuận lợi, không tai biến sau mổ, vết mổ khô sạch liền sẹo tốt.\r\n- Tình trạng hiện tại: Bệnh nhân tỉnh táo, hết sốt, huyết động ổn định, vết mổ khô sạch (đã cắt chỉ / liền sẹo), đỡ đau nhiều, đi lại và vận động phục hồi tốt, đại tiểu tiện tự chủ.\r\n- Đủ điều kiện xuất viện.",
            "Cho bệnh nhân ra viện.\r\nKê đơn thuốc điều trị ngoại trú.\r\nHẹn khám lại sau 01 tháng kèm phim chụp kiểm tra.\r\nDặn dò chế độ tập PHCN và dinh dưỡng tại nhà.",
            "Chăm sóc cấp II (CSII).\r\nHướng dẫn bệnh nhân và gia đình làm thủ tục thanh toán ra viện.",
            "Chăm sóc cấp II (CSII)",
            "BT01"
        ));

        list.Add(new ClinicalTemplate(
            "6. Theo dõi hội chẩn / Bệnh nhân nặng",
            "HỘI CHẨN CHUYÊN KHOA:\r\nBệnh nhân mệt nhiều, tri giác chậm hoặc có diễn biến cấp tính.\r\nDa xanh tái / niêm mạc nhợt.\r\nĐau nhiều vùng tổn thương (VAS 6-8đ), sưng nề chèn ép ngọn chi.\r\nĐã báo cáo lãnh đạo khoa và mời chuyên khoa liên quan hội chẩn xử trí cấp cứu.",
            "Thực hiện ngay các y lệnh cấp cứu theo biên bản hội chẩn.\r\nKhẩn trương hoàn thiện các xét nghiệm và CĐHA cấp cứu.",
            "Chăm sóc cấp I (CSCI - Bệnh nhân nặng).\r\nTheo dõi sát mạch, huyết áp, nhiệt độ, SpO2 liên tục mỗi 1 - 2 giờ.\r\nBáo bác sĩ ngay khi có diễn biến bất thường.",
            "Chăm sóc cấp I (CSCI)",
            "Cháo / Súp dinh dưỡng"
        ));
        return list;
    }

    private void InitializeClinicalTemplates()
    {
        clinicalTemplates = GetDefaultClinicalTemplates();
    }

    private void InitializeCustomComponents()
    {
        this.Text = "HỆ THỐNG TẠO TỜ ĐIỀU TRỊ & CHĂM SÓC BỆNH NHÂN NỘI TRÚ - KHOA CTCH & CỘT SỐNG (KHOA 57)";
        this.Size = new Size(1440, 850);
        this.MinimumSize = new Size(1150, 700);
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
            BackColor = Color.FromArgb(15, 76, 129), // Classic Medical Blue
            Padding = new Padding(16, 8, 16, 8)
        };

        Label lblTitle = new Label
        {
            Text = "📋 TẠO TỜ ĐIỀU TRỊ & CHẾ ĐỘ CHĂM SÓC NỘI TRÚ (HIS / MOS EMR)",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Label lblBadge = new Label
        {
            Text = "KHOA CTCH & CỘT SỐNG (KHOA 57) | BS. NGUYỄN HỮU SÂM / BS. VŨ MINH CƯỜNG",
            ForeColor = Color.FromArgb(224, 242, 254),
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
            Width = 440,
            BackColor = Color.White,
            Padding = new Padding(10),
            BorderStyle = BorderStyle.FixedSingle
        };

        Label lblSidebarTitle = new Label
        {
            Text = "📝 NỘI DUNG TỜ ĐIỀU TRỊ & Y LỆNH",
            Font = new Font("Segoe UI", 11.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 76, 129),
            Dock = DockStyle.Top,
            Height = 28
        };

        Panel pnlScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        int curY = 4;

        // 1. Time & Date Selection Box
        GroupBox grpTime = new GroupBox
        {
            Text = "1. Mốc Thời Gian Y Lệnh (Giờ cụ thể)",
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(4, curY),
            Size = new Size(405, 95)
        };

        Label lblDate = new Label { Text = "Ngày:", Location = new Point(10, 24), AutoSize = true, Font = new Font("Segoe UI", 9.0f) };
        dtpOrderDate = new DateTimePicker
        {
            Location = new Point(50, 21),
            Size = new Size(115, 24),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = new Font("Segoe UI", 9.0f)
        };

        Label lblTime = new Label { Text = "Giờ:", Location = new Point(175, 24), AutoSize = true, Font = new Font("Segoe UI", 9.0f) };
        dtpOrderTime = new DateTimePicker
        {
            Location = new Point(205, 21),
            Size = new Size(75, 24),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Font = new Font("Segoe UI", 9.0f)
        };
        dtpOrderTime.Value = DateTime.Now;

        Label lblFastTime = new Label { Text = "Chọn nhanh:", Location = new Point(10, 58), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        cboPresetTimes = new ComboBox
        {
            Location = new Point(90, 55),
            Size = new Size(190, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 8.5f)
        };
        cboPresetTimes.Items.AddRange(new object[] {
            "-- Chọn mốc giờ chuẩn --",
            "08:00 (Đi buồng sáng)",
            "11:00 (Theo dõi trưa)",
            "14:00 (Buồng bệnh chiều)",
            "16:30 (Bàn giao ca trực)",
            "20:00 (Khám trực tối)",
            "Hiện tại (Thời gian thực)"
        });
        cboPresetTimes.SelectedIndex = 1; // Default 08:00
        cboPresetTimes.SelectedIndexChanged += (s, e) =>
        {
            if (cboPresetTimes.SelectedIndex == 1) dtpOrderTime.Value = DateTime.Today.AddHours(8);
            else if (cboPresetTimes.SelectedIndex == 2) dtpOrderTime.Value = DateTime.Today.AddHours(11);
            else if (cboPresetTimes.SelectedIndex == 3) dtpOrderTime.Value = DateTime.Today.AddHours(14);
            else if (cboPresetTimes.SelectedIndex == 4) dtpOrderTime.Value = DateTime.Today.AddHours(16).AddMinutes(30);
            else if (cboPresetTimes.SelectedIndex == 5) dtpOrderTime.Value = DateTime.Today.AddHours(20);
            else if (cboPresetTimes.SelectedIndex == 6) dtpOrderTime.Value = DateTime.Now;
        };

        Button btnSetNow = new Button
        {
            Text = "⏱ Hiện tại",
            Location = new Point(290, 21),
            Size = new Size(100, 58),
            BackColor = Color.FromArgb(239, 246, 255),
            ForeColor = Color.FromArgb(29, 78, 216),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSetNow.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
        btnSetNow.Click += (s, e) =>
        {
            dtpOrderDate.Value = DateTime.Today;
            dtpOrderTime.Value = DateTime.Now;
            cboPresetTimes.SelectedIndex = 6;
        };

        grpTime.Controls.Add(lblDate);
        grpTime.Controls.Add(dtpOrderDate);
        grpTime.Controls.Add(lblTime);
        grpTime.Controls.Add(dtpOrderTime);
        grpTime.Controls.Add(lblFastTime);
        grpTime.Controls.Add(cboPresetTimes);
        grpTime.Controls.Add(btnSetNow);

        curY += 102;

        // 2. Clinical Template Box
        GroupBox grpTemplate = new GroupBox
        {
            Text = "2. Mẫu Lâm Sàng Chuẩn (Khoa 57)",
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(4, curY),
            Size = new Size(405, 58)
        };

        cboTemplates = new ComboBox
        {
            Location = new Point(10, 22),
            Size = new Size(380, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.0f)
        };
        foreach (var t in clinicalTemplates) cboTemplates.Items.Add(t);
        cboTemplates.SelectedIndex = 0;
        cboTemplates.SelectedIndexChanged += CboTemplates_SelectedIndexChanged;

        grpTemplate.Controls.Add(cboTemplates);
        curY += 64;

        // 3. Clinical Content (Diễn biến bệnh)
        GroupBox grpContent = new GroupBox
        {
            Text = "3. Diễn Biến Bệnh Lý (Content)",
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(4, curY),
            Size = new Size(405, 115)
        };
        txtContent = new TextBox
        {
            Location = new Point(8, 20),
            Size = new Size(385, 85),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 9.0f)
        };
        grpContent.Controls.Add(txtContent);
        curY += 122;

        // 4. Medical Instruction (Y lệnh bác sĩ)
        GroupBox grpMed = new GroupBox
        {
            Text = "4. Y Lệnh Bác Sĩ (Medical Instruction)",
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(4, curY),
            Size = new Size(405, 80)
        };
        txtMedicalInstruction = new TextBox
        {
            Location = new Point(8, 20),
            Size = new Size(385, 52),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 9.0f)
        };
        grpMed.Controls.Add(txtMedicalInstruction);
        curY += 86;

        // 5. Care Instruction (Chế độ chăm sóc)
        GroupBox grpCare = new GroupBox
        {
            Text = "5. Chế Độ Chăm Sóc Điều Dưỡng (Care)",
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(4, curY),
            Size = new Size(405, 195)
        };

        Label lblCareLvl = new Label { Text = "Cấp CS:", Location = new Point(8, 22), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        cboCareLevel = new ComboBox
        {
            Location = new Point(60, 19),
            Size = new Size(140, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 8.5f)
        };
        cboCareLevel.Items.AddRange(new object[] { "Chăm sóc cấp I (CSCI)", "Chăm sóc cấp II (CSII)", "Chăm sóc cấp III (CSIII)" });
        cboCareLevel.SelectedIndex = 1;
        cboCareLevel.SelectedIndexChanged += (s, e) => RebuildCareInstructionText();

        Label lblDiet = new Label { Text = "Ăn uống:", Location = new Point(208, 22), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        cboDietMode = new ComboBox
        {
            Location = new Point(265, 19),
            Size = new Size(128, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 8.5f)
        };
        cboDietMode.Items.AddRange(new object[] { "BT01 (Ăn thường)", "DD01 (ĐTĐ)", "TM01 (Tim mạch)", "Cháo / Súp dinh dưỡng", "Nhịn ăn trước mổ" });
        cboDietMode.SelectedIndex = 0;
        cboDietMode.SelectedIndexChanged += (s, e) => RebuildCareInstructionText();

        chkCareDhst2Times = new CheckBox { Text = "Theo dõi DHST 2 lần/ngày", Location = new Point(10, 48), Size = new Size(185, 20), Checked = true, Font = new Font("Segoe UI", 8.0f) };
        chkCareDhstEvery3h = new CheckBox { Text = "Theo dõi DHST mỗi 3 - 6h", Location = new Point(205, 48), Size = new Size(185, 20), Checked = false, Font = new Font("Segoe UI", 8.0f) };
        chkCareDressing = new CheckBox { Text = "Thay băng vô khuẩn hàng ngày", Location = new Point(10, 70), Size = new Size(185, 20), Checked = true, Font = new Font("Segoe UI", 8.0f) };
        chkCareGlucose = new CheckBox { Text = "Đo đường máu mao mạch", Location = new Point(205, 70), Size = new Size(185, 20), Checked = false, Font = new Font("Segoe UI", 8.0f) };
        chkCarePhysio = new CheckBox { Text = "Hướng dẫn tập PHCN tại giường", Location = new Point(10, 92), Size = new Size(185, 20), Checked = false, Font = new Font("Segoe UI", 8.0f) };
        chkCareFasting = new CheckBox { Text = "Dặn nhịn ăn từ 00h trước mổ", Location = new Point(205, 92), Size = new Size(185, 20), Checked = false, Font = new Font("Segoe UI", 8.0f) };

        chkCareDhst2Times.CheckedChanged += (s, e) => RebuildCareInstructionText();
        chkCareDhstEvery3h.CheckedChanged += (s, e) => RebuildCareInstructionText();
        chkCareDressing.CheckedChanged += (s, e) => RebuildCareInstructionText();
        chkCareGlucose.CheckedChanged += (s, e) => RebuildCareInstructionText();
        chkCarePhysio.CheckedChanged += (s, e) => RebuildCareInstructionText();
        chkCareFasting.CheckedChanged += (s, e) => RebuildCareInstructionText();

        txtCareInstruction = new TextBox
        {
            Location = new Point(8, 118),
            Size = new Size(385, 68),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 9.0f)
        };

        grpCare.Controls.Add(lblCareLvl);
        grpCare.Controls.Add(cboCareLevel);
        grpCare.Controls.Add(lblDiet);
        grpCare.Controls.Add(cboDietMode);
        grpCare.Controls.Add(chkCareDhst2Times);
        grpCare.Controls.Add(chkCareDhstEvery3h);
        grpCare.Controls.Add(chkCareDressing);
        grpCare.Controls.Add(chkCareGlucose);
        grpCare.Controls.Add(chkCarePhysio);
        grpCare.Controls.Add(chkCareFasting);
        grpCare.Controls.Add(txtCareInstruction);

        curY += 202;

        // 6. Vital Signs (DHST)
        GroupBox grpDhst = new GroupBox
        {
            Text = "6. Dấu Hiệu Sinh Tồn (DHST)",
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(4, curY),
            Size = new Size(405, 115)
        };

        chkIncludeDhst = new CheckBox
        {
            Text = "Tạo kèm Dấu hiệu sinh tồn (DHST) cùng giờ",
            Location = new Point(10, 20),
            Size = new Size(380, 20),
            Checked = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 101, 52)
        };

        Label lblPulse = new Label { Text = "Mạch (l/p):", Location = new Point(8, 48), AutoSize = true, Font = new Font("Segoe UI", 8.0f) };
        nudPulse = new NumericUpDown { Location = new Point(68, 46), Size = new Size(50, 22), Minimum = 30, Maximum = 220, Value = 80, Font = new Font("Segoe UI", 8.5f) };

        Label lblBp = new Label { Text = "HA (mmHg):", Location = new Point(126, 48), AutoSize = true, Font = new Font("Segoe UI", 8.0f) };
        nudBpMax = new NumericUpDown { Location = new Point(195, 46), Size = new Size(48, 22), Minimum = 50, Maximum = 260, Value = 120, Font = new Font("Segoe UI", 8.5f) };
        Label lblSlash = new Label { Text = "/", Location = new Point(245, 48), AutoSize = true, Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
        nudBpMin = new NumericUpDown { Location = new Point(255, 46), Size = new Size(48, 22), Minimum = 30, Maximum = 180, Value = 80, Font = new Font("Segoe UI", 8.5f) };

        Label lblTemp = new Label { Text = "Nhiệt (°C):", Location = new Point(308, 48), AutoSize = true, Font = new Font("Segoe UI", 8.0f) };
        nudTemp = new NumericUpDown { Location = new Point(360, 46), Size = new Size(40, 22), Minimum = 34, Maximum = 43, DecimalPlaces = 1, Value = 36.8m, Font = new Font("Segoe UI", 8.5f) };

        Label lblBreath = new Label { Text = "Nhịp thở (l/p):", Location = new Point(8, 78), AutoSize = true, Font = new Font("Segoe UI", 8.0f) };
        nudBreathRate = new NumericUpDown { Location = new Point(80, 76), Size = new Size(48, 22), Minimum = 8, Maximum = 60, Value = 18, Font = new Font("Segoe UI", 8.5f) };

        Label lblSpo2 = new Label { Text = "SpO2 (%):", Location = new Point(140, 78), AutoSize = true, Font = new Font("Segoe UI", 8.0f) };
        nudSpo2 = new NumericUpDown { Location = new Point(195, 76), Size = new Size(48, 22), Minimum = 60, Maximum = 100, Value = 98, Font = new Font("Segoe UI", 8.5f) };

        Label lblWeight = new Label { Text = "Cân nặng (kg):", Location = new Point(255, 78), AutoSize = true, Font = new Font("Segoe UI", 8.0f) };
        nudWeight = new NumericUpDown { Location = new Point(345, 76), Size = new Size(55, 22), Minimum = 10, Maximum = 200, DecimalPlaces = 1, Value = 60.0m, Font = new Font("Segoe UI", 8.5f) };

        chkIncludeDhst.CheckedChanged += (s, e) =>
        {
            nudPulse.Enabled = chkIncludeDhst.Checked;
            nudBpMax.Enabled = chkIncludeDhst.Checked;
            nudBpMin.Enabled = chkIncludeDhst.Checked;
            nudTemp.Enabled = chkIncludeDhst.Checked;
            nudBreathRate.Enabled = chkIncludeDhst.Checked;
            nudSpo2.Enabled = chkIncludeDhst.Checked;
            nudWeight.Enabled = chkIncludeDhst.Checked;
        };

        grpDhst.Controls.Add(chkIncludeDhst);
        grpDhst.Controls.Add(lblPulse);
        grpDhst.Controls.Add(nudPulse);
        grpDhst.Controls.Add(lblBp);
        grpDhst.Controls.Add(nudBpMax);
        grpDhst.Controls.Add(lblSlash);
        grpDhst.Controls.Add(nudBpMin);
        grpDhst.Controls.Add(lblTemp);
        grpDhst.Controls.Add(nudTemp);
        grpDhst.Controls.Add(lblBreath);
        grpDhst.Controls.Add(nudBreathRate);
        grpDhst.Controls.Add(lblSpo2);
        grpDhst.Controls.Add(nudSpo2);
        grpDhst.Controls.Add(lblWeight);
        grpDhst.Controls.Add(nudWeight);

        curY += 122;

        // 7. Action Button Panel
        btnCreateTracking = new Button
        {
            Text = "🚀 TẠO TỜ ĐIỀU TRỊ TRÊN HIS / MOS",
            Location = new Point(4, curY),
            Size = new Size(275, 46),
            BackColor = Color.FromArgb(15, 76, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCreateTracking.FlatAppearance.BorderSize = 0;
        btnCreateTracking.Click += async (s, e) => await ExecuteCreateTrackingAsync();

        btnClearInputs = new Button
        {
            Text = "🔄 Đặt lại",
            Location = new Point(285, curY),
            Size = new Size(124, 46),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnClearInputs.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnClearInputs.Click += (s, e) =>
        {
            cboTemplates.SelectedIndex = 0;
            ApplySelectedTemplate();
        };

        curY += 56;

        pnlScroll.Controls.Add(grpTime);
        pnlScroll.Controls.Add(grpTemplate);
        pnlScroll.Controls.Add(grpContent);
        pnlScroll.Controls.Add(grpMed);
        pnlScroll.Controls.Add(grpCare);
        pnlScroll.Controls.Add(grpDhst);
        pnlScroll.Controls.Add(btnCreateTracking);
        pnlScroll.Controls.Add(btnClearInputs);

        pnlLeftSidebar.Controls.Add(pnlScroll);
        pnlLeftSidebar.Controls.Add(lblSidebarTitle);

        // Fill initial template
        ApplySelectedTemplate();
    }

    private void CboTemplates_SelectedIndexChanged(object sender, EventArgs e)
    {
        ApplySelectedTemplate();
    }

    private void ApplySelectedTemplate()
    {
        var tmpl = cboTemplates.SelectedItem as ClinicalTemplate;
        if (tmpl == null) return;

        txtContent.Text = tmpl.Content;
        txtMedicalInstruction.Text = tmpl.MedicalInstruction;
        txtCareInstruction.Text = tmpl.CareInstruction;

        if (!string.IsNullOrEmpty(tmpl.CareLevel))
        {
            for (int i = 0; i < cboCareLevel.Items.Count; i++)
            {
                if (cboCareLevel.Items[i].ToString().Contains(tmpl.CareLevel))
                {
                    cboCareLevel.SelectedIndex = i;
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(tmpl.DietMode))
        {
            for (int i = 0; i < cboDietMode.Items.Count; i++)
            {
                if (cboDietMode.Items[i].ToString().Contains(tmpl.DietMode))
                {
                    cboDietMode.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void RebuildCareInstructionText()
    {
        string careLvl = cboCareLevel.SelectedItem != null ? cboCareLevel.SelectedItem.ToString() : "Chăm sóc cấp II (CSII)";
        string diet = cboDietMode.SelectedItem != null ? cboDietMode.SelectedItem.ToString() : "BT01 (Ăn thường)";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(careLvl + ".");
        sb.AppendLine("Chế độ ăn: " + diet + ".");

        List<string> actions = new List<string>();
        if (chkCareDhst2Times.Checked) actions.Add("Theo dõi DHST (Mạch, HA, Nhiệt độ) 2 lần/ngày");
        if (chkCareDhstEvery3h.Checked) actions.Add("Theo dõi DHST mỗi 3 - 6 giờ");
        if (chkCareDressing.Checked) actions.Add("Thay băng chăm sóc vết thương vô khuẩn hàng ngày");
        if (chkCareGlucose.Checked) actions.Add("Đo đường máu mao mạch theo dõi");
        if (chkCarePhysio.Checked) actions.Add("Hướng dẫn tập PHCN tại giường");
        if (chkCareFasting.Checked) actions.Add("Dặn nhịn ăn uống tuyệt đối từ 00h trước mổ");

        if (actions.Count > 0)
        {
            sb.AppendLine(string.Join(". ", actions) + ".");
        }

        txtCareInstruction.Text = sb.ToString().Trim();
    }

    private void BuildRightMainPanel()
    {
        pnlRightMain = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(10)
        };

        // 1. Top Toolbar (Patient Lookup & Filters)
        pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 175,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(10),
            BorderStyle = BorderStyle.FixedSingle
        };

        Label lblInputPrompt = new Label
        {
            Text = "🔍 Nhập Mã Bệnh Nhân / Mã Điều Trị cụ thể (hoặc dán nhiều mã cách nhau bởi dấu phẩy / dấu cách / xuống dòng):",
            Location = new Point(10, 8),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 76, 129)
        };

        txtInputCodes = new TextBox
        {
            Location = new Point(10, 30),
            Size = new Size(500, 26),
            Font = new Font("Segoe UI", 10.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        txtInputCodes.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await LookupAndAddPatientsAsync();
            }
        };

        btnLookupCodes = new Button
        {
            Text = "🔎 Tra Cứu BN",
            Location = new Point(518, 28),
            Size = new Size(125, 30),
            BackColor = Color.FromArgb(15, 76, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLookupCodes.FlatAppearance.BorderSize = 0;
        btnLookupCodes.Click += async (s, e) => await LookupAndAddPatientsAsync();

        btnLoadDept57 = new Button
        {
            Text = "🏥 Tải Toàn Bộ BN Khoa 57",
            Location = new Point(650, 28),
            Size = new Size(185, 30),
            BackColor = Color.FromArgb(2, 132, 199), // Light Blue
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLoadDept57.FlatAppearance.BorderSize = 0;
        btnLoadDept57.Click += async (s, e) => await LoadAllDept57PatientsAsync();

        Button btnClearList = new Button
        {
            Text = "🗑 Xóa danh sách",
            Location = new Point(842, 28),
            Size = new Size(120, 30),
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(100, 116, 139),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        };
        btnClearList.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnClearList.Click += (s, e) => ClearPatientList();

        // 2. Patient Card (Live Info for Selected Patient)
        pnlPatientCard = new Panel
        {
            Location = new Point(10, 64),
            Size = new Size(955, 68),
            BackColor = Color.FromArgb(239, 246, 255), // Light blue box
            BorderStyle = BorderStyle.FixedSingle
        };

        lblCardPatientName = new Label
        {
            Text = "👤 CHƯA CHỌN BỆNH NHÂN (Nhập mã bệnh nhân ở trên để tra cứu tức thì)",
            Location = new Point(10, 8),
            AutoSize = true,
            Font = new Font("Segoe UI", 10.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 138)
        };

        lblCardPatientInfo = new Label
        {
            Text = "Mã BN: -- | Mã ĐT: -- | Giới: -- | Tuổi: -- | Đối tượng: --",
            Location = new Point(10, 28),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(51, 65, 85)
        };

        lblCardLocation = new Label
        {
            Text = "Vị trí: Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57) - Phòng 734",
            Location = new Point(10, 46),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(71, 85, 105)
        };

        lblCardDiagnosis = new Label
        {
            Text = "Chẩn đoán: --",
            Location = new Point(480, 28),
            Size = new Size(465, 34),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(185, 28, 28)
        };

        pnlPatientCard.Controls.Add(lblCardPatientName);
        pnlPatientCard.Controls.Add(lblCardPatientInfo);
        pnlPatientCard.Controls.Add(lblCardLocation);
        pnlPatientCard.Controls.Add(lblCardDiagnosis);

        // 3. Filter Row
        Label lblSearch = new Label { Text = "🔍 Lọc nhanh:", Location = new Point(10, 142), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        txtSearchFilter = new TextBox { Location = new Point(90, 139), Size = new Size(200, 23), Font = new Font("Segoe UI", 8.5f) };
        txtSearchFilter.TextChanged += (s, e) => ApplyFilter();

        Label lblRoomFilter = new Label { Text = "Buồng bệnh:", Location = new Point(305, 142), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
        cboFilterRoom = new ComboBox { Location = new Point(385, 139), Size = new Size(130, 23), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
        cboFilterRoom.Items.Add("-- Tất cả buồng --");
        cboFilterRoom.SelectedIndex = 0;
        cboFilterRoom.SelectedIndexChanged += (s, e) => ApplyFilter();

        btnSelectAll = new Button
        {
            Text = "☑ Chọn tất cả",
            Location = new Point(530, 137),
            Size = new Size(100, 26),
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
            Location = new Point(636, 137),
            Size = new Size(110, 26),
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
        pnlToolbar.Controls.Add(btnClearList);
        pnlToolbar.Controls.Add(pnlPatientCard);
        pnlToolbar.Controls.Add(lblSearch);
        pnlToolbar.Controls.Add(txtSearchFilter);
        pnlToolbar.Controls.Add(lblRoomFilter);
        pnlToolbar.Controls.Add(cboFilterRoom);
        pnlToolbar.Controls.Add(btnSelectAll);
        pnlToolbar.Controls.Add(btnUnselectAll);

        // 4. DataGridView for Patients
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
        dgvPatients.SelectionChanged += DgvPatients_SelectionChanged;

        BuildGridColumns();

        // 5. Bottom Actions Panel
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
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TrackingTimeDisplay", HeaderText = "Thời Gian Tờ ĐT", Width = 130, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CreatedTrackingId", HeaderText = "ID Tờ ĐT", Width = 90, ReadOnly = true });
        dgvPatients.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ExecutionStatus", HeaderText = "Trạng Thái Tạo", Width = 160, ReadOnly = true });
    }

    private void DgvPatients_SelectionChanged(object sender, EventArgs e)
    {
        if (dgvPatients.CurrentRow != null && dgvPatients.CurrentRow.Index >= 0 && dgvPatients.CurrentRow.Index < filteredPatients.Count)
        {
            var p = filteredPatients[dgvPatients.CurrentRow.Index];
            UpdatePatientCard(p);
        }
    }

    private void UpdatePatientCard(PatientTrackingDto p)
    {
        if (p == null)
        {
            lblCardPatientName.Text = "👤 CHƯA CHỌN BỆNH NHÂN";
            lblCardPatientInfo.Text = "Mã BN: -- | Mã ĐT: -- | Giới: -- | Tuổi: -- | Đối tượng: --";
            lblCardLocation.Text = "Vị trí: Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57)";
            lblCardDiagnosis.Text = "Chẩn đoán: --";
            return;
        }

        lblCardPatientName.Text = string.Format("👤 {0} ({1} - {2} tuổi)", (p.PatientName ?? "").ToUpper(), p.GenderName, p.AgeStr);
        lblCardPatientInfo.Text = string.Format("Mã BN: {0} | Mã ĐT: {1} | Đối tượng: {2} | Treatment ID: {3}",
            p.PatientCode, p.TreatmentCode, p.PatientTypeName, p.TreatmentId);
        lblCardLocation.Text = string.Format("Vị trí: Khoa CTCH & Cột sống (Khoa 57) | {0}", !string.IsNullOrEmpty(p.BedFull) ? p.BedFull : "Phòng 734");
        lblCardDiagnosis.Text = string.Format("Chẩn đoán: [{0}] {1}", p.IcdCode, !string.IsNullOrEmpty(p.IcdText) ? p.IcdText : p.IcdName);
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
            Text = "Sẵn sàng tạo tờ điều trị.",
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
        lblStatusText.Text = "✔ Kết nối HIS thành công! Sẵn sàng tạo Tờ điều trị & Chế độ chăm sóc.";
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
        UpdatePatientCard(null);
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
                List<PatientTrackingDto> results = new List<PatientTrackingDto>();

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

            UpdateRoomFilterDropdown();
            ApplyFilter();
            txtInputCodes.Clear();
            if (addedList.Count > 0)
            {
                UpdatePatientCard(addedList[0]);
            }
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
            if (allPatients.Count > 0)
            {
                UpdatePatientCard(allPatients[0]);
            }
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

    private async Task ExecuteCreateTrackingAsync()
    {
        var selectedPatients = allPatients.Where(p => p.Selected).ToList();
        if (selectedPatients.Count == 0)
        {
            MessageBox.Show("Vui lòng tích chọn ít nhất 1 bệnh nhân trong danh sách!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DateTime orderDate = dtpOrderDate.Value.Date;
        DateTime orderTime = dtpOrderTime.Value;
        DateTime fullDateTime = new DateTime(orderDate.Year, orderDate.Month, orderDate.Day, orderTime.Hour, orderTime.Minute, 0);
        long trackingTime = long.Parse(fullDateTime.ToString("yyyyMMddHHmmss"));

        string content = (txtContent.Text ?? "").Trim();
        string medInstruction = (txtMedicalInstruction.Text ?? "").Trim();
        string careInstruction = (txtCareInstruction.Text ?? "").Trim();

        if (string.IsNullOrEmpty(content))
        {
            MessageBox.Show("Vui lòng nhập nội dung diễn biến bệnh lý (Content)!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtContent.Focus();
            return;
        }

        bool includeDhst = chkIncludeDhst.Checked;
        long? pulse = includeDhst ? (long?)nudPulse.Value : null;
        decimal? temp = includeDhst ? (decimal?)nudTemp.Value : null;
        long? bpMax = includeDhst ? (long?)nudBpMax.Value : null;
        long? bpMin = includeDhst ? (long?)nudBpMin.Value : null;
        long? breathRate = includeDhst ? (long?)nudBreathRate.Value : null;
        decimal? spo2 = includeDhst ? (decimal?)nudSpo2.Value : null;
        decimal? weight = includeDhst ? (decimal?)nudWeight.Value : null;

        string confirmMsg = string.Format(
            "XÁC NHẬN TẠO TỜ ĐIỀU TRỊ:\n\n" +
            "• Số bệnh nhân: {0} BN\n" +
            "• Mốc thời gian: {1}\n" +
            "• Chế độ chăm sóc: {2}\n" +
            "• Kèm Dấu hiệu sinh tồn: {3}\n\n" +
            "Bạn có chắc chắn muốn tiến hành ghi nhận trên hệ thống HIS / MOS?",
            selectedPatients.Count,
            fullDateTime.ToString("HH:mm dd/MM/yyyy"),
            !string.IsNullOrEmpty(careInstruction) ? careInstruction.Split('\n')[0].Trim() : "Không",
            includeDhst ? string.Format("Có (Mạch {0}, HA {1}/{2}, T {3}°C, SpO2 {4}%)", pulse, bpMax, bpMin, temp, spo2) : "Không");

        if (MessageBox.Show(confirmMsg, "Xác nhận tạo Tờ điều trị", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        btnCreateTracking.Enabled = false;
        lblStatusText.Text = "Đang tiến hành tạo tờ điều trị trên hệ thống HIS / MOS...";

        try
        {
            int totalSuccess = 0;
            int totalFail = 0;

            await Task.Run(() =>
            {
                InitSession();

                foreach (var p in selectedPatients)
                {
                    try
                    {
                        long tId = CreateSinglePatientTracking(p, trackingTime, content, medInstruction, careInstruction,
                            pulse, temp, bpMax, bpMin, breathRate, spo2, weight);

                        p.CreatedTrackingId = tId;
                        p.TrackingTimeDisplay = fullDateTime.ToString("HH:mm dd/MM/yyyy");
                        p.StatusCode = 1;
                        p.ExecutionStatus = string.Format("✔ Thành công (ID: {0})", tId);
                        totalSuccess++;
                    }
                    catch (Exception ex)
                    {
                        p.StatusCode = 2;
                        p.ExecutionStatus = "❌ Lỗi: " + ex.Message;
                        totalFail++;
                    }

                    this.Invoke(new Action(() =>
                    {
                        dgvPatients.Refresh();
                    }));
                }
            });

            lblStatusText.Text = string.Format("Hoàn tất! Thành công: {0}, Thất bại: {1}", totalSuccess, totalFail);
            MessageBox.Show(string.Format("Đã hoàn tất tạo Tờ điều trị!\n\n✔ Thành công: {0} BN\n❌ Thất bại: {1} BN", totalSuccess, totalFail),
                "Kết quả thực hiện", MessageBoxButtons.OK, totalFail == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi tổng quát khi tạo tờ điều trị:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatusText.Text = "Lỗi: " + ex.Message;
        }
        finally
        {
            btnCreateTracking.Enabled = true;
        }
    }

    private void ExportToCsv()
    {
        if (filteredPatients.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu bệnh nhân để xuất file!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SaveFileDialog sfd = new SaveFileDialog
        {
            Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
            FileName = string.Format("Danh_Sach_To_Dieu_Tri_{0}.csv", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("STT,BuongGiuong,MaBN,MaDT,HoTenBN,Gioi,Tuoi,DoiTuong,ChanDoan,ThoiGian,TrackingID,TrangThai");
                foreach (var p in filteredPatients)
                {
                    sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\"",
                        p.Stt,
                        (p.BedFull ?? "").Replace("\"", "\"\""),
                        p.PatientCode,
                        p.TreatmentCode,
                        (p.PatientName ?? "").Replace("\"", "\"\""),
                        p.GenderName,
                        p.AgeStr,
                        p.PatientTypeName,
                        (p.IcdText ?? "").Replace("\"", "\"\""),
                        p.TrackingTimeDisplay,
                        p.CreatedTrackingId > 0 ? p.CreatedTrackingId.ToString() : "",
                        (p.ExecutionStatus ?? "").Replace("\"", "\"\"")));
                }
                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Xuất file CSV thành công:\n" + sfd.FileName, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void CopyReportToClipboard()
    {
        if (filteredPatients.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu bệnh nhân để copy!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(string.Format("=== BÁO CÁO TẠO TỜ ĐIỀU TRỊ KHOA CTCH & CỘT SỐNG (KHOA 57) ==="));
        sb.AppendLine(string.Format("Thời gian xuất: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
        sb.AppendLine(string.Format("Tổng số bệnh nhân: {0}\n", filteredPatients.Count));

        foreach (var p in filteredPatients)
        {
            sb.AppendLine(string.Format("• [{0}] {1} - Mã BN: {2} | ID: {3} | {4}",
                p.BedFull, p.PatientName, p.PatientCode, p.CreatedTrackingId > 0 ? p.CreatedTrackingId.ToString() : "N/A", p.ExecutionStatus));
        }

        Clipboard.SetText(sb.ToString());
        MessageBox.Show("Đã copy toàn bộ báo cáo vào Clipboard!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // =========================================================================
    // CORE HIS / MOS BUSINESS LOGIC & SDO ADAPTERS
    // =========================================================================

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

            // Bind token session to working rooms on MOS backend
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

    public static PatientTrackingDto LookupSinglePatient(string code)
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

        PatientTrackingDto item = new PatientTrackingDto
        {
            TreatmentId = tr.ID,
            TreatmentCode = tr.TREATMENT_CODE,
            PatientCode = tr.TDL_PATIENT_CODE,
            PatientName = tr.TDL_PATIENT_NAME,
            GenderName = tr.TDL_PATIENT_GENDER_NAME,
            AgeStr = tr.TDL_PATIENT_DOB > 0 ? (DateTime.Now.Year - int.Parse(tr.TDL_PATIENT_DOB.ToString().Substring(0, 4))).ToString() : "",
            PatientTypeId = tr.TDL_PATIENT_TYPE_ID.HasValue ? tr.TDL_PATIENT_TYPE_ID.Value : 1,
            PatientTypeName = (tr.TDL_PATIENT_TYPE_ID.HasValue && tr.TDL_PATIENT_TYPE_ID.Value == 1) ? "BHYT" : "Viện phí",
            IcdCode = !string.IsNullOrEmpty(tr.ICD_CODE) ? tr.ICD_CODE : "M51.2",
            IcdName = !string.IsNullOrEmpty(tr.ICD_NAME) ? tr.ICD_NAME : "Thoát vị đĩa đệm",
            IcdSubCode = tr.ICD_SUB_CODE,
            IcdText = !string.IsNullOrEmpty(tr.ICD_TEXT) ? tr.ICD_TEXT : tr.ICD_NAME,
            WorkingRoomId = 5248
        };

        // Fetch bedroom
        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.TREATMENT_ID = tr.ID;
        tbrf.IS_IN_ROOM = true;
        var beds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, param);
        if (beds != null && beds.Count > 0)
        {
            var b = beds.OrderByDescending(x => x.ADD_TIME).First();
            item.BedRoomName = b.BED_ROOM_NAME;
            item.BedName = b.BED_NAME;
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

    public static List<PatientTrackingDto> FetchInPatientsKhoa57()
    {
        InitSession();

        List<PatientTrackingDto> results = new List<PatientTrackingDto>();

        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.IS_IN_ROOM = true;
        tbrf.TREATMENT_IS_ACTIVE = true;
        var beds = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, param);

        if (beds == null || beds.Count == 0) return results;

        var dept57Beds = beds.Where(b => b.DEPARTMENT_ID == 57).GroupBy(b => b.TREATMENT_ID).Select(g => g.First()).ToList();

        // Get treatments in batches
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

            PatientTrackingDto item = new PatientTrackingDto
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

    public static long CreateSinglePatientTracking(PatientTrackingDto patient, long trackingTime,
        string content, string medInstruction, string careInstruction,
        long? pulse = null, decimal? temp = null, long? bpMax = null, long? bpMin = null,
        long? breathRate = null, decimal? spo2 = null, decimal? weight = null)
    {
        InitSession();

        long roomId = patient.WorkingRoomId > 0 ? patient.WorkingRoomId : 5248;

        HIS_TRACKING tracking = new HIS_TRACKING
        {
            TREATMENT_ID = patient.TreatmentId,
            DEPARTMENT_ID = 57,
            ROOM_ID = roomId,
            TRACKING_TIME = trackingTime,
            CONTENT = content,
            MEDICAL_INSTRUCTION = medInstruction,
            CARE_INSTRUCTION = careInstruction,
            ICD_CODE = patient.IcdCode,
            ICD_NAME = patient.IcdName,
            ICD_SUB_CODE = patient.IcdSubCode,
            ICD_TEXT = patient.IcdText
        };

        HisTrackingSDO sdo = new HisTrackingSDO
        {
            Tracking = tracking,
            WorkingRoomId = roomId
        };

        if (pulse.HasValue || temp.HasValue || bpMax.HasValue || bpMin.HasValue || breathRate.HasValue || spo2.HasValue || weight.HasValue)
        {
            sdo.Dhst = new HIS_DHST
            {
                TREATMENT_ID = patient.TreatmentId,
                EXECUTE_TIME = trackingTime,
                EXECUTE_LOGINNAME = "vmc",
                EXECUTE_USERNAME = "Vũ Minh Cường",
                PULSE = pulse,
                TEMPERATURE = temp,
                BLOOD_PRESSURE_MAX = bpMax,
                BLOOD_PRESSURE_MIN = bpMin,
                BREATH_RATE = breathRate,
                SPO2 = spo2.HasValue ? (spo2.Value > 1.0m ? spo2.Value / 100.0m : spo2.Value) : (decimal?)null,
                WEIGHT = weight
            };
        }

        var created = myAdapter.PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, param);
        if (created == null)
        {
            throw new Exception("Hệ thống HIS / MOS từ chối tạo tờ điều trị (Success: false)!");
        }

        // Also post DHST separately if needed by certain backend versions
        if (sdo.Dhst != null)
        {
            try
            {
                myAdapter.PostData<HIS_DHST>("api/HisDhst/Create", ApiConsumers.MosConsumer, sdo.Dhst, param);
            }
            catch { }
        }

        return created.ID;
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
            Console.WriteLine("  HisTrackingCreator.exe -p <MaBN1,MaBN2,...> -time <HH:mm> -content <NoiDung> -care <CheDoChamSoc> [-med <YLenh>] [-date <yyyy-MM-dd>]");
            Console.WriteLine("  HisTrackingCreator.exe -p <MaBN> -template <1..6>");
            Console.WriteLine("\nCÁC THAM SỐ TÙY CHỌN:");
            Console.WriteLine("  -pulse <80> -temp <36.8> -bpmax <120> -bpmin <80> -breath <18> -spo2 <98> -weight <60>");
            Console.WriteLine("\nVÍ DỤ:");
            Console.WriteLine("  HisTrackingCreator.exe -p \"0003969449\" -time \"08:00\" -content \"BN tỉnh, không sốt, vết mổ khô sạch.\" -care \"CSII. BT01. Theo dõi DHST 2 lần/ngày.\"");
            Console.WriteLine("  HisTrackingCreator.exe -p \"0003969449,0003298895\" -template 1 -time \"08:00\"");
            return;
        }

        string rawPatients = "";
        string rawTime = "08:00";
        string rawDate = DateTime.Today.ToString("yyyy-MM-dd");
        string content = "Bệnh nhân tỉnh táo, tiếp xúc tốt. Da niêm mạc hồng, không sốt. Vết mổ khô sạch, đầu chi ấm.";
        string medInstruction = "Thuốc dùng theo đơn đã kê. Thay băng chăm sóc vết mổ hàng ngày.";
        string careInstruction = "Chăm sóc cấp II (CSII). Chế độ ăn BT01. Theo dõi DHST 2 lần/ngày.";
        int templateId = 0;

        long? pulse = 80;
        decimal? temp = 36.8m;
        long? bpMax = 120;
        long? bpMin = 80;
        long? breathRate = 18;
        decimal? spo2 = 98;
        decimal? weight = 60.0m;

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-p" || args[i] == "--patient") && i + 1 < args.Length) rawPatients = args[i + 1];
            if ((args[i] == "-time" || args[i] == "-t") && i + 1 < args.Length) rawTime = args[i + 1];
            if ((args[i] == "-date" || args[i] == "-d") && i + 1 < args.Length) rawDate = args[i + 1];
            if ((args[i] == "-content" || args[i] == "-c") && i + 1 < args.Length) content = args[i + 1];
            if ((args[i] == "-med" || args[i] == "-m") && i + 1 < args.Length) medInstruction = args[i + 1];
            if ((args[i] == "-care") && i + 1 < args.Length) careInstruction = args[i + 1];
            if ((args[i] == "-template" || args[i] == "-tmpl") && i + 1 < args.Length) int.TryParse(args[i + 1], out templateId);
            if (args[i] == "-pulse" && i + 1 < args.Length) pulse = long.Parse(args[i + 1]);
            if (args[i] == "-temp" && i + 1 < args.Length) temp = decimal.Parse(args[i + 1]);
            if (args[i] == "-bpmax" && i + 1 < args.Length) bpMax = long.Parse(args[i + 1]);
            if (args[i] == "-bpmin" && i + 1 < args.Length) bpMin = long.Parse(args[i + 1]);
            if (args[i] == "-breath" && i + 1 < args.Length) breathRate = long.Parse(args[i + 1]);
            if (args[i] == "-spo2" && i + 1 < args.Length) spo2 = decimal.Parse(args[i + 1]);
            if (args[i] == "-weight" && i + 1 < args.Length) weight = decimal.Parse(args[i + 1]);
        }

        if (templateId >= 1 && templateId <= 6)
        {
            var tmpls = MainForm.GetDefaultClinicalTemplates();
            var tmpl = tmpls[templateId - 1];
            if (!args.Contains("-content") && !args.Contains("-c")) content = tmpl.Content;
            if (!args.Contains("-med") && !args.Contains("-m")) medInstruction = tmpl.MedicalInstruction;
            if (!args.Contains("-care")) careInstruction = tmpl.CareInstruction;
        }

        if (string.IsNullOrEmpty(rawPatients))
        {
            Console.WriteLine("[LỖI] Thiếu danh sách mã bệnh nhân (-p \"0003969449\")!");
            return;
        }

        DateTime orderDate;
        if (!DateTime.TryParse(rawDate, out orderDate)) orderDate = DateTime.Today;

        TimeSpan tSpan;
        if (!TimeSpan.TryParse(rawTime, out tSpan)) tSpan = new TimeSpan(8, 0, 0);

        DateTime fullDateTime = new DateTime(orderDate.Year, orderDate.Month, orderDate.Day, tSpan.Hours, tSpan.Minutes, 0);
        long trackingTime = long.Parse(fullDateTime.ToString("yyyyMMddHHmmss"));

        var codes = rawPatients.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();

        Console.WriteLine(string.Format("• Số lượng bệnh nhân: {0}", codes.Count));
        Console.WriteLine(string.Format("• Thời gian tờ điều trị: {0}", fullDateTime.ToString("yyyy-MM-dd HH:mm:ss")));
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
                var p = MainForm.LookupSinglePatient(code);
                if (p == null)
                {
                    Console.WriteLine(string.Format("❌ [{0}] Không tìm thấy hồ sơ bệnh nhân!", code));
                    failCount++;
                    continue;
                }

                long tId = MainForm.CreateSinglePatientTracking(p, trackingTime, content, medInstruction, careInstruction,
                    pulse, temp, bpMax, bpMin, breathRate, spo2, weight);

                Console.WriteLine(string.Format("✔ [{0} - {1}] Tạo Tờ điều trị THÀNH CÔNG! ID: {2} | {3}",
                    p.PatientCode, p.PatientName, tId, p.BedFull));
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
