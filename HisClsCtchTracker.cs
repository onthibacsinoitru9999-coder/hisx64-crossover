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
}

public class ClsItemDto
{
    public int Stt { get; set; }
    public long ServiceReqId { get; set; }
    public string ServiceReqCode { get; set; }
    public long TreatmentId { get; set; }
    public string PatientCode { get; set; }
    public string PatientName { get; set; }
    public string GenderName { get; set; }
    public string BedRoomName { get; set; }
    public string BedName { get; set; }
    public string BedFull { get; set; }
    public string ServiceName { get; set; }
    public string ServiceCode { get; set; }
    public decimal Amount { get; set; }
    public string ServiceTypeName { get; set; }
    public string ExecuteRoomName { get; set; }
    public string RequestDepartmentName { get; set; }
    public string RequestRoomName { get; set; }
    public string RequestUserName { get; set; }
    public string InstructionTimeStr { get; set; }
    public long InstructionTimeRaw { get; set; }
    public long StatusId { get; set; }
    public string StatusName { get; set; }
    public string IcdText { get; set; }
}

public class ClsDataResult
{
    public List<ClsItemDto> Items { get; set; }
    public int TotalPatients { get; set; }
    public ClsDataResult()
    {
        Items = new List<ClsItemDto>();
    }
}

public class MainForm : Form
{
    private static BackendAdapter adapter;
    private static MyAdapter myAdapter = new MyAdapter();
    private static CommonParam param;
    private static string currentToken = null;

    private List<ClsItemDto> allItems = new List<ClsItemDto>();
    private List<ClsItemDto> filteredItems = new List<ClsItemDto>();
    private int totalInPatients = 0;

    // UI Controls
    private Panel pnlHeader;
    private Panel pnlKpi;
    private Panel pnlToolbar;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatusText;
    private ToolStripStatusLabel lblCountInfo;
    private DataGridView dgv;

    // KPI Labels
    private Label lblKpiInPatients;
    private Label lblKpiTotalCls;
    private Label lblKpiPending;
    private Label lblKpiProcessing;
    private Label lblLastUpdated;

    // Filter Controls
    private TextBox txtSearch;
    private ComboBox cboStatus;
    private ComboBox cboType;
    private ComboBox cboRoom;
    private Button btnRefresh;
    private Button btnExportCsv;
    private Button btnCopyClipboard;
    private CheckBox chkAutoRefresh;
    private System.Windows.Forms.Timer autoRefreshTimer;
    private ProgressBar progressBar;

    public MainForm()
    {
        InitializeComponents();
        this.Load += MainForm_Load;
    }

    private void InitializeComponents()
    {
        this.Text = "HỆ THỐNG THEO DÕI CẬN LÂM SÀNG CHƯA XỬ LÝ & ĐANG XỬ LÝ - KHOA CTCH & CỘT SỐNG (KHOA 57)";
        this.Size = new Size(1366, 768);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        this.BackColor = Color.FromArgb(243, 244, 246);

        // Try to load icon if exists
        try
        {
            if (File.Exists("APP.ico")) this.Icon = new Icon("APP.ico");
        }
        catch { }

        // 1. Header Panel
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.FromArgb(30, 58, 138), // Deep Navy
            Padding = new Padding(16, 8, 16, 8)
        };

        Label lblTitle = new Label
        {
            Text = "🏥 THEO DÕI CẬN LÂM SÀNG NỘI TRÚ - KHOA CHẤN THƯƠNG CHỈNH HÌNH & CỘT SỐNG",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        lblLastUpdated = new Label
        {
            Text = "Cập nhật lúc: --:--:--",
            ForeColor = Color.FromArgb(224, 242, 254),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
            Dock = DockStyle.Right,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblLastUpdated);

        // 2. KPI Cards Panel
        pnlKpi = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = Color.White,
            Padding = new Padding(12, 6, 12, 6)
        };

        TableLayoutPanel tlpKpi = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        var card1 = CreateKpiCard("BỆNH NHÂN NỘI TRÚ KHOA 57", "0 BN", Color.FromArgb(3, 105, 161), out lblKpiInPatients);
        var card2 = CreateKpiCard("TỔNG SỐ CLS TỒN ĐỌNG", "0 DV", Color.FromArgb(79, 70, 229), out lblKpiTotalCls);
        var card3 = CreateKpiCard("🔴 CHƯA XỬ LÝ (CHƯA TIẾP NHẬN)", "0", Color.FromArgb(220, 38, 38), out lblKpiPending);
        var card4 = CreateKpiCard("🟡 ĐANG XỬ LÝ (CHỜ KẾT QUẢ)", "0", Color.FromArgb(217, 119, 6), out lblKpiProcessing);

        tlpKpi.Controls.Add(card1, 0, 0);
        tlpKpi.Controls.Add(card2, 1, 0);
        tlpKpi.Controls.Add(card3, 2, 0);
        tlpKpi.Controls.Add(card4, 3, 0);
        pnlKpi.Controls.Add(tlpKpi);

        // 3. Toolbar / Filter Panel
        pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(249, 250, 251),
            Padding = new Padding(12, 8, 12, 8)
        };

        btnRefresh = new Button
        {
            Text = "🔄 Làm mới (1-Click)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(2, 132, 199),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Height = 34,
            Width = 150,
            Location = new Point(12, 9),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += async (s, e) => await LoadDataAsync();

        Label lblSearch = new Label { Text = "🔍 Tìm:", Location = new Point(172, 16), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        txtSearch = new TextBox
        {
            Location = new Point(220, 12),
            Width = 200,
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.Black
        };
        txtSearch.TextChanged += (s, e) => ApplyFilter();

        Label lblStt = new Label { Text = "Trạng thái:", Location = new Point(430, 16), AutoSize = true };
        cboStatus = new ComboBox
        {
            Location = new Point(500, 12),
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        cboStatus.Items.AddRange(new object[] { "-- Tất cả trạng thái --", "🔴 Chưa xử lý (01)", "🟡 Đang xử lý (02)" });
        cboStatus.SelectedIndex = 0;
        cboStatus.SelectedIndexChanged += (s, e) => ApplyFilter();

        Label lblType = new Label { Text = "Loại CLS:", Location = new Point(650, 16), AutoSize = true };
        cboType = new ComboBox
        {
            Location = new Point(715, 12),
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        cboType.Items.AddRange(new object[] { "-- Tất cả nhóm CLS --", "Xét nghiệm", "Chẩn đoán hình ảnh", "Siêu âm", "Thăm dò chức năng", "Giải phẫu bệnh", "Thủ thuật / Khác" });
        cboType.SelectedIndex = 0;
        cboType.SelectedIndexChanged += (s, e) => ApplyFilter();

        Label lblRoom = new Label { Text = "Buồng:", Location = new Point(865, 16), AutoSize = true };
        cboRoom = new ComboBox
        {
            Location = new Point(915, 12),
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        cboRoom.Items.Add("-- Tất cả buồng --");
        cboRoom.SelectedIndex = 0;
        cboRoom.SelectedIndexChanged += (s, e) => ApplyFilter();

        chkAutoRefresh = new CheckBox
        {
            Text = "Tự động quét (60s)",
            Location = new Point(1045, 14),
            AutoSize = true,
            Checked = true
        };
        chkAutoRefresh.CheckedChanged += (s, e) =>
        {
            autoRefreshTimer.Enabled = chkAutoRefresh.Checked;
        };

        btnExportCsv = new Button
        {
            Text = "📊 Xuất Excel (CSV)",
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Height = 32,
            Width = 125,
            Location = new Point(1190, 10),
            Cursor = Cursors.Hand
        };
        btnExportCsv.FlatAppearance.BorderSize = 0;
        btnExportCsv.Click += (s, e) => ExportToCsv();

        btnCopyClipboard = new Button
        {
            Text = "📋 Copy Giao Ban",
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
            BackColor = Color.FromArgb(75, 85, 99),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Height = 32,
            Width = 115,
            Location = new Point(1320, 10),
            Cursor = Cursors.Hand
        };
        btnCopyClipboard.FlatAppearance.BorderSize = 0;
        btnCopyClipboard.Click += (s, e) => CopyToClipboard();

        pnlToolbar.Controls.Add(btnRefresh);
        pnlToolbar.Controls.Add(lblSearch);
        pnlToolbar.Controls.Add(txtSearch);
        pnlToolbar.Controls.Add(lblStt);
        pnlToolbar.Controls.Add(cboStatus);
        pnlToolbar.Controls.Add(lblType);
        pnlToolbar.Controls.Add(cboType);
        pnlToolbar.Controls.Add(lblRoom);
        pnlToolbar.Controls.Add(cboRoom);
        pnlToolbar.Controls.Add(chkAutoRefresh);
        pnlToolbar.Controls.Add(btnExportCsv);
        pnlToolbar.Controls.Add(btnCopyClipboard);

        // 4. DataGridView
        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            EnableHeadersVisualStyles = false
        };

        // Double buffer to prevent flickering
        typeof(DataGridView).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, dgv, new object[] { true });

        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        dgv.ColumnHeadersHeight = 36;
        dgv.RowTemplate.Height = 28;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(186, 230, 253);
        dgv.DefaultCellStyle.SelectionForeColor = Color.Black;

        AddColumns();
        dgv.CellFormatting += Dgv_CellFormatting;

        // 5. StatusStrip
        statusStrip = new StatusStrip { BackColor = Color.FromArgb(241, 245, 249) };
        lblStatusText = new ToolStripStatusLabel { Text = "Sẵn sàng.", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        lblCountInfo = new ToolStripStatusLabel { Text = "Tổng: 0 chỉ định CLS", Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) };
        progressBar = new ProgressBar { Width = 150, Height = 16, Style = ProgressBarStyle.Marquee, Visible = false };
        
        statusStrip.Items.Add(lblStatusText);
        statusStrip.Items.Add(lblCountInfo);

        // Timer for auto-refresh
        autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 60000 }; // 60 seconds
        autoRefreshTimer.Tick += async (s, e) => await LoadDataAsync(true);
        autoRefreshTimer.Start();

        // Assemble controls
        this.Controls.Add(dgv);
        this.Controls.Add(pnlToolbar);
        this.Controls.Add(pnlKpi);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(statusStrip);
    }

    private Panel CreateKpiCard(string title, string value, Color accentColor, out Label valueLabel)
    {
        Panel pnl = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle
        };

        Label lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8.0f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 20,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label lblVal = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = accentColor,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnl.Controls.Add(lblVal);
        pnl.Controls.Add(lblTitle);
        valueLabel = lblVal;
        return pnl;
    }

    private void AddColumns()
    {
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Stt", HeaderText = "STT", Width = 45 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BedFull", HeaderText = "Buồng / Giường", Width = 150 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PatientCode", HeaderText = "Mã BN", Width = 95 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PatientName", HeaderText = "Họ và Tên Bệnh Nhân", Width = 165 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ServiceName", HeaderText = "Tên Dịch Vụ Cận Lâm Sàng", Width = 260 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ServiceTypeName", HeaderText = "Nhóm CLS", Width = 110 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ExecuteRoomName", HeaderText = "Phòng Thực Hiện", Width = 180 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StatusName", HeaderText = "Trạng Thái", Width = 115 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InstructionTimeStr", HeaderText = "Thời Gian Y Lệnh", Width = 125 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RequestUserName", HeaderText = "Bác Sĩ Chỉ Định", Width = 135 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ServiceReqCode", HeaderText = "Mã Y Lệnh", Width = 110 });
    }

    private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= filteredItems.Count) return;
        var item = filteredItems[e.RowIndex];

        if (dgv.Columns[e.ColumnIndex].DataPropertyName == "StatusName")
        {
            if (item.StatusId == 1) // Chưa xử lý
            {
                e.CellStyle.ForeColor = Color.FromArgb(220, 38, 38); // Red
                e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            }
            else if (item.StatusId == 2) // Đang xử lý
            {
                e.CellStyle.ForeColor = Color.FromArgb(202, 138, 4); // Amber
                e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            }
        }
        else if (dgv.Columns[e.ColumnIndex].DataPropertyName == "PatientName")
        {
            e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        }
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync(bool isSilent = false)
    {
        try
        {
            btnRefresh.Enabled = false;
            lblStatusText.Text = "Đang kết nối máy chủ HIS và tải dữ liệu CLS Khoa 57...";

            var data = await Task.Run(() => FetchClsData());

            allItems = data.Items;
            totalInPatients = data.TotalPatients;

            // Populate Room filter if needed
            var rooms = allItems.Select(i => i.BedRoomName).Where(r => !string.IsNullOrEmpty(r)).Distinct().OrderBy(r => r).ToList();
            string curSelectedRoom = cboRoom.SelectedItem != null ? cboRoom.SelectedItem.ToString() : "-- Tất cả buồng --";
            cboRoom.Items.Clear();
            cboRoom.Items.Add("-- Tất cả buồng --");
            foreach (var r in rooms) cboRoom.Items.Add(r);
            if (cboRoom.Items.Contains(curSelectedRoom)) cboRoom.SelectedItem = curSelectedRoom;
            else cboRoom.SelectedIndex = 0;

            ApplyFilter();

            lblLastUpdated.Text = "Cập nhật lúc: " + DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            lblStatusText.Text = string.Format("✔ Đã tải xong: {0} bệnh nhân nội trú | {1} chỉ định CLS tồn đọng.", totalInPatients, allItems.Count);
        }
        catch (Exception ex)
        {
            lblStatusText.Text = "❌ Lỗi: " + ex.Message;
            if (!isSilent)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu CLS:\n" + ex.Message, "Thông báo Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            btnRefresh.Enabled = true;
        }
    }

    private void ApplyFilter()
    {
        string query = (txtSearch.Text ?? "").Trim().ToLower();
        string statusFilter = cboStatus.SelectedItem != null ? cboStatus.SelectedItem.ToString() : "";
        string typeFilter = cboType.SelectedItem != null ? cboType.SelectedItem.ToString() : "";
        string roomFilter = cboRoom.SelectedItem != null ? cboRoom.SelectedItem.ToString() : "";

        var q = allItems.AsEnumerable();

        if (!string.IsNullOrEmpty(query))
        {
            q = q.Where(i =>
                (!string.IsNullOrEmpty(i.PatientName) && i.PatientName.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(i.PatientCode) && i.PatientCode.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(i.BedFull) && i.BedFull.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(i.ServiceName) && i.ServiceName.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(i.ServiceCode) && i.ServiceCode.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(i.ExecuteRoomName) && i.ExecuteRoomName.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(i.ServiceReqCode) && i.ServiceReqCode.ToLower().Contains(query)) ||
                (!string.IsNullOrEmpty(i.RequestUserName) && i.RequestUserName.ToLower().Contains(query)));
        }

        if (statusFilter.Contains("Chưa xử lý"))
        {
            q = q.Where(i => i.StatusId == 1);
        }
        else if (statusFilter.Contains("Đang xử lý"))
        {
            q = q.Where(i => i.StatusId == 2);
        }

        if (!string.IsNullOrEmpty(typeFilter) && !typeFilter.Contains("Tất cả"))
        {
            q = q.Where(i => !string.IsNullOrEmpty(i.ServiceTypeName) && i.ServiceTypeName.Contains(typeFilter));
        }

        if (!string.IsNullOrEmpty(roomFilter) && !roomFilter.Contains("Tất cả"))
        {
            q = q.Where(i => i.BedRoomName == roomFilter);
        }

        filteredItems = q.OrderBy(i => i.BedFull).ThenBy(i => i.PatientName).ThenByDescending(i => i.InstructionTimeRaw).ToList();

        for (int i = 0; i < filteredItems.Count; i++)
        {
            filteredItems[i].Stt = i + 1;
        }

        dgv.DataSource = null;
        dgv.DataSource = filteredItems;

        // Update KPIs
        int countPending = allItems.Count(i => i.StatusId == 1);
        int countProcessing = allItems.Count(i => i.StatusId == 2);

        lblKpiInPatients.Text = string.Format("{0} BN", totalInPatients);
        lblKpiTotalCls.Text = string.Format("{0} DV", allItems.Count);
        lblKpiPending.Text = string.Format("{0} ({1:P0})", countPending, allItems.Count > 0 ? (double)countPending / allItems.Count : 0);
        lblKpiProcessing.Text = string.Format("{0} ({1:P0})", countProcessing, allItems.Count > 0 ? (double)countProcessing / allItems.Count : 0);

        lblCountInfo.Text = string.Format("Hiển thị: {0} / {1} chỉ định CLS", filteredItems.Count, allItems.Count);
    }

    private void ExportToCsv()
    {
        if (filteredItems == null || filteredItems.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SaveFileDialog sfd = new SaveFileDialog
        {
            Filter = "File Excel CSV (*.csv)|*.csv",
            FileName = string.Format("DanhSach_CLS_TonDong_KhoaCTCH_{0}.csv", DateTime.Now.ToString("yyyyMMdd_HHmm"))
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                // UTF-8 BOM for Excel display in Vietnamese
                sb.AppendLine("STT,Buồng Giường,Mã BN,Họ và Tên Bệnh Nhân,Giới tính,Tên Dịch Vụ CLS,Mã Dịch Vụ,Nhóm CLS,Phòng Thực Hiện,Trạng Thái,Thời Gian Chỉ Định,Bác Sĩ Chỉ Định,Mã Y Lệnh");

                foreach (var i in filteredItems)
                {
                    sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\",\"{12}\"",
                        i.Stt,
                        EscapeCsv(i.BedFull),
                        EscapeCsv(i.PatientCode),
                        EscapeCsv(i.PatientName),
                        EscapeCsv(i.GenderName),
                        EscapeCsv(i.ServiceName),
                        EscapeCsv(i.ServiceCode),
                        EscapeCsv(i.ServiceTypeName),
                        EscapeCsv(i.ExecuteRoomName),
                        EscapeCsv(i.StatusName),
                        EscapeCsv(i.InstructionTimeStr),
                        EscapeCsv(i.RequestUserName),
                        EscapeCsv(i.ServiceReqCode)));
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

    private void CopyToClipboard()
    {
        if (filteredItems == null || filteredItems.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu để sao chép!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("📋 BÁO CÁO CẬN LÂM SÀNG TỒN ĐỌNG KHOA CTCH & CỘT SỐNG (LÚC {0})", DateTime.Now.ToString("HH:mm dd/MM/yyyy")));
            sb.AppendLine(string.Format("Tổng số: {0} dịch vụ CLS ({1} Chưa xử lý, {2} Đang xử lý)",
                filteredItems.Count, filteredItems.Count(i => i.StatusId == 1), filteredItems.Count(i => i.StatusId == 2)));
            sb.AppendLine("--------------------------------------------------");

            var groupedByRoom = filteredItems.GroupBy(i => i.BedRoomName).OrderBy(g => g.Key);
            foreach (var rg in groupedByRoom)
            {
                string rName = string.IsNullOrEmpty(rg.Key) ? "Chưa xếp buồng" : rg.Key;
                sb.AppendLine(string.Format("\n🏢 [{0}] ({1} CLS):", rName, rg.Count()));

                var groupedByPatient = rg.GroupBy(i => i.PatientCode + "_" + i.PatientName);
                foreach (var pg in groupedByPatient)
                {
                    var first = pg.First();
                    sb.AppendLine(string.Format("  👤 {0} ({1}) - {2}:", first.PatientName, first.PatientCode, first.BedName));
                    foreach (var item in pg)
                    {
                        string icon = item.StatusId == 1 ? "🔴" : "🟡";
                        sb.AppendLine(string.Format("     - {0} {1} -> [{2}] ({3})",
                            icon, item.ServiceName, item.ExecuteRoomName, item.StatusName));
                    }
                }
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show("Đã sao chép nội dung bàn giao vào Clipboard!\nBạn có thể dán (Ctrl+V) trực tiếp vào Zalo, Viber hoặc Word.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    // Backend Fetch Logic
    public static ClsDataResult FetchClsData()
    {
        InitSession();

        List<long> clsTypeIds = new List<long> { 2, 3, 4, 5, 8, 9, 13 }; // XN, HA, TT, CN, NS, SA, GB
        List<long> pendingSttIds = new List<long> { 1, 2 }; // 1: Chưa xử lý, 2: Đang xử lý

        // 1. Get in-room patients in Khoa CTCH & CS (Khoa 57)
        HisTreatmentBedRoomViewFilter tbrf = new HisTreatmentBedRoomViewFilter();
        tbrf.IS_IN_ROOM = true;
        tbrf.TREATMENT_IS_ACTIVE = true;
        var bedRooms = myAdapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", ApiConsumers.MosConsumer, tbrf, param);

        Dictionary<long, V_HIS_TREATMENT_BED_ROOM> bedMap = new Dictionary<long, V_HIS_TREATMENT_BED_ROOM>();
        List<long> dept57TreatmentIds = new List<long>();

        if (bedRooms != null)
        {
            var dept57Beds = bedRooms.Where(b => b.DEPARTMENT_ID == 57).ToList();
            foreach (var b in dept57Beds)
            {
                if (!bedMap.ContainsKey(b.TREATMENT_ID))
                {
                    bedMap.Add(b.TREATMENT_ID, b);
                    dept57TreatmentIds.Add(b.TREATMENT_ID);
                }
            }
        }

        // 2. Query Service Requests:
        // A: By active in-patient TreatmentIds in Khoa 57
        List<V_HIS_SERVICE_REQ> allReqs = new List<V_HIS_SERVICE_REQ>();
        if (dept57TreatmentIds.Count > 0)
        {
            // Batch treatment IDs if necessary (up to 500 per batch)
            int batchSize = 300;
            for (int i = 0; i < dept57TreatmentIds.Count; i += batchSize)
            {
                var batch = dept57TreatmentIds.Skip(i).Take(batchSize).ToList();
                HisServiceReqViewFilter srfTreat = new HisServiceReqViewFilter();
                srfTreat.TREATMENT_IDs = batch;
                srfTreat.SERVICE_REQ_STT_IDs = pendingSttIds;
                srfTreat.SERVICE_REQ_TYPE_IDs = clsTypeIds;
                srfTreat.IS_ACTIVE = 1;
                var res = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srfTreat, param);
                if (res != null) allReqs.AddRange(res);
            }
        }

        // B: Also query ServiceReqs created by Khoa 57 in the last 4 days (catch new assignments before room transfer)
        long fromDate = long.Parse(DateTime.Now.AddDays(-4).ToString("yyyyMMdd000000"));
        HisServiceReqViewFilter srfDept = new HisServiceReqViewFilter();
        srfDept.REQUEST_DEPARTMENT_ID = 57;
        srfDept.INTRUCTION_TIME_FROM = fromDate;
        srfDept.SERVICE_REQ_STT_IDs = pendingSttIds;
        srfDept.SERVICE_REQ_TYPE_IDs = clsTypeIds;
        srfDept.IS_ACTIVE = 1;
        var deptReqs = myAdapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", ApiConsumers.MosConsumer, srfDept, param);
        if (deptReqs != null) allReqs.AddRange(deptReqs);

        // Deduplicate service requests by ID
        var distinctReqs = allReqs.GroupBy(r => r.ID).Select(g => g.First()).ToList();

        // 3. Query Detailed Services (SERE_SERV)
        List<ClsItemDto> resultItems = new List<ClsItemDto>();
        if (distinctReqs.Count > 0)
        {
            var reqIds = distinctReqs.Select(r => r.ID).Distinct().ToList();
            List<V_HIS_SERE_SERV> allSereServs = new List<V_HIS_SERE_SERV>();

            int ssBatchSize = 250;
            for (int i = 0; i < reqIds.Count; i += ssBatchSize)
            {
                var batch = reqIds.Skip(i).Take(ssBatchSize).ToList();
                HisSereServViewFilter ssf = new HisSereServViewFilter();
                ssf.SERVICE_REQ_IDs = batch;
                ssf.IS_ACTIVE = 1;
                var ssList = myAdapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", ApiConsumers.MosConsumer, ssf, param);
                if (ssList != null) allSereServs.AddRange(ssList);
            }

            var ssByReqId = allSereServs.GroupBy(s => s.SERVICE_REQ_ID ?? 0).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var req in distinctReqs)
            {
                V_HIS_TREATMENT_BED_ROOM bedInfo = null;
                bedMap.TryGetValue(req.TREATMENT_ID, out bedInfo);

                string bRoom = bedInfo != null ? bedInfo.BED_ROOM_NAME : "";
                string bName = bedInfo != null ? bedInfo.BED_NAME : "";
                string bFull = bedInfo != null ? string.Format("{0} - {1}", bRoom, bName) : "Nội trú Khoa CTCH";

                string timeFormatted = FormatTimeString(req.INTRUCTION_TIME);
                string sttDesc = req.SERVICE_REQ_STT_ID == 1 ? "🔴 Chưa xử lý" : (req.SERVICE_REQ_STT_ID == 2 ? "🟡 Đang xử lý" : req.SERVICE_REQ_STT_NAME);

                List<V_HIS_SERE_SERV> childServices;
                if (ssByReqId.TryGetValue(req.ID, out childServices) && childServices.Count > 0)
                {
                    foreach (var s in childServices)
                    {
                        resultItems.Add(new ClsItemDto
                        {
                            ServiceReqId = req.ID,
                            ServiceReqCode = req.SERVICE_REQ_CODE,
                            TreatmentId = req.TREATMENT_ID,
                            PatientCode = req.TDL_PATIENT_CODE,
                            PatientName = req.TDL_PATIENT_NAME,
                            GenderName = req.TDL_PATIENT_GENDER_NAME,
                            BedRoomName = bRoom,
                            BedName = bName,
                            BedFull = bFull,
                            ServiceName = s.TDL_SERVICE_NAME,
                            ServiceCode = s.TDL_SERVICE_CODE,
                            Amount = s.AMOUNT,
                            ServiceTypeName = !string.IsNullOrEmpty(s.SERVICE_TYPE_NAME) ? s.SERVICE_TYPE_NAME : req.SERVICE_REQ_TYPE_NAME,
                            ExecuteRoomName = !string.IsNullOrEmpty(s.EXECUTE_ROOM_NAME) ? s.EXECUTE_ROOM_NAME : req.EXECUTE_ROOM_NAME,
                            RequestDepartmentName = req.REQUEST_DEPARTMENT_NAME,
                            RequestRoomName = req.REQUEST_ROOM_NAME,
                            RequestUserName = req.REQUEST_USERNAME,
                            InstructionTimeStr = timeFormatted,
                            InstructionTimeRaw = req.INTRUCTION_TIME,
                            StatusId = req.SERVICE_REQ_STT_ID,
                            StatusName = sttDesc,
                            IcdText = req.ICD_NAME
                        });
                    }
                }
                else
                {
                    // Fallback to ServiceReq level if SereServ is empty
                    resultItems.Add(new ClsItemDto
                    {
                        ServiceReqId = req.ID,
                        ServiceReqCode = req.SERVICE_REQ_CODE,
                        TreatmentId = req.TREATMENT_ID,
                        PatientCode = req.TDL_PATIENT_CODE,
                        PatientName = req.TDL_PATIENT_NAME,
                        GenderName = req.TDL_PATIENT_GENDER_NAME,
                        BedRoomName = bRoom,
                        BedName = bName,
                        BedFull = bFull,
                        ServiceName = req.SERVICE_REQ_TYPE_NAME,
                        ServiceCode = req.SERVICE_REQ_CODE,
                        Amount = 1,
                        ServiceTypeName = req.SERVICE_REQ_TYPE_NAME,
                        ExecuteRoomName = req.EXECUTE_ROOM_NAME,
                        RequestDepartmentName = req.REQUEST_DEPARTMENT_NAME,
                        RequestRoomName = req.REQUEST_ROOM_NAME,
                        RequestUserName = req.REQUEST_USERNAME,
                        InstructionTimeStr = timeFormatted,
                        InstructionTimeRaw = req.INTRUCTION_TIME,
                        StatusId = req.SERVICE_REQ_STT_ID,
                        StatusName = sttDesc,
                        IcdText = req.ICD_NAME
                    });
                }
            }
        }

        return new ClsDataResult { Items = resultItems, TotalPatients = dept57TreatmentIds.Count };
    }

    private static string FormatTimeString(long time)
    {
        string s = time.ToString();
        if (s.Length >= 14)
        {
            return string.Format("{0}/{1}/{2} {3}:{4}",
                s.Substring(6, 2), s.Substring(4, 2), s.Substring(0, 4), s.Substring(8, 2), s.Substring(10, 2));
        }
        return s;
    }

    public static void InitSession()
    {
        if (!string.IsNullOrEmpty(currentToken)) return;

        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1. Cache
        foreach (var cf in new[] { Path.Combine(baseDir, "doctor_standalone.token"), Path.Combine(preferredDir, "doctor_standalone.token") })
        {
            try
            {
                if (File.Exists(cf))
                {
                    var parts = File.ReadAllText(cf, Encoding.UTF8).Trim().Split('|');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]))
                    {
                        long ticks = long.Parse(parts[1]);
                        if ((DateTime.UtcNow.Ticks - ticks) < TimeSpan.FromHours(6).Ticks)
                        { currentToken = parts[0]; ApiConsumers.SetConsunmer(currentToken); adapter = new BackendAdapter(param); return; }
                    }
                }
            }
            catch { }
        }

        // 2. Live log with identity guard
        List<string> candidates = new List<string>();
        candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        try
        {
            var procs = System.Diagnostics.Process.GetProcessesByName("HIS");
            if (procs != null && procs.Length > 0)
            {
                string hp = procs[0].MainModule.FileName;
                if (hp.Contains("LBP2900_R150_V330_W64_uk_EN_2"))
                    candidates.Add(Path.Combine(Path.GetDirectoryName(hp), "Logs", "LogSystem.txt"));
            }
        }
        catch { }
        DirectoryInfo cur2 = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++) { if (cur2 == null) break; candidates.Add(Path.Combine(cur2.FullName, "Logs", "LogSystem.txt")); cur2 = cur2.Parent; }

        foreach (var lp in candidates)
        {
            if (!File.Exists(lp)) continue;
            try
            {
                using (var fs = new FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long len = fs.Length; if (len == 0) continue;
                    int bsz = (int)Math.Min(131072L, len);
                    fs.Seek(len - bsz, SeekOrigin.Begin);
                    byte[] buf = new byte[bsz]; int r = fs.Read(buf, 0, bsz);
                    string chunk = Encoding.UTF8.GetString(buf, 0, r);
                    if (chunk.Contains("IsLostToken:true") || chunk.Contains("isLogouter:true")) continue;
                    if (!chunk.Contains("034727") && !chunk.Contains("vmc")) continue;
                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0 && chunk.Length >= idx + 74)
                    { currentToken = chunk.Substring(idx + 10, 64); ApiConsumers.SetConsunmer(currentToken); adapter = new BackendAdapter(param); return; }
                }
            }
            catch { }
        }

        // 3. Standalone login
        HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
        ClientTokenManager tokenManager = new ClientTokenManager("HIS");
        param = new CommonParam();
        var token034 = tokenManager.Login(param, "034727", "9981", "2.390.0");
        if (token034 != null)
        {
            currentToken = token034.TokenCode;
            ApiConsumers.SetConsunmer(currentToken);
            adapter = new BackendAdapter(param);
            try { File.WriteAllText(Path.Combine(preferredDir, "doctor_standalone.token"), currentToken + "|" + DateTime.UtcNow.Ticks + "|034727", Encoding.UTF8); } catch { }
            return;
        }
        var tokenVmc = tokenManager.Login(param, "vmc", "789789", "2.390.0");
        if (tokenVmc != null)
        {
            currentToken = tokenVmc.TokenCode;
            ApiConsumers.SetConsunmer(currentToken);
            adapter = new BackendAdapter(param);
            try { File.WriteAllText(Path.Combine(preferredDir, "doctor_standalone.token"), currentToken + "|" + DateTime.UtcNow.Ticks + "|034727", Encoding.UTF8); } catch { }
            return;
        }
        throw new Exception("Không thể xác thực tài khoản (034727 / vmc) trên hệ thống HIS!");
    }
}

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length > 0 && (args[0] == "--cli" || args[0] == "-c" || args[0] == "--export"))
        {
            Console.WriteLine("==================================================================");
            Console.WriteLine("  HỆ THỐNG THEO DÕI CẬN LÂM SÀNG KHOA CTCH & CỘT SỐNG (KHOA 57)");
            Console.WriteLine("==================================================================");
            try
            {
                var data = MainForm.FetchClsData();
                Console.WriteLine(string.Format("✔ Tổng BN nội trú: {0} | Tổng chỉ định CLS tồn: {1}", data.TotalPatients, data.Items.Count));
                Console.WriteLine(string.Format("   - Chưa xử lý (01): {0}", data.Items.Count(i => i.StatusId == 1)));
                Console.WriteLine(string.Format("   - Đang xử lý (02): {0}", data.Items.Count(i => i.StatusId == 2)));
                Console.WriteLine("\n--- DANH SÁCH CHI TIẾT ---");

                int idx = 1;
                foreach (var item in data.Items)
                {
                    Console.WriteLine(string.Format("[{0:000}] {1,-28} | {2,-12} | {3,-22} | {4,-35} | {5,-12} | {6}",
                        idx++, item.BedFull, item.PatientCode, item.PatientName, item.ServiceName, item.StatusName, item.ExecuteRoomName));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
