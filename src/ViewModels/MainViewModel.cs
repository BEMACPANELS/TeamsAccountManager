using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
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

        public MainViewModel(ILogger<MainViewModel> logger, AuthenticationService authService)
        {
            _logger = logger;
            _authService = authService;
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
    }
}