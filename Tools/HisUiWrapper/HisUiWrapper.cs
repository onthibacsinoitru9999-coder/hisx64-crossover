using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HisUiWrapper
{
    public class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch {}

            // 1. Ensure connected to interactive user desktop
            try
            {
                IntPtr hDesk = OpenDesktop("Default", 0, false, 0x01FF);
                if (hDesk != IntPtr.Zero)
                {
                    SetThreadDesktop(hDesk);
                }
            }
            catch {}

            // 2. Parse command arguments
            bool launchHis = false;
            bool consoleMode = false;
            bool recordAll = false;
            uint explicitPid = 0;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLower();
                if (a == "--launch" || a == "-l") launchHis = true;
                else if (a == "--console" || a == "-c") consoleMode = true;
                else if (a == "--all" || a == "-a") recordAll = true;
                else if (a == "--pid" || a == "-p")
                {
                    if (i + 1 < args.Length && uint.TryParse(args[i + 1], out explicitPid))
                    {
                        i++;
                    }
                }
                else if (a == "--help" || a == "-h")
                {
                    PrintHelp();
                    return;
                }
            }

            Console.WriteLine("=============================================================================");
            Console.WriteLine(" ⚡ HIS & EMR UI INTERACTION WRAPPER (LEARN CLINICAL UI WORKFLOW)            ");
            Console.WriteLine("=============================================================================");

            // 3. Check / Launch HIS
            string hisPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HIS.exe");
            if (!File.Exists(hisPath))
            {
                hisPath = Path.GetFullPath("HIS.exe");
            }

            Process hisProcess = null;
            if (launchHis)
            {
                if (File.Exists(hisPath))
                {
                    Console.WriteLine("[+] Khởi động phần mềm HIS từ: " + hisPath);
                    try
                    {
                        hisProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = hisPath,
                            WorkingDirectory = Path.GetDirectoryName(hisPath)
                        });
                        Console.WriteLine("[+] HIS.exe đã khởi chạy (PID: " + hisProcess.Id + ")");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[!] Lỗi khởi động HIS: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("[!] Không tìm thấy HIS.exe tại: " + hisPath);
                }
            }

            // 4. Initialize Hook Engine & Session Writer
            var engine = new HisUiHookEngine();
            var writer = new HisUiSessionWriter();

            if (recordAll)
            {
                engine.RecordAllApps = true;
                Console.WriteLine("[*] Chế độ: Ghi nhận TẤT CẢ ứng dụng trên màn hình (--all)");
            }
            else if (explicitPid > 0)
            {
                engine.AddTargetPid(explicitPid);
                Console.WriteLine("[+] Đã gán PID mục tiêu: " + explicitPid);
            }
            else
            {
                engine.RefreshHisPids();
                if (engine.TargetPids.Count > 0)
                {
                    var pList = new List<string>();
                    foreach (uint pid in engine.TargetPids)
                    {
                        string pName = HisUiaInspector.GetProcessNameByPid(pid);
                        pList.Add(string.Format("{0} (PID: {1})", pName, pid));
                    }
                    Console.WriteLine("[+] Đã tự động kết nối vào các tiến trình HIS / EMR: " + string.Join(", ", pList.ToArray()));
                }
                else
                {
                    Console.WriteLine("[?] Chưa thấy HIS hoặc EMR đang chạy. Đang ở chế độ lắng nghe sẵn sàng (khi mở HIS/EMR sẽ tự bắt)...");
                }
            }

            Console.WriteLine("[+] Tệp nhật ký JSONL: " + writer.JsonlPath);
            Console.WriteLine("[+] Hỗ trợ ghi nhận liền mạch từ HIS sang EMR:");
            Console.WriteLine("    - Khi mở EMR từ HIS: Bộ ghi tự động nhận diện và ghi nhận tiếp tục");
            Console.WriteLine("[+] Phím tắt điều khiển:");
            Console.WriteLine("    - F9 : Tạm dừng / Tiếp tục ghi (Pause / Resume)");
            Console.WriteLine("    - F10: Đánh dấu mốc thao tác (Add Checkpoint Note)");
            Console.WriteLine("    - F11: Hoàn tất phiên và xuất báo cáo quy trình (Finish & Export)");
            Console.WriteLine("=============================================================================");

            engine.Start();

            engine.OnProcessDiscovered += (pName, pId) =>
            {
                Console.WriteLine(string.Format("[*] TỰ ĐỘNG PHÁT HIỆN TIẾN TRÌNH MỚI: {0} (PID: {1})", pName, pId));
            };

            // Print real-time console feed
            engine.OnActionRecorded += ev =>
            {
                Console.WriteLine(string.Format("[{0:HH:mm:ss.fff}] #{1} {2}", ev.Timestamp, ev.StepIndex, ev.ToString()));
            };

            engine.OnRecordingStateChanged += rec =>
            {
                Console.WriteLine(rec ? ">>> [REC] ĐANG TIẾP TỤC GHI NHẬN <<<" : ">>> [PAUSED] ĐÃ TẠM DỪNG GHI NHẬN <<<");
            };

            // 5. Run GUI HUD or Console Loop
            if (!consoleMode)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var hud = new HisFloatingHud(engine, writer);
                Application.Run(hud);
            }
            else
            {
                Console.WriteLine("[i] Đang chạy chế độ Console. Bấm Ctrl+C hoặc F11 để kết thúc...");
                bool running = true;
                engine.OnStopRequested += () => running = false;

                while (running)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(50);
                }

                engine.Stop();
                writer.FinalizeSession();
                Console.WriteLine("[+] Đã hoàn tất phiên ghi tại: " + writer.MarkdownPath);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Cú pháp sử dụng HisUiWrapper (HIS & EMR):");
            Console.WriteLine("  HisUiWrapper.exe               : Chạy bộ ghi với giao diện HUD nổi, tự động dò tìm HIS & EMR");
            Console.WriteLine("  HisUiWrapper.exe --launch      : Khởi chạy HIS.exe và tự động bắt đầu ghi thao tác");
            Console.WriteLine("  HisUiWrapper.exe --console     : Chạy chế độ Console dòng lệnh (không hiện HUD)");
            Console.WriteLine("  HisUiWrapper.exe --pid <PID>   : Gắn trực tiếp vào tiến trình có mã PID cụ thể");
            Console.WriteLine("  HisUiWrapper.exe --all         : Ghi nhận toàn bộ thao tác màn hình (không giới hạn HIS/EMR)");
            Console.WriteLine("  HisUiWrapper.exe --help        : Hiển thị hướng dẫn này");
        }
    }
}
