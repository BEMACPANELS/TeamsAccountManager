using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using TeamsAccountManager.Services;

namespace TeamsAccountManager.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthenticationService _authService;
        private readonly ILogger<LoginViewModel> _logger;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public ICommand LoginCommand { get; }

        public LoginViewModel(AuthenticationService authService, ILogger<LoginViewModel> logger)
        {
            _authService = authService;
            _logger = logger;
            LoginCommand = new AsyncRelayCommand(LoginAsync);
        }

        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Logging in...";

                var result = await _authService.LoginAsync();
                if (result)
                {
                    StatusMessage = "Login successful!";
                    _logger.LogInformation("User logged in successfully");
                    
                    // ログイン成功イベントを発火
                    LoginSucceeded?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    StatusMessage = "Login failed. Please try again.";
                    _logger.LogWarning("Login failed");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                _logger.LogError(ex, "Login error occurred");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event EventHandler? LoginSucceeded;
    }
}