using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TeamsAccountManager.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace TeamsAccountManager.Services
{
    /// <summary>
    /// Microsoft Graph API サービス
    /// </summary>
    public class GraphApiService
    {
        private readonly ILogger<GraphApiService> _logger;
        private readonly IConfiguration _configuration;
        private readonly AuthenticationService _authService;
        private GraphServiceClient? _graphClient;

        public GraphApiService(
            ILogger<GraphApiService> logger, 
            IConfiguration configuration,
            AuthenticationService authService)
        {
            _logger = logger;
            _configuration = configuration;
            _authService = authService;
        }

        /// <summary>
        /// Graph クライアントの初期化
        /// </summary>
        private void InitializeGraphClient()
        {
            if (_graphClient == null && _authService.IsAuthenticated)
            {
                var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(_authService));
                _graphClient = new GraphServiceClient(authProvider);
            }
        }

        /// <summary>
        /// トークンプロバイダー内部クラス
        /// </summary>
        private class TokenProvider : IAccessTokenProvider
        {
            private readonly AuthenticationService _authService;

            public TokenProvider(AuthenticationService authService)
            {
                _authService = authService;
            }

            public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_authService.AccessToken ?? string.Empty);
            }

            public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator();
        }

        /// <summary>
        /// 現在のユーザー情報を取得
        /// </summary>
        public async Task<Microsoft.Graph.Models.User?> GetCurrentUserAsync()
        {
            try
            {
                InitializeGraphClient();
                if (_graphClient == null)
                {
                    throw new InvalidOperationException("認証されていません");
                }

                var user = await _graphClient.Me.GetAsync();
                _logger.LogInformation($"現在のユーザー情報を取得: {user?.DisplayName}");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "現在のユーザー情報の取得中にエラーが発生");
                throw;
            }
        }

        /// <summary>
        /// すべてのユーザーを取得
        /// </summary>
        public async Task<List<Models.User>> GetUsersAsync(IProgress<int>? progress = null)
        {
            try
            {
                _logger.LogInformation("GetUsersAsync: 開始");
                
                InitializeGraphClient();
                if (_graphClient == null)
                {
                    throw new InvalidOperationException("認証されていません");
                }

                var users = new List<Models.User>();
                var pageSize = _configuration.GetValue<int>("Application:PageSize", 100);
                _logger.LogInformation($"GetUsersAsync: ページサイズ = {pageSize}");
                
                // 取得するプロパティを指定
                var selectProperties = new[]
                {
                    "id", "displayName", "givenName", "surname", "mail", "userPrincipalName",
                    "businessPhones", "mobilePhone", "officeLocation", "department", 
                    "jobTitle", "companyName", "country", "usageLocation", "accountEnabled"
                };
                _logger.LogInformation($"GetUsersAsync: 取得プロパティ数 = {selectProperties.Length}");

                _logger.LogInformation("GetUsersAsync: Graph API呼び出し開始");
                var request = _graphClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = selectProperties;
                        requestConfiguration.QueryParameters.Top = pageSize;
                        requestConfiguration.QueryParameters.Orderby = new[] { "displayName" };
                    });

                var response = await request ?? new UserCollectionResponse();
                _logger.LogInformation($"GetUsersAsync: 初回レスポンス取得完了");
                var totalCount = 0;

                while (response?.Value != null)
                {
                    _logger.LogInformation($"GetUsersAsync: ページ処理開始 - {response.Value.Count}件");
                    
                    foreach (var graphUser in response.Value)
                    {
                        try
                        {
                            var user = Models.User.FromGraphUser(graphUser);
                            users.Add(user);
                            totalCount++;
                            
                            // デバッグ: 最初の数件のユーザー情報を出力
                            if (totalCount <= 3)
                            {
                                _logger.LogInformation($"ユーザー {totalCount}: {user.DisplayName}");
                                _logger.LogInformation($"  - GivenName: {user.GivenName ?? "null"}");
                                _logger.LogInformation($"  - Surname: {user.Surname ?? "null"}");
                                _logger.LogInformation($"  - Country: {user.Country ?? "null"}");
                                _logger.LogInformation($"  - OfficeLocation: {user.OfficeLocation ?? "null"}");
                                _logger.LogInformation($"  - UsageLocation: {user.UsageLocation ?? "null"}");
                            }
                            
                            // 進捗報告
                            if (totalCount % 50 == 0)
                            {
                                _logger.LogInformation($"GetUsersAsync: {totalCount}件処理完了");
                                progress?.Report(totalCount);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"GetUsersAsync: ユーザー変換エラー - ID: {graphUser.Id}");
                        }
                    }

                    // 次のページがあるかチェック
                    if (!string.IsNullOrEmpty(response.OdataNextLink))
                    {
                        _logger.LogInformation("GetUsersAsync: 次のページを取得");
                        response = await _graphClient.Users
                            .WithUrl(response.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
                        _logger.LogInformation("GetUsersAsync: 全ページ取得完了");
                        break;
                    }
                }

                progress?.Report(totalCount);
                _logger.LogInformation($"ユーザー情報を取得完了: {totalCount}件");
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ユーザー一覧の取得中にエラーが発生");
                throw;
            }
        }

        /// <summary>
        /// 特定のユーザーを取得
        /// </summary>
        public async Task<Models.User?> GetUserByIdAsync(string userId)
        {
            try
            {
                InitializeGraphClient();
                if (_graphClient == null)
                {
                    throw new InvalidOperationException("認証されていません");
                }

                var selectProperties = new[]
                {
                    "id", "displayName", "givenName", "surname", "mail", "userPrincipalName",
                    "businessPhones", "mobilePhone", "officeLocation", "department", 
                    "jobTitle", "companyName", "country", "usageLocation", "accountEnabled"
                };

                var graphUser = await _graphClient.Users[userId]
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = selectProperties;
                    });

                if (graphUser != null)
                {
                    _logger.LogInformation($"ユーザー情報を取得: {graphUser.DisplayName}");
                    return Models.User.FromGraphUser(graphUser);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ユーザー {userId} の取得中にエラーが発生");
                throw;
            }
        }

        /// <summary>
        /// ユーザー情報を更新
        /// </summary>
        public async Task<bool> UpdateUserAsync(Models.User user)
        {
            try
            {
                InitializeGraphClient();
                if (_graphClient == null)
                {
                    throw new InvalidOperationException("認証されていません");
                }

                var graphUser = user.ToGraphUser();
                
                // IDは更新対象から除外
                graphUser.Id = null;

                await _graphClient.Users[user.Id]
                    .PatchAsync(graphUser);

                _logger.LogInformation($"ユーザー情報を更新: {user.DisplayName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ユーザー {user.DisplayName} の更新中にエラーが発生");
                user.ErrorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// ユーザー情報を部分更新
        /// </summary>
        public async Task<UpdateResult> UpdateUserPartialAsync(string userId, Dictionary<string, object> changes)
        {
            try
            {
                InitializeGraphClient();
                if (_graphClient == null)
                {
                    throw new InvalidOperationException("認証されていません");
                }

                var graphUser = new Microsoft.Graph.Models.User();
                
                // 変更されたプロパティのみ設定
                foreach (var change in changes)
                {
                    switch (change.Key)
                    {
                        case nameof(Models.User.DisplayName):
                            graphUser.DisplayName = change.Value?.ToString();
                            break;
                        case nameof(Models.User.Department):
                            graphUser.Department = change.Value?.ToString();
                            break;
                        case nameof(Models.User.JobTitle):
                            graphUser.JobTitle = change.Value?.ToString();
                            break;
                        case nameof(Models.User.OfficeLocation):
                            graphUser.OfficeLocation = change.Value?.ToString();
                            break;
                        case nameof(Models.User.PhoneNumber):
                            graphUser.BusinessPhones = change.Value != null 
                                ? new List<string> { change.Value.ToString()! } 
                                : new List<string>();
                            break;
                        case nameof(Models.User.AccountEnabled):
                            graphUser.AccountEnabled = change.Value as bool?;
                            break;
                    }
                }

                await _graphClient.Users[userId]
                    .PatchAsync(graphUser);

                _logger.LogInformation($"ユーザー {userId} の情報を部分更新");
                return new UpdateResult { Success = true, UserId = userId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ユーザー {userId} の部分更新中にエラーが発生");
                return new UpdateResult 
                { 
                    Success = false, 
                    UserId = userId, 
                    ErrorMessage = ex.Message 
                };
            }
        }

        /// <summary>
        /// 複数のユーザー情報を一括更新
        /// </summary>
        public async Task<List<Models.User>> UpdateUsersAsync(
            List<Models.User> users, 
            IProgress<(int current, int total, string message)>? progress = null)
        {
            var results = new List<Models.User>();
            var successCount = 0;
            var errorCount = 0;

            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                
                try
                {
                    progress?.Report((i + 1, users.Count, $"更新中: {user.DisplayName}"));
                    
                    var success = await UpdateUserAsync(user);
                    if (success)
                    {
                        successCount++;
                        user.IsModified = false;
                        user.ErrorMessage = null;
                    }
                    else
                    {
                        errorCount++;
                    }
                    
                    results.Add(user);
                    
                    // レート制限対策（100ms待機）
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"ユーザー {user.DisplayName} の更新中にエラー");
                    user.ErrorMessage = ex.Message;
                    errorCount++;
                    results.Add(user);
                }
            }

            progress?.Report((users.Count, users.Count, $"完了: 成功 {successCount}件, エラー {errorCount}件"));
            _logger.LogInformation($"一括更新完了: 成功 {successCount}件, エラー {errorCount}件");
            
            return results;
        }

        /// <summary>
        /// ユーザーを検索
        /// </summary>
        public async Task<List<Models.User>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                InitializeGraphClient();
                if (_graphClient == null)
                {
                    throw new InvalidOperationException("認証されていません");
                }

                var users = new List<Models.User>();
                
                var selectProperties = new[]
                {
                    "id", "displayName", "givenName", "surname", "mail", "userPrincipalName",
                    "businessPhones", "mobilePhone", "officeLocation", "department", 
                    "jobTitle", "companyName", "country", "usageLocation", "accountEnabled"
                };

                var response = await _graphClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = selectProperties;
                        requestConfiguration.QueryParameters.Search = $"\"displayName:{searchTerm}\" OR \"mail:{searchTerm}\" OR \"userPrincipalName:{searchTerm}\"";
                        requestConfiguration.QueryParameters.Top = 50;
                        requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                    });

                if (response?.Value != null)
                {
                    foreach (var graphUser in response.Value)
                    {
                        users.Add(Models.User.FromGraphUser(graphUser));
                    }
                }

                _logger.LogInformation($"検索完了: {users.Count}件 (検索語: {searchTerm})");
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ユーザー検索中にエラーが発生 (検索語: {searchTerm})");
                throw;
            }
        }
    }
}