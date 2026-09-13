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

public class TrackingMatchResult
{
    public long TrackingId { get; set; }
    public long TrackingTime { get; set; }
    public bool IsNewlyCreated { get; set; }
}

public class MedicineStockInfo
{
    public long MediStockId { get; set; }
    public string MediStockCode { get; set; }
    public string MediStockName { get; set; }
    public bool IsCabinet { get; set; }

    public MedicineStockInfo(long id, string code, string name, bool isCabinet = false)
    {
        MediStockId = id;
        MediStockCode = code;
        MediStockName = name;
        IsCabinet = isCabinet;
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
    
    public static string ExecutePrescription(AdapterBase adp, CommonParam prm, string loginName, string userName, long roomId, V_HIS_TREATMENT tr, V_HIS_MEDICINE_TYPE med, MedicineStockInfo stock, decimal amount, string tutorial, long trackingTime, long trackingId)
    {
        if (tr == null) throw new ArgumentNullException("tr", "Hồ sơ điều trị không được rỗng!");
        if (med == null) throw new ArgumentNullException("med", "Thuốc chỉ định không được rỗng!");

        string medName = med.MEDICINE_TYPE_NAME ?? "";
        string medCode = med.MEDICINE_TYPE_CODE ?? "";

        // 1. Kiểm tra nhóm thuốc Insulin & Quy đổi liều chuẩn xác (UI -> Lọ) theo Rule 5 AGENTS.md
        bool isInsulin = medName.IndexOf("Insulin", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         medName.IndexOf("Actrapid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         medName.IndexOf("Lantus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         medName.IndexOf("Mixtard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         medName.IndexOf("Novorapid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         medName.IndexOf("Humalog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         medName.IndexOf("1000IU", StringComparison.OrdinalIgnoreCase) >= 0;

        decimal presAmount = amount;
        long? useFormId = med.MEDICINE_USE_FORM_ID;
        string morning = null, noon = null, afternoon = null, evening = null;
        bool isExpend = false;
        decimal? speed = null;

        if (!string.IsNullOrEmpty(tutorial))
        {
            var matchSpeed = System.Text.RegularExpressions.Regex.Match(tutorial, @"(\d+)\s*(g/p|giọt|g/phút|ml/h)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (matchSpeed.Success)
            {
                decimal sVal;
                if (decimal.TryParse(matchSpeed.Groups[1].Value, out sVal)) speed = sVal;
            }
        }

        if (isInsulin)
        {
            if (amount >= 1.0m)
            {
                presAmount = amount / 1000.0m;
                Console.WriteLine(string.Format("  [SAFETY] Quy đổi liều Insulin: {0} UI -> {1:F4} lọ", amount, presAmount));
            }
            useFormId = 15; // Tiêm
            isExpend = false;

            // Phân bổ cữ tiêm theo giờ chỉ định
            string sTime = trackingTime.ToString();
            int hour = 17;
            if (sTime.Length >= 12) int.TryParse(sTime.Substring(8, 2), out hour);

            string doseStr = ((int)(presAmount * 1000.0m)).ToString("D2");
            if (hour >= 5 && hour <= 9) morning = doseStr;
            else if (hour >= 10 && hour <= 13) noon = doseStr;
            else if (hour >= 14 && hour <= 18) afternoon = doseStr;
            else evening = doseStr;

            if (string.IsNullOrEmpty(tutorial) || tutorial == "Theo chỉ dẫn của bác sĩ")
            {
                tutorial = string.Format("Tiêm dưới da {0} UI lúc {1}h theo phác đồ đường máu mao mạch", (int)(presAmount * 1000.0m), hour);
            }
        }
        else if (medCode == "TH.POVI008" || medCode == "TH.NATR047" || medName.IndexOf("Povidone", StringComparison.OrdinalIgnoreCase) >= 0 || medName.IndexOf("thay băng", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Dung dịch và vật tư thay băng tiêu hao
            useFormId = 25; // Dùng ngoài
            isExpend = true; // BẮT BUỘC bật hao phí tiêu hao
            if (string.IsNullOrEmpty(tutorial) || tutorial == "Theo chỉ dẫn của bác sĩ") tutorial = "thay băng";
        }

        bool isCabinetStock = (stock != null && stock.IsCabinet) || (stock != null && (stock.MediStockId == 810 || stock.MediStockId == 7787 || (stock.MediStockCode != null && stock.MediStockCode.StartsWith("TT"))));
        if (isCabinetStock)
        {
            string sessionKey = Guid.NewGuid().ToString();
            var takeBean = new TakeBeanSDO
            {
                TypeId = med.ID,
                MediStockId = stock.MediStockId,
                PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1,
                Amount = presAmount,
                ClientSessionKey = sessionKey,
                ExpiredDate = null
            };
            var beans = ((MyAdapter)adp).PostData<System.Collections.Generic.List<MOS.EFMODEL.DataModels.HIS_MEDICINE_BEAN>>("api/HisMedicineBean/Take", ApiConsumers.MosConsumer, takeBean, prm);
            if (beans == null || beans.Count == 0)
            {
                string errMsg = (prm != null && prm.Messages != null && prm.Messages.Count > 0) ? string.Join("; ", prm.Messages) : "Không giữ được thuốc trong tủ trực (có thể hết tồn).";
                throw new Exception(errMsg);
            }

            var outPresSDO = new OutPatientPresSDO
            {
                TreatmentId = tr.ID,
                InstructionTime = trackingTime,
                UseTimes = new System.Collections.Generic.List<long> { trackingTime },
                TrackingId = trackingId > 0 ? (long?)trackingId : null,
                RequestRoomId = roomId > 0 ? roomId : 5248,
                RequestLoginName = loginName,
                RequestUserName = userName,
                IcdCode = (tr.ICD_CODE ?? "") + (string.IsNullOrEmpty(tr.ICD_SUB_CODE) ? "" : "," + tr.ICD_SUB_CODE),
                IcdName = (tr.ICD_NAME ?? "") + (string.IsNullOrEmpty(tr.ICD_TEXT) ? "" : " - " + tr.ICD_TEXT),
                IsCabinet = true,
                ClientSessionKey = sessionKey,
                Medicines = new System.Collections.Generic.List<PresMedicineSDO>
                {
                    new PresMedicineSDO
                    {
                        MedicineTypeId = med.ID,
                        MediStockId = stock.MediStockId,
                        Amount = presAmount,
                        PresAmount = presAmount,
                        PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1,
                        Tutorial = !string.IsNullOrEmpty(tutorial) ? tutorial : "Theo chỉ dẫn của bác sĩ",
                        MedicineUseFormId = useFormId,
                        Speed = speed,
                        Morning = morning,
                        Noon = noon,
                        Afternoon = afternoon,
                        Evening = evening,
                        IsExpend = isExpend,
                        NumOfDays = 1,
                        MedicineBeanIds = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(beans, b => b.ID))
                    }
                }
            };
            var outRes = ((MyAdapter)adp).PostData<OutPatientPresResultSDO>("api/HisServiceReq/OutPatientPresCreateList", ApiConsumers.MosConsumer, new System.Collections.Generic.List<OutPatientPresSDO> { outPresSDO }, prm);
            if (outRes == null)
            {
                string errMsg = (prm != null && prm.Messages != null && prm.Messages.Count > 0) ? string.Join("; ", prm.Messages) : "Kê đơn tủ trực thất bại (API trả về null)";
                throw new Exception(errMsg);
            }
            string sReqCode = (outRes.ServiceReqs != null && outRes.ServiceReqs.Count > 0) ? outRes.ServiceReqs[0].SERVICE_REQ_CODE : null;
            string sExpCode = (outRes.ExpMests != null && outRes.ExpMests.Count > 0) ? outRes.ExpMests[0].EXP_MEST_CODE : null;
            if (!string.IsNullOrEmpty(sReqCode)) return sReqCode;
            if (!string.IsNullOrEmpty(sExpCode)) return sExpCode;
            return "OK";
        }
        else
        {
            var presSDO = new InPatientPresSDO
            {
                TreatmentId = tr.ID,
                InstructionTimes = new System.Collections.Generic.List<long> { trackingTime },
                UseTimes = new System.Collections.Generic.List<long> { trackingTime },
                TrackingId = trackingId > 0 ? (long?)trackingId : null,
                TrackingInfos = trackingId > 0 ? new System.Collections.Generic.List<TrackingInfoSDO> { new TrackingInfoSDO { TrackingId = trackingId, IntructionTime = trackingTime } } : null,
                RequestRoomId = roomId > 0 ? roomId : 5248,
                RequestLoginName = loginName,
                RequestUserName = userName,
                IcdCode = (tr.ICD_CODE ?? "") + (string.IsNullOrEmpty(tr.ICD_SUB_CODE) ? "" : "," + tr.ICD_SUB_CODE),
                IcdName = (tr.ICD_NAME ?? "") + (string.IsNullOrEmpty(tr.ICD_TEXT) ? "" : " - " + tr.ICD_TEXT),
                Medicines = new System.Collections.Generic.List<PresMedicineSDO>
                {
                    new PresMedicineSDO
                    {
                        MedicineTypeId = med.ID,
                        MediStockId = stock.MediStockId,
                        Amount = presAmount,
                        PresAmount = presAmount,
                        PatientTypeId = tr.TDL_PATIENT_TYPE_ID ?? 1,
                        Tutorial = !string.IsNullOrEmpty(tutorial) ? tutorial : "Theo chỉ dẫn của bác sĩ",
                        MedicineUseFormId = useFormId,
                        Speed = speed,
                        Morning = morning,
                        Noon = noon,
                        Afternoon = afternoon,
                        Evening = evening,
                        IsExpend = isExpend,
                        NumOfDays = 1
                    }
                }
            };
            var res = ((MyAdapter)adp).PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", ApiConsumers.MosConsumer, presSDO, prm);
            if (res == null)
            {
                string errMsg = (prm != null && prm.Messages != null && prm.Messages.Count > 0) ? string.Join("; ", prm.Messages) : "Kê đơn nội trú thất bại (API trả về null)";
                throw new Exception(errMsg);
            }
            string inReqCode = (res.ServiceReqs != null && res.ServiceReqs.Count > 0) ? res.ServiceReqs[0].SERVICE_REQ_CODE : null;
            string inExpCode = (res.ExpMests != null && res.ExpMests.Count > 0) ? res.ExpMests[0].EXP_MEST_CODE : null;
            if (!string.IsNullOrEmpty(inReqCode)) return inReqCode;
            if (!string.IsNullOrEmpty(inExpCode)) return inExpCode;
            return "OK";
        }
    }

    public static readonly List<MedicineStockInfo> CommonStocks = new List<MedicineStockInfo>
    {
        new MedicineStockInfo(810, "TT_KCTCHCS", "Tủ trực Khoa CTCH & Cột sống (Khoa 57 - HN)", true),
        new MedicineStockInfo(5142, "TTT_NBKP05.02", "Tủ trực thuốc Khu 3E (Ngoại TH - CSNB)", true),
        new MedicineStockInfo(5141, "TTT_NBKP05.01", "Tủ trực thuốc Khu 3D (Ngoại TH - CSNB)", true),
        new MedicineStockInfo(4854, "KTD_NBKP22.01", "Kho Dược chính (Cơ sở Ninh Bình)", false),
        new MedicineStockInfo(4210, "KT_KD15", "Kho thuốc viên (Hà Nội)", false),
        new MedicineStockInfo(4209, "KT_KD14", "Kho thuốc ống (Hà Nội)", false),
        new MedicineStockInfo(4208, "KT_KD13", "Kho thuốc Hướng thần (Hà Nội)", false),
        new MedicineStockInfo(4207, "KT_KD12", "Kho thuốc Gây nghiện (Hà Nội)", false),
        new MedicineStockInfo(753, "LA_TTDDLS", "Kho SP Dinh dưỡng điều trị", false),
        new MedicineStockInfo(7787, "TTSPDD_9", "Tủ trực SP Dinh dưỡng Khoa 57", true),
        new MedicineStockInfo(796, "KVT_KCTCGCS", "Kho Vật tư Khoa CTCH & Cột sống", false),
        new MedicineStockInfo(4168, "KT_KD10", "Kho Vắc xin", false)
    };

    private static Dictionary<long, List<V_HIS_MEDICINE_BEAN>> stockBeanCache = new Dictionary<long, List<V_HIS_MEDICINE_BEAN>>();

    public static TrackingMatchResult EnsureTrackingForPrescription(AdapterBase adp, CommonParam prm, V_HIS_TREATMENT tr, long departmentId, string loginName, string userName, long? targetTime = null)
    {
        if (tr == null) throw new ArgumentNullException("tr");
        long desiredTime = targetTime.HasValue && targetTime.Value > 0 ? targetTime.Value : long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
        long targetDate = desiredTime / 1000000; // YYYYMMDD

        HisTrackingViewFilter tkf = new HisTrackingViewFilter();
        tkf.TREATMENT_ID = tr.ID;
        tkf.ORDER_FIELD = "TRACKING_TIME";
        tkf.ORDER_DIRECTION = "DESC";
        var tks = ((MyAdapter)adp).FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", ApiConsumers.MosConsumer, tkf, prm);

        if (tks != null && tks.Count > 0)
        {
            var sameDayTrackings = tks.Where(tk => (tk.TRACKING_TIME / 1000000) == targetDate).ToList();
            if (sameDayTrackings.Count > 0)
            {
                var matched = sameDayTrackings.OrderBy(tk => Math.Abs(tk.TRACKING_TIME - desiredTime)).FirstOrDefault();
                if (matched != null)
                {
                    Console.WriteLine(string.Format("  ✔ [TRACKING-LINK] Gán trực tiếp vào Tờ điều trị ID {0} lúc {1} (Ngày {2})",
                        matched.ID, matched.TRACKING_TIME, targetDate));
                    return new TrackingMatchResult
                    {
                        TrackingId = matched.ID,
                        TrackingTime = matched.TRACKING_TIME,
                        IsNewlyCreated = false
                    };
                }
            }
        }

        // Tự động tạo tờ điều trị mới cho ngày hôm nay để gắn y lệnh trực tiếp
        long deptId = departmentId > 0 ? departmentId : 57;
        Console.WriteLine(string.Format("  ℹ [AUTO-TRACKING] Chưa có tờ điều trị ngày {0}. Đang tạo tờ điều trị mới lúc {1}...", targetDate, desiredTime));

        HIS_TRACKING newTk = new HIS_TRACKING
        {
            TREATMENT_ID = tr.ID,
            DEPARTMENT_ID = deptId,
            ROOM_ID = 5248,
            TRACKING_TIME = desiredTime,
            CONTENT = "Bệnh nhân tỉnh, tiếp xúc tốt. Thực hiện y lệnh thuốc tủ trực và theo dõi diễn biến.",
            CARE_INSTRUCTION = "Theo dõi toàn trạng, sinh hiệu.",
            ICD_CODE = tr.ICD_CODE,
            ICD_NAME = tr.ICD_NAME,
            ICD_SUB_CODE = tr.ICD_SUB_CODE,
            ICD_TEXT = tr.ICD_TEXT
        };
        HisTrackingSDO sdo = new HisTrackingSDO 
        { 
            Tracking = newTk,
            WorkingRoomId = 5248,
            Dhst = null
        };
        var created = ((MyAdapter)adp).PostData<HIS_TRACKING>("api/HisTracking/Create", ApiConsumers.MosConsumer, sdo, prm);
        if (created == null || created.ID == 0)
        {
            string errMsg = (prm != null && prm.Messages != null && prm.Messages.Count > 0) ? string.Join("; ", prm.Messages) : "Lỗi từ máy chủ HIS";
            throw new Exception("Không thể tạo tờ điều trị tự động: " + errMsg);
        }

        Console.WriteLine(string.Format("  ✔ [AUTO-TRACKING] Đã tạo thành công Tờ điều trị ID {0} lúc {1}", created.ID, created.TRACKING_TIME));
        return new TrackingMatchResult
        {
            TrackingId = created.ID,
            TrackingTime = created.TRACKING_TIME,
            IsNewlyCreated = true
        };
    }

    public static V_HIS_MEDICINE_TYPE FindMedicineWithStock(AdapterBase adp, CommonParam prm, string medKw, MedicineStockInfo stock)
    {
        if (string.IsNullOrEmpty(medKw)) throw new ArgumentNullException("medKw", "Tên thuốc không được rỗng!");
        string kw = medKw.Trim();
        long stockId = stock != null ? stock.MediStockId : 810;

        // 1. Quét tồn kho thực tế (Stock-Aware) trong kho/tủ trực
        if (stockId > 0)
        {
            try
            {
                List<V_HIS_MEDICINE_BEAN> beans = null;
                if (!stockBeanCache.TryGetValue(stockId, out beans) || beans == null)
                {
                    HisMedicineBeanViewFilter bf = new HisMedicineBeanViewFilter();
                    bf.MEDI_STOCK_ID = stockId;
                    bf.IS_ACTIVE = 1;
                    beans = ((MyAdapter)adp).FetchList<V_HIS_MEDICINE_BEAN>("api/HisMedicineBean/GetView", ApiConsumers.MosConsumer, bf, prm);
                    if (beans != null) stockBeanCache[stockId] = beans;
                }

                if (beans != null && beans.Count > 0)
                {
                    var activeBeans = beans.Where(b => b.AMOUNT > 0 && (
                        (b.MEDICINE_TYPE_NAME != null && b.MEDICINE_TYPE_NAME.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (b.MEDICINE_TYPE_CODE != null && b.MEDICINE_TYPE_CODE.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (b.ACTIVE_INGR_BHYT_NAME != null && b.ACTIVE_INGR_BHYT_NAME.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    )).ToList();

                    // Bí danh viết tắt Insulin lâm sàng: R -> Actrapid, L -> Lantus, M -> Mixtard
                    if (activeBeans.Count == 0)
                    {
                        string kwUpper = kw.ToUpper();
                        if (kwUpper == "R" || kwUpper.StartsWith("ACTR"))
                        {
                            activeBeans = beans.Where(b => b.AMOUNT > 0 && b.MEDICINE_TYPE_NAME != null && b.MEDICINE_TYPE_NAME.IndexOf("Actrapid", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                        }
                        else if (kwUpper == "L" || kwUpper.StartsWith("LANT"))
                        {
                            activeBeans = beans.Where(b => b.AMOUNT > 0 && b.MEDICINE_TYPE_NAME != null && b.MEDICINE_TYPE_NAME.IndexOf("Lantus", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                        }
                        else if (kwUpper == "M" || kwUpper.StartsWith("MIXT"))
                        {
                            activeBeans = beans.Where(b => b.AMOUNT > 0 && b.MEDICINE_TYPE_NAME != null && b.MEDICINE_TYPE_NAME.IndexOf("Mixtard", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                        }
                        else if (kwUpper.Contains("NACL") || kwUpper.Contains("NATRI") || kwUpper.Contains("MUOI"))
                        {
                            bool is500 = kwUpper.Contains("500");
                            bool is100 = kwUpper.Contains("100");
                            activeBeans = beans.Where(b => b.AMOUNT > 0 && (
                                (b.MEDICINE_TYPE_NAME != null && (b.MEDICINE_TYPE_NAME.IndexOf("Sodium Chloride", StringComparison.OrdinalIgnoreCase) >= 0 || b.MEDICINE_TYPE_NAME.IndexOf("Natri clorid", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                                (b.ACTIVE_INGR_BHYT_NAME != null && b.ACTIVE_INGR_BHYT_NAME.IndexOf("Natri", StringComparison.OrdinalIgnoreCase) >= 0)
                            )).ToList();
                            // Ưu tiên đường truyền tĩnh mạch (UseFormId = 20) trước muối rửa (UseFormId = 25)
                            var ivBeans = activeBeans.Where(b => b.MEDICINE_USE_FORM_ID == 20 || (b.MEDICINE_TYPE_NAME != null && b.MEDICINE_TYPE_NAME.IndexOf("Injection", StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                            if (ivBeans.Count > 0) activeBeans = ivBeans;

                            if (is500)
                            {
                                var b500 = activeBeans.Where(b => (b.MEDICINE_TYPE_NAME != null && b.MEDICINE_TYPE_NAME.Contains("500")) || (b.ACTIVE_INGR_BHYT_NAME != null && b.ACTIVE_INGR_BHYT_NAME.Contains("500"))).ToList();
                                if (b500.Count > 0) activeBeans = b500;
                            }
                            else if (is100)
                            {
                                var b100 = activeBeans.Where(b => (b.MEDICINE_TYPE_NAME != null && b.MEDICINE_TYPE_NAME.Contains("100")) || (b.ACTIVE_INGR_BHYT_NAME != null && b.ACTIVE_INGR_BHYT_NAME.Contains("100"))).ToList();
                                if (b100.Count > 0) activeBeans = b100;
                            }
                        }
                    }

                    if (activeBeans.Count > 0)
                    {
                        var bestGroup = activeBeans.GroupBy(b => b.MEDICINE_TYPE_ID)
                                                   .OrderByDescending(g => g.Sum(b => b.AMOUNT))
                                                   .First();
                        long bestTypeId = bestGroup.Key;
                        decimal totalStock = bestGroup.Sum(b => b.AMOUNT);
                        var sample = bestGroup.First();

                        Console.WriteLine(string.Format("  ✔ [STOCK-AWARE] Kho/Tủ {0}: Bốc đúng thuốc có tồn {1} ({2}), Tồn: {3:F4} (ID: {4})",
                            stockId, sample.MEDICINE_TYPE_NAME, sample.MEDICINE_TYPE_CODE, totalStock, bestTypeId));

                        HisMedicineTypeViewFilter mtf = new HisMedicineTypeViewFilter();
                        mtf.ID = bestTypeId;
                        var meds = ((MyAdapter)adp).FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", ApiConsumers.MosConsumer, mtf, prm);
                        if (meds != null && meds.Count > 0) return meds[0];

                        return new V_HIS_MEDICINE_TYPE
                        {
                            ID = sample.MEDICINE_TYPE_ID,
                            MEDICINE_TYPE_CODE = sample.MEDICINE_TYPE_CODE,
                            MEDICINE_TYPE_NAME = sample.MEDICINE_TYPE_NAME,
                            SERVICE_UNIT_NAME = sample.SERVICE_UNIT_NAME,
                            MEDICINE_USE_FORM_ID = sample.MEDICINE_USE_FORM_ID
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  ⚠️ [STOCK-AWARE] Lỗi kiểm tra tồn kho: " + ex.Message);
            }
        }

        // 2. Fallback tra cứu danh mục chung nếu không tìm thấy trong kho
        HisMedicineTypeViewFilter genFilter = new HisMedicineTypeViewFilter();
        genFilter.KEY_WORD = kw;
        genFilter.IS_ACTIVE = 1;
        var generalMeds = ((MyAdapter)adp).FetchList<V_HIS_MEDICINE_TYPE>("api/HisMedicineType/GetView", ApiConsumers.MosConsumer, genFilter, prm);
        if (generalMeds == null || generalMeds.Count == 0)
        {
            throw new Exception("Không tìm thấy thuốc khớp từ khóa: " + kw);
        }

        return generalMeds[0];
    }

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

                    // 1. Tự động đối soát và gắn trực tiếp vào tờ điều trị trong ngày để BS ký 1-click
                    var tkResult = MainForm.EnsureTrackingForPrescription(adapter, param, tr, CurrentDepartmentId, CurrentLoginName, CurrentUserName, null);
                    long trackingId = tkResult.TrackingId;
                    long trackingTime = tkResult.TrackingTime;

                    // 2. Tra cứu thuốc có tồn thực tế trong kho/tủ trực (Stock-Aware)
                    var targetMed = MainForm.FindMedicineWithStock(adapter, param, medKw, stock);

                    string code = MainForm.ExecutePrescription(adapter, param, CurrentLoginName, CurrentUserName, CurrentRoomId, tr, targetMed, stock, amount, tutorial, trackingTime, trackingId);
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

    public static string ReadLiveTokenFast()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1. Kiểm tra cache token độc lập của Bác sĩ (hạn 6 tiếng)
        try
        {
            string cacheFile = Path.Combine(baseDir, "doctor_standalone.token");
            if (!File.Exists(cacheFile))
            {
                string alt = Path.Combine(@"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB", "doctor_standalone.token");
                if (File.Exists(alt)) cacheFile = alt;
            }
            if (File.Exists(cacheFile))
            {
                string[] parts = File.ReadAllText(cacheFile, Encoding.UTF8).Split('|');
                if (parts.Length >= 2)
                {
                    long savedTime;
                    if (long.TryParse(parts[1], out savedTime))
                    {
                        DateTime savedDt = new DateTime(savedTime);
                        if ((DateTime.Now - savedDt).TotalHours < 6.0 && parts[0].Length == 64)
                        {
                            return parts[0];
                        }
                    }
                }
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
        // CHẾ ĐỘ --help / Hướng dẫn sử dụng CLI
        // ──────────────────────────────────────────────────────────
        if (args != null && args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "/?" || args[0] == "help"))
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine("  HIS AUTO PRESCRIBE - HUONG DAN SU DUNG (USAGE)");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("Usage:");
            Console.WriteLine("  1. GUI Mode (Khong truyen tham so):");
            Console.WriteLine("     HisAutoPrescribe.exe");
            Console.WriteLine("");
            Console.WriteLine("  2. Batch Mode (Ke don hang loat tu file CSV):");
            Console.WriteLine("     HisAutoPrescribe.exe --batch <duong_dan_file.csv> [user] [pass]");
            Console.WriteLine("     HisAutoPrescribe.exe -b <duong_dan_file.csv> [user] [pass]");
            Console.WriteLine("");
            Console.WriteLine("  3. Single Mode (Ke don truc tiep tu command line):");
            Console.WriteLine("     HisAutoPrescribe.exe single <MaBN> <SoLuong> <TenThuoc> [CachDung] [user] [pass]");
            Console.WriteLine("     HisAutoPrescribe.exe <MaBN> <SoLuong> <TenThuoc> [CachDung] [user] [pass]");
            Console.WriteLine("===============================================================================");
            return;
        }

        // ──────────────────────────────────────────────────────────
        // CHẾ ĐỘ --batch: Đọc file CSV hàng loạt, kê đơn Insulin
        // CSV format: patient_code,dose,medicine,tutorial,time,date
        // VD: HisAutoPrescribe.exe --batch insulin_orders.csv
        // ──────────────────────────────────────────────────────────
        if (args != null && args.Length >= 1 && (args[0] == "--batch" || args[0] == "-b"))
        {
            if (args.Length < 2)
            {
                Console.WriteLine("❌ Thiếu đường dẫn file CSV cho chế độ --batch!");
                Console.WriteLine("Usage: HisAutoPrescribe.exe --batch <duong_dan_file.csv> [user] [pass]");
                Environment.ExitCode = 1;
                return;
            }

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
                Environment.ExitCode = 1;
                return;
            }

            // Đọc & parse CSV
            var csvLines = File.ReadAllLines(csvPath, Encoding.UTF8)
                               .Skip(1) // bỏ header
                               .Where(l => !string.IsNullOrWhiteSpace(l))
                               .ToList();

            Console.WriteLine(string.Format("• Số lệnh: {0}", csvLines.Count));
            Console.WriteLine("-------------------------------------------------------------------------------");

            if (csvLines.Count == 0) { Console.WriteLine("⚠ File CSV không có dữ liệu!"); Environment.ExitCode = 1; return; }

            try
            {
                HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
                CommonParam bp = new CommonParam();
                string bToken = ReadLiveTokenFast();

                // 2. Fallback: login qua ACS nếu không có live token
                if (string.IsNullOrEmpty(bToken))
                {
                    ClientTokenManager btm = new ClientTokenManager("HIS");
                    var btok = btm.Login(bp, "034727", "998199", "2.390.0");
                    if (btok == null) btok = btm.Login(bp, batchUser, batchPass, "2.390.0");
                    if (btok != null)
                    {
                        bToken = btok.TokenCode;
                        try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token"), bToken + "|" + DateTime.Now.Ticks + "|034727", Encoding.UTF8); } catch { }
                    }
                }

                if (string.IsNullOrEmpty(bToken)) { Console.WriteLine("❌ Không lấy được Token!"); Environment.ExitCode = 1; return; }
                ApiConsumers.SetConsunmer(bToken);
                MyAdapter bad = new MyAdapter();

                // 3. Kích hoạt WorkInfo phòng làm việc Khoa 57
                try
                {
                    long[] dept57Rooms = new long[] {
                        931, 5248, 5249, 5250, 5251, 5252, 5253, 5254, 5255, 5256, 
                        5257, 5258, 5259, 5260, 5261, 5262, 5263, 5264, 5265, 5266, 
                        5267, 6622, 6623
                    };
                    var workInfo = new WorkInfoSDO { Rooms = dept57Rooms.Select(r => new RoomSDO { RoomId = r }).ToList() };
                    bad.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, bp);
                }
                catch { }

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
                        // 1. Tra cứu bệnh nhân: Mặc định ưu tiên đối chiếu BN đang điều trị tại Khoa 57
                        if (!treatmentCache.ContainsKey(bPatKey))
                        {
                            HisTreatmentViewFilter btf = new HisTreatmentViewFilter();
                            btf.KEY_WORD = bPatKey;
                            var btrs = bad.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, btf, bp);
                            if (btrs == null || btrs.Count == 0) throw new Exception("Không tìm thấy BN: " + bPatKey);

                            // Ưu tiên hồ sơ bệnh nhân đang điều trị nội trú tại Khoa 57 hoặc Khoa 915 (CSNB)
                            var deptTr = btrs.Where(t => (t.END_DEPARTMENT_ID == 57 || t.END_DEPARTMENT_ID == 915 || t.BRANCH_ID == 81) && (!t.OUT_TIME.HasValue || t.OUT_TIME == 0))
                                             .OrderByDescending(t => t.IN_TIME).FirstOrDefault();
                            if (deptTr == null)
                            {
                                deptTr = btrs.Where(t => !t.OUT_TIME.HasValue || t.OUT_TIME == 0)
                                             .OrderByDescending(t => t.IN_TIME).FirstOrDefault();
                            }
                            if (deptTr == null)
                            {
                                deptTr = btrs.OrderByDescending(t => t.IN_TIME).First();
                            }

                            treatmentCache[bPatKey] = deptTr;
                        }
                        var btr = treatmentCache[bPatKey];

                        bool isNB = (btr.BRANCH_ID == 81 || btr.END_DEPARTMENT_ID == 915);
                        long targetDeptId = isNB ? 915 : 57;
                        long targetRoomId = isNB ? 18679 : 5248;
                        MedicineStockInfo targetStock = isNB ? (MainForm.CommonStocks.FirstOrDefault(s => s.MediStockId == 5142) ?? MainForm.CommonStocks[0]) : MainForm.CommonStocks[0];

                        // 2. Tìm hoặc tạo tờ điều trị cùng ngày và gán y lệnh trực tiếp để BS ký 1-click
                        var tkResult = MainForm.EnsureTrackingForPrescription(bad, bp, btr, targetDeptId, batchUser, batchUser.ToUpper(), instructionTime);
                        long bTkId   = tkResult.TrackingId;
                        long bTkTime = tkResult.TrackingTime;

                        // 3. Tra cứu thuốc tồn thực tế trong kho/tủ trực (Stock-Aware)
                        string medKey = bMedKw + "_" + targetStock.MediStockId;
                        if (!medicineCache.ContainsKey(medKey))
                        {
                            medicineCache[medKey] = MainForm.FindMedicineWithStock(bad, bp, bMedKw, targetStock);
                        }
                        var bMed = medicineCache[medKey];

                        // 4. Tạo đơn thuốc nội trú từ Kho Tủ Trực (810 cho HN hoặc 5142 cho NB)
                        string bCode = MainForm.ExecutePrescription(bad, bp, batchUser, batchUser.ToUpper(), targetRoomId, btr, bMed, targetStock, bAmount, bTut, bTkTime, bTkId);

                        string facTag = isNB ? "CS Ninh Bình" : "Khoa 57 HN";
                        Console.WriteLine(string.Format("  ✔ [{0}] {1} | {2} {3} đv | {4} | Mã Y Lệnh: {5} [{6} - {7}]",
                            bPatKey, btr.TDL_PATIENT_NAME, bMed.MEDICINE_TYPE_NAME, bAmount, bTime, bCode, facTag, targetStock.MediStockName));
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
                if (bFail > 0 && bSucc == 0)
                {
                    Environment.ExitCode = 1;
                }
            }
            catch (Exception batchEx)
            {
                Console.WriteLine("❌ Lỗi nghiêm trọng batch mode: " + batchEx.Message);
                Environment.ExitCode = 1;
            }
            return;
        }

        bool isSingleExplicit = args != null && args.Length > 0 && args[0].Equals("single", StringComparison.OrdinalIgnoreCase);
        int argOffset = isSingleExplicit ? 1 : 0;
        int remainingArgs = (args != null) ? args.Length - argOffset : 0;

        if (isSingleExplicit || remainingArgs >= 3)
        {
            if (remainingArgs < 3)
            {
                Console.WriteLine("❌ Thiếu tham số cho chế độ kê đơn đơn lẻ (single)!");
                Console.WriteLine("Usage: HisAutoPrescribe.exe single <MaBN> <SoLuong> <TenThuoc> [CachDung] [user] [pass]");
                Environment.ExitCode = 1;
                return;
            }

            string patKey = args[argOffset];
            decimal amount = 1;
            decimal.TryParse(args[argOffset + 1], out amount);
            string medKw = args[argOffset + 2];
            string tut = remainingArgs > 3 ? args[argOffset + 3] : "Theo chỉ dẫn bác sĩ";
            string user = remainingArgs > 4 ? args[argOffset + 4] : "034727";
            string pass = remainingArgs > 5 ? args[argOffset + 5] : "998199";

            Console.WriteLine(string.Format(">>> CLI AUTO PRESCRIBE: BS {0} | BN: {1} | Thuốc: {2} | Liều: {3}", user, patKey, medKw, amount));
            try
            {
                HIS.Desktop.LocalStorage.ConfigSystem.Load.Init();
                string sToken = ReadLiveTokenFast();
                CommonParam p = new CommonParam();
                if (string.IsNullOrEmpty(sToken))
                {
                    ClientTokenManager tm = new ClientTokenManager("HIS");
                    var tok = tm.Login(p, user, pass, "2.390.0");
                    if (tok == null && user == "034727") tok = tm.Login(p, "vmc", "789789", "2.390.0");
                    if (tok != null)
                    {
                        sToken = tok.TokenCode;
                        try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_standalone.token"), sToken + "|" + DateTime.Now.Ticks + "|" + user, Encoding.UTF8); } catch { }
                    }
                }
                if (string.IsNullOrEmpty(sToken)) { Console.WriteLine("❌ Đăng nhập và tìm token thất bại!"); Environment.ExitCode = 1; return; }
                ApiConsumers.SetConsunmer(sToken);
                MyAdapter ad = new MyAdapter();

                try
                {
                    var workInfo = new WorkInfoSDO
                    {
                        Rooms = new List<RoomSDO>
                        {
                            new RoomSDO { RoomId = 5248 },
                            new RoomSDO { RoomId = 5252 },
                            new RoomSDO { RoomId = 5251 },
                            new RoomSDO { RoomId = 5257 }
                        }
                    };
                    ad.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", ApiConsumers.MosConsumer, workInfo, p);
                }
                catch { }

                long pNum;
                if (string.IsNullOrWhiteSpace(patKey) || (long.TryParse(patKey, out pNum) && pNum <= 0))
                {
                    Console.WriteLine("❌ Mã bệnh nhân không hợp lệ: " + patKey);
                    Environment.ExitCode = 1;
                    return;
                }

                HisTreatmentViewFilter tf = new HisTreatmentViewFilter();
                long pCodeNum;
                if (long.TryParse(patKey, out pCodeNum))
                {
                    tf.PATIENT_CODE__EXACT = patKey.PadLeft(10, '0');
                }
                else
                {
                    tf.KEY_WORD = patKey;
                }
                var trs = ad.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, p);
                if ((trs == null || trs.Count == 0) && long.TryParse(patKey, out pCodeNum))
                {
                    tf = new HisTreatmentViewFilter();
                    tf.KEY_WORD = patKey;
                    trs = ad.FetchList<V_HIS_TREATMENT>("api/HisTreatment/GetView", ApiConsumers.MosConsumer, tf, p);
                }
                if (trs == null || trs.Count == 0) { Console.WriteLine("❌ Không tìm thấy bệnh nhân: " + patKey); Environment.ExitCode = 1; return; }
                var tr = trs.Where(t => t.END_DEPARTMENT_ID == 57 && (!t.OUT_TIME.HasValue || t.OUT_TIME == 0)).OrderByDescending(t => t.IN_TIME).FirstOrDefault() 
                         ?? trs.OrderByDescending(t => t.IN_TIME).First();

                Console.WriteLine(string.Format("  ✔ BN: {0} ({1}) | Mã ĐT: {2} | Khoa: {3}",
                    tr.TDL_PATIENT_NAME, tr.TDL_PATIENT_CODE, tr.TREATMENT_CODE, tr.END_DEPARTMENT_NAME ?? "Khoa 57"));

                // 2. Tìm hoặc tạo tờ điều trị cùng ngày và gán y lệnh trực tiếp để BS ký 1-click
                var tkResult = MainForm.EnsureTrackingForPrescription(ad, p, tr, 57, user, user.ToUpper(), null);
                long tkId = tkResult.TrackingId;
                long tkTime = tkResult.TrackingTime;

                // 3. Tra cứu thuốc tồn thực tế trong tủ trực (Stock-Aware)
                var med = MainForm.FindMedicineWithStock(ad, p, medKw, MainForm.CommonStocks[0]);

                string code = MainForm.ExecutePrescription(ad, p, user, user.ToUpper(), 5248, tr, med, MainForm.CommonStocks[0], amount, tut, tkTime, tkId);
                Console.WriteLine(string.Format("✔ Kê đơn thành công! Mã y lệnh / phiếu xuất: {0}", code));
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi: " + ex.Message);
                Environment.ExitCode = 1;
            }
            return;
        }

        if (args != null && args.Length > 0)
        {
            Console.WriteLine("❌ Tham số không hợp lệ: " + string.Join(" ", args));
            Console.WriteLine("Sử dụng 'HisAutoPrescribe.exe --help' để xem hướng dẫn cú pháp.");
            Environment.ExitCode = 1;
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
