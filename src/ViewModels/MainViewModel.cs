using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TeamsAccountManager.Services;

namespace TeamsAccountManager.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly AuthenticationService _authService;

        [ObservableProperty]
        private string _currentUserName = string.Empty;

        [ObservableProperty]
        private bool _isAuthenticated;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedLanguage = "ja-JP";

        [ObservableProperty]
        private object? _currentViewModel;

        public AuthenticationService AuthService { get; }

        public MainViewModel(ILogger<MainViewModel> logger, AuthenticationService authService)
        {
            _logger = logger;
            _authService = authService;
            AuthService = authService;
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            // 起動時の初期化処理
            await CheckAuthenticationStatusAsync();
        }

        private async Task CheckAuthenticationStatusAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "認証状態を確認中...";

                // サイレント認証を試行
                if (await _authService.TrySignInSilentlyAsync())
                {
                    SetAuthenticatedUser(_authService.CurrentUserName ?? "Unknown");
                    StatusMessage = "Ready";
                }
                else
                {
                    StatusMessage = "ログインが必要です";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "認証状態の確認中にエラー");
                StatusMessage = "エラーが発生しました";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task Logout()
        {
            try
            {
                _logger.LogInformation("ユーザーがログアウトしました");
                await _authService.SignOutAsync();
                IsAuthenticated = false;
                CurrentUserName = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログアウト中にエラーが発生しました");
            }
        }

        public void SetAuthenticatedUser(string userName)
        {
            CurrentUserName = userName;
            IsAuthenticated = true;
            _logger.LogInformation($"ユーザー {userName} が認証されました");
        }

        public void UpdateStatus(string message, bool isLoading = false)
        {
            StatusMessage = message;
            IsLoading = isLoading;
        }

        [RelayCommand]
        public void ChangeLanguage(string languageCode)
        {
            SelectedLanguage = languageCode;
            _logger.LogInformation($"言語を変更: {languageCode}");
            
            // 言語リソースの切り替え処理（App.xamlで実装）
            var app = System.Windows.Application.Current;
            if (app != null)
            {
                var langDict = new Uri($"/Resources/Languages/Resources.{languageCode}.xaml", UriKind.Relative);
                var resourceDict = new System.Windows.ResourceDictionary { Source = langDict };
                
                // 既存の言語リソースを削除
                var existingDict = app.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.OriginalString.Contains("/Resources/Languages/") == true);
                if (existingDict != null)
                {
                    app.Resources.MergedDictionaries.Remove(existingDict);
                }
                
                // 新しい言語リソースを追加
                app.Resources.MergedDictionaries.Add(resourceDict);
            }
        }
    }
}