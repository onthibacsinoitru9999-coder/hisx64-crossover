using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
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
        public string ControlName { get; set; }
        public string ControlType { get; set; }
        public string ControlId { get; set; }
        public string ClassName { get; set; }
        public string WindowTitle { get; set; }
        public string ProcessName { get; set; }
        public string WebUrl { get; set; }
        public string ScreenshotPath { get; set; }
        public string UserNote { get; set; }
        public string Payload { get; set; }
        public int ClickX { get; set; }
        public int ClickY { get; set; }
    }

    internal struct RawClickEvent
    {
        public int X;
        public int Y;
        public string ButtonType;
        public DateTime Time;
        public string WindowTitle;
        public IntPtr WindowHandle;
    }

    public class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        public static void Main()
        {
            try
            {
                SetProcessDPIAware();
            }
            catch {}

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
        private CheckBox chkCaptureScreenshots;
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
        private Button btnOpenScreenshots;
        private Button btnRecordAgain;
        private Button btnOpenLogs;
        private Button btnExitReview;

        // Timers & State
        private System.Windows.Forms.Timer timerUi;
        private System.Windows.Forms.Timer timerDuration;
        private Stopwatch swDuration;
        private DateTime lastEventTime;
        private bool isRecording = false;
        private bool isFloatingMode = false;
        private int stepCounter = 0;

        // LogSystem Stream Reader
        private string logFilePath = "";
        private FileStream fsLog;
        private StreamReader srLog;
        private string lastToken = "";

        // Background worker for heavy events (Screenshots & UI Automation)
        private ConcurrentQueue<RawClickEvent> pendingClicks = new ConcurrentQueue<RawClickEvent>();
        private ConcurrentQueue<ActionItem> pendingEvents = new ConcurrentQueue<ActionItem>();
        private List<ActionItem> recordedList = new List<ActionItem>();
        private Thread clickWorkerThread;
        private bool isWorkerRunning = false;

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

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public MainForm()
        {
            InitializeComponent();
            LocateLogFile();
            StartClickWorker();
            SetupHooks();
        }

        private void LocateLogFile()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates = new string[]
            {
                Path.Combine(appDir, "Logs\\LogSystem.txt"),
                "D:\\his-x64-28-11fix GDYK\\his-x64\\Logs\\LogSystem.txt",
                "E:\\his-x64-28-11fix GDYK\\his-x64\\Logs\\LogSystem.txt",
                "D:\\New folder\\his-x64-28-11fix GDYK\\his-x64\\Logs\\LogSystem.txt",
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
            this.Text = "HIS Action Recorder v3.5 - Ghi Nhận & Học Thao Tác UI (Web & Win)";
            this.Size = new Size(1120, 760);
            this.MinimumSize = new Size(500, 70);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.Icon = SystemIcons.Application;
            this.ControlBox = true;
            this.MinimizeBox = true;
            this.MaximizeBox = true;

            // Timer UI
            timerUi = new System.Windows.Forms.Timer();
            timerUi.Interval = 100;
            timerUi.Tick += TimerUi_Tick;

            // Timer Duration
            timerDuration = new System.Windows.Forms.Timer();
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
            lblBanner.Text = "🎬 HỆ THỐNG GHI & HỌC THAO TÁC UI (WEB & WIN) v3.5";
            lblBanner.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblBanner.ForeColor = Color.FromArgb(24, 43, 73);
            lblBanner.AutoSize = true;
            lblBanner.Location = new Point(30, 20);
            panelSetup.Controls.Add(lblBanner);

            Label lblDesc = new Label();
            lblDesc.Text = "Tự động lắng nghe thao tác Click chuột, gõ phím, chụp ảnh màn hình tại điểm click,\nnhận diện phần tử UI (UI Automation trên Win/Web) và bắt gói tin API HIS.\nBác sĩ thực hiện mẫu thao tác, hệ thống sẽ xuất báo cáo chi tiết kèm ảnh để AI học thành kỹ năng!";
            lblDesc.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            lblDesc.ForeColor = Color.FromArgb(80, 90, 105);
            lblDesc.AutoSize = true;
            lblDesc.Location = new Point(32, 60);
            panelSetup.Controls.Add(lblDesc);

            GroupBox gbSetup = new GroupBox();
            gbSetup.Text = " 1. Khai báo mục đích thao tác trước khi ghi ";
            gbSetup.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            gbSetup.ForeColor = Color.FromArgb(13, 110, 253);
            gbSetup.Location = new Point(30, 130);
            gbSetup.Size = new Size(1040, 390);
            gbSetup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Field: Purpose
            Label lblP = new Label();
            lblP.Text = "Mục đích thao tác muốn ghi nhận (*):";
            lblP.Font = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            lblP.ForeColor = Color.FromArgb(33, 37, 41);
            lblP.Location = new Point(25, 35);
            lblP.AutoSize = true;
            gbSetup.Controls.Add(lblP);

            cboPurpose = new ComboBox();
            cboPurpose.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            cboPurpose.Location = new Point(25, 60);
            cboPurpose.Size = new Size(985, 30);
            cboPurpose.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cboPurpose.Items.AddRange(new object[] {
                "Trình ký & duyệt biên bản PT-01 (Thông qua mổ) trên Web / Win",
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
            lblPt.Location = new Point(25, 105);
            lblPt.AutoSize = true;
            gbSetup.Controls.Add(lblPt);

            txtPatientCode = new TextBox();
            txtPatientCode.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            txtPatientCode.Location = new Point(25, 130);
            txtPatientCode.Size = new Size(380, 30);
            txtPatientCode.Text = "";
            gbSetup.Controls.Add(txtPatientCode);

            // Field: Initial Note
            Label lblNote = new Label();
            lblNote.Text = "Ghi chú ban đầu (Ví dụ: Thao tác trình ký PT-01 trên Web EMR / phần mềm HIS...):";
            lblNote.Font = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            lblNote.ForeColor = Color.FromArgb(33, 37, 41);
            lblNote.Location = new Point(25, 175);
            lblNote.AutoSize = true;
            gbSetup.Controls.Add(lblNote);

            txtInitialNote = new TextBox();
            txtInitialNote.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            txtInitialNote.Location = new Point(25, 202);
            txtInitialNote.Size = new Size(985, 65);
            txtInitialNote.Multiline = true;
            txtInitialNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtInitialNote.Text = "Làm mẫu thao tác quy trình trình biên bản PT-01 để AI học thành kỹ năng tự động.";
            gbSetup.Controls.Add(txtInitialNote);

            // Checkbox capture screenshots
            chkCaptureScreenshots = new CheckBox();
            chkCaptureScreenshots.Text = "📸 Tự động chụp ảnh màn hình tại mỗi cú Click chuột (đánh dấu chấm đỏ vị trí click)";
            chkCaptureScreenshots.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            chkCaptureScreenshots.ForeColor = Color.FromArgb(40, 167, 69);
            chkCaptureScreenshots.Location = new Point(25, 285);
            chkCaptureScreenshots.AutoSize = true;
            chkCaptureScreenshots.Checked = true;
            gbSetup.Controls.Add(chkCaptureScreenshots);

            // Checkbox auto float
            chkAutoFloat = new CheckBox();
            chkAutoFloat.Text = "📌 Tự động thu nhỏ thành Thanh Nổi góc màn hình khi bắt đầu ghi (luôn hiện nút 'Tắt Ghi' trên mặt HIS/Web)";
            chkAutoFloat.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            chkAutoFloat.ForeColor = Color.FromArgb(13, 110, 253);
            chkAutoFloat.Location = new Point(25, 325);
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
            btnStartRecord.Size = new Size(400, 55);
            btnStartRecord.Location = new Point(30, 545);
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
            btnExitSetup.Location = new Point(450, 545);
            btnExitSetup.Cursor = Cursors.Hand;
            btnExitSetup.Click += (s, e) => this.Close();
            panelSetup.Controls.Add(btnExitSetup);

            Label lblF9Tip = new Label();
            lblF9Tip.Text = "💡 Phím tắt F9 hoạt động toàn cục: Bác sĩ có thể bấm F9 bất kỳ lúc nào để BẮT ĐẦU hoặc DỪNG GHI.";
            lblF9Tip.Font = new Font("Segoe UI", 9.5f, FontStyle.Italic);
            lblF9Tip.ForeColor = Color.FromArgb(108, 117, 125);
            lblF9Tip.Location = new Point(30, 615);
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
            lblRecStatus.Text = "🔴 ĐANG GHI NHẬN THAO TÁC TRÊN WEB / WIN...";
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
            btnStopRecord.Location = new Point(600, 15);
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
            btnFloatRecord.Location = new Point(870, 15);
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
            btnExitRecord.Location = new Point(990, 15);
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
            lblLiveTitle.Text = "Dòng sự kiện trực tiếp (Live Feed) - Thao tác chuột, phím, UI Elements & API phát hiện:";
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
            lvLiveFeed.Size = new Size(1050, 500);
            lvLiveFeed.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            lvLiveFeed.Columns.Add("Thời gian", 100);
            lvLiveFeed.Columns.Add("Loại", 120);
            lvLiveFeed.Columns.Add("Phần tử / Chi tiết sự kiện / API", 480);
            lvLiveFeed.Columns.Add("Cửa sổ / Web đang làm việc", 340);

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
            btnCancelRecord.Location = new Point(855, 660);
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

            // BOTTOM BAR — Dock=Bottom
            Panel bottomBar = new Panel();
            bottomBar.Dock = DockStyle.Bottom;
            bottomBar.Height = 70;
            bottomBar.BackColor = Color.FromArgb(33, 37, 41);

            btnSaveExport = new Button();
            btnSaveExport.Text = "💾 LƯU BÁO CÁO";
            btnSaveExport.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnSaveExport.BackColor = Color.FromArgb(13, 110, 253);
            btnSaveExport.ForeColor = Color.White;
            btnSaveExport.FlatStyle = FlatStyle.Flat;
            btnSaveExport.FlatAppearance.BorderSize = 0;
            btnSaveExport.Size = new Size(180, 48);
            btnSaveExport.Location = new Point(10, 11);
            btnSaveExport.Cursor = Cursors.Hand;
            btnSaveExport.Click += BtnSaveExport_Click;
            bottomBar.Controls.Add(btnSaveExport);

            btnCopyClipboard = new Button();
            btnCopyClipboard.Text = "📋 COPY GỬI CHAT";
            btnCopyClipboard.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnCopyClipboard.BackColor = Color.FromArgb(40, 167, 69);
            btnCopyClipboard.ForeColor = Color.White;
            btnCopyClipboard.FlatStyle = FlatStyle.Flat;
            btnCopyClipboard.FlatAppearance.BorderSize = 0;
            btnCopyClipboard.Size = new Size(185, 48);
            btnCopyClipboard.Location = new Point(200, 11);
            btnCopyClipboard.Cursor = Cursors.Hand;
            btnCopyClipboard.Click += BtnCopyClipboard_Click;
            bottomBar.Controls.Add(btnCopyClipboard);

            btnOpenScreenshots = new Button();
            btnOpenScreenshots.Text = "🖼️ Mở Thư Mục Ảnh";
            btnOpenScreenshots.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            btnOpenScreenshots.BackColor = Color.FromArgb(23, 162, 184);
            btnOpenScreenshots.ForeColor = Color.White;
            btnOpenScreenshots.FlatStyle = FlatStyle.Flat;
            btnOpenScreenshots.FlatAppearance.BorderSize = 0;
            btnOpenScreenshots.Size = new Size(160, 48);
            btnOpenScreenshots.Location = new Point(395, 11);
            btnOpenScreenshots.Cursor = Cursors.Hand;
            btnOpenScreenshots.Click += (s, e) => {
                string scDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs\\ActionScreenshots");
                if (Directory.Exists(scDir)) Process.Start("explorer.exe", scDir);
                else MessageBox.Show("Chưa có ảnh chụp nào được lưu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            bottomBar.Controls.Add(btnOpenScreenshots);

            btnRecordAgain = new Button();
            btnRecordAgain.Text = "🔄 Ghi Lại / Mục Khác";
            btnRecordAgain.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            btnRecordAgain.BackColor = Color.FromArgb(108, 117, 125);
            btnRecordAgain.ForeColor = Color.White;
            btnRecordAgain.FlatStyle = FlatStyle.Flat;
            btnRecordAgain.FlatAppearance.BorderSize = 0;
            btnRecordAgain.Size = new Size(160, 48);
            btnRecordAgain.Location = new Point(565, 11);
            btnRecordAgain.Cursor = Cursors.Hand;
            btnRecordAgain.Click += (s, e) => ShowPanel(panelSetup);
            bottomBar.Controls.Add(btnRecordAgain);

            btnOpenLogs = new Button();
            btnOpenLogs.Text = "📂 Thư Mục Logs";
            btnOpenLogs.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            btnOpenLogs.BackColor = Color.FromArgb(60, 70, 80);
            btnOpenLogs.ForeColor = Color.FromArgb(200, 210, 220);
            btnOpenLogs.FlatStyle = FlatStyle.Flat;
            btnOpenLogs.FlatAppearance.BorderSize = 0;
            btnOpenLogs.Size = new Size(150, 48);
            btnOpenLogs.Location = new Point(735, 11);
            btnOpenLogs.Cursor = Cursors.Hand;
            btnOpenLogs.Click += (s, e) => {
                string logDir = Path.GetDirectoryName(logFilePath);
                if (Directory.Exists(logDir)) Process.Start("explorer.exe", logDir);
            };
            bottomBar.Controls.Add(btnOpenLogs);

            btnExitReview = new Button();
            btnExitReview.Text = "❌ THOÁT";
            btnExitReview.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnExitReview.BackColor = Color.FromArgb(220, 53, 69);
            btnExitReview.ForeColor = Color.White;
            btnExitReview.FlatStyle = FlatStyle.Flat;
            btnExitReview.FlatAppearance.BorderSize = 0;
            btnExitReview.Size = new Size(140, 48);
            btnExitReview.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExitReview.Location = new Point(950, 11);
            btnExitReview.Cursor = Cursors.Hand;
            btnExitReview.Click += (s, e) => this.Close();
            bottomBar.Controls.Add(btnExitReview);

            panelReview.Controls.Add(bottomBar);

            // TOP HEADER — Dock=Top
            Panel topHeader = new Panel();
            topHeader.Dock = DockStyle.Top;
            topHeader.Height = 80;
            topHeader.BackColor = Color.FromArgb(13, 110, 253);

            lblReviewTitle = new Label();
            lblReviewTitle.Text = "📋 BẢNG ĐỐI SOÁT & CHÚ THÍCH THAO TÁC (KÈM ẢNH CHỤP)";
            lblReviewTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblReviewTitle.ForeColor = Color.White;
            lblReviewTitle.AutoSize = true;
            lblReviewTitle.Location = new Point(16, 8);
            topHeader.Controls.Add(lblReviewTitle);

            lblReviewSummary = new Label();
            lblReviewSummary.Text = "Nhấp đúp vào ô 'Ảnh chụp' để xem ảnh phóng to. Điền ghi chú mục đích từng bước rồi bấm LƯU hoặc COPY.";
            lblReviewSummary.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblReviewSummary.ForeColor = Color.FromArgb(210, 230, 255);
            lblReviewSummary.AutoSize = true;
            lblReviewSummary.Location = new Point(18, 44);
            topHeader.Controls.Add(lblReviewSummary);

            panelReview.Controls.Add(topHeader);

            // DataGridView — Dock=Fill
            dgvReview = new DataGridView();
            dgvReview.Dock = DockStyle.Fill;
            dgvReview.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            dgvReview.BackgroundColor = Color.White;
            dgvReview.RowHeadersVisible = false;
            dgvReview.AllowUserToAddRows = false;
            dgvReview.AllowUserToDeleteRows = true;
            dgvReview.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvReview.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            dgvReview.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            dgvReview.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 58, 64);
            dgvReview.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReview.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvReview.ColumnHeadersHeight = 36;
            dgvReview.EnableHeadersVisualStyles = false;
            dgvReview.CellDoubleClick += DgvReview_CellDoubleClick;

            DataGridViewTextBoxColumn colId = new DataGridViewTextBoxColumn();
            colId.HeaderText = "STT"; colId.Width = 45; colId.ReadOnly = true;
            colId.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvReview.Columns.Add(colId);

            DataGridViewTextBoxColumn colTime = new DataGridViewTextBoxColumn();
            colTime.HeaderText = "Thời gian"; colTime.Width = 105; colTime.ReadOnly = true;
            dgvReview.Columns.Add(colTime);

            DataGridViewTextBoxColumn colType = new DataGridViewTextBoxColumn();
            colType.HeaderText = "Hành động"; colType.Width = 115; colType.ReadOnly = true;
            dgvReview.Columns.Add(colType);

            DataGridViewTextBoxColumn colElement = new DataGridViewTextBoxColumn();
            colElement.HeaderText = "Phần tử UI / Kỹ thuật (API)"; colElement.Width = 260; colElement.ReadOnly = true;
            dgvReview.Columns.Add(colElement);

            DataGridViewTextBoxColumn colWin = new DataGridViewTextBoxColumn();
            colWin.HeaderText = "Cửa sổ / Trình duyệt"; colWin.Width = 180; colWin.ReadOnly = true;
            dgvReview.Columns.Add(colWin);

            DataGridViewTextBoxColumn colImg = new DataGridViewTextBoxColumn();
            colImg.HeaderText = "Ảnh chụp"; colImg.Width = 90; colImg.ReadOnly = true;
            colImg.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colImg.DefaultCellStyle.ForeColor = Color.FromArgb(13, 110, 253);
            dgvReview.Columns.Add(colImg);

            DataGridViewTextBoxColumn colNote = new DataGridViewTextBoxColumn();
            colNote.HeaderText = "✍️ Bác sĩ ghi chú (Mục đích bước này để làm gì?)";
            colNote.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colNote.MinimumWidth = 200; colNote.ReadOnly = false;
            colNote.DefaultCellStyle.ForeColor = Color.FromArgb(13, 110, 253);
            colNote.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvReview.Columns.Add(colNote);

            panelReview.Controls.Add(dgvReview);

            this.Controls.Add(panelReview);
        }

        private void DgvReview_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvReview.Rows[e.RowIndex];
            ActionItem it = row.Tag as ActionItem;
            if (it != null && !string.IsNullOrEmpty(it.ScreenshotPath) && File.Exists(it.ScreenshotPath))
            {
                try
                {
                    Process.Start(it.ScreenshotPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không mở được ảnh: " + ex.Message);
                }
            }
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
                this.Size = new Size(1120, 760);
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
                                EnqueueSimpleEvent("⌨️ Gõ Phím", "[" + key.ToString() + "]", "", winTitle);
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
                    IntPtr fg = GetForegroundWindow();
                    string winTitle = GetActiveWindowTitle(fg);
                    if (!IsOwnWindow(winTitle))
                    {
                        MSLLHOOKSTRUCT mouseStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                        string btnType = (wParam == (IntPtr)WM_LBUTTONDOWN) ? "Click Trái" : "Click Phải";

                        // Enqueue to background worker immediately (Microseconds execution)
                        pendingClicks.Enqueue(new RawClickEvent()
                        {
                            X = mouseStruct.pt.x,
                            Y = mouseStruct.pt.y,
                            ButtonType = btnType,
                            Time = DateTime.Now,
                            WindowTitle = winTitle,
                            WindowHandle = fg
                        });
                    }
                }
            }
            return CallNextHookEx(hookMouse, nCode, wParam, lParam);
        }

        private void StartClickWorker()
        {
            isWorkerRunning = true;
            clickWorkerThread = new Thread(ProcessClickEvents)
            {
                IsBackground = true,
                Name = "HisActionRecorder_ClickWorker"
            };
            clickWorkerThread.Start();
        }

        private void ProcessClickEvents()
        {
            while (isWorkerRunning)
            {
                RawClickEvent raw;
                if (pendingClicks.TryDequeue(out raw))
                {
                    try
                    {
                        int currentStep = Interlocked.Increment(ref stepCounter);
                        string screenshotPath = "";

                        if (chkCaptureScreenshots.Checked)
                        {
                            screenshotPath = CaptureScreenWithMarker(raw.X, raw.Y, currentStep);
                        }

                        // Extract UI Automation info & process
                        string controlName = "";
                        string controlType = "";
                        string controlId = "";
                        string className = "";
                        string procName = "";
                        string webUrl = "";

                        try
                        {
                            uint pid = 0;
                            GetWindowThreadProcessId(raw.WindowHandle, out pid);
                            if (pid > 0)
                            {
                                using (Process p = Process.GetProcessById((int)pid))
                                {
                                    procName = p.ProcessName;
                                }
                            }
                        }
                        catch {}

                        try
                        {
                            var wpfPt = new System.Windows.Point(raw.X, raw.Y);
                            AutomationElement el = AutomationElement.FromPoint(wpfPt);
                            if (el != null)
                            {
                                controlName = el.Current.Name ?? "";
                                controlType = el.Current.ControlType != null ? el.Current.ControlType.ProgrammaticName.Replace("ControlType.", "") : "";
                                controlId = el.Current.AutomationId ?? "";
                                className = el.Current.ClassName ?? "";

                                // Try to extract browser address if clicked on or inside browser
                                if (procName.Equals("chrome", StringComparison.OrdinalIgnoreCase) ||
                                    procName.Equals("msedge", StringComparison.OrdinalIgnoreCase))
                                {
                                    webUrl = TryExtractBrowserUrl(raw.WindowHandle);
                                }
                            }
                        }
                        catch {}

                        string detail;
                        if (!string.IsNullOrEmpty(controlName))
                        {
                            detail = string.Format("{0} vào '{1}' [{2}] ({3}, {4})", raw.ButtonType, controlName, controlType, raw.X, raw.Y);
                        }
                        else
                        {
                            detail = string.Format("{0} ({1}, {2})", raw.ButtonType, raw.X, raw.Y);
                        }

                        DateTime now = raw.Time;
                        long waitMs = (lastEventTime == DateTime.MinValue) ? 0 : (long)(now - lastEventTime).TotalMilliseconds;
                        lastEventTime = now;
                        string delayStr = (waitMs > 0) ? string.Format("+{0:0.0}s", waitMs / 1000.0) : "0s";

                        string autoNote = AutoSuggestNote("🖱️ Click Chuột", detail + " " + controlName, raw.WindowTitle);

                        ActionItem item = new ActionItem()
                        {
                            Timestamp = now,
                            TimeStr = now.ToString("HH:mm:ss"),
                            DelayStr = delayStr,
                            ActionType = "🖱️ Click Chuột",
                            Detail = detail,
                            ControlName = controlName,
                            ControlType = controlType,
                            ControlId = controlId,
                            ClassName = className,
                            WindowTitle = raw.WindowTitle,
                            ProcessName = procName,
                            WebUrl = webUrl,
                            ScreenshotPath = screenshotPath,
                            ClickX = raw.X,
                            ClickY = raw.Y,
                            UserNote = autoNote
                        };

                        pendingEvents.Enqueue(item);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ProcessClick error: " + ex.Message);
                    }
                }
                else
                {
                    Thread.Sleep(20);
                }
            }
        }

        private string CaptureScreenWithMarker(int clickX, int clickY, int stepIndex)
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

                        // Draw target marker (Red outer ring + White inner ring + Red center dot)
                        int radius = 20;
                        using (Pen penOuter = new Pen(Color.FromArgb(230, 220, 53, 69), 3))
                        using (Pen penInner = new Pen(Color.FromArgb(255, 255, 255, 255), 1.5f))
                        using (Brush brushDot = new SolidBrush(Color.FromArgb(240, 220, 53, 69)))
                        {
                            g.DrawEllipse(penOuter, clickX - radius, clickY - radius, radius * 2, radius * 2);
                            g.DrawEllipse(penInner, clickX - radius + 2, clickY - radius + 2, (radius - 2) * 2, (radius - 2) * 2);
                            g.FillEllipse(brushDot, clickX - 4, clickY - 4, 8, 8);
                        }
                    }

                    string appDir = AppDomain.CurrentDomain.BaseDirectory;
                    string dir = Path.Combine(appDir, "Logs\\ActionScreenshots");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string fileName = string.Format("Step_{0:D3}_{1}.png", stepIndex, DateTime.Now.ToString("HHmmss_fff"));
                    string fullPath = Path.Combine(dir, fileName);
                    bmp.Save(fullPath, ImageFormat.Png);
                    return fullPath;
                }
            }
            catch
            {
                return "";
            }
        }

        private string TryExtractBrowserUrl(IntPtr hWnd)
        {
            try
            {
                AutomationElement root = AutomationElement.FromHandle(hWnd);
                if (root == null) return "";

                var cond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
                var edits = root.FindAll(TreeScope.Descendants, cond);
                foreach (AutomationElement ed in edits)
                {
                    string nm = ed.Current.Name ?? "";
                    string id = ed.Current.AutomationId ?? "";
                    if (nm.Contains("Address") || nm.Contains("địa chỉ") || id.Contains("address") || id.Contains("url"))
                    {
                        object pattern;
                        if (ed.TryGetCurrentPattern(ValuePattern.Pattern, out pattern))
                        {
                            return ((ValuePattern)pattern).Current.Value;
                        }
                    }
                }
            }
            catch {}
            return "";
        }

        private string GetActiveWindowTitle()
        {
            IntPtr handle = GetForegroundWindow();
            return GetActiveWindowTitle(handle);
        }

        private string GetActiveWindowTitle(IntPtr handle)
        {
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

        private void EnqueueSimpleEvent(string type, string detail, string payload, string winTitle)
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
                if (detail.Contains("HisConsultation") || detail.Contains("Consultation")) return "Tạo / Cập nhật biên bản hội chẩn PT-01";
                if (detail.Contains("EmrSigner") || detail.Contains("Signer")) return "Mời ký số EMR / Phân quyền ký";
                if (detail.Contains("ServiceReq")) return "Tạo phiếu y lệnh / Chỉ định dịch vụ";
                if (detail.Contains("ExpMest")) return "Xuất dược / Tủ trực";
                if (detail.Contains("Tracking")) return "Tạo / Lưu tờ điều trị";
                if (detail.Contains("SarPrint")) return "In / Xem biểu mẫu";
            }
            else if (type.Contains("Click"))
            {
                if (detail.Contains("PT-01") || detail.Contains("pt-01") || detail.Contains("Thông qua mổ")) return "Chọn biểu mẫu PT-01";
                if (detail.Contains("In ấn") || winTitle.Contains("In ấn")) return "Chọn menu In ấn";
                if (detail.Contains("Biểu mẫu khác") || winTitle.Contains("Biểu mẫu khác")) return "Mở Biểu mẫu khác hồ sơ điều trị";
                if (detail.Contains("EMR") || detail.Contains("Thiết lập ký")) return "Mở thiết lập ký số EMR";
                if (detail.Contains("Tạo luồng ký")) return "Tạo luồng ký và chọn người duyệt";
                if (detail.Contains("Lưu") || detail.Contains("Save")) return "Lưu dữ liệu";
                if (detail.Contains("Chủ tọa") || detail.Contains("Thư ký")) return "Phân công Chủ tọa / Thư ký";
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
            RawClickEvent dummyClick;
            while (pendingClicks.TryDequeue(out dummyClick)) { }
            lvLiveFeed.Items.Clear();

            isRecording = true;
            stepCounter = 0;
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
            this.Size = new Size(1120, 760);
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

            // Allow click worker a short grace period to drain
            Thread.Sleep(200);

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
                        EnqueueSimpleEvent("🌐 Gọi API", method + " api:" + apiName, "", winTitle);
                        continue;
                    }

                    Match mPayload = Regex.Match(line, @"SerializeObject data api: (.*)");
                    if (mPayload.Success)
                    {
                        string json = mPayload.Groups[1].Value;
                        string preview = (json.Length > 120) ? json.Substring(0, 120) + "..." : json;
                        EnqueueSimpleEvent("📦 Dữ Liệu Gửi", preview, json, winTitle);
                        continue;
                    }

                    Match mToken = Regex.Match(line, @"TokenCode\|([a-fA-F0-9]{64})");
                    if (mToken.Success)
                    {
                        string t = mToken.Groups[1].Value;
                        if (t != lastToken)
                        {
                            lastToken = t;
                            EnqueueSimpleEvent("🔑 Token Mới", t.Substring(0, 16) + "...", t, winTitle);
                        }
                        continue;
                    }

                    if (line.Contains("UpdateWorkInfo") || line.Contains("WorkInfoSDO"))
                    {
                        EnqueueSimpleEvent("🏢 Cấu hình Khoa/Phòng", "Cập nhật phòng làm việc", line, winTitle);
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

                string imgText = (!string.IsNullOrEmpty(it.ScreenshotPath) && File.Exists(it.ScreenshotPath)) ? "🖼️ Xem ảnh" : "";

                int rowIdx = dgvReview.Rows.Add(
                    stt++,
                    it.TimeStr + " (" + it.DelayStr + ")",
                    it.ActionType,
                    it.Detail,
                    it.WindowTitle,
                    imgText,
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

                if (!string.IsNullOrEmpty(imgText))
                {
                    row.Cells[5].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Underline);
                    row.Cells[5].Style.ForeColor = Color.FromArgb(13, 110, 253);
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
            string htmlPath = Path.Combine(logsDir, string.Format("ActionRecord_{0}.html", ts));
            string latestHtmlPath = Path.Combine(logsDir, "ActionRecord_Latest.html");

            string mdContent = GenerateMarkdown();
            string htmlContent = GenerateHtml();

            File.WriteAllText(mdPath, mdContent, Encoding.UTF8);
            File.WriteAllText(latestMdPath, mdContent, Encoding.UTF8);
            File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);
            File.WriteAllText(latestHtmlPath, htmlContent, Encoding.UTF8);

            MessageBox.Show(
                "Đã lưu thành công báo cáo thao tác & ảnh chụp vào:\n" + mdPath + "\nvà " + htmlPath + "\n\n👉 Bác sĩ có thể bấm 'COPY GỬI CHAT' để gửi ngay cho Agent hoặc mở file HTML để xem chi tiết!",
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
                if (it != null && row.Cells[6].Value != null)
                {
                    it.UserNote = row.Cells[6].Value.ToString();
                }
            }
        }

        private string GenerateMarkdown()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 🎬 BÁO CÁO THAO TÁC UI LÂM SÀNG (ACTION RECORDER)");
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
            sb.AppendLine("| STT | Thời gian | Hành động | Phần tử UI / API | Cửa sổ / Trình duyệt | 📸 Ảnh | ✍️ Bác sĩ ghi chú (Mục đích) |");
            sb.AppendLine("| :---: | :--- | :--- | :--- | :--- | :---: | :--- |");

            int stt = 1;
            foreach (DataGridViewRow row in dgvReview.Rows)
            {
                ActionItem it = row.Tag as ActionItem;
                string time = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";
                string type = row.Cells[2].Value != null ? row.Cells[2].Value.ToString() : "";
                string detail = row.Cells[3].Value != null ? row.Cells[3].Value.ToString() : "";
                string win = row.Cells[4].Value != null ? row.Cells[4].Value.ToString() : "";
                string note = row.Cells[6].Value != null ? row.Cells[6].Value.ToString() : "";

                detail = detail.Replace("|", "\\|");
                win = win.Replace("|", "\\|");
                note = note.Replace("|", "\\|");

                string imgRef = (it != null && !string.IsNullOrEmpty(it.ScreenshotPath)) ? 
                    string.Format("[Ảnh]({0})", Path.GetFileName(it.ScreenshotPath)) : "-";

                sb.AppendLine(string.Format("| {0} | {1} | {2} | `{3}` | {4} | {5} | **{6}** |", stt++, time, type, detail, win, imgRef, note));
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

        private string GenerateHtml()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"vi\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<title>Báo Cáo Thao Tác UI - " + cboPurpose.Text + "</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background: #f4f6f9; color: #212529; margin: 0; padding: 25px; }");
            sb.AppendLine(".container { max-width: 1200px; margin: 0 auto; background: #fff; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.08); overflow: hidden; }");
            sb.AppendLine(".header { background: linear-gradient(135deg, #0d6efd, #0b5ed7); color: #fff; padding: 25px 30px; }");
            sb.AppendLine(".header h1 { margin: 0 0 10px 0; font-size: 24px; }");
            sb.AppendLine(".header p { margin: 0; opacity: 0.9; font-size: 14px; }");
            sb.AppendLine(".meta-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 15px; padding: 20px 30px; background: #f8f9fa; border-bottom: 1px solid #e9ecef; }");
            sb.AppendLine(".meta-card { background: #fff; padding: 12px 18px; border-radius: 6px; border: 1px solid #dee2e6; }");
            sb.AppendLine(".meta-card .label { font-size: 12px; color: #6c757d; font-weight: bold; text-transform: uppercase; }");
            sb.AppendLine(".meta-card .val { font-size: 16px; font-weight: bold; color: #212529; margin-top: 4px; }");
            sb.AppendLine(".table-container { padding: 30px; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 14px; }");
            sb.AppendLine("th { background: #343a40; color: #fff; text-align: left; padding: 12px; font-weight: 600; }");
            sb.AppendLine("td { padding: 12px; border-bottom: 1px solid #dee2e6; vertical-align: middle; }");
            sb.AppendLine("tr:hover { background: #f1f3f5; }");
            sb.AppendLine(".badge { display: inline-block; padding: 4px 8px; border-radius: 4px; font-size: 12px; font-weight: bold; }");
            sb.AppendLine(".badge-click { background: #d4edda; color: #155724; }");
            sb.AppendLine(".badge-key { background: #e2e3e5; color: #383d41; }");
            sb.AppendLine(".badge-api { background: #cce5ff; color: #004085; }");
            sb.AppendLine(".thumb { width: 120px; height: 68px; object-fit: cover; border-radius: 4px; border: 1px solid #ccc; cursor: pointer; transition: transform 0.2s; }");
            sb.AppendLine(".thumb:hover { transform: scale(1.08); box-shadow: 0 4px 8px rgba(0,0,0,0.2); }");
            sb.AppendLine(".note { color: #0d6efd; font-weight: 600; }");
            sb.AppendLine(".footer { text-align: center; padding: 20px; color: #6c757d; font-size: 13px; border-top: 1px solid #dee2e6; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"container\">");
            sb.AppendLine("  <div class=\"header\">");
            sb.AppendLine("    <h1>🎬 BÁO CÁO THAO TÁC UI (ACTION RECORDER)</h1>");
            sb.AppendLine("    <p>Hệ thống tự động ghi nhận quy trình lâm sàng trên HIS Desktop & Web EMR</p>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class=\"meta-grid\">");
            sb.AppendLine("    <div class=\"meta-card\"><div class=\"label\">Mục đích</div><div class=\"val\">" + cboPurpose.Text + "</div></div>");
            sb.AppendLine("    <div class=\"meta-card\"><div class=\"label\">Mã Bệnh Nhân</div><div class=\"val\">" + (string.IsNullOrEmpty(txtPatientCode.Text) ? "Chưa nhập" : txtPatientCode.Text) + "</div></div>");
            sb.AppendLine("    <div class=\"meta-card\"><div class=\"label\">Thời gian thực hiện</div><div class=\"val\">" + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " (" + string.Format("{0:mm\\:ss}", swDuration.Elapsed) + ")</div></div>");
            sb.AppendLine("    <div class=\"meta-card\"><div class=\"label\">Tổng số bước</div><div class=\"val\">" + dgvReview.Rows.Count + " bước</div></div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class=\"table-container\">");
            sb.AppendLine("    <h2>📋 Danh Sách Các Bước Thao Tác</h2>");
            sb.AppendLine("    <table>");
            sb.AppendLine("      <thead>");
            sb.AppendLine("        <tr>");
            sb.AppendLine("          <th style=\"width:40px; text-align:center;\">#</th>");
            sb.AppendLine("          <th style=\"width:90px;\">Thời gian</th>");
            sb.AppendLine("          <th style=\"width:110px;\">Hành động</th>");
            sb.AppendLine("          <th>Phần tử UI / Kỹ thuật</th>");
            sb.AppendLine("          <th>Cửa sổ / Web</th>");
            sb.AppendLine("          <th style=\"width:130px; text-align:center;\">Ảnh chụp</th>");
            sb.AppendLine("          <th>Ghi chú của Bác sĩ</th>");
            sb.AppendLine("        </tr>");
            sb.AppendLine("      </thead>");
            sb.AppendLine("      <tbody>");

            int stt = 1;
            foreach (DataGridViewRow row in dgvReview.Rows)
            {
                ActionItem it = row.Tag as ActionItem;
                string time = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";
                string type = row.Cells[2].Value != null ? row.Cells[2].Value.ToString() : "";
                string detail = row.Cells[3].Value != null ? row.Cells[3].Value.ToString() : "";
                string win = row.Cells[4].Value != null ? row.Cells[4].Value.ToString() : "";
                string note = row.Cells[6].Value != null ? row.Cells[6].Value.ToString() : "";

                string badgeClass = "badge-key";
                if (type.Contains("Click")) badgeClass = "badge-click";
                else if (type.Contains("API")) badgeClass = "badge-api";

                string imgTag = "-";
                if (it != null && !string.IsNullOrEmpty(it.ScreenshotPath) && File.Exists(it.ScreenshotPath))
                {
                    string relPath = "ActionScreenshots/" + Path.GetFileName(it.ScreenshotPath);
                    imgTag = string.Format("<a href=\"{0}\" target=\"_blank\"><img src=\"{0}\" class=\"thumb\" title=\"Nhấp để xem ảnh đầy đủ\" /></a>", relPath);
                }

                sb.AppendLine("        <tr>");
                sb.AppendLine(string.Format("          <td style=\"text-align:center; font-weight:bold;\">{0}</td>", stt++));
                sb.AppendLine(string.Format("          <td>{0}</td>", time));
                sb.AppendLine(string.Format("          <td><span class=\"badge {0}\">{1}</span></td>", badgeClass, type));
                sb.AppendLine(string.Format("          <td><code>{0}</code></td>", System.Web.HttpUtility.HtmlEncode(detail)));
                sb.AppendLine(string.Format("          <td>{0}</td>", System.Web.HttpUtility.HtmlEncode(win)));
                sb.AppendLine(string.Format("          <td style=\"text-align:center;\">{0}</td>", imgTag));
                sb.AppendLine(string.Format("          <td class=\"note\">{0}</td>", System.Web.HttpUtility.HtmlEncode(note)));
                sb.AppendLine("        </tr>");
            }

            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class=\"footer\">Hệ thống HIS Automation Support - Khoa CTCH & CS Bạch Mai</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopRecording(false);
            isWorkerRunning = false;
            if (hookKeyboard != IntPtr.Zero) UnhookWindowsHookEx(hookKeyboard);
            if (hookMouse != IntPtr.Zero) UnhookWindowsHookEx(hookMouse);
            base.OnFormClosing(e);
        }
    }
}
