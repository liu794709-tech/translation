using System.Collections.Generic;
using System.Windows;
using System.Collections.ObjectModel; // <-- 引入 ObservableCollection

namespace translation
{
    public partial class HistoryWindow : Window
    {
        // --- 核心新增 (1): 我们需要一个对 HistoryService 的引用 ---
        private readonly HistoryService _historyService;

        // --- 核心新增 (2): 使用 ObservableCollection ---
        // 这是一个“智能”的列表，当它的内容变化时，会自动通知 UI 更新
        private ObservableCollection<TranslationRecord> _records;

        // 构造函数现在需要传入 HistoryService
        public HistoryWindow(HistoryService historyService)
        {
            InitializeComponent();
            _historyService = historyService;
        }

        public void ShowHistory(List<TranslationRecord> records)
        {
            // 将传入的普通列表，转换为 ObservableCollection
            _records = new ObservableCollection<TranslationRecord>(records);
            // 将这个“智能”列表绑定到 ListView
            HistoryListView.ItemsSource = _records;
        }

        // --- 核心新增 (3): "删除" 按钮的点击事件 ---
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取用户在 ListView 中选中的那一条记录
            var selectedRecord = HistoryListView.SelectedItem as TranslationRecord;

            if (selectedRecord == null)
            {
                MessageBox.Show("请先在列表中选择一条要删除的记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 调用服务来从文件中删除
            _historyService.DeleteRecord(selectedRecord);
            // 从我们本地的“智能”列表中也移除它，UI 会自动更新
            _records.Remove(selectedRecord);
        }

        // --- 核心新增 (4): "清空" 按钮的点击事件 ---
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("您确定要清空所有历史记录吗？\n此操作不可恢复。", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // 调用服务来清空文件
                _historyService.ClearHistory();
                // 清空本地的“智能”列表，UI 会自动更新
                _records.Clear();
            }
        }
    }
}