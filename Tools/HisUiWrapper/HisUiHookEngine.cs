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
        public ActionType Type { get; set; }
        public string Button { get; set; }
        public string KeyText { get; set; }
        public string TextValue { get; set; }
        public string Note { get; set; }
        public UiElementInfo Element { get; set; }

        public override string ToString()
        {
            switch (Type)
            {
                case ActionType.Click:
                case ActionType.DoubleClick:
                case ActionType.RightClick:
                    return string.Format("[{0}] {1} on '{2}' ({3}) in '{4}'",
                        Type, Button, Element.Name, Element.AutomationId, Element.TopWindowText);
                case ActionType.TextInput:
                    return string.Format("[Type] \"{0}\" into '{1}' ({2})",
                        TextValue, Element.Name, Element.AutomationId);
                case ActionType.KeyPress:
                    return string.Format("[Key] {0} in '{1}'", KeyText, Element.TopWindowText);
                case ActionType.CheckpointNote:
                    return string.Format("[Note] {0}", Note);
                default:
                    return string.Format("[{0}]", Type);
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

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

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
        public bool IsRecording { get; set; }
        private int _stepCounter = 0;

        private IntPtr _mouseHook = IntPtr.Zero;
        private IntPtr _keyboardHook = IntPtr.Zero;
        private LowLevelHookProc _mouseProc;
        private LowLevelHookProc _keyboardProc;

        // Smart Typing Accumulator
        private readonly object _lock = new object();
        private StringBuilder _typingBuffer = new StringBuilder();
        private UiElementInfo _typingElement = null;
        private DateTime _lastTypingTime = DateTime.MinValue;
        private System.Threading.Timer _typingFlushTimer;

        // Mouse double click detection
        private DateTime _lastLeftDown = DateTime.MinValue;
        private POINT _lastLeftDownPt;

        // Events
        public event Action<UiActionEvent> OnActionRecorded;
        public event Action<bool> OnRecordingStateChanged;
        public event Action OnStopRequested;
        public event Action OnNoteRequested;

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
                foreach (var p in Process.GetProcessesByName("HIS"))
                {
                    TargetPids.Add((uint)p.Id);
                }
                foreach (var p in Process.GetProcessesByName("ConnectToEMR"))
                {
                    TargetPids.Add((uint)p.Id);
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
            var ev = new UiActionEvent
            {
                StepIndex = Interlocked.Increment(ref _stepCounter),
                Timestamp = DateTime.Now,
                Type = ActionType.CheckpointNote,
                Note = note,
                Element = new UiElementInfo { TopWindowText = "Ghi chú thủ công" }
            };
            FireActionRecorded(ev);
        }

        private bool IsPidAllowed(uint pid)
        {
            if (TargetPids.Count == 0) return true; // allow all if no target specified
            return TargetPids.Contains(pid);
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

                    // Privacy check: only process if inside HIS
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
                                // Detect double click manually within 400ms & 4px
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
                                    var ev = new UiActionEvent
                                    {
                                        StepIndex = Interlocked.Increment(ref _stepCounter),
                                        Timestamp = DateTime.Now,
                                        Type = actionType,
                                        Button = button,
                                        Element = elInfo
                                    };
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

                    // Global hotkeys (always active even outside HIS)
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
                                    var ev = new UiActionEvent
                                    {
                                        StepIndex = Interlocked.Increment(ref _stepCounter),
                                        Timestamp = DateTime.Now,
                                        Type = ActionType.KeyPress,
                                        KeyText = keyDesc,
                                        Element = elInfo
                                    };
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
            lock (_lock)
            {
                if (_typingElement == null)
                {
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
                _lastTypingTime = DateTime.Now;

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
            lock (_lock)
            {
                if (_typingBuffer.Length > 0)
                {
                    string text = _typingBuffer.ToString();
                    UiElementInfo el = _typingElement ?? HisUiaInspector.InspectFocusedElement();
                    _typingBuffer.Length = 0;
                    _typingElement = null;

                    var ev = new UiActionEvent
                    {
                        StepIndex = Interlocked.Increment(ref _stepCounter),
                        Timestamp = DateTime.Now,
                        Type = ActionType.TextInput,
                        TextValue = text,
                        Element = el
                    };
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
