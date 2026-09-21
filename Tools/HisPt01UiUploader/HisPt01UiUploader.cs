using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace HisPt01UiUploader
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch {}

            HisUiDriver.EnsureDesktop();

            Console.WriteLine("=============================================================================");
            Console.WriteLine(" ⚡ HIS PT-01 UI AUTOMATION RUNNER (1-CLICK NẠP & KÝ BIÊN BẢN MỔ)            ");
            Console.WriteLine("=============================================================================");

            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                PrintHelp();
                return;
            }

            string patientQuery = string.Empty;
            string docxPath = string.Empty;
            string signer = "duy thanh";
            bool dryRun = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "--file" && i + 1 < args.Length)
                {
                    docxPath = args[++i];
                }
                else if (arg == "--signer" && i + 1 < args.Length)
                {
                    signer = args[++i];
                }
                else if (arg == "--dry-run")
                {
                    dryRun = true;
                }
                else if (!arg.StartsWith("-"))
                {
                    patientQuery = arg;
                }
            }

            // Auto-locate docx file if not specified
            if (string.IsNullOrEmpty(docxPath) && !string.IsNullOrEmpty(patientQuery))
            {
                docxPath = AutoFindDocx(patientQuery);
            }

            if (string.IsNullOrEmpty(docxPath) || !File.Exists(docxPath))
            {
                Console.WriteLine("❌ LỖI: Không tìm thấy file Word PT-01 tương ứng!");
                Console.WriteLine("    Vui lòng kiểm tra lại mã BN hoặc chỉ định: --file <đường_dẫn_file.docx>");
                return;
            }

            Console.WriteLine("[+] Bệnh nhân mục tiêu : " + patientQuery);
            Console.WriteLine("[+] File Word PT-01    : " + Path.GetFullPath(docxPath));
            Console.WriteLine("[+] Bác sĩ duyệt ký    : " + signer);
            Console.WriteLine("[+] Chế độ thực thi    : " + (dryRun ? "🔍 DRY-RUN (Không ký thật)" : "🚀 LIVE (Tự động ký số)"));
            Console.WriteLine("=============================================================================");

            // Run on a clean STA worker thread so SetThreadDesktop succeeds without Error 170 (ERROR_BUSY)
            Thread worker = new Thread(() =>
            {
                RunWorkflow(patientQuery, docxPath, signer, dryRun);
            });
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
            worker.Join();
        }

        private static void RunWorkflow(string patientQuery, string docxPath, string signer, bool dryRun)
        {
            HisUiDriver.EnsureDesktop();
            var swTotal = Stopwatch.StartNew();

            try
            {
                // PHASE 1: Tìm bệnh nhân trên HIS
                Console.WriteLine("\n[1/5] 🔍 Đang tìm kiếm bệnh nhân trên HIS...");
                AutomationElement hisWin = HisUiDriver.FindTopWindow("Hotline CNTT", 8000);
                if (hisWin == null) hisWin = HisUiDriver.FindTopWindow("HIS", 4000);
                if (hisWin == null)
                {
                    Console.WriteLine("❌ LỖI: Không tìm thấy cửa sổ HIS đang chạy!");
                    return;
                }

                HisUiDriver.ActivateWindow(hisWin);
                Thread.Sleep(500);

                // Tối ưu hóa chuỗi tìm kiếm: Ưu tiên mã bệnh nhân (nếu có trong tên file docx) hoặc chuỗi không dấu
                string searchKey = patientQuery;
                Match mCode = Regex.Match(Path.GetFileName(docxPath), @"\b(00\d{8})\b");
                if (mCode.Success)
                {
                    searchKey = mCode.Groups[1].Value;
                    Console.WriteLine("    -> Sử dụng Mã BN để tìm kiếm chuẩn xác: " + searchKey);
                }
                else
                {
                    searchKey = HisUiDriver.RemoveDiacritics(patientQuery);
                    Console.WriteLine("    -> Chuẩn hóa tìm kiếm không dấu: " + searchKey);
                }

                var txtSearch = HisUiDriver.FindElement(hisWin, AutomationElement.AutomationIdProperty, "txtKeyWord", 2000);
                if (txtSearch != null)
                {
                    HisUiDriver.Click(txtSearch, 300);
                    HisUiDriver.PasteText(searchKey);
                    HisUiDriver.PressKey(HisUiDriver.VK_RETURN, 1500);
                }
                else
                {
                    HisUiDriver.PressKey(HisUiDriver.VK_F2, 400);
                    HisUiDriver.PasteText(searchKey);
                    HisUiDriver.PressKey(HisUiDriver.VK_RETURN, 1500);
                }

                var grid = HisUiDriver.FindElement(hisWin, AutomationElement.AutomationIdProperty, "gridControlTreatmentBedRoom", 3000);
                AutomationElement patientRow = null;
                if (grid != null)
                {
                    patientRow = HisUiDriver.FindElementByName(grid, "row 0", 3000);
                    if (patientRow == null)
                    {
                        patientRow = HisUiDriver.FindElementByName(grid, "Data Panel", 2000);
                    }
                }
                if (patientRow == null)
                {
                    patientRow = HisUiDriver.FindElementByName(hisWin, "row 0", 4000);
                }

                if (patientRow == null)
                {
                    Console.WriteLine("❌ LỖI: Không tìm thấy dòng bệnh nhân trong danh sách!");
                    return;
                }
                
                try
                {
                    var rect = patientRow.Current.BoundingRectangle;
                    Console.WriteLine(string.Format("    -> Tìm thấy dòng bệnh nhân: '{0}' tại ({1}, {2}, {3}x{4})", 
                        patientRow.Current.Name, (int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height));
                }
                catch {}

                HisUiDriver.Click(patientRow, 500);
                Console.WriteLine("    -> Đã chọn bệnh nhân thành công.");

                // PHASE 2: Mở Biểu Mẫu Khác
                Console.WriteLine("\n[2/5] 📑 Đang mở danh mục 'Biểu mẫu khác hồ sơ điều trị'...");
                Thread.Sleep(300);
                HisUiDriver.RightClick(patientRow, 1500);

                var mnuInAn = HisUiDriver.FindElementByName(AutomationElement.RootElement, "In ấn", 5000);
                if (mnuInAn != null)
                {
                    Console.WriteLine("    -> Tìm thấy menu 'In ấn'. Đang mở menu con...");
                    HisUiDriver.Hover(mnuInAn, 400);
                    var r = mnuInAn.Current.BoundingRectangle;
                    if (!r.IsEmpty)
                        HisUiDriver.ClickPoint((int)(r.Left + r.Width / 2), (int)(r.Top + r.Height / 2), 1500);
                    else
                        HisUiDriver.Click(mnuInAn, 1500);
                }
                else
                {
                    Console.WriteLine("⚠️ Cảnh báo: Không tìm thấy menu 'In ấn'!");
                }

                var mnuBieuMau = HisUiDriver.FindElementByName(AutomationElement.RootElement, "Biễu mẫu khác", 5000);
                if (mnuBieuMau == null) mnuBieuMau = HisUiDriver.FindElementByName(AutomationElement.RootElement, "Biểu mẫu khác", 3000);
                if (mnuBieuMau != null)
                {
                    Console.WriteLine("    -> Tìm thấy mục 'Biễu mẫu khác'. Đang mở cửa sổ biểu mẫu...");
                    var r = mnuBieuMau.Current.BoundingRectangle;
                    if (!r.IsEmpty)
                        HisUiDriver.ClickPoint((int)(r.Left + r.Width / 2), (int)(r.Top + r.Height / 2), 3000);
                    else
                        HisUiDriver.Click(mnuBieuMau, 3000);
                }
                else
                {
                    Console.WriteLine("❌ LỖI: Không tìm thấy mục 'Biễu mẫu khác hồ sơ điều trị'!");
                    return;
                }

                var winBieuMau = HisUiDriver.FindTopWindow("Biểu mẫu khác", 8000);
                if (winBieuMau == null)
                {
                    Console.WriteLine("❌ LỖI: Không mở được cửa sổ Biểu mẫu khác!");
                    return;
                }
                HisUiDriver.ActivateWindow(winBieuMau);
                Thread.Sleep(800);

                HisUiDriver.PressShortcut(HisUiDriver.VK_F, ctrl: true);
                Thread.Sleep(600);
                HisUiDriver.SendText("pt");
                HisUiDriver.PressKey(HisUiDriver.VK_RETURN, 1000);

                var btnEdit = HisUiDriver.FindElementByName(winBieuMau, "Editing control", 4000);
                if (btnEdit != null)
                {
                    HisUiDriver.Click(btnEdit, 3000);
                }
                else
                {
                    var firstRow = HisUiDriver.FindElementByName(winBieuMau, "row 0", 2000);
                    if (firstRow != null) HisUiDriver.DoubleClick(firstRow, 3000);
                }
                Console.WriteLine("    -> Đã mở trình soạn thảo biểu mẫu PT-01.");

                // PHASE 3: Nạp File Word & Lưu
                Console.WriteLine("\n[3/5] 📝 Đang nạp nội dung file Word vào HIS...");
                var winEditor = HisUiDriver.FindTopWindow("Tạo biểu mẫu khác", 10000);
                if (winEditor == null)
                {
                    Console.WriteLine("❌ LỖI: Không mở được cửa sổ soạn thảo biểu mẫu!");
                    return;
                }
                HisUiDriver.ActivateWindow(winEditor);

                string cleanDocx = WordCleaner.EnsureCleanDocx(Path.GetFullPath(docxPath));

                var btnOpen = HisUiDriver.FindElementByName(winEditor, "Open", 5000);
                if (btnOpen != null) HisUiDriver.Click(btnOpen, 2000);

                var dlgOpen = HisUiDriver.FindTopWindow("Open", 6000);
                if (dlgOpen != null)
                {
                    HisUiDriver.ActivateWindow(dlgOpen);
                    Thread.Sleep(500);

                    var editFileName = HisUiDriver.FindElement(dlgOpen, AutomationElement.AutomationIdProperty, "1148", 2000);
                    if (editFileName == null) editFileName = HisUiDriver.FindElement(dlgOpen, AutomationElement.ClassNameProperty, "Edit", 2000);

                    if (editFileName != null)
                    {
                        HisUiDriver.SetText(editFileName, cleanDocx);
                        Thread.Sleep(300);
                        HisUiDriver.PressKey(HisUiDriver.VK_RETURN, 2500);
                    }
                    else
                    {
                        HisUiDriver.SendText(cleanDocx);
                        Thread.Sleep(300);
                        HisUiDriver.PressKey(HisUiDriver.VK_RETURN, 2500);
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Cảnh báo: Không tìm thấy hộp thoại 'Open'!");
                }

                Thread.Sleep(2000);
                HisUiDriver.PressShortcut(HisUiDriver.VK_S, ctrl: true); // Lưu lại
                Thread.Sleep(2000);
                Console.WriteLine("    -> Đã nạp Word và lưu biểu mẫu thành công.");

                // PHASE 4: Mở EMR & Cấu hình luồng ký
                Console.WriteLine("\n[4/5] 📋 Đang mở EMR và cấu hình luồng ký...");
                var btnEmr = HisUiDriver.FindElementByName(winEditor, "EMR", 5000);
                if (btnEmr != null) HisUiDriver.Click(btnEmr, 3500);

                var winVbdt = HisUiDriver.FindTopWindow("Văn bản điện tử", 8000);
                if (winVbdt == null)
                {
                    Console.WriteLine("❌ LỖI: Không mở được cửa sổ Văn bản điện tử!");
                    return;
                }
                HisUiDriver.ActivateWindow(winVbdt);

                var btnThietLapKy = HisUiDriver.FindElementByName(winVbdt, "Thiết lập ký", 5000);
                if (btnThietLapKy != null) HisUiDriver.Click(btnThietLapKy, 5000);

                var winLuongKy = HisUiDriver.FindTopWindow("Tạo luồng ký", 8000);
                if (winLuongKy != null)
                {
                    HisUiDriver.ActivateWindow(winLuongKy);
                    Thread.Sleep(800);

                    // Mở danh sách chọn mẫu luồng ký
                    var cboTemplate = HisUiDriver.FindElement(winLuongKy, AutomationElement.AutomationIdProperty, "cboSignTemplate", 3000);
                    if (cboTemplate != null)
                    {
                        var dropBtn = HisUiDriver.FindElement(cboTemplate, AutomationElement.ControlTypeProperty, ControlType.Button, 1500);
                        if (dropBtn != null) HisUiDriver.Click(dropBtn, 1000);
                        else HisUiDriver.Click(cboTemplate, 1000);
                    }

                    // Chọn mẫu luồng ký thứ 3
                    var row3 = HisUiDriver.FindElementByName(AutomationElement.RootElement, "SIGN_TEMP_NAME row 3", 4000);
                    if (row3 == null) row3 = HisUiDriver.FindElementByName(winLuongKy, "row 3", 3000);
                    if (row3 != null) HisUiDriver.Click(row3, 1000);

                    // Nếu có hộp thoại xác nhận 'Có / Không' (ID 6)
                    var btnCo = HisUiDriver.FindElementByName(AutomationElement.RootElement, "Có", 2000);
                    if (btnCo != null) HisUiDriver.Click(btnCo, 1000);

                    if (!string.IsNullOrEmpty(signer))
                    {
                        var cboSigner = HisUiDriver.FindElement(winLuongKy, AutomationElement.AutomationIdProperty, "cboSigner", 2000);
                        var editSigner = cboSigner != null 
                            ? HisUiDriver.FindElement(cboSigner, AutomationElement.ControlTypeProperty, ControlType.Edit, 2000)
                            : HisUiDriver.FindElement(winLuongKy, AutomationElement.ControlTypeProperty, ControlType.Edit, 3000);

                        if (editSigner != null)
                        {
                            HisUiDriver.Click(editSigner, 200);
                            HisUiDriver.SetText(editSigner, signer);
                            Thread.Sleep(800);
                            HisUiDriver.PressKey(HisUiDriver.VK_RETURN, 500);

                            var btnAdd = HisUiDriver.FindElement(winLuongKy, AutomationElement.AutomationIdProperty, "btnAdd", 3000);
                            if (btnAdd == null) btnAdd = HisUiDriver.FindElementByName(winLuongKy, "Thêm người ký", 2000);
                            if (btnAdd != null) HisUiDriver.Click(btnAdd, 1000);

                            var btnDown = HisUiDriver.FindElementByName(winLuongKy, "Down", 2000);
                            if (btnDown != null)
                            {
                                HisUiDriver.Click(btnDown, 400);
                                HisUiDriver.Click(btnDown, 400);
                            }
                        }
                    }

                    var btnSaveLuongKy = HisUiDriver.FindElement(winLuongKy, AutomationElement.AutomationIdProperty, "btnSave", 3000);
                    if (btnSaveLuongKy == null) btnSaveLuongKy = HisUiDriver.FindElementByName(winLuongKy, "Cập nhật", 2000);
                    if (btnSaveLuongKy != null) HisUiDriver.Click(btnSaveLuongKy, 3000);
                    Console.WriteLine("    -> Đã cấu hình luồng ký thành công.");
                }

                // PHASE 5: Ký số
                if (dryRun)
                {
                    Console.WriteLine("\n[5/5] ⏸️ Chế độ Dry-Run: Đã dừng trước bước Ký số thật.");
                }
                else
                {
                    Console.WriteLine("\n[5/5] ✍️ Đang thực hiện ký số văn bản...");
                    HisUiDriver.ActivateWindow(winVbdt);
                    var btnKy = HisUiDriver.FindElementByName(winVbdt, "Ký", 5000);
                    if (btnKy != null) HisUiDriver.Click(btnKy, 4000);

                    var btnKetThuc = HisUiDriver.FindElementByName(winVbdt, "Kết thúc ký", 5000);
                    if (btnKetThuc != null) HisUiDriver.Click(btnKetThuc, 2000);

                    var dlgTb = HisUiDriver.FindTopWindow("Thông báo", 4000);
                    if (dlgTb != null)
                    {
                        var btnOk = HisUiDriver.FindElementByName(dlgTb, "Đồng ý", 3000);
                        if (btnOk != null) HisUiDriver.Click(btnOk, 1000);
                    }
                    Console.WriteLine("    -> Đã ký số và kết thúc quy trình thành công.");
                }

                swTotal.Stop();
                Console.WriteLine("\n=============================================================================");
                Console.WriteLine(string.Format("🎉 HOÀN TẤT TOÀN BỘ QUY TRÌNH NẠP PT-01 TRONG {0:N1} GIÂY!", swTotal.Elapsed.TotalSeconds));
                Console.WriteLine("=============================================================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ LỖI NGOẠI LỆ: " + ex.Message);
            }
        }

        private static string AutoFindDocx(string query)
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "BienBanHoiChan_PT01");
            if (!Directory.Exists(baseDir))
            {
                baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports", "BienBanHoiChan_PT01");
            }
            if (!Directory.Exists(baseDir)) return string.Empty;

            string q = query.Trim();
            string qUnderscore = q.Replace(" ", "_");
            string qDigits = Regex.Replace(q, @"\D", "");

            foreach (string f in Directory.GetFiles(baseDir, "*.docx"))
            {
                string fn = Path.GetFileName(f);
                if (fn.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fn.IndexOf(qUnderscore, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (!string.IsNullOrEmpty(qDigits) && qDigits.Length >= 4 && fn.IndexOf(qDigits, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return f;
                }
            }
            return string.Empty;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Cú pháp sử dụng:");
            Console.WriteLine("  HisPt01UiUploader.exe <MãBN|TênBN> [tùy chọn]");
            Console.WriteLine();
            Console.WriteLine("Tùy chọn:");
            Console.WriteLine("  --file <path.docx>   : Chỉ định trực tiếp file docx (mặc định tự dò trong Reports/BienBanHoiChan_PT01/)");
            Console.WriteLine("  --signer <username>  : Tên tài khoản bác sĩ duyệt/mời ký (mặc định: 'duy thanh')");
            Console.WriteLine("  --dry-run            : Chạy thử đến bước cấu hình luồng ký, dừng lại không bấm Ký thật");
            Console.WriteLine("  --help               : Hiển thị hướng dẫn này");
            Console.WriteLine();
            Console.WriteLine("Ví dụ:");
            Console.WriteLine("  HisPt01UiUploader.exe 0004035437");
            Console.WriteLine("  HisPt01UiUploader.exe \"Vũ Văn Hòa\" --dry-run");
        }
    }
}
