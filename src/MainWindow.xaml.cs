using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using TeamsAccountManager.Services;

namespace TeamsAccountManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // タイトルにバージョンを追加
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"Teams Account Manager v{version?.Major}.{version?.Minor}.{version?.Build}";
            
            // 言語選択イベント
            LanguageComboBox.SelectionChanged += OnLanguageChanged;
            
            // 初期状態表示
            UpdateStatus("準備完了", false);
            UserInfoTextBlock.Text = "未ログイン";
        }
        
        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedIndex < 0) return;
            
            var cultures = new[] { "ja-JP", "en-US", "vi-VN" };
            var selectedCulture = cultures[LanguageComboBox.SelectedIndex];
            
            // カルチャーを設定
            var culture = new CultureInfo(selectedCulture);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            
            // UI要素を更新（簡易版）
            switch (LanguageComboBox.SelectedIndex)
            {
                case 0: // 日本語
                    Title = "Teams Account Manager";
                    LogoutButton.Content = "ログアウト";
                    UpdateButtonsText("Download from Server", "Upload to Server", "一括編集", "エクスポート", "インポート");
                    break;
                case 1: // English
                    Title = "Teams Account Manager";
                    LogoutButton.Content = "Logout";
                    UpdateButtonsText("Download from Server", "Upload to Server", "Bulk Edit", "Export", "Import");
                    break;
                case 2: // Tiếng Việt
                    Title = "Teams Account Manager";
                    LogoutButton.Content = "Đăng xuất";
                    UpdateButtonsText("Download from Server", "Upload to Server", "Sửa hàng loạt", "Xuất", "Nhập");
                    break;
            }
            
            StatusTextBlock.Text = $"Language changed to {selectedCulture}";
        }
        
        private void UpdateButtonsText(string refresh, string save, string bulkEdit, string export, string import)
        {
            // UserListViewのボタンのテキストを更新する処理（後で実装）
            // 現在のコンテンツがUserListView_Simpleの場合のみ更新
            if (MainContent.Content is Views.UserListView_Simple userListView)
            {
                userListView.RefreshButton.Content = $"⬇️ {refresh}";
                userListView.SaveButton.Content = $"⬆️ {save}";
                userListView.BulkEditButton.Content = $"✏️ {bulkEdit}";
                userListView.ExportButton.Content = $"📥 {export}";
                userListView.ImportButton.Content = $"📤 {import}";
            }
        }
        
        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("ログアウトしますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    UpdateStatus("ログアウト中...", true);
                    
                    // 認証サービスでログアウト
                    var authService = App.GetService<AuthenticationService>();
                    await authService.SignOutAsync();
                    
                    // ログイン画面に戻る
                    UserInfoTextBlock.Text = "未ログイン";
                    var loginView = new Views.LoginView();
                    NavigateToContent(loginView);
                    
                    UpdateStatus("ログアウトしました", false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ログアウト中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus("エラーが発生しました", false);
                }
            }
        }
        
        public void UpdateStatus(string message, bool showProgress = false)
        {
            StatusTextBlock.Text = message;
            StatusProgressBar.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
            
            if (!showProgress)
            {
                StatusProgressBar.IsIndeterminate = false;
                StatusProgressBar.Value = 0;
            }
            else
            {
                StatusProgressBar.IsIndeterminate = true;
            }
        }
        
        public void NavigateToContent(object content)
        {
            MainContent.Content = content;
        }
    }
}