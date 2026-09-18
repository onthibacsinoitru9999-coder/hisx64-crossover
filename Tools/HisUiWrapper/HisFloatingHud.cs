using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HisUiWrapper
{
    public class HisFloatingHud : Form
    {
        private Label _lblTitle;
        private Label _lblStatus;
        private Label _lblStepCount;
        private Label _lblTargetApp;
        private Label _lblTargetWindow;
        private Label _lblLastAction;
        private Button _btnToggle;
        private Button _btnCheckpoint;
        private Button _btnFinish;
        private Button _btnClose;
        private Panel _headerPanel;

        private readonly HisUiHookEngine _engine;
        private readonly HisUiSessionWriter _writer;

        // Enable dragging of borderless form
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        public HisFloatingHud(HisUiHookEngine engine, HisUiSessionWriter writer)
        {
            _engine = engine;
            _writer = writer;

            InitializeComponents();

            // Register events
            _engine.OnActionRecorded += Engine_OnActionRecorded;
            _engine.OnRecordingStateChanged += Engine_OnRecordingStateChanged;
            _engine.OnStopRequested += Engine_OnStopRequested;
            _engine.OnNoteRequested += Engine_OnNoteRequested;
            _engine.OnProcessDiscovered += Engine_OnProcessDiscovered;
        }

        private void InitializeComponents()
        {
            this.Size = new Size(400, 165);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(28, 28, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.ShowInTaskbar = true;
            this.Text = "HIS & EMR UI Interaction Recorder";

            // Position at top-right corner with 25px padding
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - this.Width - 25, 25);

            // Header Panel
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(40, 40, 44)
            };
            _headerPanel.MouseDown += Header_MouseDown;

            _lblTitle = new Label
            {
                Text = "⚡ HIS & EMR UI RECORDER (AI LEARN)",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 210, 255),
                AutoSize = true,
                Location = new Point(8, 8)
            };
            _lblTitle.MouseDown += Header_MouseDown;

            _btnClose = new Button
            {
                Text = "✕",
                Size = new Size(24, 24),
                Location = new Point(this.Width - 28, 4),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Gray,
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => FinishAndExit();

            _headerPanel.Controls.Add(_lblTitle);
            _headerPanel.Controls.Add(_btnClose);

            _lblStatus = new Label
            {
                Text = "🔴 ĐANG GHI (REC)",
                ForeColor = Color.FromArgb(255, 75, 75),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 38)
            };

            _lblStepCount = new Label
            {
                Text = "0 thao tác",
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(155, 38)
            };

            _lblTargetApp = new Label
            {
                Text = "[HIS + EMR]",
                ForeColor = Color.FromArgb(150, 220, 100),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(250, 38)
            };

            _lblTargetWindow = new Label
            {
                Text = "Cửa sổ: (Đang chờ thao tác trên HIS/EMR...)",
                ForeColor = Color.FromArgb(170, 170, 170),
                Font = new Font("Segoe UI", 8F),
                Location = new Point(10, 60),
                Size = new Size(380, 18),
                AutoEllipsis = true
            };

            _lblLastAction = new Label
            {
                Text = "Sẵn sàng ghi nhận click / phím trong HIS & EMR",
                ForeColor = Color.FromArgb(240, 240, 240),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(10, 80),
                Size = new Size(380, 36),
                AutoEllipsis = true
            };

            // Buttons
            _btnToggle = new Button
            {
                Text = "⏸ Tạm dừng (F9)",
                Size = new Size(120, 28),
                Location = new Point(10, 124),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 56),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F),
                Cursor = Cursors.Hand
            };
            _btnToggle.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 78);
            _btnToggle.Click += (s, e) => _engine.ToggleRecording();

            _btnCheckpoint = new Button
            {
                Text = "📌 Đánh dấu (F10)",
                Size = new Size(120, 28),
                Location = new Point(140, 124),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 56),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F),
                Cursor = Cursors.Hand
            };
            _btnCheckpoint.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 78);
            _btnCheckpoint.Click += (s, e) => PromptCheckpointNote();

            _btnFinish = new Button
            {
                Text = "💾 Hoàn tất (F11)",
                Size = new Size(120, 28),
                Location = new Point(270, 124),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnFinish.FlatAppearance.BorderSize = 0;
            _btnFinish.Click += (s, e) => FinishAndExit();

            this.Controls.Add(_headerPanel);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(_lblStepCount);
            this.Controls.Add(_lblTargetApp);
            this.Controls.Add(_lblTargetWindow);
            this.Controls.Add(_lblLastAction);
            this.Controls.Add(_btnToggle);
            this.Controls.Add(_btnCheckpoint);
            this.Controls.Add(_btnFinish);
        }

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void Engine_OnActionRecorded(UiActionEvent ev)
        {
            _writer.AppendEvent(ev);

            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    _lblStepCount.Text = string.Format("{0} thao tác", ev.StepIndex);

                    string app = ev.AppBadge;
                    if (app == "EMR")
                    {
                        _lblTargetApp.Text = "📋 EMR";
                        _lblTargetApp.ForeColor = Color.FromArgb(220, 140, 255);
                    }
                    else
                    {
                        _lblTargetApp.Text = "🏥 HIS";
                        _lblTargetApp.ForeColor = Color.FromArgb(0, 210, 255);
                    }

                    if (ev.Element != null && !string.IsNullOrEmpty(ev.Element.TopWindowText))
                    {
                        _lblTargetWindow.Text = string.Format("[{0}] Cửa sổ: {1}", app, ev.Element.TopWindowText);
                    }
                    _lblLastAction.Text = ev.ToString();
                });
            }
        }

        private void Engine_OnProcessDiscovered(string procName, uint pid)
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    _lblLastAction.Text = string.Format("[+] Tự động phát hiện & gắn kết: {0} (PID: {1})", procName, pid);
                });
            }
        }

        private void Engine_OnRecordingStateChanged(bool isRecording)
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (isRecording)
                    {
                        _lblStatus.Text = "🔴 ĐANG GHI (REC)";
                        _lblStatus.ForeColor = Color.FromArgb(255, 75, 75);
                        _btnToggle.Text = "⏸ Tạm dừng (F9)";
                    }
                    else
                    {
                        _lblStatus.Text = "⏸️ TẠM DỪNG (PAUSED)";
                        _lblStatus.ForeColor = Color.FromArgb(240, 180, 40);
                        _btnToggle.Text = "▶ Tiếp tục (F9)";
                    }
                });
            }
        }

        private void Engine_OnStopRequested()
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)FinishAndExit);
            }
        }

        private void Engine_OnNoteRequested()
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)PromptCheckpointNote);
            }
        }

        private void PromptCheckpointNote()
        {
            string note = ShowCustomInputBox(
                "Nhập nội dung ghi chú cho bước này (ví dụ: 'Mở EMR từ HIS và chọn mẫu biên bản'):",
                "Ghi Chú Mốc Thao Tác (F10)",
                "Mốc: Chuyển sang thao tác EMR");

            if (!string.IsNullOrEmpty(note))
            {
                _engine.RecordCheckpoint(note);
            }
        }

        private static string ShowCustomInputBox(string prompt, string title, string defaultText)
        {
            using (Form promptForm = new Form())
            {
                promptForm.Width = 450;
                promptForm.Height = 160;
                promptForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                promptForm.Text = title;
                promptForm.StartPosition = FormStartPosition.CenterScreen;
                promptForm.TopMost = true;

                Label lblPrompt = new Label() { Left = 20, Top = 15, Text = prompt, AutoSize = true };
                TextBox textBox = new TextBox() { Left = 20, Top = 45, Width = 390, Text = defaultText };
                Button btnOk = new Button() { Text = "Đồng ý", Left = 230, Width = 85, Top = 80, DialogResult = DialogResult.OK };
                Button btnCancel = new Button() { Text = "Hủy", Left = 325, Width = 85, Top = 80, DialogResult = DialogResult.Cancel };

                promptForm.Controls.Add(lblPrompt);
                promptForm.Controls.Add(textBox);
                promptForm.Controls.Add(btnOk);
                promptForm.Controls.Add(btnCancel);
                promptForm.AcceptButton = btnOk;
                promptForm.CancelButton = btnCancel;

                return promptForm.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : string.Empty;
            }
        }

        private void FinishAndExit()
        {
            _engine.Stop();
            _writer.FinalizeSession();

            MessageBox.Show(
                string.Format("Đã hoàn tất phiên ghi thao tác giao diện HIS & EMR!\n\n" +
                              "- Tệp JSONL: {0}\n" +
                              "- Tệp Quy trình MD: {1}\n" +
                              "- Tệp Mã Replay: {2}\n\n" +
                              "Hệ thống sẽ mở thư mục chứa tệp để xem kết quả.",
                    System.IO.Path.GetFileName(_writer.JsonlPath),
                    System.IO.Path.GetFileName(_writer.MarkdownPath),
                    System.IO.Path.GetFileName(_writer.ReplayCodePath)),
                "HIS & EMR UI Recorder - Hoàn Tất",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            try
            {
                Process.Start("explorer.exe", string.Format("/select,\"{0}\"", _writer.MarkdownPath));
            }
            catch {}

            Application.Exit();
        }
    }
}
