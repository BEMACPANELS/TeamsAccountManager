using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using TeamsAccountManager.Models;
using TeamsAccountManager.Services;

namespace TeamsAccountManager.Views
{
    /// <summary>
    /// UserListView_Simple.xaml の相互作用ロジック
    /// </summary>
    public partial class UserListView_Simple : UserControl
    {
        private ObservableCollection<User> users = new ObservableCollection<User>();
        private Dictionary<string, Dictionary<string, object>> changedUsers = new Dictionary<string, Dictionary<string, object>>();
        private List<User> selectedUsers = new List<User>();
        private ICollectionView usersView;
        private Dictionary<string, string> columnFilters = new Dictionary<string, string>();
        
        // 各列のユニークな値を保持
        private HashSet<string> uniqueDomains = new HashSet<string>();
        private HashSet<string> uniqueDepartments = new HashSet<string>();
        private HashSet<string> uniqueJobTitles = new HashSet<string>();
        private HashSet<string> uniqueCountries = new HashSet<string>();
        private HashSet<string> uniqueOffices = new HashSet<string>();
        private HashSet<string> uniqueUsageLocations = new HashSet<string>();
        
        private bool _isInitializing = true;
        
        public UserListView_Simple()
        {
            InitializeComponent();
            
            // コレクションビューを作成
            usersView = CollectionViewSource.GetDefaultView(users);
            usersView.Filter = UserFilter;
            
            // データグリッドにバインド
            UsersDataGrid.ItemsSource = usersView;
            
            // 初期化完了
            _isInitializing = false;
            
            // 初回データ読み込みは削除（手動でダウンロードボタンを押すようにする）
            // _ = LoadRealDataAsync();
        }
        
        private void LoadSampleData()
        {
            // テスト用のサンプルデータ
            users.Clear();
            users.Add(new User 
            { 
                Id = "user001",
                DisplayName = "山田 太郎", 
                Email = "yamada@example.com", 
                Department = "営業部", 
                JobTitle = "マネージャー", 
                AccountEnabled = true 
            });
            users.Add(new User 
            { 
                Id = "user002",
                DisplayName = "鈴木 花子", 
                Email = "suzuki@example.com", 
                Department = "開発部", 
                JobTitle = "エンジニア", 
                AccountEnabled = true 
            });
            users.Add(new User 
            { 
                Id = "user003",
                DisplayName = "佐藤 次郎", 
                Email = "sato@example.com", 
                Department = "人事部", 
                JobTitle = "担当者", 
                AccountEnabled = false 
            });
            
            UpdateStatus();
        }
        
        private async Task LoadRealDataAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                StatusTextBlock.Text = "ユーザー情報を読み込み中...";
                
                var graphService = App.GetService<GraphApiService>();
                var loggerFactory = App.GetService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<UserListView_Simple>();
                
                logger.LogInformation("LoadRealDataAsync: 開始");
                
                var progress = new Progress<int>(count =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusTextBlock.Text = $"ユーザー情報を読み込み中... {count}件";
                    });
                });
                
                var userList = await graphService.GetUsersAsync(progress);
                logger.LogInformation($"LoadRealDataAsync: {userList.Count}件取得完了");
                
                users.Clear();
                foreach (var user in userList)
                {
                    users.Add(user);
                }
                
                // ユニークな値を収集
                CollectUniqueValues();
                
                logger.LogInformation("LoadRealDataAsync: UIへの反映完了");
                UpdateStatus();
            }
            catch (Exception ex)
            {
                var loggerFactory = App.GetService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<UserListView_Simple>();
                logger.LogError(ex, "LoadRealDataAsync: エラー発生");
                
                MessageBox.Show($"データ読み込み中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                // エラー時はサンプルデータを表示
                LoadSampleData();
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                RefreshButton.IsEnabled = false;
                StatusTextBlock.Text = "更新中...";
                
                // 実際のデータを再読み込み
                await LoadRealDataAsync();
                
                MessageBox.Show("ユーザー一覧を更新しました", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                RefreshButton.IsEnabled = true;
            }
        }
        
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "エクスポート先を選択",
                    Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                    DefaultExt = "xlsx",
                    FileName = $"Users_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    LoadingOverlay.Visibility = Visibility.Visible;
                    StatusTextBlock.Text = "エクスポート中...";
                    
                    // フィルター適用後のユーザーリストを取得
                    var filteredUsers = usersView.Cast<User>().ToList();
                    
                    bool success = false;
                    if (dialog.FilterIndex == 1) // Excel
                    {
                        var excelService = App.GetService<ExcelService>();
                        success = await excelService.ExportUsersAsync(filteredUsers, dialog.FileName);
                    }
                    else // CSV
                    {
                        var csvService = App.GetService<CsvService>();
                        success = await csvService.ExportUsersAsync(filteredUsers, dialog.FileName);
                    }
                    
                    if (success)
                    {
                        var message = $"エクスポートが完了しました\n{dialog.FileName}\n\n";
                        if (filteredUsers.Count < users.Count)
                        {
                            message += $"エクスポート件数: {filteredUsers.Count} / {users.Count} 件（フィルター適用）";
                        }
                        else
                        {
                            message += $"エクスポート件数: {filteredUsers.Count} 件";
                        }
                        MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("エクスポート中にエラーが発生しました", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エクスポート中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                UpdateStatus();
            }
        }
        
        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "インポートファイルを選択",
                    Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    LoadingOverlay.Visibility = Visibility.Visible;
                    StatusTextBlock.Text = "インポート中...";
                    
                    List<User>? importedUsers = null;
                    if (dialog.FilterIndex == 1) // Excel
                    {
                        var excelService = App.GetService<ExcelService>();
                        importedUsers = await excelService.ImportUsersAsync(dialog.FileName) ?? new List<User>();
                    }
                    else // CSV
                    {
                        var csvService = App.GetService<CsvService>();
                        importedUsers = await csvService.ImportUsersAsync(dialog.FileName) ?? new List<User>();
                    }
                    
                    if (importedUsers != null && importedUsers.Count > 0)
                    {
                        var result = MessageBox.Show($"{importedUsers.Count}件のユーザーをインポートしますか？\n\n注意: 現在の表示は置き換えられます", 
                            "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            users.Clear();
                            foreach (var user in importedUsers)
                            {
                                users.Add(user);
                            }
                            UpdateStatus();
                            MessageBox.Show($"{importedUsers.Count}件のユーザーをインポートしました", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("インポート可能なデータが見つかりませんでした", "情報", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"インポート中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                UpdateStatus();
            }
        }
        
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            if (e.Row.DataContext is User user && e.Column is DataGridBoundColumn column)
            {
                var binding = column.Binding as System.Windows.Data.Binding;
                var propertyName = binding?.Path?.Path;
                
                if (propertyName != null)
                {
                    object? newValue = null;
                    
                    if (e.EditingElement is TextBox textBox)
                    {
                        newValue = textBox.Text;
                    }
                    else if (e.EditingElement is CheckBox checkBox)
                    {
                        newValue = checkBox.IsChecked ?? false;
                    }
                    
                    if (newValue != null)
                    {
                        // 変更を追跡
                        if (!changedUsers.ContainsKey(user.Id))
                        {
                            changedUsers[user.Id] = new Dictionary<string, object>();
                        }
                        
                        changedUsers[user.Id][propertyName] = newValue;
                        
                        // 保存ボタンを有効化
                        SaveButton.IsEnabled = changedUsers.Count > 0;
                        UpdateStatus();
                    }
                }
            }
        }
        
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUsers = UsersDataGrid.SelectedItems.Cast<User>().ToList();
            BulkEditButton.IsEnabled = selectedUsers.Count > 1;
        }
        
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                SaveButton.IsEnabled = false;
                StatusTextBlock.Text = "変更を保存中...";
                
                var graphService = App.GetService<GraphApiService>();
                var successCount = 0;
                var errorCount = 0;
                var errors = new List<string>();
                
                // 各ユーザーの変更を保存
                foreach (var userChange in changedUsers)
                {
                    var userId = userChange.Key;
                    var changes = userChange.Value;
                    var user = users.FirstOrDefault(u => u.Id == userId);
                    
                    if (user != null)
                    {
                        try
                        {
                            // 変更されたプロパティを適用
                            foreach (var change in changes)
                            {
                                var property = user.GetType().GetProperty(change.Key);
                                if (property != null)
                                {
                                    property.SetValue(user, change.Value);
                                }
                            }
                            
                            // Graph APIで更新
                            var success = await graphService.UpdateUserAsync(user);
                            if (success)
                            {
                                successCount++;
                            }
                            else
                            {
                                errorCount++;
                                errors.Add($"{user.DisplayName}: 更新に失敗しました");
                            }
                        }
                        catch (Exception userEx)
                        {
                            errorCount++;
                            errors.Add($"{user.DisplayName}: {userEx.Message}");
                        }
                    }
                }
                
                // 結果を表示
                var message = $"保存結果:\n成功: {successCount} 件\n失敗: {errorCount} 件";
                if (errors.Count > 0)
                {
                    message += $"\n\nエラー詳細:\n{string.Join("\n", errors.Take(5))}";
                    if (errors.Count > 5)
                    {
                        message += $"\n... 他 {errors.Count - 5} 件のエラー";
                    }
                }
                
                if (successCount > 0)
                {
                    changedUsers.Clear();
                }
                
                MessageBox.Show(message, errorCount > 0 ? "保存完了（一部エラー）" : "保存完了", 
                    MessageBoxButton.OK, errorCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = changedUsers.Count > 0;
            }
        }
        
        private void BulkEditButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BulkEditDialog_Simple(selectedUsers);
            dialog.Owner = Window.GetWindow(this);
            
            if (dialog.ShowDialog() == true)
            {
                var changes = dialog.GetChanges();
                
                // 選択したユーザーに変更を適用
                foreach (var user in selectedUsers)
                {
                    foreach (var change in changes)
                    {
                        var property = user.GetType().GetProperty(change.Key);
                        if (property != null && change.Value != null)
                        {
                            property.SetValue(user, change.Value);
                            
                            // 変更を追跡
                            if (!changedUsers.ContainsKey(user.Id))
                            {
                                changedUsers[user.Id] = new Dictionary<string, object>();
                            }
                            changedUsers[user.Id][change.Key] = change.Value;
                        }
                    }
                }
                
                // DataGridを更新
                UsersDataGrid.Items.Refresh();
                SaveButton.IsEnabled = changedUsers.Count > 0;
                UpdateStatus();
            }
        }
        
        private void UpdateStatus()
        {
            var visibleCount = usersView?.Cast<User>().Count() ?? users.Count;
            
            if (changedUsers.Count > 0)
            {
                if (visibleCount < users.Count)
                {
                    StatusTextBlock.Text = $"{visibleCount} / {users.Count} 件のユーザーを表示中 ({changedUsers.Count} 件の未保存の変更)";
                }
                else
                {
                    StatusTextBlock.Text = $"{users.Count} 件のユーザーを表示中 ({changedUsers.Count} 件の未保存の変更)";
                }
            }
            else
            {
                if (visibleCount < users.Count)
                {
                    StatusTextBlock.Text = $"{visibleCount} / {users.Count} 件のユーザーを表示中";
                }
                else
                {
                    StatusTextBlock.Text = $"{users.Count} 件のユーザーを表示中";
                }
            }
        }
        
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // フィルターを更新
            usersView?.Refresh();
            UpdateStatus();
        }
        
        private bool UserFilter(object item)
        {
            var user = item as User;
            if (user == null)
                return false;
            
            // 全体検索フィルター
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                var searchText = SearchTextBox.Text.ToLower();
                var matchesSearch = (user.DisplayName?.ToLower().Contains(searchText) ?? false) ||
                                  (user.Email?.ToLower().Contains(searchText) ?? false) ||
                                  (user.Department?.ToLower().Contains(searchText) ?? false) ||
                                  (user.JobTitle?.ToLower().Contains(searchText) ?? false) ||
                                  (user.EmailDomain?.ToLower().Contains(searchText) ?? false);
                
                if (!matchesSearch)
                    return false;
            }
            
            // 列ごとのフィルター
            foreach (var filter in columnFilters)
            {
                if (string.IsNullOrWhiteSpace(filter.Value))
                    continue;
                
                var filterValue = filter.Value.ToLower();
                bool matches = false;
                
                switch (filter.Key)
                {
                    case "DisplayName":
                        matches = user.DisplayName?.ToLower().Contains(filterValue) ?? false;
                        break;
                    case "GivenName":
                        matches = user.GivenName?.ToLower().Contains(filterValue) ?? false;
                        break;
                    case "Surname":
                        matches = user.Surname?.ToLower().Contains(filterValue) ?? false;
                        break;
                    case "Email":
                        matches = user.Email?.ToLower().Contains(filterValue) ?? false;
                        break;
                    case "EmailDomain":
                        matches = user.EmailDomain?.ToLower().Contains(filterValue) ?? false;
                        break;
                    case "Department":
                        matches = user.Department?.ToLower().Contains(filterValue) ?? false;
                        break;
                    case "JobTitle":
                        matches = user.JobTitle?.ToLower().Contains(filterValue) ?? false;
                        break;
                    case "Country":
                        matches = user.Country?.ToLower().Contains(filterValue) ?? false;
                        break;
                    case "OfficeLocation":
                        matches = user.OfficeLocation?.ToLower().Contains(filterValue) ?? false;
                        break;
                    case "UsageLocation":
                        matches = user.UsageLocation?.ToLower().Contains(filterValue) ?? false;
                        break;
                    case "AccountEnabled":
                        if (filterValue == "true")
                            matches = user.AccountEnabled == true;
                        else if (filterValue == "false")
                            matches = user.AccountEnabled == false;
                        break;
                    case "IsGuest":
                        if (filterValue == "true")
                            matches = user.IsGuest == true;
                        else if (filterValue == "false")
                            matches = user.IsGuest == false;
                        break;
                    case "HasLicense":
                        if (filterValue == "true")
                            matches = user.HasLicense == true;
                        else if (filterValue == "false")
                            matches = user.HasLicense == false;
                        break;
                }
                
                if (!matches)
                    return false;
            }
            
            return true;
        }
        
        private void FilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string columnName)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    columnFilters.Remove(columnName);
                }
                else
                {
                    columnFilters[columnName] = textBox.Text;
                }
                
                usersView?.Refresh();
                UpdateStatus();
            }
        }
        
        private void FilterEnabledChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterEnabled == null || usersView == null)
                return;
            
            switch (FilterEnabled.SelectedIndex)
            {
                case 0: // 全て
                    columnFilters.Remove("AccountEnabled");
                    break;
                case 1: // 有効
                    columnFilters["AccountEnabled"] = "true";
                    break;
                case 2: // 無効
                    columnFilters["AccountEnabled"] = "false";
                    break;
            }
            
            usersView.Refresh();
            UpdateStatus();
        }
        
        private void FilterGuestChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterGuest == null || usersView == null)
                return;
            
            switch (FilterGuest.SelectedIndex)
            {
                case 0: // 全て
                    columnFilters.Remove("IsGuest");
                    break;
                case 1: // ゲスト
                    columnFilters["IsGuest"] = "true";
                    break;
                case 2: // メンバー
                    columnFilters["IsGuest"] = "false";
                    break;
            }
            
            usersView.Refresh();
            UpdateStatus();
        }
        
        private void FilterLicenseChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterLicense == null || usersView == null)
                return;
            
            switch (FilterLicense.SelectedIndex)
            {
                case 0: // 全て
                    columnFilters.Remove("HasLicense");
                    break;
                case 1: // 有り
                    columnFilters["HasLicense"] = "true";
                    break;
                case 2: // 無し
                    columnFilters["HasLicense"] = "false";
                    break;
            }
            
            usersView.Refresh();
            UpdateStatus();
        }
        
        private void CollectUniqueValues()
        {
            // 既存の値をクリア
            uniqueDomains.Clear();
            uniqueDepartments.Clear();
            uniqueJobTitles.Clear();
            uniqueCountries.Clear();
            uniqueOffices.Clear();
            uniqueUsageLocations.Clear();
            
            // ユーザーから一意の値を収集
            foreach (var user in users)
            {
                if (!string.IsNullOrWhiteSpace(user.EmailDomain))
                    uniqueDomains.Add(user.EmailDomain);
                if (!string.IsNullOrWhiteSpace(user.Department))
                    uniqueDepartments.Add(user.Department);
                if (!string.IsNullOrWhiteSpace(user.JobTitle))
                    uniqueJobTitles.Add(user.JobTitle);
                if (!string.IsNullOrWhiteSpace(user.Country))
                    uniqueCountries.Add(user.Country);
                if (!string.IsNullOrWhiteSpace(user.OfficeLocation))
                    uniqueOffices.Add(user.OfficeLocation);
                if (!string.IsNullOrWhiteSpace(user.UsageLocation))
                    uniqueUsageLocations.Add(user.UsageLocation);
            }
            
            // ComboBoxに候補を設定
            SetupAutoCompleteForFilters();
        }
        
        private void SetupAutoCompleteForFilters()
        {
            // 各ComboBoxに候補を設定
            Dispatcher.Invoke(() =>
            {
                // ドメイン
                FilterDomain.Items.Clear();
                FilterDomain.Items.Add(""); // 空の選択肢
                foreach (var domain in uniqueDomains.OrderBy(d => d))
                {
                    FilterDomain.Items.Add(domain);
                }
                
                // 部署
                FilterDepartment.Items.Clear();
                FilterDepartment.Items.Add(""); // 空の選択肢
                foreach (var dept in uniqueDepartments.OrderBy(d => d))
                {
                    FilterDepartment.Items.Add(dept);
                }
                
                // 役職
                FilterJobTitle.Items.Clear();
                FilterJobTitle.Items.Add(""); // 空の選択肢
                foreach (var title in uniqueJobTitles.OrderBy(t => t))
                {
                    FilterJobTitle.Items.Add(title);
                }
                
                // 国
                FilterCountry.Items.Clear();
                FilterCountry.Items.Add(""); // 空の選択肢
                foreach (var country in uniqueCountries.OrderBy(c => c))
                {
                    FilterCountry.Items.Add(country);
                }
                
                // オフィス
                FilterOffice.Items.Clear();
                FilterOffice.Items.Add(""); // 空の選択肢
                foreach (var office in uniqueOffices.OrderBy(o => o))
                {
                    FilterOffice.Items.Add(office);
                }
                
                // 使用場所
                FilterUsage.Items.Clear();
                FilterUsage.Items.Add(""); // 空の選択肢
                foreach (var usage in uniqueUsageLocations.OrderBy(u => u))
                {
                    FilterUsage.Items.Add(usage);
                }
            });
        }
        
        private void FilterComboBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            // 初期化中は処理しない
            if (_isInitializing) return;
            
            // TextBoxを取得
            var textBox = e.OriginalSource as TextBox;
            if (textBox == null) return;
            
            // 親のComboBoxを取得
            var comboBox = textBox.TemplatedParent as ComboBox;
            if (comboBox?.Tag is string columnName)
            {
                var filterValue = textBox.Text;
                
                if (string.IsNullOrWhiteSpace(filterValue))
                {
                    columnFilters.Remove(columnName);
                }
                else
                {
                    columnFilters[columnName] = filterValue;
                }
                
                // Dispatcherを使用して、UIスレッドの次のサイクルでRefreshを実行
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        usersView?.Refresh();
                        UpdateStatus();
                    }
                    catch (InvalidOperationException)
                    {
                        // トランザクション中の場合は無視
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }
}