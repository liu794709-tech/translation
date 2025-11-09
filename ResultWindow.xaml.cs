using System.Windows;

namespace translation // <-- 确保这是你的项目命名空间
{
    public partial class ResultWindow : Window
    {
        public ResultWindow()
        {
            InitializeComponent();
        }

        // 新增一个公共方法，用于设置并显示翻译结果
        public void ShowResult(string text)
        {
            ResultTextBlock.Text = text;
            this.Show();
        }
    }
}