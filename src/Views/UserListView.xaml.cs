using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using TeamsAccountManager.Models;
using TeamsAccountManager.ViewModels;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;
using System.Linq;

namespace TeamsAccountManager.Views
{
    /// <summary>
    /// UserListView.xaml の相互作用ロジック
    /// </summary>
    public partial class UserListView : UserControl
    {
        public UserListView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserListViewModel viewModel)
            {
                // イベントをサブスクライブ
                viewModel.ExportRequested += OnExportRequested;
                viewModel.ImportRequested += OnImportRequested;
                viewModel.ExportCompleted += OnExportCompleted;
                viewModel.ImportCompleted += OnImportCompleted;
                viewModel.ImportConfirmationRequested += OnImportConfirmationRequested;
                viewModel.ImportProgress += OnImportProgress;
            }
        }

        private async void OnExportRequested(object? sender, ExportRequestedEventArgs e)
        {
            if (DataContext is not UserListViewModel viewModel) return;

            var dialog = new SaveFileDialog
            {
                Title = e.IsExcel ? "Excelファイルとして保存" : "CSVファイルとして保存",
                Filter = e.IsExcel ? "Excel Files (*.xlsx)|*.xlsx" : "CSV Files (*.csv)|*.csv",
                DefaultExt = e.IsExcel ? "xlsx" : "csv",
                FileName = $"Users_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                viewModel.ExportFilePath = dialog.FileName;
                await viewModel.ExportUsersAsync(e.IsExcel);
            }
        }

        private async void OnImportRequested(object? sender, ImportRequestedEventArgs e)
        {
            if (DataContext is not UserListViewModel viewModel) return;

            var dialog = new OpenFileDialog
            {
                Title = e.IsExcel ? "Excelファイルを選択" : "CSVファイルを選択",
                Filter = e.IsExcel ? "Excel Files (*.xlsx)|*.xlsx" : "CSV Files (*.csv)|*.csv",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                viewModel.ImportFilePath = dialog.FileName;
                await viewModel.ImportUsersAsync(e.IsExcel);
            }
        }

        private void OnExportCompleted(object? sender, DataOperationEventArgs e)
        {
            // スナックバーでメッセージを表示
            MessageSnackbar.MessageQueue?.Enqueue(e.Message);
        }

        private void OnImportCompleted(object? sender, DataOperationEventArgs e)
        {
            // スナックバーでメッセージを表示
            MessageSnackbar.MessageQueue?.Enqueue(e.Message);
        }

        private async void OnImportConfirmationRequested(object? sender, ImportConfirmationEventArgs e)
        {
            if (DataContext is not UserListViewModel viewModel) return;

            // 確認ダイアログを表示
            var dialog = new ConfirmationDialog
            {
                Message = e.Message,
                Users = e.Users
            };

            var result = await DialogHost.Show(dialog, "RootDialog");
            if (result is bool confirm && confirm)
            {
                await viewModel.ConfirmImportAsync(e.Users);
            }
        }

        private void OnImportProgress(object? sender, ProgressEventArgs e)
        {
            // 進捗バーを更新 - 一時的にコメントアウト
            /*
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.UpdateStatus($"{e.Message} ({e.Current}/{e.Total})", true);
                }
            });
            */
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            if (e.EditingElement is TextBox textBox && e.Row.DataContext is User user)
            {
                var binding = (e.Column as DataGridBoundColumn)?.Binding as System.Windows.Data.Binding;
                var propertyName = binding?.Path?.Path;
                if (propertyName != null)
                {
                    // 変更前の値を保存
                    var originalValue = user.GetType().GetProperty(propertyName)?.GetValue(user);
                    
                    // ViewModelに通知
                    if (DataContext is UserListViewModel viewModel)
                    {
                        viewModel.TrackUserChange(user, propertyName, originalValue, textBox.Text);
                    }
                }
            }
            else if (e.EditingElement is CheckBox checkBox && e.Row.DataContext is User checkBoxUser)
            {
                var binding = (e.Column as DataGridBoundColumn)?.Binding as System.Windows.Data.Binding;
                var propertyName = binding?.Path?.Path;
                if (propertyName != null)
                {
                    // 変更前の値を保存
                    var originalValue = checkBoxUser.GetType().GetProperty(propertyName)?.GetValue(checkBoxUser);
                    
                    // ViewModelに通知
                    if (DataContext is UserListViewModel viewModel)
                    {
                        viewModel.TrackUserChange(checkBoxUser, propertyName, originalValue, checkBox.IsChecked);
                    }
                }
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is UserListViewModel viewModel && sender is DataGrid dataGrid)
            {
                var selectedUsers = dataGrid.SelectedItems.Cast<User>().ToList();
                viewModel.SetSelectedUsers(selectedUsers);
            }
        }
    }
}