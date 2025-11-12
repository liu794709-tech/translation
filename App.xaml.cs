using System;
using System.Threading; // <-- 1. 引入线程处理所需的命名空间
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace translation
{
    public partial class App : Application
    {
        // --- 2. 核心新增：定义一个全局的、唯一的 Mutex ---
        private static Mutex _mutex = null;

        // ... 其他所有字段保持不变 ...
        private TaskbarIcon _notifyIcon;
        private GlobalMouseHook _mouseHook;
        private SettingsService _settingsService;
        private HistoryService _historyService;
        private OcrService _ocrService;
        private MainWindow _mainWindow;
        private SettingsWindow _settingsWindow;
        private HistoryWindow _historyWindow;
        private SelectionWindow _selectionWindow;
        private ResultWindow _resultWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            // --- 3. 核心新增：在所有操作之前，进行单例检查 ---

            // 创建一个唯一的、全局的应用程序名称
            const string appName = "Global\\TranslationTool_Mutex_2A8A2E8D_8B4E_4C4F_A8D7_3B6C9B0E1F2A";
            bool createdNew;

            // 尝试创建并获取 Mutex
            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // 如果 createdNew 是 false，意味着 Mutex 已经存在，说明已有实例在运行
                MessageBox.Show("划词翻译工具已经在运行中！\n\n请在屏幕右下角的托盘区找到它的图标。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

                // 立即关闭当前这个（第二个）实例
                Application.Current.Shutdown();
                return; // 必须 return，以阻止后续代码的执行
            }

            // --- 如果是第一个实例，则正常执行所有启动逻辑 ---

            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _settingsService = new SettingsService();
            _historyService = new HistoryService();
            _ocrService = new OcrService();
            _selectionWindow = new SelectionWindow();
            _resultWindow = new ResultWindow();

            _mainWindow = new MainWindow();
            _mainWindow.InitializeServices(_settingsService, _selectionWindow, _ocrService, _resultWindow, _historyService);

            _notifyIcon = new TaskbarIcon();
            _notifyIcon.ToolTipText = "划词翻译工具";
            _notifyIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/translation.ico"));

            var contextMenu = new ContextMenu();

            var settingsMenuItem = new MenuItem { Header = "设置" };
            settingsMenuItem.Click += SettingsMenuItem_Click;
            contextMenu.Items.Add(settingsMenuItem);

            var historyMenuItem = new MenuItem { Header = "查看翻译历史" };
            historyMenuItem.Click += HistoryMenuItem_Click;
            contextMenu.Items.Add(historyMenuItem);

            contextMenu.Items.Add(new Separator());

            var exitMenuItem = new MenuItem { Header = "退出" };
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenu = contextMenu;

            _mouseHook = new GlobalMouseHook();
            _mouseHook.Install();
            _mainWindow.SubscribeToMouseHook(_mouseHook);

            // --- (可选但推荐) 首次启动时给一个提示 ---
            _notifyIcon.ShowBalloonTip("程序已启动", "划词翻译工具已在后台运行。", BalloonIcon.Info);
        }

        // ... 其他所有方法完全保持不变 ...

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Activate();
            }
            else
            {
                _settingsWindow = new SettingsWindow(_settingsService);
                _settingsWindow.Show();
            }
        }

        private void HistoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_historyWindow != null && _historyWindow.IsVisible)
            {
                _historyWindow.Activate();
            }
            else
            {
                // --- 核心修正：创建窗口时，把 HistoryService 传进去 ---
                _historyWindow = new HistoryWindow(_historyService);
                _historyWindow.ShowHistory(_historyService.GetHistory());
                _historyWindow.Show();
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow?.Close();
            _settingsWindow?.Close();
            _historyWindow?.Close();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            _mouseHook?.Dispose();
            // 确保在程序退出时释放 Mutex
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}