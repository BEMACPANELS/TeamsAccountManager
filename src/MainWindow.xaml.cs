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
            LogoutButton.Click += OnLogoutClick;

            // 初期画面の表示
            ShowLoginView();
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                var culture = item.Tag.ToString();
                ChangeLanguage(culture!);
            }
        }

        private void ChangeLanguage(string culture)
        {
            var dict = new ResourceDictionary();
            dict.Source = new Uri($"Resources/Languages/Strings.{culture}.xaml", UriKind.Relative);

            // 既存の言語リソースを削除
            var oldDict = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Languages/Strings"));
            
            if (oldDict != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(oldDict);
            }

            // 新しい言語リソースを追加
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            _viewModel.Logout();
            ShowLoginView();
        }

        private void ShowLoginView()
        {
            var loginView = ((App)Application.Current).Services.GetService(typeof(LoginView)) as LoginView;
            MainFrame.Navigate(loginView);
            LogoutButton.IsEnabled = false;
            UserNameText.Text = string.Empty;
        }

        public void ShowUserListView(string userName)
        {
            var userListView = ((App)Application.Current).Services.GetService(typeof(UserListView)) as UserListView;
            MainFrame.Navigate(userListView);
            LogoutButton.IsEnabled = true;
            UserNameText.Text = userName;
        }

        public void UpdateStatus(string message, bool showProgress = false)
        {
            StatusText.Text = message;
            StatusProgress.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}