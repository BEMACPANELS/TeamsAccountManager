using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using TeamsAccountManager.Models;
using TeamsAccountManager.Services;
using TeamsAccountManager.Views;
using MaterialDesignThemes.Wpf;

namespace TeamsAccountManager.ViewModels
{
    public partial class UserListViewModel : ObservableObject
    {
        private readonly GraphApiService _graphApiService;
        private readonly ExcelService _excelService;
        private readonly CsvService _csvService;
        private readonly ILogger<UserListViewModel> _logger;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedDepartment = "All";

        [ObservableProperty]
        private bool _showActiveOnly;

        [ObservableProperty]
        private User? _selectedUser;

        // 変更追跡用のコレクション
        private readonly Dictionary<string, List<UserChange>> _userChanges = new();
        
        [ObservableProperty]
        private bool _hasUnsavedChanges;

        // 選択されたユーザー
        private List<User> _selectedUsers = new();
        
        [ObservableProperty]
        private bool _hasSelectedUsers;

        public ICollectionView UsersView { get; }
        public ObservableCollection<string> Departments { get; } = new() { "All" };

        public ICommand LoadUsersCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand ExportToCsvCommand { get; }
        public ICommand ImportFromExcelCommand { get; }
        public ICommand ImportFromCsvCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand BulkEditCommand { get; }
        public ICommand SaveChangesCommand { get; }
        public ICommand ShowHistoryCommand { get; }

        public UserListViewModel(
            GraphApiService graphApiService,
            ExcelService excelService,
            CsvService csvService,
            ILogger<UserListViewModel> logger)
        {
            _graphApiService = graphApiService;
            _excelService = excelService;
            _csvService = csvService;
            _logger = logger;

            LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
            ExportToExcelCommand = new AsyncRelayCommand(ExportToExcelAsync);
            ExportToCsvCommand = new AsyncRelayCommand(ExportToCsvAsync);
            ImportFromExcelCommand = new AsyncRelayCommand(ImportFromExcelAsync);
            ImportFromCsvCommand = new AsyncRelayCommand(ImportFromCsvAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            BulkEditCommand = new AsyncRelayCommand(BulkEditAsync, () => HasSelectedUsers);
            SaveChangesCommand = new AsyncRelayCommand(SaveChangesAsync, () => HasUnsavedChanges);
            ShowHistoryCommand = new AsyncRelayCommand(ShowHistoryAsync);

            UsersView = CollectionViewSource.GetDefaultView(Users);
            UsersView.Filter = FilterUsers;
        }

        partial void OnSearchTextChanged(string value)
        {
            UsersView.Refresh();
        }

        partial void OnSelectedDepartmentChanged(string value)
        {
            UsersView.Refresh();
        }

        partial void OnShowActiveOnlyChanged(bool value)
        {
            UsersView.Refresh();
        }

        private bool FilterUsers(object obj)
        {
            if (obj is not User user) return false;

            // 検索フィルター
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!user.DisplayName.ToLower().Contains(searchLower) &&
                    !user.Email.ToLower().Contains(searchLower))
                {
                    return false;
                }
            }

            // 部署フィルター
            if (SelectedDepartment != "All" && user.Department != SelectedDepartment)
            {
                return false;
            }

            // アクティブユーザーフィルター
            if (ShowActiveOnly && user.AccountEnabled != true)
            {
                return false;
            }

            return true;
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                IsLoading = true;
                var users = await _graphApiService.GetUsersAsync();
                
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }

                // 部署リストを更新
                UpdateDepartmentsList();
                
                _logger.LogInformation($"Loaded {users.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load users");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateDepartmentsList()
        {
            var departments = Users
                .Where(u => !string.IsNullOrEmpty(u.Department))
                .Select(u => u.Department)
                .Distinct()
                .OrderBy(d => d);

            Departments.Clear();
            Departments.Add("All");
            foreach (var dept in departments)
            {
                if (!string.IsNullOrEmpty(dept))
                {
                    Departments.Add(dept);
                }
            }
        }

        private async Task ExportToExcelAsync()
        {
            await ExportUsersAsync(true);
        }

        private async Task ExportToCsvAsync()
        {
            await ExportUsersAsync(false);
        }

        private async Task ImportFromExcelAsync()
        {
            await ImportUsersAsync(true);
        }

        private async Task ImportFromCsvAsync()
        {
            await ImportUsersAsync(false);
        }

        public async Task ExportUsersAsync(bool isExcel)
        {
            try
            {
                IsLoading = true;
                var selectedUsers = UsersView.Cast<User>().ToList();
                
                if (!selectedUsers.Any())
                {
                    ExportCompleted?.Invoke(this, new DataOperationEventArgs { Success = false, Message = "エクスポートするユーザーがありません" });
                    return;
                }

                // ファイルパスはView側から設定される
                if (string.IsNullOrEmpty(ExportFilePath))
                {
                    ExportRequested?.Invoke(this, new ExportRequestedEventArgs { IsExcel = isExcel });
                    return;
                }

                bool success;
                if (isExcel)
                {
                    success = await _excelService.ExportUsersAsync(selectedUsers, ExportFilePath);
                }
                else
                {
                    success = await _csvService.ExportUsersAsync(selectedUsers, ExportFilePath);
                }

                var message = success ? 
                    $"{selectedUsers.Count}件のユーザー情報をエクスポートしました" : 
                    "エクスポートに失敗しました";
                
                ExportCompleted?.Invoke(this, new DataOperationEventArgs { Success = success, Message = message });
                _logger.LogInformation(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export failed");
                ExportCompleted?.Invoke(this, new DataOperationEventArgs { Success = false, Message = ex.Message });
            }
            finally
            {
                IsLoading = false;
                ExportFilePath = null;
            }
        }

        public async Task ImportUsersAsync(bool isExcel)
        {
            try
            {
                IsLoading = true;

                // ファイルパスはView側から設定される
                if (string.IsNullOrEmpty(ImportFilePath))
                {
                    ImportRequested?.Invoke(this, new ImportRequestedEventArgs { IsExcel = isExcel });
                    return;
                }

                List<User> importedUsers;
                if (isExcel)
                {
                    importedUsers = await _excelService.ImportUsersAsync(ImportFilePath);
                }
                else
                {
                    importedUsers = await _csvService.ImportUsersAsync(ImportFilePath);
                }

                // データ検証
                var validationResult = ValidateImportedUsers(importedUsers);
                if (!validationResult.IsValid)
                {
                    ImportCompleted?.Invoke(this, new DataOperationEventArgs 
                    { 
                        Success = false, 
                        Message = $"データ検証エラー: {string.Join(", ", validationResult.Errors)}" 
                    });
                    return;
                }

                // インポート確認
                ImportConfirmationRequested?.Invoke(this, new ImportConfirmationEventArgs 
                { 
                    Users = importedUsers,
                    Message = $"{importedUsers.Count}件のユーザー情報をインポートしますか？"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import failed");
                ImportCompleted?.Invoke(this, new DataOperationEventArgs { Success = false, Message = ex.Message });
            }
            finally
            {
                IsLoading = false;
                ImportFilePath = null;
            }
        }

        public async Task ConfirmImportAsync(List<User> users)
        {
            try
            {
                IsLoading = true;
                
                // Graph APIで更新
                var results = await _graphApiService.UpdateUsersAsync(users, 
                    new Progress<(int current, int total, string message)>(progress =>
                    {
                        ImportProgress?.Invoke(this, new ProgressEventArgs 
                        { 
                            Current = progress.current, 
                            Total = progress.total, 
                            Message = progress.message 
                        });
                    }));

                var successCount = results.Count(r => string.IsNullOrEmpty(r.ErrorMessage));
                var errorCount = results.Count(r => !string.IsNullOrEmpty(r.ErrorMessage));

                var message = $"インポート完了: 成功 {successCount}件, エラー {errorCount}件";
                ImportCompleted?.Invoke(this, new DataOperationEventArgs { Success = errorCount == 0, Message = message });
                
                // リストを更新
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import confirmation failed");
                ImportCompleted?.Invoke(this, new DataOperationEventArgs { Success = false, Message = ex.Message });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private ValidationResult ValidateImportedUsers(List<User> users)
        {
            var result = new ValidationResult { IsValid = true };
            var errors = new List<string>();

            if (!users.Any())
            {
                errors.Add("インポートするデータがありません");
            }

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.UserPrincipalName))
                {
                    errors.Add($"ユーザー {user.DisplayName} のメールアドレスが設定されていません");
                }

                if (string.IsNullOrWhiteSpace(user.DisplayName))
                {
                    errors.Add($"ユーザー {user.UserPrincipalName} の表示名が設定されていません");
                }
            }

            if (errors.Any())
            {
                result.IsValid = false;
                result.Errors = errors;
            }

            return result;
        }

        // プロパティ
        public string? ExportFilePath { get; set; }
        public string? ImportFilePath { get; set; }

        // イベント
        public event EventHandler<ExportRequestedEventArgs>? ExportRequested;
        public event EventHandler<ImportRequestedEventArgs>? ImportRequested;
        public event EventHandler<DataOperationEventArgs>? ExportCompleted;
        public event EventHandler<DataOperationEventArgs>? ImportCompleted;
        public event EventHandler<ImportConfirmationEventArgs>? ImportConfirmationRequested;
        public event EventHandler<ProgressEventArgs>? ImportProgress;

        private async Task RefreshAsync()
        {
            await LoadUsersAsync();
        }

        /// <summary>
        /// ユーザーの変更を追跡
        /// </summary>
        public void TrackUserChange(User user, string propertyName, object? originalValue, object? newValue)
        {
            if (user.Id == null) return;

            if (!_userChanges.ContainsKey(user.Id))
            {
                _userChanges[user.Id] = new List<UserChange>();
            }

            // 同じプロパティの既存の変更を探す
            var existingChange = _userChanges[user.Id].FirstOrDefault(c => c.PropertyName == propertyName);
            
            if (existingChange != null)
            {
                // 既存の変更を更新
                existingChange.NewValue = newValue;
                existingChange.ChangedAt = DateTime.Now;
            }
            else
            {
                // 新しい変更を追加
                _userChanges[user.Id].Add(new UserChange
                {
                    UserId = user.Id,
                    PropertyName = propertyName,
                    OriginalValue = originalValue,
                    NewValue = newValue,
                    ChangedAt = DateTime.Now,
                    Status = ChangeStatus.Pending
                });
            }

            // 実際に変更があるかチェック
            HasUnsavedChanges = _userChanges.Values.Any(changes => 
                changes.Any(c => c.HasChanged && c.Status == ChangeStatus.Pending));
            
            // 保存コマンドの実行可能状態を更新
            ((AsyncRelayCommand)SaveChangesCommand).NotifyCanExecuteChanged();
        }

        /// <summary>
        /// すべての変更を取得
        /// </summary>
        public Dictionary<string, List<UserChange>> GetAllChanges()
        {
            return _userChanges
                .Where(kvp => kvp.Value.Any(c => c.HasChanged && c.Status == ChangeStatus.Pending))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Where(c => c.HasChanged && c.Status == ChangeStatus.Pending).ToList());
        }

        /// <summary>
        /// 変更をクリア
        /// </summary>
        public void ClearChanges()
        {
            _userChanges.Clear();
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// 選択されたユーザーを設定
        /// </summary>
        public void SetSelectedUsers(List<User> selectedUsers)
        {
            _selectedUsers = selectedUsers;
            HasSelectedUsers = selectedUsers.Any();
            ((AsyncRelayCommand)BulkEditCommand).NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 一括編集
        /// </summary>
        private async Task BulkEditAsync()
        {
            if (!_selectedUsers.Any()) return;

            try
            {
                var viewModel = new BulkEditViewModel(_selectedUsers);
                var dialog = new BulkEditDialog { DataContext = viewModel };

                var result = await DialogHost.Show(dialog, "RootDialog");
                
                if (result is bool confirm && confirm)
                {
                    var changes = viewModel.GetChanges();
                    if (changes.HasChanges)
                    {
                        await ApplyBulkChangesAsync(changes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk edit failed");
            }
        }

        /// <summary>
        /// 一括変更を適用
        /// </summary>
        private async Task ApplyBulkChangesAsync(BulkEditChanges changes)
        {
            IsLoading = true;
            try
            {
                foreach (var user in changes.TargetUsers)
                {
                    // 各フィールドの変更を追跡
                    if (!string.IsNullOrEmpty(changes.Department))
                    {
                        TrackUserChange(user, nameof(User.Department), user.Department, changes.Department);
                        user.Department = changes.Department;
                    }

                    if (!string.IsNullOrEmpty(changes.JobTitle))
                    {
                        TrackUserChange(user, nameof(User.JobTitle), user.JobTitle, changes.JobTitle);
                        user.JobTitle = changes.JobTitle;
                    }

                    if (!string.IsNullOrEmpty(changes.OfficeLocation))
                    {
                        TrackUserChange(user, nameof(User.OfficeLocation), user.OfficeLocation, changes.OfficeLocation);
                        user.OfficeLocation = changes.OfficeLocation;
                    }

                    if (changes.AccountEnabled.HasValue)
                    {
                        TrackUserChange(user, nameof(User.AccountEnabled), user.AccountEnabled, changes.AccountEnabled.Value);
                        user.AccountEnabled = changes.AccountEnabled.Value;
                    }
                }

                // UIを更新（非同期で少し待機してUIスレッドに制御を戻す）
                await Task.Delay(1);
                UsersView.Refresh();
                
                // スナックバーで通知
                var message = $"{changes.TargetUsers.Count}名のユーザーに変更を適用しました";
                ImportCompleted?.Invoke(this, new DataOperationEventArgs { Success = true, Message = message });
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 変更を保存
        /// </summary>
        private async Task SaveChangesAsync()
        {
            if (!HasUnsavedChanges) return;

            IsLoading = true;
            try
            {
                var allChanges = GetAllChanges();
                var results = new List<UpdateResult>();
                var total = allChanges.Count;
                var current = 0;

                foreach (var userChanges in allChanges)
                {
                    current++;
                    var userId = userChanges.Key;
                    var changes = userChanges.Value;
                    
                    // 変更されたプロパティを辞書に変換
                    var changesDict = new Dictionary<string, object>();
                    foreach (var change in changes.Where(c => c.HasChanged))
                    {
                        if (change.NewValue != null)
                        {
                            changesDict[change.PropertyName] = change.NewValue;
                        }
                    }

                    // 進捗を報告
                    ImportProgress?.Invoke(this, new ProgressEventArgs 
                    { 
                        Current = current, 
                        Total = total, 
                        Message = $"ユーザー情報を更新中..." 
                    });

                    // 更新実行
                    var result = await _graphApiService.UpdateUserPartialAsync(userId, changesDict);
                    results.Add(result);

                    // 成功した変更のステータスを更新
                    if (result.Success)
                    {
                        foreach (var change in changes)
                        {
                            change.Status = ChangeStatus.Applied;
                        }
                    }
                }

                // 結果を集計
                var successCount = results.Count(r => r.Success);
                var errorCount = results.Count(r => !r.Success);

                var message = errorCount == 0 
                    ? $"{successCount}件のユーザー情報を正常に更新しました" 
                    : $"更新完了: 成功 {successCount}件, エラー {errorCount}件";

                ImportCompleted?.Invoke(this, new DataOperationEventArgs 
                { 
                    Success = errorCount == 0, 
                    Message = message 
                });

                // 成功した変更をクリア
                var successfulUserIds = results.Where(r => r.Success).Select(r => r.UserId).ToList();
                foreach (var userId in successfulUserIds)
                {
                    if (_userChanges.ContainsKey(userId))
                    {
                        _userChanges[userId].RemoveAll(c => c.Status == ChangeStatus.Applied);
                        if (!_userChanges[userId].Any())
                        {
                            _userChanges.Remove(userId);
                        }
                    }
                }

                // 変更状態を更新
                HasUnsavedChanges = _userChanges.Any();
                ((AsyncRelayCommand)SaveChangesCommand).NotifyCanExecuteChanged();

                // エラーがあった場合、詳細を表示
                if (errorCount > 0)
                {
                    var errors = results.Where(r => !r.Success).ToList();
                    // TODO: エラー詳細の表示
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "変更の保存中にエラーが発生");
                ImportCompleted?.Invoke(this, new DataOperationEventArgs 
                { 
                    Success = false, 
                    Message = $"エラー: {ex.Message}" 
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 変更履歴を表示
        /// </summary>
        private async Task ShowHistoryAsync()
        {
            try
            {
                var viewModel = new ChangeHistoryViewModel(
                    _userChanges,
                    Users.ToList(),
                    RevertChange,
                    ClearAllHistory);
                
                var dialog = new ChangeHistoryDialog { DataContext = viewModel };
                await DialogHost.Show(dialog, "RootDialog");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "履歴表示中にエラーが発生");
            }
        }

        /// <summary>
        /// 変更を元に戻す
        /// </summary>
        private void RevertChange(ChangeHistoryItem item)
        {
            var user = Users.FirstOrDefault(u => u.Id == item.UserId);
            if (user != null)
            {
                // プロパティを元の値に戻す
                var property = user.GetType().GetProperty(item.PropertyName);
                if (property != null && property.CanWrite)
                {
                    var originalValue = ConvertToPropertyType(item.OriginalValue, property.PropertyType);
                    property.SetValue(user, originalValue);
                    
                    // 変更追跡から削除
                    if (_userChanges.ContainsKey(item.UserId))
                    {
                        _userChanges[item.UserId].RemoveAll(c => 
                            c.PropertyName == item.PropertyName && c.Status == ChangeStatus.Applied);
                        
                        if (!_userChanges[item.UserId].Any())
                        {
                            _userChanges.Remove(item.UserId);
                        }
                    }
                    
                    // UIを更新
                    UsersView.Refresh();
                    HasUnsavedChanges = _userChanges.Any();
                    ((AsyncRelayCommand)SaveChangesCommand).NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// すべての履歴をクリア
        /// </summary>
        private void ClearAllHistory()
        {
            // 適用済みの変更のみクリア
            foreach (var kvp in _userChanges.ToList())
            {
                kvp.Value.RemoveAll(c => c.Status == ChangeStatus.Applied);
                if (!kvp.Value.Any())
                {
                    _userChanges.Remove(kvp.Key);
                }
            }
            
            HasUnsavedChanges = _userChanges.Any();
            ((AsyncRelayCommand)SaveChangesCommand).NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 文字列を指定された型に変換
        /// </summary>
        private static object? ConvertToPropertyType(string? value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (targetType == typeof(bool))
                return bool.Parse(value);
            
            if (targetType == typeof(bool?))
                return string.IsNullOrEmpty(value) ? null : bool.Parse(value);
                
            return value;
        }
    }
}