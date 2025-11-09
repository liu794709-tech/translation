using System;
using System.Diagnostics;
using System.IO; // 1. 引入 IO 命名空间
using System.Reflection; // 2. 引入 Reflection 命名空间
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace translation
{
    public partial class MainWindow : Window
    {
        // ... (所有变量声明不变) ...
        private GlobalMouseHook _mouseHook;
        private SelectionWindow _selectionWindow;
        private OcrService _ocrService;
        private TranslationService _translationService;
        private ResultWindow _resultWindow;

        private bool isMiddleButtonDown = false;
        private Point selectionStartPoint;

        public MainWindow()
        {
            // --- 这是本次修复最最核心的一步 ---
            // 3. 在所有操作之前，强制设置底层库的搜索路径
            try
            {
                string exeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", $"{exeFolder}\\x64;{path}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置环境变量失败: " + ex.ToString());
            }

            InitializeComponent(); // 保持在设置之后

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        // ... (所有其他方法保持不变) ...

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _selectionWindow = new SelectionWindow();
            _ocrService = new OcrService();
            _translationService = new TranslationService();
            _resultWindow = new ResultWindow();
            _mouseHook = new GlobalMouseHook();
            _mouseHook.MiddleButtonDown += MouseHook_MiddleButtonDown;
            _mouseHook.MiddleButtonUp += HandleMiddleButtonUpAsync;
            _mouseHook.MouseMove += MouseHook_MouseMove;
            _mouseHook.Install();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _selectionWindow?.Close();
            _resultWindow?.Close();
            _mouseHook?.Uninstall();
        }

        private Matrix GetDpiTransformMatrix()
        {
            var source = PresentationSource.FromVisual(this);
            return source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        }

        private void MouseHook_MiddleButtonDown(Point point)
        {
            _resultWindow.Hide();
            isMiddleButtonDown = true;
            selectionStartPoint = point;
            var transform = GetDpiTransformMatrix();
            _selectionWindow.Left = point.X / transform.M11;
            _selectionWindow.Top = point.Y / transform.M22;
            _selectionWindow.Width = 0;
            _selectionWindow.Height = 0;
            _selectionWindow.Show();
        }

        private async void HandleMiddleButtonUpAsync(Point point)
        {
            if (!isMiddleButtonDown) return;
            isMiddleButtonDown = false;
            System.Windows.Rect selectionRect = new System.Windows.Rect(_selectionWindow.Left, _selectionWindow.Top, _selectionWindow.Width, _selectionWindow.Height);
            _selectionWindow.Hide();
            if (selectionRect.Width < 10 || selectionRect.Height < 10) return;
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
                        _resultWindow.ShowResult(translatedText);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("处理截图识别时发生错误: " + ex.Message);
            }
        }

        private void MouseHook_MouseMove(Point point)
        {
            if (isMiddleButtonDown)
            {
                var transform = GetDpiTransformMatrix();
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
    }
}