using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // <-- 引入 LINQ 命名空间
using Newtonsoft.Json;

public class HistoryService
{
    private readonly string _historyFilePath;
    private List<TranslationRecord> _historyCache;

    public HistoryService()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolderPath = Path.Combine(appDataPath, "TranslationTool");
        Directory.CreateDirectory(appFolderPath);
        _historyFilePath = Path.Combine(appFolderPath, "history.json");

        _historyCache = LoadHistory();
    }

    public List<TranslationRecord> GetHistory()
    {
        return _historyCache;
    }

    public void AddRecord(string original, string translated)
    {
        var record = new TranslationRecord
        {
            OriginalText = original,
            TranslatedText = translated,
            Timestamp = DateTime.Now
        };
        _historyCache.Insert(0, record);
        SaveHistory();
    }

    // --- 核心新增：删除一条指定的记录 ---
    public void DeleteRecord(TranslationRecord recordToDelete)
    {
        if (recordToDelete == null) return;

        // 从缓存列表中移除指定的记录对象
        _historyCache.Remove(recordToDelete);

        // 保存更改
        SaveHistory();
    }

    // --- 核心新增：清空所有历史记录 ---
    public void ClearHistory()
    {
        _historyCache.Clear(); // 清空缓存列表
        SaveHistory();         // 将空的列表保存回文件
    }

    private List<TranslationRecord> LoadHistory()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                return new List<TranslationRecord>();
            }
            string json = File.ReadAllText(_historyFilePath);
            return JsonConvert.DeserializeObject<List<TranslationRecord>>(json) ?? new List<TranslationRecord>();
        }
        catch (Exception)
        {
            return new List<TranslationRecord>();
        }
    }

    private void SaveHistory()
    {
        try
        {
            string json = JsonConvert.SerializeObject(_historyCache, Formatting.Indented);
            File.WriteAllText(_historyFilePath, json);
        }
        catch (Exception)
        {
            // 忽略保存错误
        }
    }
}