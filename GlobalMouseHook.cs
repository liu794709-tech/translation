using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

public class GlobalMouseHook : IDisposable
{
    // 定义鼠标事件的委托（delegate），这是 C# 中定义事件处理函数签名的方式
    public delegate void MouseHookCallback(Point point);
    public delegate void MouseWheelCallback(int delta);

    // 定义我们将要监听的事件
    public event MouseHookCallback MiddleButtonDown;
    public event MouseHookCallback MiddleButtonUp;
    public event MouseWheelCallback MouseWheelScroll;
    public event MouseHookCallback MouseMove;

    // Windows API 常量：WH_MOUSE_LL 表示低级鼠标钩子
    private const int WH_MOUSE_LL = 14;

    // Windows 消息常量
    private const int WM_MBUTTONDOWN = 0x0207; // 鼠标中键按下
    private const int WM_MBUTTONUP = 0x0208;   // 鼠标中键弹起
    private const int WM_MOUSEWHEEL = 0x020A;  // 鼠标滚轮滚动
    private const int WM_MOUSEMOVE = 0x0200;

    // 这是钩子回调函数的委托
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    // 导入 Windows User32.dll 中的函数
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // 钩子句柄，用来标识我们安装的钩子
    private IntPtr _hookID = IntPtr.Zero;
    // 保存回调函数，防止被垃圾回收
    private readonly LowLevelMouseProc _proc;

    public GlobalMouseHook()
    {
        // 构造函数中，我们将我们的回调方法 HookCallback 关联起来
        _proc = HookCallback;
    }

    // 安装钩子
    public void Install()
    {
        if (_hookID != IntPtr.Zero) return; // 防止重复安装
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            // 设置钩子
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // 卸载钩子（非常重要！）
    public void Uninstall()
    {
        if (_hookID == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookID);
        _hookID = IntPtr.Zero;
    }

    // 实现 IDisposable 接口，确保在对象销毁时卸载钩子
    public void Dispose()
    {
        Uninstall();
    }

    // 钩子回调函数：当鼠标事件发生时，Windows 会调用这个方法
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            // 从 lParam 中解析出鼠标的详细信息
            MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            Point currentPoint = new Point(hookStruct.pt.x, hookStruct.pt.y);

            // 判断具体是哪个事件
            switch ((int)wParam)
            {
                case WM_MBUTTONDOWN:
                    // 触发中键按下事件
                    MiddleButtonDown?.Invoke(currentPoint);
                    break;

                case WM_MBUTTONUP:
                    // 触发中键弹起事件
                    MiddleButtonUp?.Invoke(currentPoint);
                    break;

                case WM_MOUSEWHEEL:
                    // 滚轮事件的数据在高16位
                    int delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                    // 触发滚轮滚动事件
                    MouseWheelScroll?.Invoke(delta);
                    break;

                case WM_MOUSEMOVE: // <-- 添加这整个 case
                    MouseMove?.Invoke(currentPoint);
                    break;
            }
        }

        // 把事件传递给系统中的下一个钩子，否则其他程序可能无法收到鼠标事件
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    // 用于接收鼠标事件信息的结构体
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