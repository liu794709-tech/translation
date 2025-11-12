using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace translation
{
    public partial class MainWindow : Window
    {
        private SelectionWindow _selectionWindow;
        private OcrService _ocrService;
        private TranslationService _translationService;
        private ResultWindow _resultWindow;
        private HistoryService _historyService;
        // --- 1. 新增：保存对 SettingsService 的引用 ---
        private SettingsService _settingsService;

        private bool isSelectionActive = false; // 使用一个更通用的名字
        private Point selectionStartPoint;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void InitializeServices(
            SettingsService settingsService,
            SelectionWindow selectionWindow,
            OcrService ocrService,
            ResultWindow resultWindow,
            HistoryService historyService)
        {
            // --- 2. 保存 SettingsService 的引用 ---
            _settingsService = settingsService;
            _selectionWindow = selectionWindow;
            _ocrService = ocrService;
            _resultWindow = resultWindow;
            _historyService = historyService;
            _translationService = new TranslationService(settingsService);
            this.Closing += MainWindow_Closing;
        }

        // --- 3. 改造：订阅新的、更通用的事件 ---
        public void SubscribeToMouseHook(GlobalMouseHook mouseHook)
        {
            if (mouseHook != null)
            {
                // 移除旧的订阅
                // mouseHook.MiddleButtonDown -= MouseHook_MiddleButtonDown;
                // mouseHook.MiddleButtonUp -= HandleMiddleButtonUpAsync;

                // 添加新的订阅
                mouseHook.ButtonDown += OnButtonDown;
                mouseHook.ButtonUp += OnButtonUp;
                mouseHook.MouseMove += OnMouseMove;
            }
        }

        // --- 4. 新增：统一的按下事件处理器 ---
        private void OnButtonDown(Point point, GlobalMouseHook.MouseButton button, GlobalMouseHook.ModifierKeys modifiers)
        {
            // 如果已经在选择中，则忽略新的按下事件
            if (isSelectionActive) return;

            var triggerMode = _settingsService.Settings.Trigger;
            bool isTriggered = false;

            // 检查当前事件是否匹配用户设置的触发方式
            switch (triggerMode)
            {
                case TriggerMode.MiddleMouse:
                    isTriggered = (button == GlobalMouseHook.MouseButton.Middle && modifiers == GlobalMouseHook.ModifierKeys.None);
                    break;
                case TriggerMode.RightMouse:
                    isTriggered = (button == GlobalMouseHook.MouseButton.Right && modifiers == GlobalMouseHook.ModifierKeys.None);
                    break;
                case TriggerMode.AltAndLeftMouse:
                    // 使用 & 运算符检查是否包含 Alt 键，允许同时按下其他修饰键（如 Shift+Alt）
                    isTriggered = (button == GlobalMouseHook.MouseButton.Left && (modifiers & GlobalMouseHook.ModifierKeys.Alt) != 0);
                    break;
            }

            if (isTriggered)
            {
                _resultWindow.Hide();
                isSelectionActive = true;
                selectionStartPoint = point;

                _selectionWindow.Width = 0;
                _selectionWindow.Height = 0;
                _selectionWindow.Show();

                var transform = GetDpiTransformMatrix();
                if (transform.M11 == 0 || transform.M22 == 0) return;

                _selectionWindow.Left = point.X / transform.M11;
                _selectionWindow.Top = point.Y / transform.M22;
            }
        }

        // --- 5. 新增：统一的弹起事件处理器 ---
        private void OnButtonUp(Point point, GlobalMouseHook.MouseButton button, GlobalMouseHook.ModifierKeys modifiers)
        {
            // 只要之前开始了选择，任何鼠标按键的弹起都应该结束选择
            if (isSelectionActive)
            {
                isSelectionActive = false;
                System.Windows.Rect selectionRect = new System.Windows.Rect(_selectionWindow.Left, _selectionWindow.Top, _selectionWindow.Width, _selectionWindow.Height);
                _selectionWindow.Hide();
                if (selectionRect.Width < 10 || selectionRect.Height < 10) return;

                // 为了避免 UI 线程阻塞，我们将异步操作放到一个独立的方法里
                ProcessSelectionAsync(selectionRect);
            }
        }

        // 异步处理方法
        private async void ProcessSelectionAsync(Rect selectionRect)
        {
            try
            {
                string ocrResult = await _ocrService.RecognizeTextAsync(selectionRect);
                if (!string.IsNullOrWhiteSpace(ocrResult))
                {
                    string translatedText = await _translationService.TranslateAsync(ocrResult, "auto", "zh-CN");
                    if (!string.IsNullOrWhiteSpace(translatedText))
                    {
                        _resultWindow.Left = selectionRect.Right;
                        _resultWindow.Top = selectionRect.Top;
                        _resultWindow.SetResultText(translatedText);
                        _resultWindow.ShowAndAutoHide();
                        _historyService.AddRecord(ocrResult, translatedText);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("处理截图识别时发生错误: " + ex.Message);
            }
        }

        // --- 6. 改造：将 MouseMove 处理器重命名并使用新状态变量 ---
        private void OnMouseMove(Point point)
        {
            if (isSelectionActive)
            {
                var transform = GetDpiTransformMatrix();
                if (transform.M11 == 0 || transform.M22 == 0) return;
                var x = Math.Min(point.X, selectionStartPoint.X);
                var y = Math.Min(point.Y, selectionStartPoint.Y);
                var width = Math.Abs(point.X - selectionStartPoint.X);
                var height = Math.Abs(point.Y - selectionStartPoint.Y);
                _selectionWindow.Left = x / transform.M11;
                _selectionWindow.Top = y / transform.M22;
                _selectionWindow.Width = width / transform.M11;
                _selectionWindow.Height = height / transform.M22;
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _selectionWindow?.Close();
            _resultWindow?.Close();
        }

        private Matrix GetDpiTransformMatrix()
        {
            PresentationSource source = null;
            foreach (Window window in Application.Current.Windows)
            {
                if (window.IsVisible)
                {
                    source = PresentationSource.FromVisual(window);
                    break;
                }
            }
            if (source == null)
            {
                source = PresentationSource.FromVisual(Application.Current.MainWindow);
            }
            return source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        }
    }
}