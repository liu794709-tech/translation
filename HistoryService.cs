using System;
using System.IO;
using System.Threading.Tasks;

public class HistoryService
{
    // --- 核心修改：将私有变量改为公共属性 ---
    public string HistoryFilePath { get; private set; }

    public HistoryService()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string historyDir = Path.Combine(appDataPath, "MyScreenTranslator");
        Directory.CreateDirectory(historyDir);

        // 使用公共属性来存储路径
        HistoryFilePath = Path.Combine(historyDir, "history.tsv");
    }

    // AddEntryAsync 方法保持不变...
    public async Task AddEntryAsync(string originalText, string translatedText)
    {
        // ... (代码完全不变)
    }
}