using System.Windows;

namespace translation
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            LoadSettings();
        }

        private void LoadSettings()
        {
            AppIdTextBox.Text = _settingsService.Settings.BaiduAppId;
            SecretKeyTextBox.Text = _settingsService.Settings.BaiduSecretKey;

            // --- 核心新增：根据保存的设置，勾选对应的单选按钮 ---
            switch (_settingsService.Settings.Trigger)
            {
                case TriggerMode.MiddleMouse:
                    MiddleMouseRadio.IsChecked = true;
                    break;
                case TriggerMode.RightMouse:
                    RightMouseRadio.IsChecked = true;
                    break;
                case TriggerMode.AltAndLeftMouse:
                    AltLeftMouseRadio.IsChecked = true;
                    break;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.Settings.BaiduAppId = AppIdTextBox.Text;
            _settingsService.Settings.BaiduSecretKey = SecretKeyTextBox.Text;

            // --- 核心新增：根据用户勾选的单选按钮，保存设置 ---
            if (MiddleMouseRadio.IsChecked == true)
            {
                _settingsService.Settings.Trigger = TriggerMode.MiddleMouse;
            }
            else if (RightMouseRadio.IsChecked == true)
            {
                _settingsService.Settings.Trigger = TriggerMode.RightMouse;
            }
            else if (AltLeftMouseRadio.IsChecked == true)
            {
                _settingsService.Settings.Trigger = TriggerMode.AltAndLeftMouse;
            }

            _settingsService.SaveSettings();

            MessageBox.Show("设置已保存！\n\n注意：新的触发方式将在您重启程序后生效。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}