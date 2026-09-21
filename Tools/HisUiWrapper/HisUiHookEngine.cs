using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HisUiWrapper
{
    public enum ActionType
    {
        Click,
        DoubleClick,
        RightClick,
        KeyPress,
        TextInput,
        CheckpointNote
    }

    public class UiActionEvent
    {
        public int StepIndex { get; set; }
        public DateTime Timestamp { get; set; }
        public int DelayMs { get; set; } // Thời gian chờ (delay) tính bằng mili-giây kể từ thao tác trước
        public bool IsNewWindow { get; set; }
        public string WaitReason { get; set; }
        public ActionType Type { get; set; }
        public string Button { get; set; }
        public string KeyText { get; set; }
        public string TextValue { get; set; }
        public string Note { get; set; }
        public UiElementInfo Element { get; set; }

        public string AppBadge
        {
            get { return Element != null && !string.IsNullOrEmpty(Element.AppBadge) ? Element.AppBadge : "HIS"; }
        }

        public string DelayFormatted
        {
            get
            {
                if (DelayMs <= 0) return "0ms";
                if (DelayMs < 1000) return DelayMs + "ms";
                return string.Format("{0:N1}s", DelayMs / 1000.0);
            }
        }

        public override string ToString()
        {
            string appPart = "[" + AppBadge + "]";
            string delayPart = DelayMs > 500 ? string.Format("(⏱️ chờ {0}) ", DelayFormatted) : "";

            switch (Type)
            {
                case ActionType.Click:
                case ActionType.DoubleClick:
                case ActionType.RightClick:
                    return string.Format("{0} {1}[{2}] {3} on '{4}' ({5}) in '{6}'",
                        appPart, delayPart, Type, Button, Element.Name, Element.AutomationId, Element.TopWindowText);
                case ActionType.TextInput:
                    return string.Format("{0} {1}[Type] \"{2}\" into '{3}' ({4})",
                        appPart, delayPart, TextValue, Element.Name, Element.AutomationId);
                case ActionType.KeyPress:
                    return string.Format("{0} {1}[Key] {2} in '{3}'", appPart, delayPart, KeyText, Element.TopWindowText);
                case ActionType.CheckpointNote:
                    return string.Format("[Note] {0}", Note);
                default:
                    return string.Format("{0} {1}[{2}]", appPart, delayPart, Type);
            }
        }
    }

    public class HisUiHookEngine : IDisposable
    {
        // Win32 Hook Constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_LBUTTONDBLCLK = 0x0203;

        private const int VK_F9 = 0x78;
        private const int VK_F10 = 0x79;
        private const int VK_F11 = 0x7A;
        private const int VK_RETURN = 0x0D;
        private const int VK_TAB = 0x09;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_BACK = 0x08;

        // Delegates
        private delegate IntPtr LowLevelHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelHookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // State & Configuration
        public HashSet<uint> TargetPids { get; private set; }
        public bool RecordAllApps { get; set; }
        public bool IsRecording { get; set; }
        private int _stepCounter = 0;

        private IntPtr _mouseHook = IntPtr.Zero;
        private IntPtr _keyboardHook = IntPtr.Zero;
        private LowLevelHookProc _mouseProc;
        private LowLevelHookProc _keyboardProc;

        // Delay & Window tracking
        private DateTime _lastActionTime = DateTime.MinValue;
        private IntPtr _lastWindowHwnd = IntPtr.Zero;
        private string _lastWindowText = string.Empty;
        private readonly object _actionLock = new object();

        // Smart Typing Accumulator
        private readonly object _typingLock = new object();
        private StringBuilder _typingBuffer = new StringBuilder();
        private UiElementInfo _typingElement = null;
        private DateTime _typingStartTime = DateTime.MinValue;
        private System.Threading.Timer _typingFlushTimer;

        // Mouse double click detection
        private DateTime _lastLeftDown = DateTime.MinValue;
        private POINT _lastLeftDownPt;

        // Events
        public event Action<UiActionEvent> OnActionRecorded;
        public event Action<bool> OnRecordingStateChanged;
        public event Action OnStopRequested;
        public event Action OnNoteRequested;
        public event Action<string, uint> OnProcessDiscovered;

        public HisUiHookEngine()
        {
            TargetPids = new HashSet<uint>();
            IsRecording = true;
            _mouseProc = MouseHookCallback;
            _keyboardProc = KeyboardHookCallback;
            _typingFlushTimer = new System.Threading.Timer(TypingTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void AddTargetPid(uint pid)
        {
            if (pid > 0) TargetPids.Add(pid);
        }

        public void RefreshHisPids()
        {
            try
            {
                // Scan all processes with HIS, EMR, EHR, or Inventec
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        string name = p.ProcessName;
                        if (name.Equals("HIS", StringComparison.OrdinalIgnoreCase) ||
                            name.IndexOf("EMR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("ConnectToEMR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("EHR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.StartsWith("HIS.Desktop", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("Inventec.", StringComparison.OrdinalIgnoreCase))
                        {
                            TargetPids.Add((uint)p.Id);
                        }
                    }
                    catch {}
                }
            }
            catch {}
        }

        public void Start()
        {
            RefreshHisPids();
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hMod = GetModuleHandle(curModule.ModuleName);
                _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
                _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, hMod, 0);
            }
        }

        public void Stop()
        {
            FlushTypingBuffer();
            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
            if (_keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }
            if (_typingFlushTimer != null)
            {
                _typingFlushTimer.Dispose();
                _typingFlushTimer = null;
            }
        }

        public void ToggleRecording()
        {
            IsRecording = !IsRecording;
            if (!IsRecording) FlushTypingBuffer();
            if (OnRecordingStateChanged != null) OnRecordingStateChanged(IsRecording);
        }

        public void RecordCheckpoint(string note)
        {
            FlushTypingBuffer();
            var ev = CreateActionEvent(ActionType.CheckpointNote, null);
            ev.Note = note;
            ev.Element = new UiElementInfo { TopWindowText = "Ghi chú thủ công", AppBadge = "NOTE" };
            FireActionRecorded(ev);
        }

        private UiActionEvent CreateActionEvent(ActionType type, UiElementInfo elInfo)
        {
            DateTime now = DateTime.Now;
            int delay = 0;
            bool isNewWin = false;
            string waitReason = "Thao tác liên tiếp";

            lock (_actionLock)
            {
                if (_lastActionTime != DateTime.MinValue)
                {
                    delay = (int)(now - _lastActionTime).TotalMilliseconds;
                }

                if (elInfo != null && elInfo.TopWindowHwnd != IntPtr.Zero && elInfo.TopWindowHwnd != _lastWindowHwnd)
                {
                    isNewWin = true;
                    if (!string.IsNullOrEmpty(_lastWindowText))
                    {
                        waitReason = string.Format("Chờ mở cửa sổ mới: '{0}' ({1}ms)", elInfo.TopWindowText, delay);
                    }
                    else
                    {
                        waitReason = string.Format("Mở cửa sổ '{0}' ({1}ms)", elInfo.TopWindowText, delay);
                    }
                }
                else if (delay > 2000)
                {
                    waitReason = string.Format("Chờ tải dữ liệu / Dừng thao tác ({0:N1}s)", delay / 1000.0);
                }

                _lastActionTime = now;
                if (elInfo != null && elInfo.TopWindowHwnd != IntPtr.Zero)
                {
                    _lastWindowHwnd = elInfo.TopWindowHwnd;
                    _lastWindowText = elInfo.TopWindowText;
                }
            }

            return new UiActionEvent
            {
                StepIndex = Interlocked.Increment(ref _stepCounter),
                Timestamp = now,
                DelayMs = delay,
                IsNewWindow = isNewWin,
                WaitReason = waitReason,
                Type = type,
                Element = elInfo
            };
        }

        private bool IsPidAllowed(uint pid)
        {
            if (pid == 0) return false;
            if (RecordAllApps) return true;
            if (TargetPids.Contains(pid)) return true;

            // Dynamic check: Did user just open EMR or a new HIS child window?
            string procName = HisUiaInspector.GetProcessNameByPid(pid);
            if (!string.IsNullOrEmpty(procName))
            {
                if (procName.Equals("HIS", StringComparison.OrdinalIgnoreCase) ||
                    procName.IndexOf("EMR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    procName.IndexOf("ConnectToEMR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    procName.IndexOf("EHR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    procName.StartsWith("HIS.Desktop", StringComparison.OrdinalIgnoreCase) ||
                    procName.StartsWith("Inventec.", StringComparison.OrdinalIgnoreCase))
                {
                    TargetPids.Add(pid);
                    if (OnProcessDiscovered != null)
                    {
                        OnProcessDiscovered(procName, pid);
                    }
                    return true;
                }
            }

            return false;
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_LBUTTONDBLCLK)
                {
                    var hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    POINT pt = hookStruct.pt;
                    
                    IntPtr hwnd = HisUiaInspector.WindowFromPoint(new HisUiaInspector.POINT(pt.x, pt.y));
                    uint pid = HisUiaInspector.GetHwndPid(hwnd);

                    // Privacy check: only process if inside HIS or EMR
                    if (IsPidAllowed(pid))
                    {
                        FlushTypingBuffer();

                        if (IsRecording)
                        {
                            ActionType actionType = ActionType.Click;
                            string button = "Left";

                            if (msg == WM_RBUTTONDOWN)
                            {
                                actionType = ActionType.RightClick;
                                button = "Right";
                            }
                            else if (msg == WM_LBUTTONDBLCLK)
                            {
                                actionType = ActionType.DoubleClick;
                            }
                            else
                            {
                                // Detect double click manually within 400ms & 5px
                                double elapsed = (DateTime.Now - _lastLeftDown).TotalMilliseconds;
                                if (elapsed < 400 && Math.Abs(pt.x - _lastLeftDownPt.x) < 5 && Math.Abs(pt.y - _lastLeftDownPt.y) < 5)
                                {
                                    actionType = ActionType.DoubleClick;
                                }
                                _lastLeftDown = DateTime.Now;
                                _lastLeftDownPt = pt;
                            }

                            // Run UIA inspection in background task to avoid mouse lag
                            ThreadPool.QueueUserWorkItem(_ =>
                            {
                                try
                                {
                                    var elInfo = HisUiaInspector.InspectAtPoint(pt.x, pt.y);
                                    var ev = CreateActionEvent(actionType, elInfo);
                                    ev.Button = button;
                                    FireActionRecorded(ev);
                                }
                                catch {}
                            });
                        }
                    }
                }
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    var kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    int vk = (int)kb.vkCode;

                    // Global hotkeys (always active even outside HIS/EMR)
                    if (vk == VK_F9)
                    {
                        ToggleRecording();
                        return (IntPtr)1; // swallow
                    }
                    if (vk == VK_F10)
                    {
                        if (OnNoteRequested != null) OnNoteRequested();
                        return (IntPtr)1;
                    }
                    if (vk == VK_F11)
                    {
                        FlushTypingBuffer();
                        if (OnStopRequested != null) OnStopRequested();
                        return (IntPtr)1;
                    }

                    // Process check for recording
                    IntPtr fgHwnd = GetForegroundWindow();
                    uint fgPid = HisUiaInspector.GetHwndPid(fgHwnd);

                    if (IsRecording && IsPidAllowed(fgPid))
                    {
                        bool isCtrl = (GetAsyncKeyState(0x11) & 0x8000) != 0;
                        bool isAlt = (GetAsyncKeyState(0x12) & 0x8000) != 0;
                        bool isShift = (GetAsyncKeyState(0x10) & 0x8000) != 0;

                        // Check if it's a special key or shortcut
                        bool isSpecialKey = (vk >= 0x70 && vk <= 0x7B) // F1-F12
                                            || vk == VK_RETURN 
                                            || vk == VK_TAB 
                                            || vk == VK_ESCAPE 
                                            || isCtrl 
                                            || isAlt;

                        if (isSpecialKey)
                        {
                            FlushTypingBuffer();

                            string keyDesc = FormatKeyDescription(vk, isCtrl, isAlt, isShift);
                            ThreadPool.QueueUserWorkItem(_ =>
                            {
                                try
                                {
                                    var elInfo = HisUiaInspector.InspectFocusedElement();
                                    var ev = CreateActionEvent(ActionType.KeyPress, elInfo);
                                    ev.KeyText = keyDesc;
                                    FireActionRecorded(ev);
                                }
                                catch {}
                            });
                        }
                        else
                        {
                            // Regular typing key -> accumulate
                            string charTyped = GetCharsFromKeys((uint)vk, kb.scanCode);
                            if (!string.IsNullOrEmpty(charTyped))
                            {
                                AppendTyping(charTyped);
                            }
                            else if (vk == VK_BACK)
                            {
                                AppendTyping("[Backspace]");
                            }
                        }
                    }
                }
            }

            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private void AppendTyping(string ch)
        {
            lock (_typingLock)
            {
                if (_typingBuffer.Length == 0)
                {
                    _typingStartTime = DateTime.Now;
                    _typingElement = HisUiaInspector.InspectFocusedElement();
                }

                if (ch == "[Backspace]")
                {
                    if (_typingBuffer.Length > 0) _typingBuffer.Length--;
                }
                else
                {
                    _typingBuffer.Append(ch);
                }

                // Reset timer for 1000ms idle flush
                if (_typingFlushTimer != null)
                {
                    _typingFlushTimer.Change(1000, Timeout.Infinite);
                }
            }
        }

        private void TypingTimerCallback(object state)
        {
            FlushTypingBuffer();
        }

        public void FlushTypingBuffer()
        {
            lock (_typingLock)
            {
                if (_typingBuffer.Length > 0)
                {
                    string text = _typingBuffer.ToString();
                    UiElementInfo el = _typingElement ?? HisUiaInspector.InspectFocusedElement();
                    _typingBuffer.Length = 0;
                    _typingElement = null;

                    var ev = CreateActionEvent(ActionType.TextInput, el);
                    ev.TextValue = text;
                    FireActionRecorded(ev);
                }
            }
        }

        private void FireActionRecorded(UiActionEvent ev)
        {
            if (OnActionRecorded != null)
            {
                OnActionRecorded(ev);
            }
        }

        private string FormatKeyDescription(int vk, bool ctrl, bool alt, bool shift)
        {
            var sb = new StringBuilder();
            if (ctrl) sb.Append("Ctrl+");
            if (alt) sb.Append("Alt+");
            if (shift) sb.Append("Shift+");

            if (vk >= 0x70 && vk <= 0x7B) sb.Append("F" + (vk - 0x6F));
            else if (vk == VK_RETURN) sb.Append("Enter");
            else if (vk == VK_TAB) sb.Append("Tab");
            else if (vk == VK_ESCAPE) sb.Append("Esc");
            else if (vk >= 0x30 && vk <= 0x39) sb.Append((char)vk);
            else if (vk >= 0x41 && vk <= 0x5A) sb.Append((char)vk);
            else sb.Append("Key_" + vk);

            return sb.ToString();
        }

        private string GetCharsFromKeys(uint keys, uint scanCode)
        {
            var buf = new StringBuilder(256);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);
            int ret = ToUnicode(keys, scanCode, keyboardState, buf, 256, 0);
            if (ret > 0)
            {
                return buf.ToString();
            }
            return string.Empty;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
