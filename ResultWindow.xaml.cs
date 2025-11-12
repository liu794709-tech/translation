using System;
using System.Windows;
using System.Windows.Controls; // <-- 核心修正：添加这一行 using 声明
using System.Windows.Input;
using System.Windows.Threading;

namespace translation
{
    public partial class ResultWindow : Window
    {
        private DispatcherTimer _closeTimer;

        public ResultWindow()
        {
            InitializeComponent();

            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromSeconds(5);
            _closeTimer.Tick += CloseTimer_Tick;
        }

        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            _closeTimer.Stop();
            this.Hide();
        }

        public void SetResultText(string text)
        {
            ResultTextBox.Text = text;
        }



        public void ShowAndAutoHide()
        {
            AdjustPosition();
            this.Show();

            this.Focus();

            _closeTimer.Stop();
            _closeTimer.Start();
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            _closeTimer.Stop();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (!this.IsMouseCaptured)
            {
                _closeTimer.Start();
            }
        }

        private void AdjustPosition()
        {
            this.UpdateLayout();
            var screen = SystemParameters.WorkArea;
            double actualRight = this.Left + this.ActualWidth;
            double actualBottom = this.Top + this.ActualHeight;

            if (actualRight > screen.Right)
            {
                this.Left -= (actualRight - screen.Right + 10);
            }
            if (actualBottom > screen.Bottom)
            {
                this.Top -= (actualBottom - screen.Bottom + 10);
            }
            if (this.Left < screen.Left)
            {
                this.Left = screen.Left;
            }
            if (this.Top < screen.Top)
            {
                this.Top = screen.Top;
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 检查用户是否在 TextBox 上点击以选择文本
            // 如果是，则不触发拖动
            if (e.OriginalSource is TextBox)
            {
                return;
            }

            // 如果用户点击的是窗口的其他部分（比如边距），则开始拖动
            this.DragMove();
        }
    }
}