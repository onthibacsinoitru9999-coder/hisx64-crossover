using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HisActionRecorder
{
    public class ActionItem
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimeStr { get; set; }
        public string DelayStr { get; set; }
        public string ActionType { get; set; }
        public string Detail { get; set; }
        public string Payload { get; set; }
        public string WindowTitle { get; set; }
        public string UserNote { get; set; }
    }

    public class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // UI Controls - Panels
        private Panel panelSetup;
        private Panel panelRecording;
        private Panel panelFloating;
        private Panel panelReview;

        // Setup Controls
        private ComboBox cboPurpose;
        private TextBox txtPatientCode;
        private TextBox txtInitialNote;
        private CheckBox chkAutoFloat;
        private Button btnStartRecord;
        private Button btnExitSetup;

        // Recording Controls (Full View)
        private Label lblRecStatus;
        private Label lblRecTimer;
        private Label lblRecCount;
        private ListView lvLiveFeed;
        private Button btnStopRecord;
        private Button btnFloatRecord;
        private Button btnCancelRecord;
        private Button btnExitRecord;

        // Floating Bar Controls (Always on top)
        private Label lblFloatStatus;
        private Button btnFloatStop;
        private Button btnFloatExpand;
        private Button btnFloatExit;

        // Review Controls
        private Label lblReviewTitle;
        private Label lblReviewSummary;
        private DataGridView dgvReview;
        private Button btnSaveExport;
        private Button btnCopyClipboard;
        private Button btnRecordAgain;
        private Button btnOpenLogs;
        private Button btnExitReview;

        // Timers & State
        private Timer timerUi;
        private Timer timerDuration;
        private Stopwatch swDuration;
        private DateTime lastEventTime;
        private bool isRecording = false;
        private bool isFloatingMode = false;

        // LogSystem Stream Reader
        private string logFilePath = "";
        private FileStream fsLog;
        private StreamReader srLog;
        private string lastToken = "";

        // Recorded Items
        private List<ActionItem> recordedList = new List<ActionItem>();
        private ConcurrentQueue<ActionItem> pendingEvents = new ConcurrentQueue<ActionItem>();

        // Win32 Hooks
        private LowLevelProc keyboardProc;
        private LowLevelProc mouseProc;
        private IntPtr hookKeyboard = IntPtr.Zero;
        private IntPtr hookMouse = IntPtr.Zero;

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int VK_F9 = 0x78;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public MainForm()
        {
            InitializeComponent();
            LocateLogFile();
            SetupHooks();
        }

        private void LocateLogFile()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates = new string[]
            {
                Path.Combine(appDir, "Logs\\LogSystem.txt"),
                "D:\\his 3-9\\his-x64-28-11fix GDYK\\his-x64\\Logs\\LogSystem.txt",
                "D:\\his-x64-28-11fix GDYK\\his-x64\\Logs\\LogSystem.txt",
                "E:\\his-x64-28-11fix GDYK\\his-x64\\Logs\\LogSystem.txt",
                "D:\\his-x64\\Logs\\LogSystem.txt",
                "E:\\his-x64\\Logs\\LogSystem.txt"
            };

            foreach (string c in candidates)
            {
                if (File.Exists(c))
                {
                    logFilePath = c;
                    break;
                }
            }

            if (string.IsNullOrEmpty(logFilePath))
            {
                logFilePath = Path.Combine(appDir, "Logs\\LogSystem.txt");
            }
        }

        private void InitializeComponent()
        {
            this.Text = "HIS Action Recorder v3.0 - Ghi Nhận & Học Thao Tác Lâm Sàng";
            this.Size = new Size(1060, 720);
            this.MinimumSize = new Size(500, 70);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.Icon = SystemIcons.Application;
            this.ControlBox = true;
            this.MinimizeBox = true;
            this.MaximizeBox = true;

            // Timer UI
            timerUi = new Timer();
            timerUi.Interval = 100;
            timerUi.Tick += TimerUi_Tick;

            // Timer Duration
            timerDuration = new Timer();
            timerDuration.Interval = 500;
            timerDuration.Tick += TimerDuration_Tick;
            swDuration = new Stopwatch();

            // 1. Panel Setup
            BuildPanelSetup();

            // 2. Panel Recording (Full)
            BuildPanelRecording();

            // 3. Panel Floating Bar (Compact TopMost)
            BuildPanelFloating();

            // 4. Panel Review
            BuildPanelReview();

            // Default show Setup
            ShowPanel(panelSetup);
        }

        private void BuildPanelSetup()
        {
            panelSetup = new Panel();
            panelSetup.Dock = DockStyle.Fill;
            panelSetup.BackColor = Color.FromArgb(248, 249, 250);
            panelSetup.Padding = new Padding(30);

            Label lblBanner = new Label();
            lblBanner.Text = "🎬 HỆ THỐNG GHI & HỌC THAO TÁC HIS (v3.0)";
            lblBanner.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblBanner.ForeColor = Color.FromArgb(24, 43, 73);
            lblBanner.AutoSize = true;
            lblBanner.Location = new Point(30, 25);
            panelSetup.Controls.Add(lblBanner);

            Label lblDesc = new Label();
            lblDesc.Text = "Công cụ tự động lắng nghe thao tác chuột, bàn phím và các gói tin API khi Bác sĩ làm việc trên HIS.\nKhi đang ghi, thanh điều khiển nổi sẽ luôn hiện nút 'TẮT GHI' ngay góc màn hình để Bác sĩ tiện dừng bất kỳ lúc nào.";
            lblDesc.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            lblDesc.ForeColor = Color.FromArgb(80, 90, 105);
            lblDesc.AutoSize = true;
            lblDesc.Location = new Point(32, 65);
            panelSetup.Controls.Add(lblDesc);

            GroupBox gbSetup = new GroupBox();
            gbSetup.Text = " 1. Khai báo mục đích thao tác trước khi ghi ";
            gbSetup.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            gbSetup.ForeColor = Color.FromArgb(13, 110, 253);
            gbSetup.Location = new Point(30, 125);
            gbSetup.Size = new Size(980, 360);
            gbSetup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Field: Purpose
            Label lblP = new Label();
            lblP.Text = "Mục đích thao tác muốn ghi nhận (*):";
            lblP.Font = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            lblP.ForeColor = Color.FromArgb(33, 37, 41);
            lblP.Location = new Point(25, 38);
            lblP.AutoSize = true;
            gbSetup.Controls.Add(lblP);

            cboPurpose = new ComboBox();
            cboPurpose.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            cboPurpose.Location = new Point(25, 65);
            cboPurpose.Size = new Size(925, 30);
            cboPurpose.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cboPurpose.Items.AddRange(new object[] {
                "Mời ký biên bản hội chẩn PT-01 (Thông qua mổ)",
                "Chỉ định hội chẩn chuyên khoa & Mời ký EMR (Type 17)",
                "Tạo tờ điều trị mới & ghi nhận diễn biến/y lệnh",
                "Kê đơn thuốc tủ trực (810 Hà Nội / 5142 Ninh Bình)",
                "Kê đơn lĩnh kho dược nội trú",
                "Chỉ định suất ăn dinh dưỡng bệnh lý (BT01, DD01...)",
                "In ấn và sửa biểu mẫu hồ sơ bệnh án",
                "Tùy chỉnh: Nhập nội dung nghiệp vụ khác..."
            });
            cboPurpose.SelectedIndex = 0;
            gbSetup.Controls.Add(cboPurpose);

            // Field: Patient Code
            Label lblPt = new Label();
            lblPt.Text = "Mã bệnh nhân mẫu đang thực hiện (nếu có):";
            lblPt.Font = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            lblPt.ForeColor = Color.FromArgb(33, 37, 41);
            lblPt.Location = new Point(25, 115);
            lblPt.AutoSize = true;
            gbSetup.Controls.Add(lblPt);

            txtPatientCode = new TextBox();
            txtPatientCode.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            txtPatientCode.Location = new Point(25, 142);
            txtPatientCode.Size = new Size(350, 30);
            txtPatientCode.Text = "";
            gbSetup.Controls.Add(txtPatientCode);

            // Field: Initial Note
            Label lblNote = new Label();
            lblNote.Text = "Ghi chú ban đầu (Ví dụ: Thao tác chọn BS Hà Đức Cường làm Chủ tọa, BS Sâm làm Thư ký...):";
            lblNote.Font = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            lblNote.ForeColor = Color.FromArgb(33, 37, 41);
            lblNote.Location = new Point(25, 190);
            lblNote.AutoSize = true;
            gbSetup.Controls.Add(lblNote);

            txtInitialNote = new TextBox();
            txtInitialNote.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            txtInitialNote.Location = new Point(25, 218);
            txtInitialNote.Size = new Size(925, 60);
            txtInitialNote.Multiline = true;
            txtInitialNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtInitialNote.Text = "Ghi lại quy trình lập biên bản PT-01 và gửi lời mời ký số EMR trên giao diện HIS.";
            gbSetup.Controls.Add(txtInitialNote);

            // Checkbox auto float
            chkAutoFloat = new CheckBox();
            chkAutoFloat.Text = "Tự động thu nhỏ thành Thanh Nổi góc màn hình khi bắt đầu ghi (luôn hiện nút 'Tắt Ghi' trên mặt HIS)";
            chkAutoFloat.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            chkAutoFloat.ForeColor = Color.FromArgb(13, 110, 253);
            chkAutoFloat.Location = new Point(25, 300);
            chkAutoFloat.AutoSize = true;
            chkAutoFloat.Checked = true;
            gbSetup.Controls.Add(chkAutoFloat);

            panelSetup.Controls.Add(gbSetup);

            // Button Start
            btnStartRecord = new Button();
            btnStartRecord.Text = "▶️ BẮT ĐẦU GHI THAO TÁC (F9)";
            btnStartRecord.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnStartRecord.BackColor = Color.FromArgb(40, 167, 69);
            btnStartRecord.ForeColor = Color.White;
            btnStartRecord.FlatStyle = FlatStyle.Flat;
            btnStartRecord.FlatAppearance.BorderSize = 0;
            btnStartRecord.Size = new Size(380, 55);
            btnStartRecord.Location = new Point(30, 510);
            btnStartRecord.Cursor = Cursors.Hand;
            btnStartRecord.Click += BtnStartRecord_Click;
            panelSetup.Controls.Add(btnStartRecord);

            // Button Exit Application
            btnExitSetup = new Button();
            btnExitSetup.Text = "❌ THOÁT ỨNG DỤNG";
            btnExitSetup.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnExitSetup.BackColor = Color.FromArgb(220, 53, 69);
            btnExitSetup.ForeColor = Color.White;
            btnExitSetup.FlatStyle = FlatStyle.Flat;
            btnExitSetup.FlatAppearance.BorderSize = 0;
            btnExitSetup.Size = new Size(220, 55);
            btnExitSetup.Location = new Point(430, 510);
            btnExitSetup.Cursor = Cursors.Hand;
            btnExitSetup.Click += (s, e) => this.Close();
            panelSetup.Controls.Add(btnExitSetup);

            Label lblF9Tip = new Label();
            lblF9Tip.Text = "💡 Phím tắt F9 hoạt động toàn cục: Bác sĩ có thể bấm F9 bất kỳ lúc nào để BẮT ĐẦU hoặc DỪNG GHI.";
            lblF9Tip.Font = new Font("Segoe UI", 9.5f, FontStyle.Italic);
            lblF9Tip.ForeColor = Color.FromArgb(108, 117, 125);
            lblF9Tip.Location = new Point(30, 580);
            lblF9Tip.AutoSize = true;
            panelSetup.Controls.Add(lblF9Tip);

            this.Controls.Add(panelSetup);
        }

        private void BuildPanelRecording()
        {
            panelRecording = new Panel();
            panelRecording.Dock = DockStyle.Fill;
            panelRecording.BackColor = Color.FromArgb(245, 247, 250);
            panelRecording.Padding = new Padding(25);

            // Header Bar
            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 85;
            topBar.BackColor = Color.FromArgb(255, 243, 205);
            topBar.BorderStyle = BorderStyle.FixedSingle;
            topBar.Padding = new Padding(15);

            lblRecStatus = new Label();
            lblRecStatus.Text = "🔴 ĐANG GHI NHẬN THAO TÁC TRÊN HIS...";
            lblRecStatus.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblRecStatus.ForeColor = Color.FromArgb(180, 40, 40);
            lblRecStatus.AutoSize = true;
            lblRecStatus.Location = new Point(15, 12);
            topBar.Controls.Add(lblRecStatus);

            lblRecTimer = new Label();
            lblRecTimer.Text = "⏱️ Thời gian: 00:00";
            lblRecTimer.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblRecTimer.ForeColor = Color.FromArgb(33, 37, 41);
            lblRecTimer.AutoSize = true;
            lblRecTimer.Location = new Point(18, 45);
            topBar.Controls.Add(lblRecTimer);

            lblRecCount = new Label();
            lblRecCount.Text = "📊 Đã bắt: 0 sự kiện";
            lblRecCount.Font = new Font("Segoe UI", 11f, FontStyle.Regular);
            lblRecCount.ForeColor = Color.FromArgb(70, 80, 95);
            lblRecCount.AutoSize = true;
            lblRecCount.Location = new Point(230, 45);
            topBar.Controls.Add(lblRecCount);

            // Nút Tắt Ghi & Đối Soát (Big Red)
            btnStopRecord = new Button();
            btnStopRecord.Text = "⏹️ TẮT GHI & ĐỐI SOÁT (F9)";
            btnStopRecord.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnStopRecord.BackColor = Color.FromArgb(220, 53, 69);
            btnStopRecord.ForeColor = Color.White;
            btnStopRecord.FlatStyle = FlatStyle.Flat;
            btnStopRecord.FlatAppearance.BorderSize = 0;
            btnStopRecord.Size = new Size(260, 55);
            btnStopRecord.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStopRecord.Location = new Point(540, 15);
            btnStopRecord.Cursor = Cursors.Hand;
            btnStopRecord.Click += BtnStopRecord_Click;
            topBar.Controls.Add(btnStopRecord);

            // Nút Thu Nhỏ Thành Thanh Nổi
            btnFloatRecord = new Button();
            btnFloatRecord.Text = "📌 Thu Nhỏ";
            btnFloatRecord.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnFloatRecord.BackColor = Color.FromArgb(13, 110, 253);
            btnFloatRecord.ForeColor = Color.White;
            btnFloatRecord.FlatStyle = FlatStyle.Flat;
            btnFloatRecord.FlatAppearance.BorderSize = 0;
            btnFloatRecord.Size = new Size(110, 55);
            btnFloatRecord.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFloatRecord.Location = new Point(810, 15);
            btnFloatRecord.Cursor = Cursors.Hand;
            btnFloatRecord.Click += (s, e) => SwitchToFloatingMode(true);
            topBar.Controls.Add(btnFloatRecord);

            // Nút Tắt Ứng Dụng Ngay
            btnExitRecord = new Button();
            btnExitRecord.Text = "❌ Thoát";
            btnExitRecord.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnExitRecord.BackColor = Color.FromArgb(108, 117, 125);
            btnExitRecord.ForeColor = Color.White;
            btnExitRecord.FlatStyle = FlatStyle.Flat;
            btnExitRecord.FlatAppearance.BorderSize = 0;
            btnExitRecord.Size = new Size(80, 55);
            btnExitRecord.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExitRecord.Location = new Point(930, 15);
            btnExitRecord.Cursor = Cursors.Hand;
            btnExitRecord.Click += (s, e) => {
                if (MessageBox.Show("Bác sĩ có muốn dừng ghi và thoát ứng dụng?", "Thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.Close();
                }
            };
            topBar.Controls.Add(btnExitRecord);

            panelRecording.Controls.Add(topBar);

            // Instructions
            Label lblLiveTitle = new Label();
            lblLiveTitle.Text = "Dòng sự kiện trực tiếp (Live Feed) - Thao tác chuột, phím & API phát hiện:";
            lblLiveTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblLiveTitle.ForeColor = Color.FromArgb(50, 60, 70);
            lblLiveTitle.Location = new Point(25, 120);
            lblLiveTitle.AutoSize = true;
            panelRecording.Controls.Add(lblLiveTitle);

            // ListView Live Feed
            lvLiveFeed = new ListView();
            lvLiveFeed.View = View.Details;
            lvLiveFeed.FullRowSelect = true;
            lvLiveFeed.GridLines = true;
            lvLiveFeed.Font = new Font("Consolas", 9.5f, FontStyle.Regular);
            lvLiveFeed.Location = new Point(25, 145);
            lvLiveFeed.Size = new Size(990, 470);
            lvLiveFeed.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            lvLiveFeed.Columns.Add("Thời gian", 100);
            lvLiveFeed.Columns.Add("Loại", 120);
            lvLiveFeed.Columns.Add("Chi tiết sự kiện / API", 420);
            lvLiveFeed.Columns.Add("Cửa sổ đang làm việc", 320);

            panelRecording.Controls.Add(lvLiveFeed);

            // Bottom bar Cancel
            btnCancelRecord = new Button();
            btnCancelRecord.Text = "❌ Hủy Phiên Ghi (Không Lưu)";
            btnCancelRecord.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            btnCancelRecord.BackColor = Color.FromArgb(108, 117, 125);
            btnCancelRecord.ForeColor = Color.White;
            btnCancelRecord.FlatStyle = FlatStyle.Flat;
            btnCancelRecord.FlatAppearance.BorderSize = 0;
            btnCancelRecord.Size = new Size(220, 35);
            btnCancelRecord.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancelRecord.Location = new Point(795, 625);
            btnCancelRecord.Cursor = Cursors.Hand;
            btnCancelRecord.Click += (s, e) => {
                if (MessageBox.Show("Bác sĩ có chắc muốn hủy phiên ghi hiện tại?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    StopRecording(false);
                    ShowPanel(panelSetup);
                }
            };
            panelRecording.Controls.Add(btnCancelRecord);

            this.Controls.Add(panelRecording);
        }

        private void BuildPanelFloating()
        {
            panelFloating = new Panel();
            panelFloating.Dock = DockStyle.Fill;
            panelFloating.BackColor = Color.FromArgb(33, 37, 41);
            panelFloating.Padding = new Padding(10);

            lblFloatStatus = new Label();
            lblFloatStatus.Text = "🔴 ĐANG GHI [00:00]";
            lblFloatStatus.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblFloatStatus.ForeColor = Color.FromArgb(255, 193, 7);
            lblFloatStatus.AutoSize = true;
            lblFloatStatus.Location = new Point(12, 18);
            panelFloating.Controls.Add(lblFloatStatus);

            btnFloatStop = new Button();
            btnFloatStop.Text = "⏹️ TẮT GHI (F9)";
            btnFloatStop.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnFloatStop.BackColor = Color.FromArgb(220, 53, 69);
            btnFloatStop.ForeColor = Color.White;
            btnFloatStop.FlatStyle = FlatStyle.Flat;
            btnFloatStop.FlatAppearance.BorderSize = 0;
            btnFloatStop.Size = new Size(180, 42);
            btnFloatStop.Location = new Point(220, 10);
            btnFloatStop.Cursor = Cursors.Hand;
            btnFloatStop.Click += BtnStopRecord_Click;
            panelFloating.Controls.Add(btnFloatStop);

            btnFloatExpand = new Button();
            btnFloatExpand.Text = "🔍 Mở Rộng";
            btnFloatExpand.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnFloatExpand.BackColor = Color.FromArgb(13, 110, 253);
            btnFloatExpand.ForeColor = Color.White;
            btnFloatExpand.FlatStyle = FlatStyle.Flat;
            btnFloatExpand.FlatAppearance.BorderSize = 0;
            btnFloatExpand.Size = new Size(100, 42);
            btnFloatExpand.Location = new Point(410, 10);
            btnFloatExpand.Cursor = Cursors.Hand;
            btnFloatExpand.Click += (s, e) => SwitchToFloatingMode(false);
            panelFloating.Controls.Add(btnFloatExpand);

            btnFloatExit = new Button();
            btnFloatExit.Text = "❌ Thoát";
            btnFloatExit.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnFloatExit.BackColor = Color.FromArgb(108, 117, 125);
            btnFloatExit.ForeColor = Color.White;
            btnFloatExit.FlatStyle = FlatStyle.Flat;
            btnFloatExit.FlatAppearance.BorderSize = 0;
            btnFloatExit.Size = new Size(80, 42);
            btnFloatExit.Location = new Point(520, 10);
            btnFloatExit.Cursor = Cursors.Hand;
            btnFloatExit.Click += (s, e) => {
                if (MessageBox.Show("Bác sĩ có muốn dừng ghi và thoát ứng dụng?", "Thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.Close();
                }
            };
            panelFloating.Controls.Add(btnFloatExit);

            this.Controls.Add(panelFloating);
        }

        private void BuildPanelReview()
        {
            panelReview = new Panel();
            panelReview.Dock = DockStyle.Fill;
            panelReview.BackColor = Color.FromArgb(248, 249, 250);
            panelReview.Padding = new Padding(25);

            // Title
            lblReviewTitle = new Label();
            lblReviewTitle.Text = "📋 BẢNG ĐỐI SOÁT & CHÚ THÍCH CÁC BƯỚC THAO TÁC";
            lblReviewTitle.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            lblReviewTitle.ForeColor = Color.FromArgb(24, 43, 73);
            lblReviewTitle.AutoSize = true;
            lblReviewTitle.Location = new Point(25, 20);
            panelReview.Controls.Add(lblReviewTitle);

            lblReviewSummary = new Label();
            lblReviewSummary.Text = "Mục đích: ... | Bác sĩ hãy nhấp vào ô 'Bác sĩ ghi chú' để nhập giải thích mục đích của từng bước.";
            lblReviewSummary.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            lblReviewSummary.ForeColor = Color.FromArgb(13, 110, 253);
            lblReviewSummary.AutoSize = true;
            lblReviewSummary.Location = new Point(27, 55);
            panelReview.Controls.Add(lblReviewSummary);

            // Top-right close button in Review
            Button btnTopCloseReview = new Button();
            btnTopCloseReview.Text = "❌ Đóng Ứng Dụng";
            btnTopCloseReview.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnTopCloseReview.BackColor = Color.FromArgb(220, 53, 69);
            btnTopCloseReview.ForeColor = Color.White;
            btnTopCloseReview.FlatStyle = FlatStyle.Flat;
            btnTopCloseReview.FlatAppearance.BorderSize = 0;
            btnTopCloseReview.Size = new Size(160, 40);
            btnTopCloseReview.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTopCloseReview.Location = new Point(860, 20);
            btnTopCloseReview.Cursor = Cursors.Hand;
            btnTopCloseReview.Click += (s, e) => this.Close();
            panelReview.Controls.Add(btnTopCloseReview);

            // DataGridView
            dgvReview = new DataGridView();
            dgvReview.Location = new Point(25, 90);
            dgvReview.Size = new Size(995, 520);
            dgvReview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvReview.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            dgvReview.BackgroundColor = Color.White;
            dgvReview.RowHeadersVisible = false;
            dgvReview.AllowUserToAddRows = false;
            dgvReview.AllowUserToDeleteRows = true;
            dgvReview.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvReview.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvReview.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);

            // Define Columns
            DataGridViewTextBoxColumn colId = new DataGridViewTextBoxColumn();
            colId.HeaderText = "STT";
            colId.Width = 50;
            colId.ReadOnly = true;
            colId.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvReview.Columns.Add(colId);

            DataGridViewTextBoxColumn colTime = new DataGridViewTextBoxColumn();
            colTime.HeaderText = "Thời gian";
            colTime.Width = 110;
            colTime.ReadOnly = true;
            dgvReview.Columns.Add(colTime);

            DataGridViewTextBoxColumn colType = new DataGridViewTextBoxColumn();
            colType.HeaderText = "Hành động";
            colType.Width = 130;
            colType.ReadOnly = true;
            dgvReview.Columns.Add(colType);

            DataGridViewTextBoxColumn colDetail = new DataGridViewTextBoxColumn();
            colDetail.HeaderText = "Chi tiết kỹ thuật (API / Tọa độ click)";
            colDetail.Width = 260;
            colDetail.ReadOnly = true;
            dgvReview.Columns.Add(colDetail);

            DataGridViewTextBoxColumn colWin = new DataGridViewTextBoxColumn();
            colWin.HeaderText = "Cửa sổ HIS";
            colWin.Width = 180;
            colWin.ReadOnly = true;
            dgvReview.Columns.Add(colWin);

            DataGridViewTextBoxColumn colNote = new DataGridViewTextBoxColumn();
            colNote.HeaderText = "✍️ Bác sĩ ghi chú (Bước này để làm gì?)";
            colNote.Width = 260;
            colNote.ReadOnly = false;
            colNote.DefaultCellStyle.ForeColor = Color.FromArgb(13, 110, 253);
            colNote.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvReview.Columns.Add(colNote);

            panelReview.Controls.Add(dgvReview);

            // Bottom Buttons
            btnSaveExport = new Button();
            btnSaveExport.Text = "💾 LƯU BÁO CÁO & XUẤT CHO AI";
            btnSaveExport.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSaveExport.BackColor = Color.FromArgb(13, 110, 253);
            btnSaveExport.ForeColor = Color.White;
            btnSaveExport.FlatStyle = FlatStyle.Flat;
            btnSaveExport.FlatAppearance.BorderSize = 0;
            btnSaveExport.Size = new Size(240, 45);
            btnSaveExport.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSaveExport.Location = new Point(25, 620);
            btnSaveExport.Cursor = Cursors.Hand;
            btnSaveExport.Click += BtnSaveExport_Click;
            panelReview.Controls.Add(btnSaveExport);

            btnCopyClipboard = new Button();
            btnCopyClipboard.Text = "📋 COPY GỬI CHAT";
            btnCopyClipboard.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnCopyClipboard.BackColor = Color.FromArgb(40, 167, 69);
            btnCopyClipboard.ForeColor = Color.White;
            btnCopyClipboard.FlatStyle = FlatStyle.Flat;
            btnCopyClipboard.FlatAppearance.BorderSize = 0;
            btnCopyClipboard.Size = new Size(160, 45);
            btnCopyClipboard.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCopyClipboard.Location = new Point(275, 620);
            btnCopyClipboard.Cursor = Cursors.Hand;
            btnCopyClipboard.Click += BtnCopyClipboard_Click;
            panelReview.Controls.Add(btnCopyClipboard);

            btnRecordAgain = new Button();
            btnRecordAgain.Text = "🔄 Ghi Mục Khác";
            btnRecordAgain.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            btnRecordAgain.BackColor = Color.FromArgb(108, 117, 125);
            btnRecordAgain.ForeColor = Color.White;
            btnRecordAgain.FlatStyle = FlatStyle.Flat;
            btnRecordAgain.FlatAppearance.BorderSize = 0;
            btnRecordAgain.Size = new Size(130, 45);
            btnRecordAgain.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnRecordAgain.Location = new Point(445, 620);
            btnRecordAgain.Cursor = Cursors.Hand;
            btnRecordAgain.Click += (s, e) => ShowPanel(panelSetup);
            panelReview.Controls.Add(btnRecordAgain);

            btnOpenLogs = new Button();
            btnOpenLogs.Text = "📂 Mở Logs";
            btnOpenLogs.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            btnOpenLogs.BackColor = Color.White;
            btnOpenLogs.ForeColor = Color.FromArgb(33, 37, 41);
            btnOpenLogs.FlatStyle = FlatStyle.Flat;
            btnOpenLogs.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
            btnOpenLogs.Size = new Size(110, 45);
            btnOpenLogs.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnOpenLogs.Location = new Point(585, 620);
            btnOpenLogs.Cursor = Cursors.Hand;
            btnOpenLogs.Click += (s, e) => {
                string logDir = Path.GetDirectoryName(logFilePath);
                if (Directory.Exists(logDir)) Process.Start("explorer.exe", logDir);
            };
            panelReview.Controls.Add(btnOpenLogs);

            btnExitReview = new Button();
            btnExitReview.Text = "❌ THOÁT ỨNG DỤNG";
            btnExitReview.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnExitReview.BackColor = Color.FromArgb(220, 53, 69);
            btnExitReview.ForeColor = Color.White;
            btnExitReview.FlatStyle = FlatStyle.Flat;
            btnExitReview.FlatAppearance.BorderSize = 0;
            btnExitReview.Size = new Size(180, 45);
            btnExitReview.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnExitReview.Location = new Point(840, 620);
            btnExitReview.Cursor = Cursors.Hand;
            btnExitReview.Click += (s, e) => this.Close();
            panelReview.Controls.Add(btnExitReview);

            this.Controls.Add(panelReview);
        }

        private void ShowPanel(Panel target)
        {
            panelSetup.Visible = (target == panelSetup);
            panelRecording.Visible = (target == panelRecording);
            panelFloating.Visible = (target == panelFloating);
            panelReview.Visible = (target == panelReview);
        }

        private void SwitchToFloatingMode(bool floating)
        {
            isFloatingMode = floating;
            if (floating)
            {
                this.TopMost = true;
                this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                this.Size = new Size(625, 100);

                int screenW = Screen.PrimaryScreen.WorkingArea.Width;
                this.Location = new Point(screenW - 640, 25);

                ShowPanel(panelFloating);
            }
            else
            {
                this.TopMost = false;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Size = new Size(1060, 720);
                this.CenterToScreen();

                ShowPanel(panelRecording);
            }
        }

        private void SetupHooks()
        {
            try
            {
                keyboardProc = HookCallbackKeyboard;
                mouseProc = HookCallbackMouse;

                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    IntPtr modHandle = GetModuleHandle(curModule.ModuleName);
                    hookKeyboard = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, modHandle, 0);
                    hookMouse = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, modHandle, 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SetupHooks Error: " + ex.Message);
            }
        }

        private IntPtr HookCallbackKeyboard(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    // Global Hotkey F9
                    if (vkCode == VK_F9)
                    {
                        this.BeginInvoke(new Action(ToggleRecording));
                        return (IntPtr)1;
                    }

                    if (isRecording)
                    {
                        string winTitle = GetActiveWindowTitle();
                        if (!IsOwnWindow(winTitle))
                        {
                            Keys key = (Keys)vkCode;
                            if (key == Keys.Enter || key == Keys.Tab || key == Keys.Escape || key == Keys.Space ||
                                (key >= Keys.F1 && key <= Keys.F12) || key == Keys.Up || key == Keys.Down ||
                                key == Keys.Left || key == Keys.Right)
                            {
                                EnqueueEvent("⌨️ Gõ Phím", "[" + key.ToString() + "]", "", winTitle);
                            }
                        }
                    }
                }
            }
            return CallNextHookEx(hookKeyboard, nCode, wParam, lParam);
        }

        private IntPtr HookCallbackMouse(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && isRecording)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN)
                {
                    string winTitle = GetActiveWindowTitle();
                    if (!IsOwnWindow(winTitle))
                    {
                        MSLLHOOKSTRUCT mouseStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                        string btnType = (wParam == (IntPtr)WM_LBUTTONDOWN) ? "Click Trái" : "Click Phải";
                        string coord = string.Format("{0} ({1}, {2})", btnType, mouseStruct.pt.x, mouseStruct.pt.y);
                        EnqueueEvent("🖱️ Click Chuột", coord, "", winTitle);
                    }
                }
            }
            return CallNextHookEx(hookMouse, nCode, wParam, lParam);
        }

        private string GetActiveWindowTitle()
        {
            IntPtr handle = GetForegroundWindow();
            StringBuilder sb = new StringBuilder(256);
            if (GetWindowText(handle, sb, sb.Capacity) > 0)
            {
                return sb.ToString();
            }
            return "";
        }

        private bool IsOwnWindow(string title)
        {
            if (string.IsNullOrEmpty(title)) return false;
            return title.Contains("HIS Action Recorder");
        }

        private void EnqueueEvent(string type, string detail, string payload, string winTitle)
        {
            DateTime now = DateTime.Now;
            long waitMs = (lastEventTime == DateTime.MinValue) ? 0 : (long)(now - lastEventTime).TotalMilliseconds;
            lastEventTime = now;

            string delayStr = (waitMs > 0) ? string.Format("+{0:0.0}s", waitMs / 1000.0) : "0s";

            ActionItem item = new ActionItem()
            {
                Timestamp = now,
                TimeStr = now.ToString("HH:mm:ss"),
                DelayStr = delayStr,
                ActionType = type,
                Detail = detail,
                Payload = payload,
                WindowTitle = winTitle,
                UserNote = AutoSuggestNote(type, detail, winTitle)
            };

            pendingEvents.Enqueue(item);
        }

        private string AutoSuggestNote(string type, string detail, string winTitle)
        {
            if (type.Contains("API"))
            {
                if (detail.Contains("HisConsultation") || detail.Contains("Consultation")) return "Tạo / Cập nhật biên bản hội chẩn";
                if (detail.Contains("EmrSigner") || detail.Contains("Signer")) return "Mời ký số EMR / Phân quyền ký";
                if (detail.Contains("ServiceReq")) return "Tạo phiếu y lệnh / Chỉ định dịch vụ";
                if (detail.Contains("ExpMest")) return "Xuất dược / Tủ trực";
                if (detail.Contains("Tracking")) return "Tạo / Lưu tờ điều trị";
                if (detail.Contains("SarPrint")) return "In / Xem biểu mẫu";
            }
            else if (type.Contains("Click"))
            {
                if (winTitle.Contains("In ấn") || winTitle.Contains("Biểu mẫu")) return "Chọn biểu mẫu trong danh sách";
                if (winTitle.Contains("Hội chẩn")) return "Thao tác trên cửa sổ Hội chẩn";
                if (winTitle.Contains("Ký")) return "Bấm nút Mời ký";
            }
            return "";
        }

        private void ToggleRecording()
        {
            if (isRecording)
            {
                BtnStopRecord_Click(this, EventArgs.Empty);
            }
            else if (panelSetup.Visible)
            {
                BtnStartRecord_Click(this, EventArgs.Empty);
            }
        }

        private void BtnStartRecord_Click(object sender, EventArgs e)
        {
            recordedList.Clear();
            ActionItem dummy;
            while (pendingEvents.TryDequeue(out dummy)) { }
            lvLiveFeed.Items.Clear();

            isRecording = true;
            lastEventTime = DateTime.MinValue;
            lastToken = "";

            try
            {
                if (File.Exists(logFilePath))
                {
                    fsLog = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fsLog.Seek(0, SeekOrigin.End);
                    srLog = new StreamReader(fsLog, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Open LogSystem error: " + ex.Message);
            }

            swDuration.Reset();
            swDuration.Start();
            timerDuration.Start();
            timerUi.Start();

            if (chkAutoFloat.Checked)
            {
                SwitchToFloatingMode(true);
            }
            else
            {
                SwitchToFloatingMode(false);
            }
        }

        private void BtnStopRecord_Click(object sender, EventArgs e)
        {
            StopRecording(true);

            // Restore normal window
            this.TopMost = false;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Size = new Size(1060, 720);
            this.CenterToScreen();

            PopulateReviewGrid();
            ShowPanel(panelReview);

            this.BringToFront();
            SetForegroundWindow(this.Handle);
        }

        private void StopRecording(bool keepData)
        {
            isRecording = false;
            timerUi.Stop();
            timerDuration.Stop();
            swDuration.Stop();

            if (srLog != null) { srLog.Close(); srLog = null; }
            if (fsLog != null) { fsLog.Close(); fsLog = null; }

            if (keepData)
            {
                DrainPendingEvents();
            }
        }

        private void TimerDuration_Tick(object sender, EventArgs e)
        {
            TimeSpan ts = swDuration.Elapsed;
            string timeText = string.Format("{0:mm\\:ss}", ts);
            lblRecTimer.Text = "⏱️ Thời gian: " + timeText;
            lblFloatStatus.Text = "🔴 ĐANG GHI [" + timeText + "]";
        }

        private void TimerUi_Tick(object sender, EventArgs e)
        {
            DrainPendingEvents();
            PollLogSystem();

            int clickCount = 0;
            int apiCount = 0;
            int keyCount = 0;
            foreach (ActionItem it in recordedList)
            {
                if (it.ActionType.Contains("Click")) clickCount++;
                else if (it.ActionType.Contains("API")) apiCount++;
                else if (it.ActionType.Contains("Phím")) keyCount++;
            }
            lblRecCount.Text = string.Format("📊 Đã bắt: {0} sự kiện (Click: {1} | API: {2} | Phím: {3})",
                recordedList.Count, clickCount, apiCount, keyCount);
        }

        private void DrainPendingEvents()
        {
            ActionItem it;
            while (pendingEvents.TryDequeue(out it))
            {
                it.Id = recordedList.Count + 1;
                recordedList.Add(it);

                ListViewItem lvi = new ListViewItem(it.TimeStr + " (" + it.DelayStr + ")");
                lvi.SubItems.Add(it.ActionType);
                lvi.SubItems.Add(it.Detail);
                lvi.SubItems.Add(it.WindowTitle);

                if (it.ActionType.Contains("API")) lvi.ForeColor = Color.FromArgb(13, 110, 253);
                else if (it.ActionType.Contains("Click")) lvi.ForeColor = Color.FromArgb(40, 167, 69);
                else if (it.ActionType.Contains("Phím")) lvi.ForeColor = Color.FromArgb(108, 117, 125);

                lvLiveFeed.Items.Add(lvi);
                lvLiveFeed.EnsureVisible(lvLiveFeed.Items.Count - 1);
            }
        }

        private void PollLogSystem()
        {
            if (srLog == null) return;
            try
            {
                string line;
                int maxLinesPerTick = 40;
                int count = 0;

                while ((line = srLog.ReadLine()) != null && count++ < maxLinesPerTick)
                {
                    if (line.Contains("HIS.Desktop.Notify") || line.Contains("ProcessSyncToRAM") || line.Contains("ModuleControlDispose"))
                        continue;

                    string winTitle = GetActiveWindowTitle();

                    Match mApi = Regex.Match(line, @"WebApiClient\.(Post|Get)\.Begin.*?api:([^\s_]+)");
                    if (mApi.Success)
                    {
                        string method = mApi.Groups[1].Value.ToUpper();
                        string apiName = mApi.Groups[2].Value;
                        EnqueueEvent("🌐 Gọi API", method + " api:" + apiName, "", winTitle);
                        continue;
                    }

                    Match mPayload = Regex.Match(line, @"SerializeObject data api: (.*)");
                    if (mPayload.Success)
                    {
                        string json = mPayload.Groups[1].Value;
                        string preview = (json.Length > 120) ? json.Substring(0, 120) + "..." : json;
                        EnqueueEvent("📦 Dữ Liệu Gửi", preview, json, winTitle);
                        continue;
                    }

                    Match mToken = Regex.Match(line, @"TokenCode\|([a-fA-F0-9]{64})");
                    if (mToken.Success)
                    {
                        string t = mToken.Groups[1].Value;
                        if (t != lastToken)
                        {
                            lastToken = t;
                            EnqueueEvent("🔑 Token Mới", t.Substring(0, 16) + "...", t, winTitle);
                        }
                        continue;
                    }

                    if (line.Contains("UpdateWorkInfo") || line.Contains("WorkInfoSDO"))
                    {
                        EnqueueEvent("🏢 Cấu hình Khoa/Phòng", "Cập nhật phòng làm việc", line, winTitle);
                        continue;
                    }
                }
            }
            catch {}
        }

        private void PopulateReviewGrid()
        {
            dgvReview.Rows.Clear();
            string purpose = cboPurpose.Text;
            string ptCode = txtPatientCode.Text.Trim();

            lblReviewSummary.Text = string.Format("🎯 Mục đích: {0} {1} | ⏱️ Tổng thời gian: {2:mm\\:ss} | 📊 Tổng số thao tác: {3}",
                purpose,
                string.IsNullOrEmpty(ptCode) ? "" : "(BN: " + ptCode + ")",
                swDuration.Elapsed,
                recordedList.Count);

            int stt = 1;
            foreach (ActionItem it in recordedList)
            {
                if (it.ActionType.Contains("Dữ Liệu Gửi")) continue;

                int rowIdx = dgvReview.Rows.Add(
                    stt++,
                    it.TimeStr + " (" + it.DelayStr + ")",
                    it.ActionType,
                    it.Detail,
                    it.WindowTitle,
                    it.UserNote
                );

                DataGridViewRow row = dgvReview.Rows[rowIdx];
                row.Tag = it;

                if (it.ActionType.Contains("API"))
                {
                    row.Cells[2].Style.ForeColor = Color.FromArgb(13, 110, 253);
                    row.Cells[2].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (it.ActionType.Contains("Click"))
                {
                    row.Cells[2].Style.ForeColor = Color.FromArgb(40, 167, 69);
                }
            }
        }

        private void BtnSaveExport_Click(object sender, EventArgs e)
        {
            UpdateNotesFromGrid();

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string logsDir = Path.Combine(appDir, "Logs");
            if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);

            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string mdPath = Path.Combine(logsDir, string.Format("ActionRecord_{0}.md", ts));
            string latestMdPath = Path.Combine(logsDir, "ActionRecord_Latest.md");

            string mdContent = GenerateMarkdown();

            File.WriteAllText(mdPath, mdContent, Encoding.UTF8);
            File.WriteAllText(latestMdPath, mdContent, Encoding.UTF8);

            MessageBox.Show(
                "Đã lưu thành công báo cáo thao tác vào:\n" + mdPath + "\n\nBác sĩ có thể bấm nút 'Copy Gửi Chat' để gửi ngay cho AI Agent!",
                "Lưu Thành Công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void BtnCopyClipboard_Click(object sender, EventArgs e)
        {
            UpdateNotesFromGrid();
            string mdContent = GenerateMarkdown();
            Clipboard.SetText(mdContent);

            MessageBox.Show(
                "Đã sao chép toàn bộ bảng đối soát và ghi chú vào Clipboard!\n\n👉 Bác sĩ chỉ cần bấm Ctrl + V vào khung chat Antigravity để gửi cho Agent xử lý.",
                "Đã Copy",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateNotesFromGrid()
        {
            foreach (DataGridViewRow row in dgvReview.Rows)
            {
                ActionItem it = row.Tag as ActionItem;
                if (it != null && row.Cells[5].Value != null)
                {
                    it.UserNote = row.Cells[5].Value.ToString();
                }
            }
        }

        private string GenerateMarkdown()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 🎬 BÁO CÁO THAO TÁC LÂM SÀNG HIS (ACTION RECORDER)");
            sb.AppendLine(string.Format("- **Mục đích**: {0}", cboPurpose.Text));
            if (!string.IsNullOrEmpty(txtPatientCode.Text.Trim()))
            {
                sb.AppendLine(string.Format("- **Mã Bệnh Nhân**: `{0}`", txtPatientCode.Text.Trim()));
            }
            sb.AppendLine(string.Format("- **Thời gian ghi nhận**: {0} ({1:mm\\:ss})", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), swDuration.Elapsed));
            if (!string.IsNullOrEmpty(txtInitialNote.Text.Trim()))
            {
                sb.AppendLine(string.Format("- **Ghi chú ban đầu**: {0}", txtInitialNote.Text.Trim()));
            }
            sb.AppendLine();
            sb.AppendLine("## 📋 Bảng Chi Tiết Từng Bước & Chú Thích Của Bác Sĩ");
            sb.AppendLine();
            sb.AppendLine("| STT | Thời gian | Hành động | Chi tiết kỹ thuật / API | Cửa sổ HIS | ✍️ Bác sĩ ghi chú (Mục đích bước) |");
            sb.AppendLine("| :---: | :--- | :--- | :--- | :--- | :--- |");

            int stt = 1;
            foreach (DataGridViewRow row in dgvReview.Rows)
            {
                string time = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";
                string type = row.Cells[2].Value != null ? row.Cells[2].Value.ToString() : "";
                string detail = row.Cells[3].Value != null ? row.Cells[3].Value.ToString() : "";
                string win = row.Cells[4].Value != null ? row.Cells[4].Value.ToString() : "";
                string note = row.Cells[5].Value != null ? row.Cells[5].Value.ToString() : "";

                detail = detail.Replace("|", "\\|");
                win = win.Replace("|", "\\|");
                note = note.Replace("|", "\\|");

                sb.AppendLine(string.Format("| {0} | {1} | {2} | `{3}` | {4} | **{5}** |", stt++, time, type, detail, win, note));
            }

            sb.AppendLine();
            sb.AppendLine("## 📦 Chi Tiết Gói Tin API Payload (Nếu có)");
            int payloadIdx = 1;
            foreach (ActionItem it in recordedList)
            {
                if (!string.IsNullOrEmpty(it.Payload) && it.ActionType.Contains("Dữ Liệu Gửi"))
                {
                    sb.AppendLine(string.Format("### Gói tin {0} ({1})", payloadIdx++, it.TimeStr));
                    sb.AppendLine("```json");
                    sb.AppendLine(it.Payload);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopRecording(false);
            if (hookKeyboard != IntPtr.Zero) UnhookWindowsHookEx(hookKeyboard);
            if (hookMouse != IntPtr.Zero) UnhookWindowsHookEx(hookMouse);
            base.OnFormClosing(e);
        }
    }
}
