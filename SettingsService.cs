using System.IO;
using Newtonsoft.Json;

// --- 1. 新增：在文件顶部定义触发方式的枚举 ---
// 把它放在这里，可以让其他文件（如 MainWindow, SettingsWindow）也能访问到
public enum TriggerMode
{
    MiddleMouse,
    RightMouse,
    AltAndLeftMouse
}

public class AppSettings
{
    public string BaiduAppId { get; set; } = string.Empty;
    public string BaiduSecretKey { get; set; } = string.Empty;
    // --- 2. 新增：在设置类中添加一个字段来保存用户的选择 ---
    public TriggerMode Trigger { get; set; } = TriggerMode.MiddleMouse; // 默认是中键
}

public class SettingsService
{
    private readonly string _settingsFilePath;
    public AppSettings Settings { get; private set; }

    public SettingsService()
    {
        string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        string appFolderPath = Path.Combine(appDataPath, "TranslationTool");
        Directory.CreateDirectory(appFolderPath);
        _settingsFilePath = Path.Combine(appFolderPath, "settings.json");

        Settings = LoadSettings();
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch { /* 忽略保存错误 */ }
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }
            string json = File.ReadAllText(_settingsFilePath);
            return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}