using System;
using System.Windows;
using System.Windows.Controls;
using TeamsAccountManager.Services;
using TeamsAccountManager.ViewModels;

namespace TeamsAccountManager.Views
{
    /// <summary>
    /// LoginView.xaml の相互作用ロジック
    /// </summary>
    public partial class LoginView : UserControl
    {
        private readonly LoginViewModel _viewModel;
        
        public LoginView()
        {
            InitializeComponent();
            
            // ViewModelを取得
            _viewModel = App.GetService<LoginViewModel>();
            DataContext = _viewModel;
            
            // Loadedイベントを追加
            this.Loaded += LoginView_Loaded;
            
            // ログイン成功イベントのハンドリング
            _viewModel.LoginSucceeded += OnLoginSucceeded;
        }
        
        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            // フォーカスを別の場所に移動
            this.Focus();
            
            // ボタンにClickイベントを手動で設定
            LoginButton.Click += LoginButton_Click;
        }
        
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // UI更新
                LoginButton.IsEnabled = false;
                StatusTextBlock.Text = "ログイン中...";
                StatusTextBlock.Visibility = Visibility.Visible;
                LoginProgressBar.Visibility = Visibility.Visible;
                
                // ViewModelのログインコマンドを実行
                if (_viewModel.LoginCommand.CanExecute(null))
                {
                    _viewModel.LoginCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"エラー: {ex.Message}";
                MessageBox.Show($"ログイン中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnLoginSucceeded(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // メインウィンドウに通知
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    var authService = App.GetService<AuthenticationService>();
                    var userName = authService.CurrentUserName ?? "不明なユーザー";
                    
                    // 権限情報を簡易的に設定（今は読み書き可能で固定）
                    var roles = "読み書き可能";
                    
                    // ユーザー情報と権限を表示
                    mainWindow.UserInfoTextBlock.Text = $"{userName} ({roles})";
                    
                    // UserListViewへ遷移
                    var userListView = new UserListView_Simple();
                    mainWindow.NavigateToContent(userListView);
                }
                
                // UI状態をリセット
                LoginButton.IsEnabled = true;
                LoginProgressBar.Visibility = Visibility.Collapsed;
                StatusTextBlock.Visibility = Visibility.Collapsed;
            });
        }
    }
}