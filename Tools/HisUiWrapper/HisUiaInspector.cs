using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace HisUiWrapper
{
    public class UiElementInfo
    {
        public string AppBadge { get; set; } // "HIS", "EMR", etc.
        public string ProcessName { get; set; }
        public uint ProcessId { get; set; }
        public string AutomationId { get; set; }
        public string Name { get; set; }
        public string ControlType { get; set; }
        public string ClassName { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double RelX { get; set; }
        public double RelY { get; set; }
        public double RelPctX { get; set; }
        public double RelPctY { get; set; }
        public string CurrentValue { get; set; }
        public string TopWindowText { get; set; }
        public string TopWindowClass { get; set; }
        public IntPtr TopWindowHwnd { get; set; }
        public IntPtr ElementHwnd { get; set; }
        public string HierarchyPath { get; set; }

        public override string ToString()
        {
            string appPart = !string.IsNullOrEmpty(AppBadge) ? "[" + AppBadge + "]" : "";
            string idPart = !string.IsNullOrEmpty(AutomationId) ? "[" + AutomationId + "]" : "";
            string namePart = !string.IsNullOrEmpty(Name) ? "\"" + Name + "\"" : "";
            return string.Format("{0} {1} {2} {3}", appPart, ControlType, idPart, namePart).Trim();
        }
    }

    public static class HisUiaInspector
    {
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public const uint GA_PARENT = 1;
        public const uint GA_ROOT = 2;
        public const uint GA_ROOTOWNER = 3;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
            public POINT(int x, int y) { this.x = x; this.y = y; }
        }

        private static readonly Dictionary<uint, string> _procNameCache = new Dictionary<uint, string>();

        public static string GetProcessNameByPid(uint pid)
        {
            if (pid == 0) return string.Empty;
            lock (_procNameCache)
            {
                string name;
                if (_procNameCache.TryGetValue(pid, out name)) return name;

                try
                {
                    using (var p = Process.GetProcessById((int)pid))
                    {
                        name = p.ProcessName;
                        _procNameCache[pid] = name;
                        return name;
                    }
                }
                catch
                {
                    _procNameCache[pid] = string.Empty;
                    return string.Empty;
                }
            }
        }

        public static string DetermineAppBadge(string procName)
        {
            if (string.IsNullOrEmpty(procName)) return "HIS";
            if (procName.IndexOf("EMR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                procName.IndexOf("EHR", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "EMR";
            }
            if (procName.IndexOf("HIS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "HIS";
            }
            return procName;
        }

        public static string GetHwndTitle(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return string.Empty;
            var sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, 512);
            return sb.ToString();
        }

        public static string GetHwndClass(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return string.Empty;
            var sb = new StringBuilder(512);
            GetClassName(hWnd, sb, 512);
            return sb.ToString();
        }

        public static uint GetHwndPid(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return 0;
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            return pid;
        }

        public static UiElementInfo InspectAtPoint(int screenX, int screenY)
        {
            var info = new UiElementInfo
            {
                X = screenX,
                Y = screenY,
                AutomationId = string.Empty,
                Name = string.Empty,
                ControlType = "Custom",
                ClassName = string.Empty,
                HierarchyPath = string.Empty,
                CurrentValue = string.Empty
            };

            // 1. Win32 Native HWND Info
            IntPtr rawHwnd = WindowFromPoint(new POINT(screenX, screenY));
            info.ElementHwnd = rawHwnd;

            uint pid = GetHwndPid(rawHwnd);
            info.ProcessId = pid;
            info.ProcessName = GetProcessNameByPid(pid);
            info.AppBadge = DetermineAppBadge(info.ProcessName);

            IntPtr rootHwnd = GetAncestor(rawHwnd, GA_ROOT);
            if (rootHwnd == IntPtr.Zero) rootHwnd = rawHwnd;
            info.TopWindowHwnd = rootHwnd;
            info.TopWindowText = GetHwndTitle(rootHwnd);
            info.TopWindowClass = GetHwndClass(rootHwnd);

            // 2. UI Automation Deep Inspection
            try
            {
                var point = new System.Windows.Point(screenX, screenY);
                AutomationElement el = AutomationElement.FromPoint(point);

                if (el != null)
                {
                    PopulateFromAutomationElement(el, info, screenX, screenY);
                }
                else if (rawHwnd != IntPtr.Zero)
                {
                    el = AutomationElement.FromHandle(rawHwnd);
                    if (el != null)
                    {
                        PopulateFromAutomationElement(el, info, screenX, screenY);
                    }
                }
            }
            catch
            {
                // Fallback to Win32 info
                info.ClassName = GetHwndClass(rawHwnd);
                info.Name = GetHwndTitle(rawHwnd);
            }

            return info;
        }

        public static UiElementInfo InspectFocusedElement()
        {
            var info = new UiElementInfo
            {
                AutomationId = string.Empty,
                Name = string.Empty,
                ControlType = "Custom",
                ClassName = string.Empty,
                HierarchyPath = string.Empty,
                CurrentValue = string.Empty
            };

            try
            {
                AutomationElement el = AutomationElement.FocusedElement;
                if (el != null)
                {
                    System.Windows.Rect rect = el.Current.BoundingRectangle;
                    int midX = (int)(rect.Left + rect.Width / 2);
                    int midY = (int)(rect.Top + rect.Height / 2);
                    PopulateFromAutomationElement(el, info, midX, midY);

                    IntPtr rootHwnd = GetAncestor(info.ElementHwnd, GA_ROOT);
                    if (rootHwnd != IntPtr.Zero)
                    {
                        info.TopWindowHwnd = rootHwnd;
                        info.TopWindowText = GetHwndTitle(rootHwnd);
                        info.TopWindowClass = GetHwndClass(rootHwnd);
                        uint pid = GetHwndPid(rootHwnd);
                        info.ProcessId = pid;
                        info.ProcessName = GetProcessNameByPid(pid);
                        info.AppBadge = DetermineAppBadge(info.ProcessName);
                    }
                }
            }
            catch {}

            return info;
        }

        private static void PopulateFromAutomationElement(AutomationElement el, UiElementInfo info, int clickX, int clickY)
        {
            try
            {
                info.AutomationId = el.Current.AutomationId ?? string.Empty;
                info.Name = el.Current.Name ?? string.Empty;
                info.ControlType = el.Current.ControlType != null 
                    ? el.Current.ControlType.ProgrammaticName.Replace("ControlType.", "") 
                    : "Unknown";
                info.ClassName = el.Current.ClassName ?? string.Empty;

                try
                {
                    info.ElementHwnd = new IntPtr(el.Current.NativeWindowHandle);
                    if (info.ProcessId == 0 && info.ElementHwnd != IntPtr.Zero)
                    {
                        uint pid = GetHwndPid(info.ElementHwnd);
                        info.ProcessId = pid;
                        info.ProcessName = GetProcessNameByPid(pid);
                        info.AppBadge = DetermineAppBadge(info.ProcessName);
                    }
                }
                catch {}

                System.Windows.Rect rect = el.Current.BoundingRectangle;
                if (!rect.IsEmpty)
                {
                    info.X = rect.X;
                    info.Y = rect.Y;
                    info.Width = rect.Width;
                    info.Height = rect.Height;

                    double rx = clickX - rect.X;
                    double ry = clickY - rect.Y;
                    info.RelX = rx;
                    info.RelY = ry;
                    info.RelPctX = rect.Width > 0 ? Math.Round((rx / rect.Width) * 100, 1) : 0;
                    info.RelPctY = rect.Height > 0 ? Math.Round((ry / rect.Height) * 100, 1) : 0;
                }

                // Try to extract value pattern
                object valPattern;
                if (el.TryGetCurrentPattern(ValuePattern.Pattern, out valPattern))
                {
                    var vp = valPattern as ValuePattern;
                    if (vp != null) info.CurrentValue = vp.Current.Value;
                }
                else
                {
                    object textPattern;
                    if (el.TryGetCurrentPattern(TextPattern.Pattern, out textPattern))
                    {
                        var tp = textPattern as TextPattern;
                        if (tp != null && tp.DocumentRange != null)
                        {
                            string text = tp.DocumentRange.GetText(256);
                            if (!string.IsNullOrEmpty(text)) info.CurrentValue = text.Trim();
                        }
                    }
                }

                // Build hierarchy path (up to 4 levels)
                var pathParts = new List<string>();
                AutomationElement curr = el;
                int depth = 0;
                while (curr != null && depth < 4)
                {
                    try
                    {
                        string desc = !string.IsNullOrEmpty(curr.Current.AutomationId) 
                            ? curr.Current.AutomationId 
                            : curr.Current.Name;
                        if (!string.IsNullOrEmpty(desc))
                        {
                            pathParts.Insert(0, desc);
                        }
                        curr = TreeWalker.RawViewWalker.GetParent(curr);
                        depth++;
                    }
                    catch { break; }
                }
                info.HierarchyPath = string.Join(" > ", pathParts.ToArray());
            }
            catch {}
        }
    }
}
