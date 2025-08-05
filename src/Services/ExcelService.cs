using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TeamsAccountManager.Models;

namespace TeamsAccountManager.Services
{
    public class ExcelService
    {
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(ILogger<ExcelService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ExportUsersAsync(IEnumerable<User> users, string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Users");

                // ヘッダー行
                worksheet.Cell(1, 1).Value = "Display Name";
                worksheet.Cell(1, 2).Value = "Email";
                worksheet.Cell(1, 3).Value = "Department";
                worksheet.Cell(1, 4).Value = "Job Title";
                worksheet.Cell(1, 5).Value = "Office Location";
                worksheet.Cell(1, 6).Value = "Phone Number";
                worksheet.Cell(1, 7).Value = "Account Enabled";
                worksheet.Cell(1, 8).Value = "Last Sign In";

                // データ行
                var row = 2;
                foreach (var user in users)
                {
                    worksheet.Cell(row, 1).Value = user.DisplayName;
                    worksheet.Cell(row, 2).Value = user.Email;
                    worksheet.Cell(row, 3).Value = user.Department;
                    worksheet.Cell(row, 4).Value = user.JobTitle;
                    worksheet.Cell(row, 5).Value = user.OfficeLocation;
                    worksheet.Cell(row, 6).Value = user.PhoneNumber;
                    worksheet.Cell(row, 7).Value = user.AccountEnabled == true ? "Yes" : "No";
                    worksheet.Cell(row, 8).Value = user.LastSignIn?.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                // スタイル設定
                var headerRange = worksheet.Range(1, 1, 1, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 列幅自動調整
                worksheet.Columns().AdjustToContents();

                // ファイル保存
                await Task.Run(() => workbook.SaveAs(filePath));
                _logger.LogInformation($"Exported {users.Count()} users to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export users to Excel");
                return false;
            }
        }

        public async Task<List<User>> ImportUsersAsync(string filePath)
        {
            var users = new List<User>();

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // ヘッダー行をスキップ
                if (rows == null) return users;

                await Task.Run(() =>
                {
                    foreach (var row in rows)
                    {
                        var user = new User
                        {
                            DisplayName = row.Cell(1).GetString(),
                            Email = row.Cell(2).GetString(),
                            Department = row.Cell(3).GetString(),
                            JobTitle = row.Cell(4).GetString(),
                            OfficeLocation = row.Cell(5).GetString(),
                            PhoneNumber = row.Cell(6).GetString(),
                            AccountEnabled = row.Cell(7).GetString().Equals("Yes", StringComparison.OrdinalIgnoreCase)
                        };

                        if (DateTime.TryParse(row.Cell(8).GetString(), out var lastSignIn))
                        {
                            user.LastSignIn = lastSignIn;
                        }

                        users.Add(user);
                    }
                });

                _logger.LogInformation($"Imported {users.Count} users from {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import users from Excel");
                throw;
            }

            return users;
        }
    }
}