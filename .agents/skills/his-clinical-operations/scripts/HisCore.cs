using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;

namespace HisAutomation.Core
{
    /// <summary>
    /// Adapter tiện ích dùng chung cho kết nối Backend MOS/SDA.
    /// </summary>
    public class HisAdapter : AdapterBase
    {
        public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
        {
            return Get<List<T>>(uri, consumer, filter, param);
        }

        public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
        {
            return Post<T>(uri, consumer, data, param);
        }
    }

    /// <summary>
    /// Đọc Token đăng nhập của phiên làm việc HIS an toàn và siêu tốc từ LogSystem.txt.
    /// </summary>
    public static class HisTokenReader
    {
        // Seek 128KB cuối của LogSystem.txt với FileShare.ReadWrite để lấy Token nhanh nhất
        public static string ReadLiveTokenFast()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            List<string> candidates = new List<string>();

            try
            {
                var procs = System.Diagnostics.Process.GetProcessesByName(HIS);
                if (procs != null && procs.Length > 0)
                {
                    string hisDir = Path.GetDirectoryName(procs[0].MainModule.FileName);
                    candidates.Add(Path.Combine(hisDir, Logs, LogSystem.txt));
                }
            }
            catch { }

            DirectoryInfo cur = new DirectoryInfo(baseDir);
            for (int i = 0; i < 5; i++)
            {
                if (cur == null) break;
                candidates.Add(Path.Combine(cur.FullName, Logs, LogSystem.txt));
                candidates.Add(Path.Combine(cur.FullName, Logs, HLSLogSystem.txt));
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
                        int idx = chunk.LastIndexOf(TokenCode|);
                        if (idx >= 0)
                        {
                            int start = idx + 10;
                            if (chunk.Length >= start + 64)
                            {
                                return chunk.Substring(start, 64);
                            }
                        }
                    }
                }
                catch { }
            }
            return null;
        }
    }

    /// <summary>
    /// Ngữ cảnh mặc định Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57).
    /// </summary>
    public static class HisContext
    {
        // Cố định Khoa 57 và Phòng 734 mặc định cho Khoa CTCH & Cột sống
        public const long DepartmentId = 57;
        public const long RoomId = 5248; // P734
        public const string DefaultDoctorLogin = 034727; // Ths.BS Nguyễn Hữu Sâm
        public const string BackupDoctorLogin = vmc; // BS. Vũ Minh Cường
    }
}
