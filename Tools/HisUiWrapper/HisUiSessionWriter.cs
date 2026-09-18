using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HisUiWrapper
{
    public class HisUiSessionWriter
    {
        public string SessionId { get; private set; }
        public string OutputDirectory { get; private set; }
        public string JsonlPath { get; private set; }
        public string MarkdownPath { get; private set; }
        public string ReplayCodePath { get; private set; }
        
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }

        private readonly List<UiActionEvent> _events = new List<UiActionEvent>();
        private readonly object _fileLock = new object();

        public HisUiSessionWriter(string baseDir = null)
        {
            StartTime = DateTime.Now;
            SessionId = StartTime.ToString("yyyyMMdd_HHmmss");

            if (string.IsNullOrEmpty(baseDir))
            {
                baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "ui_recordings");
            }
            OutputDirectory = baseDir;

            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }

            JsonlPath = Path.Combine(OutputDirectory, string.Format("session_{0}.jsonl", SessionId));
            MarkdownPath = Path.Combine(OutputDirectory, string.Format("session_{0}_workflow.md", SessionId));
            ReplayCodePath = Path.Combine(OutputDirectory, string.Format("session_{0}_replay.cs", SessionId));
        }

        public void AppendEvent(UiActionEvent ev)
        {
            lock (_fileLock)
            {
                _events.Add(ev);
                string jsonLine = SerializeEventToJson(ev);
                try
                {
                    File.AppendAllText(JsonlPath, jsonLine + Environment.NewLine, Encoding.UTF8);
                }
                catch {}
            }
        }

        public void FinalizeSession()
        {
            EndTime = DateTime.Now;
            lock (_fileLock)
            {
                GenerateMarkdownSummary();
                GenerateReplayScript();
            }
        }

        private void GenerateMarkdownSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Nhật Ký Thao Tác Giao Diện HIS & EMR (Learned UI Workflow)");
            sb.AppendLine();
            sb.AppendLine(string.Format("- **Phiên ghi (Session ID)**: `{0}`", SessionId));
            sb.AppendLine(string.Format("- **Bắt đầu**: `{0:yyyy-MM-dd HH:mm:ss}`", StartTime));
            sb.AppendLine(string.Format("- **Kết thúc**: `{0:yyyy-MM-dd HH:mm:ss}`", EndTime));
            sb.AppendLine(string.Format("- **Tổng số thao tác (Steps)**: `{0}`", _events.Count));
            sb.AppendLine(string.Format("- **Tệp dữ liệu máy (JSONL)**: `{0}`", Path.GetFileName(JsonlPath)));
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 1. Tóm Tắt Quy Trình Lâm Sàng (Action Walkthrough)");
            sb.AppendLine();

            int stepNo = 1;
            foreach (var ev in _events)
            {
                string badge = ev.AppBadge == "EMR" ? "📋 **[EMR]**" : "🏥 **[HIS]**";
                if (ev.Type == ActionType.CheckpointNote) badge = "📌 **[NOTE]**";

                string delayTag = string.Empty;
                if (ev.DelayMs > 800)
                {
                    delayTag = string.Format("⏱️ *(Chờ: {0})* ", ev.DelayFormatted);
                }

                string win = !string.IsNullOrEmpty(ev.Element.TopWindowText) ? ev.Element.TopWindowText : "Cửa sổ làm việc";
                string targetDesc = FormatTargetDescription(ev.Element);
                string coordsDesc = FormatCoordinates(ev.Element);

                switch (ev.Type)
                {
                    case ActionType.Click:
                        sb.AppendLine(string.Format("{0}. {1} {2}**Click** `{3}` trên *\"{4}\"* {5}",
                            stepNo++, badge, delayTag, targetDesc, win, coordsDesc));
                        break;
                    case ActionType.DoubleClick:
                        sb.AppendLine(string.Format("{0}. {1} {2}**Double Click** `{3}` trên *\"{4}\"* {5}",
                            stepNo++, badge, delayTag, targetDesc, win, coordsDesc));
                        break;
                    case ActionType.RightClick:
                        sb.AppendLine(string.Format("{0}. {1} {2}**Click Phải** vào `{3}` trên *\"{4}\"* {5}",
                            stepNo++, badge, delayTag, targetDesc, win, coordsDesc));
                        break;
                    case ActionType.TextInput:
                        sb.AppendLine(string.Format("{0}. {1} {2}**Nhập văn bản** `\"{3}\"` vào ô `{4}` trên *\"{5}\"*",
                            stepNo++, badge, delayTag, ev.TextValue, targetDesc, win));
                        break;
                    case ActionType.KeyPress:
                        sb.AppendLine(string.Format("{0}. {1} {2}**Nhấn phím** `{3}` khi đang ở *\"{4}\"*",
                            stepNo++, badge, delayTag, ev.KeyText, win));
                        break;
                    case ActionType.CheckpointNote:
                        sb.AppendLine(string.Format("> {0} *{1}*", badge, ev.Note));
                        break;
                }
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 2. Bảng Đối Soát Toàn Diện: Tọa Độ, Thời Gian Chờ & UI Elements");
            sb.AppendLine();
            sb.AppendLine("| Bước | Ứng Dụng | Thời Điểm | Chờ (Delay) | Cửa Sổ Cha | Thao Tác | Control Type | AutomationId | Tên Hiển Thị | Tọa Độ Màn Hình | Tọa Độ Cửa Sổ | Tỷ Lệ Control |");
            sb.AppendLine("| :---: | :---: | :---: | :---: | :--- | :--- | :---: | :--- | :--- | :---: | :---: | :---: |");

            foreach (var ev in _events)
            {
                string appStr = ev.AppBadge == "EMR" ? "📋 EMR" : "🏥 HIS";
                string timeStr = ev.Timestamp.ToString("HH:mm:ss.fff");
                string delayStr = ev.DelayFormatted;
                if (ev.IsNewWindow) delayStr += " 🚀 [Cửa sổ mới]";

                string winStr = EscapeMd(ev.Element.TopWindowText);
                string actStr = ev.Type.ToString();
                if (ev.Type == ActionType.KeyPress) actStr += " (" + ev.KeyText + ")";
                else if (ev.Type == ActionType.TextInput) actStr += " (\"" + Truncate(ev.TextValue, 20) + "\")";

                string cType = ev.Element.ControlType ?? "-";
                string autoId = !string.IsNullOrEmpty(ev.Element.AutomationId) ? "`" + ev.Element.AutomationId + "`" : "-";
                string name = !string.IsNullOrEmpty(ev.Element.Name) ? EscapeMd(Truncate(ev.Element.Name, 25)) : "-";
                
                string screenCoord = string.Format("({0}, {1})", ev.Element.ScreenClickX, ev.Element.ScreenClickY);
                string winCoord = string.Format("({0}, {1})", ev.Element.WindowClickX, ev.Element.WindowClickY);
                string relPct = string.Format("{0}%, {1}%", ev.Element.RelPctX, ev.Element.RelPctY);

                sb.AppendLine(string.Format("| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8} | {9} | {10} | {11} |",
                    ev.StepIndex, appStr, timeStr, delayStr, winStr, actStr, cType, autoId, name, screenCoord, winCoord, relPct));
            }

            try
            {
                File.WriteAllText(MarkdownPath, sb.ToString(), Encoding.UTF8);
            }
            catch {}
        }

        private void GenerateReplayScript()
        {
            var sb = new StringBuilder();
            sb.AppendLine("// =============================================================================");
            sb.AppendLine("// HIS & EMR UI AUTOMATION REPLAY RECIPE - AUTO GENERATED BY HIS UI WRAPPER");
            sb.AppendLine(string.Format("// Session: {0} | Created: {1:yyyy-MM-dd HH:mm:ss}", SessionId, DateTime.Now));
            sb.AppendLine("// =============================================================================");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Diagnostics;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Windows.Automation;");
            sb.AppendLine();
            sb.AppendLine("namespace HisUiAutomation");
            sb.AppendLine("{");
            sb.AppendLine("    public class Program");
            sb.AppendLine("    {");
            sb.AppendLine("        public static void Main(string[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("            Console.WriteLine(\"Khởi động kịch bản Replay tự động...\");");
            sb.AppendLine("            AutomationElement root = AutomationElement.RootElement;");
            sb.AppendLine();

            int step = 1;
            foreach (var ev in _events)
            {
                if (ev.Type == ActionType.CheckpointNote) continue;

                int waitSleep = Math.Max(ev.DelayMs, 300);
                // Capped sleep to avoid extremely long pauses in replay
                if (waitSleep > 5000) waitSleep = 5000;

                sb.AppendLine(string.Format("            // -------------------------------------------------------------"));
                sb.AppendLine(string.Format("            // Bước {0}: {1}", step++, ev.ToString()));
                sb.AppendLine(string.Format("            // Thời gian chờ tải thực tế: {0}ms | Tọa độ màn hình: ({1}, {2})",
                    ev.DelayMs, ev.Element.ScreenClickX, ev.Element.ScreenClickY));
                sb.AppendLine(string.Format("            // -------------------------------------------------------------"));
                sb.AppendLine("            try");
                sb.AppendLine("            {");

                if (ev.DelayMs > 500)
                {
                    sb.AppendLine(string.Format("                Console.WriteLine(\"Chờ tải dữ liệu ({0}ms)...\");", waitSleep));
                    sb.AppendLine(string.Format("                Thread.Sleep({0});", waitSleep));
                }

                if (!string.IsNullOrEmpty(ev.Element.AutomationId))
                {
                    sb.AppendLine(string.Format("                var target = FindElementWithWait(root, AutomationElement.AutomationIdProperty, \"{0}\", 5000);", ev.Element.AutomationId));
                    sb.AppendLine("                if (target != null)");
                    sb.AppendLine("                {");
                    if (ev.Type == ActionType.Click || ev.Type == ActionType.DoubleClick)
                    {
                        sb.AppendLine("                    object invPattern;");
                        sb.AppendLine("                    if (target.TryGetCurrentPattern(InvokePattern.Pattern, out invPattern))");
                        sb.AppendLine("                        ((InvokePattern)invPattern).Invoke();");
                    }
                    else if (ev.Type == ActionType.TextInput)
                    {
                        sb.AppendLine("                    object valPattern;");
                        sb.AppendLine("                    if (target.TryGetCurrentPattern(ValuePattern.Pattern, out valPattern))");
                        sb.AppendLine(string.Format("                        ((ValuePattern)valPattern).SetValue(\"{0}\");", EscapeCSharpString(ev.TextValue)));
                    }
                    sb.AppendLine("                }");
                }
                else if (!string.IsNullOrEmpty(ev.Element.Name))
                {
                    sb.AppendLine(string.Format("                var target = FindElementWithWait(root, AutomationElement.NameProperty, \"{0}\", 5000);", EscapeCSharpString(ev.Element.Name)));
                }

                sb.AppendLine("                Thread.Sleep(300);");
                sb.AppendLine("            }");
                sb.AppendLine("            catch (Exception ex) { Console.WriteLine(\"Lỗi bước: \" + ex.Message); }");
                sb.AppendLine();
            }

            sb.AppendLine("            Console.WriteLine(\"Replay Hoàn Tất Thành Công.\");");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static AutomationElement FindElementWithWait(AutomationElement root, AutomationProperty prop, string val, int timeoutMs)");
            sb.AppendLine("        {");
            sb.AppendLine("            var cond = new PropertyCondition(prop, val);");
            sb.AppendLine("            var sw = Stopwatch.StartNew();");
            sb.AppendLine("            while (sw.ElapsedMilliseconds < timeoutMs)");
            sb.AppendLine("            {");
            sb.AppendLine("                var el = root.FindFirst(TreeScope.Descendants, cond);");
            sb.AppendLine("                if (el != null) return el;");
            sb.AppendLine("                Thread.Sleep(200);");
            sb.AppendLine("            }");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            try
            {
                File.WriteAllText(ReplayCodePath, sb.ToString(), Encoding.UTF8);
            }
            catch {}
        }

        private string FormatTargetDescription(UiElementInfo el)
        {
            if (!string.IsNullOrEmpty(el.AutomationId) && !string.IsNullOrEmpty(el.Name))
            {
                return string.Format("[{0}] \"{1}\"", el.AutomationId, el.Name);
            }
            if (!string.IsNullOrEmpty(el.AutomationId)) return "[" + el.AutomationId + "]";
            if (!string.IsNullOrEmpty(el.Name)) return "\"" + el.Name + "\"";
            if (!string.IsNullOrEmpty(el.ClassName)) return "[" + el.ClassName + "]";
            return "[Element]";
        }

        private string FormatCoordinates(UiElementInfo el)
        {
            var sb = new StringBuilder();
            sb.Append(string.Format("(Màn hình: [{0}, {1}] | Cửa sổ: [{2}, {3}]",
                el.ScreenClickX, el.ScreenClickY, el.WindowClickX, el.WindowClickY));

            if (el.Width > 0 && el.Height > 0)
            {
                sb.Append(string.Format(" | Tỷ lệ control: {0}%, {1}%", el.RelPctX, el.RelPctY));
            }
            sb.Append(")");
            return sb.ToString();
        }

        private string EscapeMd(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");
        }

        private string Truncate(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.Length <= maxLen) return s;
            return s.Substring(0, maxLen) + "...";
        }

        private string EscapeCSharpString(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private string SerializeEventToJson(UiActionEvent ev)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"step\":{0},", ev.StepIndex);
            sb.AppendFormat("\"timestamp\":\"{0:yyyy-MM-ddTHH:mm:ss.fff}\",", ev.Timestamp);
            sb.AppendFormat("\"app\":\"{0}\",", ev.AppBadge);
            sb.AppendFormat("\"delayMs\":{0},", ev.DelayMs);
            sb.AppendFormat("\"isNewWindow\":{0},", ev.IsNewWindow ? "true" : "false");
            sb.AppendFormat("\"waitReason\":\"{0}\",", EscapeJson(ev.WaitReason));
            sb.AppendFormat("\"type\":\"{0}\",", ev.Type);
            sb.AppendFormat("\"button\":\"{0}\",", ev.Button ?? "");
            sb.AppendFormat("\"keyText\":\"{0}\",", EscapeJson(ev.KeyText));
            sb.AppendFormat("\"textValue\":\"{0}\",", EscapeJson(ev.TextValue));
            sb.AppendFormat("\"note\":\"{0}\",", EscapeJson(ev.Note));

            sb.Append("\"element\":{");
            if (ev.Element != null)
            {
                sb.AppendFormat("\"appBadge\":\"{0}\",", EscapeJson(ev.Element.AppBadge));
                sb.AppendFormat("\"processName\":\"{0}\",", EscapeJson(ev.Element.ProcessName));
                sb.AppendFormat("\"processId\":{0},", ev.Element.ProcessId);
                sb.AppendFormat("\"automationId\":\"{0}\",", EscapeJson(ev.Element.AutomationId));
                sb.AppendFormat("\"name\":\"{0}\",", EscapeJson(ev.Element.Name));
                sb.AppendFormat("\"controlType\":\"{0}\",", EscapeJson(ev.Element.ControlType));
                sb.AppendFormat("\"className\":\"{0}\",", EscapeJson(ev.Element.ClassName));
                sb.AppendFormat("\"screenClickX\":{0},", ev.Element.ScreenClickX);
                sb.AppendFormat("\"screenClickY\":{0},", ev.Element.ScreenClickY);
                sb.AppendFormat("\"windowClickX\":{0},", ev.Element.WindowClickX);
                sb.AppendFormat("\"windowClickY\":{0},", ev.Element.WindowClickY);
                sb.AppendFormat("\"boundsX\":{0},", ev.Element.X);
                sb.AppendFormat("\"boundsY\":{0},", ev.Element.Y);
                sb.AppendFormat("\"boundsW\":{0},", ev.Element.Width);
                sb.AppendFormat("\"boundsH\":{0},", ev.Element.Height);
                sb.AppendFormat("\"relX\":{0},", ev.Element.RelX);
                sb.AppendFormat("\"relY\":{0},", ev.Element.RelY);
                sb.AppendFormat("\"relPctX\":{0},", ev.Element.RelPctX);
                sb.AppendFormat("\"relPctY\":{0},", ev.Element.RelPctY);
                sb.AppendFormat("\"currentValue\":\"{0}\",", EscapeJson(ev.Element.CurrentValue));
                sb.AppendFormat("\"topWindowText\":\"{0}\",", EscapeJson(ev.Element.TopWindowText));
                sb.AppendFormat("\"topWindowClass\":\"{0}\",", EscapeJson(ev.Element.TopWindowClass));
                sb.AppendFormat("\"topWindowHwnd\":\"0x{0:X}\",", ev.Element.TopWindowHwnd.ToInt64());
                sb.AppendFormat("\"hierarchyPath\":\"{0}\"", EscapeJson(ev.Element.HierarchyPath));
            }
            sb.Append("}");

            sb.Append("}");
            return sb.ToString();
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }
    }
}
