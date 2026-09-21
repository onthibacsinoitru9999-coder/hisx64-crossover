using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;

namespace HisPt01UiUploader
{
    public static class HisUiDriver
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern short VkKeyScan(char ch);

        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint KEYEVENTF_KEYUP = 0x0002;

        public const byte VK_RETURN = 0x0D;
        public const byte VK_TAB = 0x09;
        public const byte VK_ESCAPE = 0x1B;
        public const byte VK_CONTROL = 0x11;
        public const byte VK_F2 = 0x71;
        public const byte VK_F = 0x46;
        public const byte VK_S = 0x53;
        public const byte VK_A = 0x41;
        public const byte VK_V = 0x56;

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public static int TargetProcessId = 0;

        public static bool EnsureDesktop()
        {
            try
            {
                IntPtr hDesk = OpenDesktop("Default", 0, false, 0x01FF);
                if (hDesk != IntPtr.Zero)
                {
                    return SetThreadDesktop(hDesk);
                }
            }
            catch {}
            return false;
        }

        public static AutomationElement FindTopWindow(string titleContains, int timeoutMs = 8000)
        {
            EnsureDesktop();
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                // 1. First attempt: Win32 Native EnumWindows (fast, robust, cross-desktop)
                IntPtr targetHwnd = IntPtr.Zero;
                uint foundPid = 0;
                EnumWindows((hWnd, lParam) =>
                {
                    var sb = new StringBuilder(512);
                    GetWindowText(hWnd, sb, 512);
                    string t = sb.ToString();
                    if (IsWindowVisible(hWnd) && !string.IsNullOrEmpty(t) && t.IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        targetHwnd = hWnd;
                        GetWindowThreadProcessId(hWnd, out foundPid);
                        return false;
                    }
                    if (titleContains.Equals("HIS", StringComparison.OrdinalIgnoreCase))
                    {
                        uint pid;
                        GetWindowThreadProcessId(hWnd, out pid);
                        try
                        {
                            var p = Process.GetProcessById((int)pid);
                            if (p.ProcessName.IndexOf("HIS", StringComparison.OrdinalIgnoreCase) >= 0 && IsWindowVisible(hWnd) && t.Length > 0)
                            {
                                targetHwnd = hWnd;
                                foundPid = pid;
                                return false;
                            }
                        }
                        catch {}
                    }
                    return true;
                }, IntPtr.Zero);

                if (targetHwnd != IntPtr.Zero)
                {
                    if (TargetProcessId == 0 && foundPid > 0)
                    {
                        TargetProcessId = (int)foundPid;
                    }
                    try
                    {
                        var el = AutomationElement.FromHandle(targetHwnd);
                        if (el != null) return el;
                    }
                    catch {}
                }

                // 2. Fallback: UIA Root Children
                try
                {
                    AutomationElement root = AutomationElement.RootElement;
                    var windows = root.FindAll(TreeScope.Children, Condition.TrueCondition);
                    foreach (AutomationElement win in windows)
                    {
                        try
                        {
                            string name = win.Current.Name;
                            if (!string.IsNullOrEmpty(name) && name.IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                if (TargetProcessId == 0 && win.Current.ProcessId > 0)
                                {
                                    TargetProcessId = win.Current.ProcessId;
                                }
                                return win;
                            }
                        }
                        catch {}
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[-] UIA Root find ex: " + ex.Message);
                }

                Thread.Sleep(300);
            }
            return null;
        }

        public static AutomationElement FindElement(AutomationElement root, AutomationProperty prop, object val, int timeoutMs = 5000)
        {
            var cond = new PropertyCondition(prop, val);
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    var el = root.FindFirst(TreeScope.Descendants, cond);
                    if (el != null) return el;
                }
                catch {}
                Thread.Sleep(250);
            }
            return null;
        }

        public static void Hover(AutomationElement el, int sleepAfter = 400)
        {
            if (el == null) return;
            try
            {
                System.Windows.Rect rect = el.Current.BoundingRectangle;
                if (!rect.IsEmpty)
                {
                    int cx = (int)(rect.Left + rect.Width / 2);
                    int cy = (int)(rect.Top + rect.Height / 2);
                    SetCursorPos(cx, cy);
                    Thread.Sleep(sleepAfter);
                }
            }
            catch {}
        }

        public static void RightClickPoint(int x, int y, int sleepAfter = 600)
        {
            SetCursorPos(x, y);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, x, y, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_RIGHTUP, x, y, 0, UIntPtr.Zero);
            Thread.Sleep(sleepAfter);
        }

        public static bool RightClick(AutomationElement el, int sleepAfter = 600)
        {
            if (el == null) return false;
            try
            {
                System.Windows.Rect rect = el.Current.BoundingRectangle;
                if (!rect.IsEmpty && rect.Width > 0 && rect.Height > 0)
                {
                    int cx = (int)(rect.Left + rect.Width / 2);
                    int cy = (int)(rect.Top + rect.Height / 2);
                    Console.WriteLine(string.Format("    [DEBUG] RightClick at ({0}, {1}), rect: {2}", cx, cy, rect));
                    RightClickPoint(cx, cy, sleepAfter);
                    return true;
                }
                else
                {
                    Console.WriteLine("    [DEBUG] RightClick failed: Rect is empty or zero size");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("    [DEBUG] RightClick ex: " + ex.Message);
            }
            return false;
        }

        public static AutomationElement FindElementByName(AutomationElement root, string namePart, int timeoutMs = 5000)
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                // Fast path 1: Try exact match with PropertyCondition
                try
                {
                    var cond = new PropertyCondition(AutomationElement.NameProperty, namePart);
                    var exact = root.FindFirst(TreeScope.Descendants, cond);
                    if (exact != null) return exact;
                }
                catch {}

                // Fast path 2: If root is RootElement, prioritize small popup windows
                try
                {
                    if (root == AutomationElement.RootElement)
                    {
                        var topWins = root.FindAll(TreeScope.Children, Condition.TrueCondition);

                        // Ưu tiên 1: Quét các cửa sổ popup nhỏ (menu context, dropdown)
                        foreach (AutomationElement win in topWins)
                        {
                            try
                            {
                                if (TargetProcessId > 0 && win.Current.ProcessId != TargetProcessId) continue;
                                string winName = win.Current.Name;
                                string winClass = win.Current.ClassName;
                                bool isPopup = string.IsNullOrEmpty(winName) || winClass.Contains("20808") || winClass.Contains("20008") || winClass.Contains("#32770");
                                if (isPopup)
                                {
                                    var cond = new PropertyCondition(AutomationElement.NameProperty, namePart);
                                    var el = win.FindFirst(TreeScope.Descendants, cond);
                                    if (el != null) return el;

                                    var all = win.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                                    foreach (AutomationElement item in all)
                                    {
                                        string n = item.Current.Name;
                                        if (!string.IsNullOrEmpty(n) && n.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            return item;
                                        }
                                    }
                                }
                            }
                            catch {}
                        }

                        // Ưu tiên 2: Cửa sổ chính nếu chưa tìm thấy
                        foreach (AutomationElement win in topWins)
                        {
                            try
                            {
                                if (TargetProcessId > 0 && win.Current.ProcessId != TargetProcessId) continue;
                                string winName = win.Current.Name;
                                string winClass = win.Current.ClassName;
                                bool isPopup = string.IsNullOrEmpty(winName) || winClass.Contains("20808") || winClass.Contains("20008") || winClass.Contains("#32770");
                                if (!isPopup)
                                {
                                    var cond = new PropertyCondition(AutomationElement.NameProperty, namePart);
                                    var el = win.FindFirst(TreeScope.Descendants, cond);
                                    if (el != null) return el;
                                }
                            }
                            catch {}
                        }
                    }
                    else
                    {
                        var cond = new PropertyCondition(AutomationElement.NameProperty, namePart);
                        var el = root.FindFirst(TreeScope.Descendants, cond);
                        if (el != null) return el;

                        var all = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                        foreach (AutomationElement elSub in all)
                        {
                            string n = elSub.Current.Name;
                            if (!string.IsNullOrEmpty(n) && n.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return elSub;
                            }
                        }
                    }
                }
                catch {}

                Thread.Sleep(150);
            }
            return null;
        }

        public static void DoubleClickPoint(int x, int y, int sleepAfter = 400)
        {
            SetCursorPos(x, y);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, UIntPtr.Zero);
            Thread.Sleep(sleepAfter);
        }

        public static bool DoubleClick(AutomationElement el, int sleepAfter = 400)
        {
            if (el == null) return false;
            try
            {
                System.Windows.Rect rect = el.Current.BoundingRectangle;
                if (!rect.IsEmpty)
                {
                    int cx = (int)(rect.Left + rect.Width / 2);
                    int cy = (int)(rect.Top + rect.Height / 2);
                    DoubleClickPoint(cx, cy, sleepAfter);
                    return true;
                }
            }
            catch {}
            return false;
        }

        public static bool Click(AutomationElement el, int sleepAfter = 400)
        {
            if (el == null) return false;
            try
            {
                object invPattern;
                if (el.TryGetCurrentPattern(InvokePattern.Pattern, out invPattern))
                {
                    ((InvokePattern)invPattern).Invoke();
                    Thread.Sleep(sleepAfter);
                    return true;
                }
            }
            catch {}

            // Fallback to Physical Mouse Click on center of element
            try
            {
                System.Windows.Rect rect = el.Current.BoundingRectangle;
                if (!rect.IsEmpty)
                {
                    int cx = (int)(rect.Left + rect.Width / 2);
                    int cy = (int)(rect.Top + rect.Height / 2);
                    ClickPoint(cx, cy, sleepAfter);
                    return true;
                }
            }
            catch {}

            return false;
        }

        public static void ClickPoint(int x, int y, int sleepAfter = 400)
        {
            SetCursorPos(x, y);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, UIntPtr.Zero);
            Thread.Sleep(sleepAfter);
        }



        public static bool SetText(AutomationElement el, string text, int sleepAfter = 300)
        {
            if (el == null) return false;
            try
            {
                object valPattern;
                if (el.TryGetCurrentPattern(ValuePattern.Pattern, out valPattern))
                {
                    ((ValuePattern)valPattern).SetValue(text);
                    Thread.Sleep(sleepAfter);
                    return true;
                }
            }
            catch {}

            // Fallback: Click to focus and paste text
            Click(el, 200);
            PasteText(text);
            Thread.Sleep(sleepAfter);
            return true;
        }

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
        }

        public static void PasteText(string text)
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(text);
                PressShortcut(VK_A, ctrl: true);
                Thread.Sleep(50);
                PressShortcut(VK_V, ctrl: true);
                Thread.Sleep(100);
                return;
            }
            catch {}
            SendText(RemoveDiacritics(text));
        }

        public static void SendText(string text)
        {
            string safeText = RemoveDiacritics(text);
            foreach (char c in safeText)
            {
                short vk = VkKeyScan(c);
                byte key = (byte)(vk & 0xFF);
                bool shift = ((vk >> 8) & 1) != 0;

                if (shift) keybd_event(0x10, 0, 0, UIntPtr.Zero); // Shift down
                keybd_event(key, 0, 0, UIntPtr.Zero);
                keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if (shift) keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Shift up
                Thread.Sleep(20);
            }
        }

        public static void PressShortcut(byte key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            if (ctrl) keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            if (alt) keybd_event(0x12, 0, 0, UIntPtr.Zero);
            if (shift) keybd_event(0x10, 0, 0, UIntPtr.Zero);

            keybd_event(key, 0, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            if (shift) keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (alt) keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (ctrl) keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            Thread.Sleep(300);
        }

        public static void PressKey(byte key, int sleepAfter = 300)
        {
            keybd_event(key, 0, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(sleepAfter);
        }

        public static void ActivateWindow(AutomationElement win)
        {
            if (win == null) return;
            try
            {
                IntPtr hwnd = new IntPtr(win.Current.NativeWindowHandle);
                if (hwnd != IntPtr.Zero)
                {
                    uint dummy;
                    uint winThread = GetWindowThreadProcessId(hwnd, out dummy);
                    uint curThread = GetCurrentThreadId();
                    if (winThread != 0 && winThread != curThread)
                    {
                        AttachThreadInput(curThread, winThread, true);
                        ShowWindow(hwnd, 9); // SW_RESTORE
                        BringWindowToTop(hwnd);
                        SetForegroundWindow(hwnd);
                        AttachThreadInput(curThread, winThread, false);
                    }
                    else
                    {
                        ShowWindow(hwnd, 9);
                        BringWindowToTop(hwnd);
                        SetForegroundWindow(hwnd);
                    }
                    Thread.Sleep(400);
                }
            }
            catch {}
        }
    }
}
