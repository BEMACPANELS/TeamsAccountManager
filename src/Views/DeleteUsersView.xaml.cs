using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeamsAccountManager.Models;
using TeamsAccountManager.Services;
using Microsoft.Extensions.Logging;

namespace TeamsAccountManager.Views
{
    public partial class DeleteUsersView : UserControl
    {
        private ObservableCollection<DeletableUser> users = new ObservableCollection<DeletableUser>();
        private string _searchText = "";
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                FilterUsers();
            }
        }
        
        public DeleteUsersView()
        {
            InitializeComponent();
            DataContext = this;
            UsersDataGrid.ItemsSource = users;
            
            // CollectionChangedイベントを購読
            users.CollectionChanged += Users_CollectionChanged;
        }
        
        private void Users_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 新しいアイテムが追加された場合
            if (e.NewItems != null)
            {
                foreach (DeletableUser user in e.NewItems)
                {
                    user.PropertyChanged += User_PropertyChanged;
                }
            }
            
            // アイテムが削除された場合
            if (e.OldItems != null)
            {
                foreach (DeletableUser user in e.OldItems)
                {
                    user.PropertyChanged -= User_PropertyChanged;
                }
            }
            
            UpdateSelectionStatus();
        }
        
        private void User_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeletableUser.IsSelected))
            {
                UpdateSelectionStatus();
            }
        }
        
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // チェックボックスの変更を即座に反映
            if (e.Column.Header?.ToString() == "選択")
            {
                // UIスレッドで遅延実行
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateSelectionStatus();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            // チェックボックスがクリックされたら即座に更新
            UpdateSelectionStatus();
        }
        
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }
        
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ = LoadUsersAsync();
            }
        }
        
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            users.Clear();
            UpdateStatus();
        }
        
        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var user in users)
            {
                user.IsSelected = true;
            }
            UpdateSelectionStatus();
        }
        
        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var user in users)
            {
                user.IsSelected = false;
            }
            UpdateSelectionStatus();
        }
        
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUsers = users.Where(u => u.IsSelected).ToList();
            
            if (selectedUsers.Count == 0)
            {
                MessageBox.Show("削除するユーザーを選択してください", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // 削除確認ダイアログ
            var message = $"以下の {selectedUsers.Count} 名のユーザーを削除しますか？\n\n";
            message += string.Join("\n", selectedUsers.Take(5).Select(u => $"• {u.DisplayName} ({u.Email})"));
            if (selectedUsers.Count > 5)
            {
                message += $"\n... 他 {selectedUsers.Count - 5} 名";
            }
            message += "\n\nこの操作は取り消すことができません。";
            
            var result = MessageBox.Show(message, "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            
            // 最終確認
            var finalConfirm = MessageBox.Show(
                $"本当に {selectedUsers.Count} 名のユーザーを削除してもよろしいですか？", 
                "最終確認", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);
            
            if (finalConfirm != MessageBoxResult.Yes)
            {
                return;
            }
            
            await DeleteSelectedUsersAsync(selectedUsers);
        }
        
        private async Task LoadUsersAsync()
        {
            try
            {
                SearchingOverlay.Visibility = Visibility.Visible;
                SearchButton.IsEnabled = false;
                StatusTextBlock.Text = "ユーザーを検索中...";
                
                var graphService = App.GetService<GraphApiService>();
                List<User> userList;
                
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    // 全ユーザーを取得
                    var progress = new Progress<int>(count =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StatusTextBlock.Text = $"ユーザーを読み込み中... {count}件";
                        });
                    });
                    
                    userList = await graphService.GetUsersAsync(progress);
                }
                else
                {
                    // 検索
                    userList = await graphService.SearchUsersAsync(SearchText);
                }
                
                users.Clear();
                foreach (var user in userList)
                {
                    users.Add(new DeletableUser(user));
                }
                
                SelectAllButton.IsEnabled = users.Count > 0;
                DeselectAllButton.IsEnabled = users.Count > 0;
                
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ユーザーの読み込み中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SearchingOverlay.Visibility = Visibility.Collapsed;
                SearchButton.IsEnabled = true;
            }
        }
        
        private async Task DeleteSelectedUsersAsync(List<DeletableUser> selectedUsers)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                DeleteButton.IsEnabled = false;
                
                var graphService = App.GetService<GraphApiService>();
                var successCount = 0;
                var failedUsers = new List<(DeletableUser User, string Error)>();
                
                for (int i = 0; i < selectedUsers.Count; i++)
                {
                    var user = selectedUsers[i];
                    LoadingText.Text = $"削除中... {i + 1}/{selectedUsers.Count}\n{user.DisplayName}";
                    
                    try
                    {
                        var success = await graphService.DeleteUserAsync(user.Id);
                        if (success)
                        {
                            successCount++;
                            // 成功したユーザーをリストから削除
                            users.Remove(user);
                        }
                        else
                        {
                            failedUsers.Add((user, "削除に失敗しました"));
                        }
                    }
                    catch (Exception ex)
                    {
                        failedUsers.Add((user, ex.Message));
                    }
                    
                    // レート制限対策
                    await Task.Delay(100);
                }
                
                // 結果表示
                var message = $"削除結果:\n成功: {successCount} 名";
                
                if (failedUsers.Count > 0)
                {
                    message += $"\n失敗: {failedUsers.Count} 名\n\nエラー詳細:";
                    foreach (var failed in failedUsers.Take(5))
                    {
                        message += $"\n• {failed.User.DisplayName}: {failed.Error}";
                    }
                    if (failedUsers.Count > 5)
                    {
                        message += $"\n... 他 {failedUsers.Count - 5} 件のエラー";
                    }
                    
                    MessageBox.Show(message, "削除完了（一部エラー）", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(message, "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                UpdateStatus();
                UpdateSelectionStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"削除処理中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                DeleteButton.IsEnabled = true;
            }
        }
        
        private void FilterUsers()
        {
            // ここでフィルタリング処理を実装（必要に応じて）
        }
        
        private void UpdateStatus()
        {
            if (users.Count == 0)
            {
                StatusTextBlock.Text = "ユーザーを検索してください";
            }
            else
            {
                StatusTextBlock.Text = $"{users.Count} 件のユーザーを表示中";
            }
            
            UpdateSelectionStatus();
        }
        
        private void UpdateSelectionStatus()
        {
            var selectedCount = users.Count(u => u.IsSelected);
            
            // デバッグ用ログ
            var logger = App.GetService<ILoggerFactory>().CreateLogger<DeleteUsersView>();
            logger.LogInformation($"UpdateSelectionStatus: 選択数 = {selectedCount}");
            
            if (selectedCount > 0)
            {
                SelectionStatusTextBlock.Text = $"{selectedCount} 名選択中";
                DeleteButton.IsEnabled = true;
            }
            else
            {
                SelectionStatusTextBlock.Text = "";
                DeleteButton.IsEnabled = false;
            }
        }
    }
    
    // 削除可能なユーザーモデル
    public class DeletableUser : INotifyPropertyChanged
    {
        private bool _isSelected;
        
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public string? Country { get; set; }
        public bool AccountEnabled { get; set; }
        public DateTime? LastSignIn { get; set; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public DeletableUser(User user)
        {
            Id = user.Id;
            DisplayName = user.DisplayName;
            Email = user.Email;
            Department = user.Department;
            JobTitle = user.JobTitle;
            Country = user.Country;
            AccountEnabled = user.AccountEnabled ?? false;
            LastSignIn = user.LastSignIn;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? ""));
        }
    }
}