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

        public ICollectionView UsersView { get; }
        public ObservableCollection<string> Departments { get; } = new() { "All" };

        public ICommand LoadUsersCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand ExportToCsvCommand { get; }
        public ICommand ImportFromExcelCommand { get; }
        public ICommand ImportFromCsvCommand { get; }
        public ICommand RefreshCommand { get; }

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
            if (ShowActiveOnly && !user.AccountEnabled)
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
                Departments.Add(dept);
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
    }
}