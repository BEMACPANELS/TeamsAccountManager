using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TeamsAccountManager.ViewModels;
using TeamsAccountManager.Views;

namespace TeamsAccountManager
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // 言語選択の変更イベント
            LanguageComboBox.SelectionChanged += OnLanguageChanged;
            
            // ログアウトボタンのイベント
            LogoutButton.Click += async (s, e) => await _viewModel.LogoutCommand.ExecuteAsync(null);

            // ViewModelのプロパティ変更を監視
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // 初期画面の表示
            InitializeView();
        }

        private void InitializeView()
        {
            // 初期状態では常にログイン画面を表示
            if (!_viewModel.IsAuthenticated)
            {
                ShowLoginView();
            }
            else
            {
                ShowUserListView();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsAuthenticated))
            {
                if (_viewModel.IsAuthenticated)
                {
                    ShowUserListView();
                }
                else
                {
                    ShowLoginView();
                }
            }
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                var culture = item.Tag.ToString()!;
                _viewModel.ChangeLanguageCommand.Execute(culture);
            }
        }

        private void ShowLoginView()
        {
            var serviceProvider = ((App)Application.Current).Services;
            var loginView = serviceProvider.GetRequiredService<LoginView>();
            var loginViewModel = serviceProvider.GetRequiredService<LoginViewModel>();
            
            // ログイン成功イベントをサブスクライブ
            loginViewModel.LoginSucceeded += OnLoginSucceeded;
            loginView.DataContext = loginViewModel;
            
            MainFrame.Navigate(loginView);
            LogoutButton.IsEnabled = false;
            UserNameText.Text = string.Empty;
        }

        private void ShowUserListView()
        {
            var serviceProvider = ((App)Application.Current).Services;
            var userListView = serviceProvider.GetRequiredService<UserListView>();
            var userListViewModel = serviceProvider.GetRequiredService<UserListViewModel>();
            
            userListView.DataContext = userListViewModel;
            MainFrame.Navigate(userListView);
            
            LogoutButton.IsEnabled = true;
            UserNameText.Text = _viewModel.CurrentUserName;
            
            // ユーザーリストをロード
            userListViewModel.LoadUsersCommand.Execute(null);
        }

        private void OnLoginSucceeded(object? sender, EventArgs e)
        {
            // ログイン成功時の処理
            _viewModel.SetAuthenticatedUser(_viewModel.AuthService.CurrentUserName ?? "Unknown");
            
            // イベントのサブスクライブを解除
            if (sender is LoginViewModel loginViewModel)
            {
                loginViewModel.LoginSucceeded -= OnLoginSucceeded;
            }
        }

        public void UpdateStatus(string message, bool showProgress = false)
        {
            StatusText.Text = message;
            StatusProgress.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}