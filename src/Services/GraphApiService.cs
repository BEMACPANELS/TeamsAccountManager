using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeamsAccountManager.Models;

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
                InitializeGraphClient();
                if (_graphClient == null)
                {
                    throw new InvalidOperationException("認証されていません");
                }

                var users = new List<Models.User>();
                var pageSize = _configuration.GetValue<int>("Application:PageSize", 100);
                
                // 取得するプロパティを指定
                var selectProperties = new[]
                {
                    "id", "businessPhones", "displayName", "givenName", "jobTitle", "mail",
                    "mobilePhone", "officeLocation", "preferredLanguage", "surname", 
                    "userPrincipalName", "department", "companyName", "country", "city",
                    "postalCode", "state", "usageLocation", "accountEnabled"
                };

                var request = _graphClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = selectProperties;
                        requestConfiguration.QueryParameters.Top = pageSize;
                        requestConfiguration.QueryParameters.Orderby = new[] { "displayName" };
                    });

                var response = await request ?? new UserCollectionResponse();
                var totalCount = 0;

                while (response?.Value != null)
                {
                    foreach (var graphUser in response.Value)
                    {
                        users.Add(Models.User.FromGraphUser(graphUser));
                        totalCount++;
                        
                        // 進捗報告
                        if (totalCount % 50 == 0)
                        {
                            progress?.Report(totalCount);
                        }
                    }

                    // 次のページがあるかチェック
                    // 次のページを取得
                    if (!string.IsNullOrEmpty(response.OdataNextLink))
                    {
                        response = await _graphClient.Users
                            .WithUrl(response.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
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
                    "id", "businessPhones", "displayName", "givenName", "jobTitle", "mail",
                    "mobilePhone", "officeLocation", "preferredLanguage", "surname", 
                    "userPrincipalName", "department", "companyName", "country", "city",
                    "postalCode", "state", "usageLocation", "accountEnabled"
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
                    "id", "businessPhones", "displayName", "givenName", "jobTitle", "mail",
                    "mobilePhone", "officeLocation", "preferredLanguage", "surname", 
                    "userPrincipalName", "department", "companyName", "country", "city",
                    "postalCode", "state", "usageLocation", "accountEnabled"
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