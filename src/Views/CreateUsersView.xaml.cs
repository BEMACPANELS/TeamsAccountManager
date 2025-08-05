using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TeamsAccountManager.Models;
using TeamsAccountManager.Services;
using Microsoft.Extensions.Logging;

namespace TeamsAccountManager.Views
{
    public partial class CreateUsersView : UserControl
    {
        private ObservableCollection<NewUserModel> newUsers = new ObservableCollection<NewUserModel>();
        
        public CreateUsersView()
        {
            InitializeComponent();
            NewUsersDataGrid.ItemsSource = newUsers;
            
            // 初期行を1行追加
            AddNewUserRow();
            
            UpdateStatus();
        }
        
        private void AddRowButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewUserRow();
        }
        
        private void RemoveRowButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = NewUsersDataGrid.SelectedItems.Cast<NewUserModel>().ToList();
            foreach (var item in selectedItems)
            {
                newUsers.Remove(item);
            }
            UpdateStatus();
        }
        
        private void GeneratePasswordsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var user in newUsers)
            {
                if (string.IsNullOrWhiteSpace(user.Password))
                {
                    user.Password = GenerateSecurePassword();
                }
            }
        }
        
        private async void CreateUsersButton_Click(object sender, RoutedEventArgs e)
        {
            // 入力検証
            var validUsers = new List<NewUserModel>();
            var errors = new List<string>();
            
            foreach (var user in newUsers)
            {
                if (string.IsNullOrWhiteSpace(user.DisplayName))
                {
                    errors.Add($"行 {newUsers.IndexOf(user) + 1}: 表示名は必須です");
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(user.UserPrincipalName))
                {
                    errors.Add($"行 {newUsers.IndexOf(user) + 1}: メールアドレスは必須です");
                    continue;
                }
                
                if (!user.UserPrincipalName.Contains("@"))
                {
                    errors.Add($"行 {newUsers.IndexOf(user) + 1}: メールアドレスの形式が正しくありません");
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(user.Password))
                {
                    user.Password = GenerateSecurePassword();
                }
                else
                {
                    // 手動入力されたパスワードの検証
                    var passwordError = ValidatePassword(user.Password);
                    if (!string.IsNullOrEmpty(passwordError))
                    {
                        errors.Add($"行 {newUsers.IndexOf(user) + 1}: パスワード - {passwordError}");
                        continue;
                    }
                }
                
                validUsers.Add(user);
            }
            
            if (errors.Count > 0)
            {
                MessageBox.Show($"入力エラー:\n{string.Join("\n", errors.Take(5))}", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (validUsers.Count == 0)
            {
                MessageBox.Show("作成するユーザーがありません", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var result = MessageBox.Show($"{validUsers.Count}名のユーザーを作成しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                CreateUsersButton.IsEnabled = false;
                
                var graphService = App.GetService<GraphApiService>();
                var createdUsers = new List<(NewUserModel Model, bool Success, string Error)>();
                
                foreach (var user in validUsers)
                {
                    StatusTextBlock.Text = $"ユーザー作成中... {validUsers.IndexOf(user) + 1}/{validUsers.Count}";
                    
                    try
                    {
                        var success = await graphService.CreateUserAsync(user);
                        createdUsers.Add((user, success, ""));
                    }
                    catch (Exception ex)
                    {
                        createdUsers.Add((user, false, ex.Message));
                    }
                }
                
                // 結果をExcelに出力
                await ExportCreatedUsersToExcel(createdUsers);
                
                // 結果表示
                var successCount = createdUsers.Count(u => u.Success);
                var failCount = createdUsers.Count(u => !u.Success);
                
                var message = $"作成結果:\n成功: {successCount}名\n失敗: {failCount}名";
                if (failCount > 0)
                {
                    message += "\n\n詳細はエクスポートされたExcelファイルを確認してください";
                }
                
                MessageBox.Show(message, "作成完了", MessageBoxButton.OK, 
                    failCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                
                // 成功したユーザーを削除
                foreach (var created in createdUsers.Where(c => c.Success))
                {
                    newUsers.Remove(created.Model);
                }
                
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                CreateUsersButton.IsEnabled = true;
            }
        }
        
        private Task ExportCreatedUsersToExcel(List<(NewUserModel Model, bool Success, string Error)> createdUsers)
        {
            var fileName = $"CreatedUsers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = System.IO.Path.Combine(desktopPath, fileName);
            
            // ClosedXMLを直接使用して詳細情報を出力
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("作成結果");
                
                // ヘッダー作成
                worksheet.Cell(1, 1).Value = "状態";
                worksheet.Cell(1, 2).Value = "表示名";
                worksheet.Cell(1, 3).Value = "名";
                worksheet.Cell(1, 4).Value = "姓";
                worksheet.Cell(1, 5).Value = "メールアドレス";
                worksheet.Cell(1, 6).Value = "パスワード";
                worksheet.Cell(1, 7).Value = "部署";
                worksheet.Cell(1, 8).Value = "役職";
                worksheet.Cell(1, 9).Value = "国/地域";
                worksheet.Cell(1, 10).Value = "使用場所";
                worksheet.Cell(1, 11).Value = "パスワード変更要求";
                worksheet.Cell(1, 12).Value = "エラー詳細";
                worksheet.Cell(1, 13).Value = "作成日時";
                
                // ヘッダーのスタイル設定
                var headerRange = worksheet.Range(1, 1, 1, 13);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                
                // データ作成
                int row = 2;
                foreach (var result in createdUsers)
                {
                    worksheet.Cell(row, 1).Value = result.Success ? "成功" : "失敗";
                    worksheet.Cell(row, 2).Value = result.Model.DisplayName;
                    worksheet.Cell(row, 3).Value = result.Model.GivenName;
                    worksheet.Cell(row, 4).Value = result.Model.Surname;
                    worksheet.Cell(row, 5).Value = result.Model.UserPrincipalName;
                    worksheet.Cell(row, 6).Value = result.Model.Password;
                    worksheet.Cell(row, 7).Value = result.Model.Department;
                    worksheet.Cell(row, 8).Value = result.Model.JobTitle;
                    worksheet.Cell(row, 9).Value = result.Model.Country;
                    worksheet.Cell(row, 10).Value = result.Model.UsageLocation;
                    worksheet.Cell(row, 11).Value = result.Model.ForceChangePassword ? "有" : "無";
                    worksheet.Cell(row, 12).Value = result.Error;
                    worksheet.Cell(row, 13).Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    
                    // 失敗行を赤色に
                    if (!result.Success)
                    {
                        worksheet.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(255, 200, 200);
                    }
                    
                    row++;
                }
                
                // 列幅自動調整
                worksheet.Columns().AdjustToContents();
                
                // 保存
                workbook.SaveAs(filePath);
            }
            
            MessageBox.Show($"作成結果をエクスポートしました:\n{filePath}", "エクスポート完了", MessageBoxButton.OK, MessageBoxImage.Information);
            
            return Task.CompletedTask;
        }
        
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveRowButton.IsEnabled = NewUsersDataGrid.SelectedItems.Count > 0;
        }
        
        private void AddNewUserRow()
        {
            var newUser = new NewUserModel
            {
                ForceChangePassword = true
            };
            newUsers.Add(newUser);
            UpdateStatus();
        }
        
        private void UpdateStatus()
        {
            StatusTextBlock.Text = $"{newUsers.Count} 行のユーザー情報";
        }
        
        private string GenerateSecurePassword()
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()-_=+[]{}|;:,.<>?";
            
            var random = new Random();
            var password = new StringBuilder();
            
            // 各種類から最低2文字を含める（より複雑に）
            password.Append(upperCase[random.Next(upperCase.Length)]);
            password.Append(upperCase[random.Next(upperCase.Length)]);
            password.Append(lowerCase[random.Next(lowerCase.Length)]);
            password.Append(lowerCase[random.Next(lowerCase.Length)]);
            password.Append(digits[random.Next(digits.Length)]);
            password.Append(digits[random.Next(digits.Length)]);
            password.Append(special[random.Next(special.Length)]);
            password.Append(special[random.Next(special.Length)]);
            
            // 残りをランダムに生成（合計16文字）
            var allChars = upperCase + lowerCase + digits + special;
            for (int i = 8; i < 16; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }
            
            // シャッフル
            return new string(password.ToString().OrderBy(x => random.Next()).ToArray());
        }
        
        private string ValidatePassword(string password)
        {
            if (password.Length < 8)
            {
                return "パスワードは8文字以上必要です";
            }
            
            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasLowerCase = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));
            
            var missingTypes = new List<string>();
            if (!hasUpperCase) missingTypes.Add("大文字");
            if (!hasLowerCase) missingTypes.Add("小文字");
            if (!hasDigit) missingTypes.Add("数字");
            if (!hasSpecial) missingTypes.Add("特殊文字");
            
            if (missingTypes.Count > 0)
            {
                return $"次の文字種が必要です: {string.Join("、", missingTypes)}";
            }
            
            // 連続する文字のチェック
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] == password[i + 1] && password[i] == password[i + 2])
                {
                    return "同じ文字を3回以上連続で使用することはできません";
                }
            }
            
            // よくある弱いパターンのチェック
            var lowerPassword = password.ToLower();
            string[] weakPatterns = { "password", "123456", "qwerty", "admin", "letmein", "welcome", "test" };
            if (weakPatterns.Any(pattern => lowerPassword.Contains(pattern)))
            {
                return "よく使われる弱いパスワードパターンが含まれています";
            }
            
            return string.Empty; // エラーなし
        }
    }
    
    public class NewUserModel
    {
        public string DisplayName { get; set; } = "";
        public string GivenName { get; set; } = "";
        public string Surname { get; set; } = "";
        public string UserPrincipalName { get; set; } = "";
        public string Password { get; set; } = "";
        public string Department { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public string Country { get; set; } = "";
        public string UsageLocation { get; set; } = "";
        public bool ForceChangePassword { get; set; } = true;
    }
}