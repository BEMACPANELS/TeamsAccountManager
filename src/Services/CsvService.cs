using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamsAccountManager.Models;

namespace TeamsAccountManager.Services
{
    public class CsvService
    {
        private readonly ILogger<CsvService> _logger;

        public CsvService(ILogger<CsvService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ExportUsersAsync(IEnumerable<User> users, string filePath)
        {
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Encoding = Encoding.UTF8,
                    HasHeaderRecord = true
                };

                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                using var csv = new CsvWriter(writer, config);

                // カスタムヘッダーマッピング
                csv.WriteField("Display Name");
                csv.WriteField("Email");
                csv.WriteField("Department");
                csv.WriteField("Job Title");
                csv.WriteField("Office Location");
                csv.WriteField("Phone Number");
                csv.WriteField("Account Enabled");
                csv.WriteField("Last Sign In");
                await csv.NextRecordAsync();

                // データ書き込み
                foreach (var user in users)
                {
                    csv.WriteField(user.DisplayName);
                    csv.WriteField(user.Email);
                    csv.WriteField(user.Department);
                    csv.WriteField(user.JobTitle);
                    csv.WriteField(user.OfficeLocation);
                    csv.WriteField(user.PhoneNumber);
                    csv.WriteField(user.AccountEnabled == true ? "Yes" : "No");
                    csv.WriteField(user.LastSignIn?.ToString("yyyy-MM-dd HH:mm:ss"));
                    await csv.NextRecordAsync();
                }

                await writer.FlushAsync();
                _logger.LogInformation($"Exported {users.Count()} users to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export users to CSV");
                return false;
            }
        }

        public async Task<List<User>> ImportUsersAsync(string filePath)
        {
            var users = new List<User>();

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Encoding = Encoding.UTF8,
                    HasHeaderRecord = true,
                    MissingFieldFound = null
                };

                using var reader = new StreamReader(filePath, Encoding.UTF8);
                using var csv = new CsvReader(reader, config);

                // ヘッダーを読む
                await csv.ReadAsync();
                csv.ReadHeader();

                // データを読む
                while (await csv.ReadAsync())
                {
                    var user = new User
                    {
                        DisplayName = csv.GetField<string>(0) ?? string.Empty,
                        Email = csv.GetField<string>(1) ?? string.Empty,
                        Department = csv.GetField<string>(2) ?? string.Empty,
                        JobTitle = csv.GetField<string>(3) ?? string.Empty,
                        OfficeLocation = csv.GetField<string>(4) ?? string.Empty,
                        PhoneNumber = csv.GetField<string>(5) ?? string.Empty,
                        AccountEnabled = csv.GetField<string>(6)?.Equals("Yes", StringComparison.OrdinalIgnoreCase) ?? false
                    };

                    var lastSignInStr = csv.GetField<string>(7);
                    if (!string.IsNullOrEmpty(lastSignInStr) && DateTime.TryParse(lastSignInStr, out var lastSignIn))
                    {
                        user.LastSignIn = lastSignIn;
                    }

                    users.Add(user);
                }

                _logger.LogInformation($"Imported {users.Count} users from {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import users from CSV");
                throw;
            }

            return users;
        }
    }
}