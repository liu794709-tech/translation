using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

public class GlobalMouseHook : IDisposable
{
    public enum MouseButton
    {
        None,
        Left,
        Right,
        Middle
    }

    [Flags]
    public enum ModifierKeys : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8 // 虽然我们不用 Win 键，但定义出来更完整
    }

    public delegate void MouseKeyCombinationCallback(Point point, MouseButton button, ModifierKeys modifiers);
    public event MouseKeyCombinationCallback ButtonDown;
    public event MouseKeyCombinationCallback ButtonUp;

    public delegate void MouseHookCallback(Point point);
    public delegate void MouseWheelCallback(int delta);

    public event MouseHookCallback MiddleButtonDown;
    public event MouseHookCallback MiddleButtonUp;
    public event MouseWheelCallback MouseWheelScroll;
    public event MouseHookCallback MouseMove;

    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEMOVE = 0x0200;

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // ALT 键的虚拟键码

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private IntPtr _hookID = IntPtr.Zero;
    private readonly LowLevelMouseProc _proc;

    public GlobalMouseHook()
    {
        _proc = HookCallback;
    }

    private ModifierKeys GetCurrentModifiers()
    {
        ModifierKeys modifiers = ModifierKeys.None;
        if ((GetKeyState(VK_MENU) & 0x8000) != 0) modifiers |= ModifierKeys.Alt;
        if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) modifiers |= ModifierKeys.Control;
        if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) modifiers |= ModifierKeys.Shift;
        return modifiers;
    }

    public void Install()
    {
        if (_hookID != IntPtr.Zero) return;
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    public void Uninstall()
    {
        if (_hookID == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookID);
        _hookID = IntPtr.Zero;
    }

    public void Dispose()
    {
        Uninstall();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            object marshalledMouseStruct = Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            if (marshalledMouseStruct != null)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)marshalledMouseStruct;
                Point currentPoint = new Point(hookStruct.pt.x, hookStruct.pt.y);

                ModifierKeys modifiers = GetCurrentModifiers();
                MouseButton button = MouseButton.None;
                bool isDown = false;

                // 判断具体是哪个鼠标事件
                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN: button = MouseButton.Left; isDown = true; break;
                    case WM_LBUTTONUP: button = MouseButton.Left; isDown = false; break;
                    case WM_RBUTTONDOWN: button = MouseButton.Right; isDown = true; break;
                    case WM_RBUTTONUP: button = MouseButton.Right; isDown = false; break;
                    case WM_MBUTTONDOWN: button = MouseButton.Middle; isDown = true; break;
                    case WM_MBUTTONUP: button = MouseButton.Middle; isDown = false; break;
                    case WM_MOUSEMOVE: MouseMove?.Invoke(currentPoint); break;
                }

                // 如果是按键事件，则触发统一的 ButtonDown 或 ButtonUp 事件
                if (button != MouseButton.None)
                {
                    if (isDown)
                        ButtonDown?.Invoke(currentPoint, button, modifiers);
                    else
                        ButtonUp?.Invoke(currentPoint, button, modifiers);
                }
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

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
}