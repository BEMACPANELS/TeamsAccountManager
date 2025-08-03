using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;

namespace TeamsAccountManager.Services
{
    /// <summary>
    /// 認証サービス
    /// </summary>
    public class AuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IPublicClientApplication _app;
        
        private string? _accessToken;
        private AuthenticationResult? _authResult;

        public AuthenticationService(ILogger<AuthenticationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // MSAL設定の読み込み
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            var redirectUri = _configuration["AzureAd:RedirectUri"];

            // PublicClientApplicationの作成
            _app = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithRedirectUri(redirectUri)
                .WithLogging(LogCallback, LogLevel.Verbose, true)
                .Build();
        }

        /// <summary>
        /// 認証状態を取得
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

        /// <summary>
        /// 現在のユーザー名を取得
        /// </summary>
        public string? CurrentUserName => _authResult?.Account?.Username;

        /// <summary>
        /// アクセストークンを取得
        /// </summary>
        public string? AccessToken => _accessToken;

        /// <summary>
        /// サイレント認証を試行
        /// </summary>
        public async Task<bool> TrySignInSilentlyAsync()
        {
            try
            {
                var scopes = _configuration.GetSection("AzureAd:Scopes").Get<string[]>() ?? Array.Empty<string>();
                var accounts = await _app.GetAccountsAsync();
                
                if (accounts.Any())
                {
                    var account = accounts.FirstOrDefault();
                    var result = await _app.AcquireTokenSilent(scopes, account)
                        .ExecuteAsync();
                    
                    _authResult = result;
                    _accessToken = result.AccessToken;
                    
                    _logger.LogInformation($"サイレント認証成功: {account?.Username}");
                    return true;
                }
            }
            catch (MsalUiRequiredException)
            {
                _logger.LogInformation("サイレント認証失敗: UI認証が必要");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "サイレント認証中にエラーが発生");
            }

            return false;
        }

        /// <summary>
        /// インタラクティブ認証を実行
        /// </summary>
        public async Task<bool> SignInAsync()
        {
            try
            {
                var scopes = _configuration.GetSection("AzureAd:Scopes").Get<string[]>() ?? Array.Empty<string>();
                
                var result = await _app.AcquireTokenInteractive(scopes)
                    .WithParentActivityOrWindow(GetParentWindow())
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync();

                _authResult = result;
                _accessToken = result.AccessToken;

                _logger.LogInformation($"インタラクティブ認証成功: {result.Account.Username}");
                return true;
            }
            catch (MsalException ex)
            {
                _logger.LogError(ex, $"認証エラー: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "認証中に予期しないエラーが発生");
                return false;
            }
        }

        /// <summary>
        /// サインアウト
        /// </summary>
        public async Task SignOutAsync()
        {
            try
            {
                if (_authResult?.Account != null)
                {
                    await _app.RemoveAsync(_authResult.Account);
                }

                _authResult = null;
                _accessToken = null;

                _logger.LogInformation("サインアウト完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "サインアウト中にエラーが発生");
            }
        }

        /// <summary>
        /// トークンの更新
        /// </summary>
        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                if (_authResult?.Account == null)
                {
                    return false;
                }

                var scopes = _configuration.GetSection("AzureAd:Scopes").Get<string[]>() ?? Array.Empty<string>();
                var result = await _app.AcquireTokenSilent(scopes, _authResult.Account)
                    .ExecuteAsync();

                _authResult = result;
                _accessToken = result.AccessToken;

                _logger.LogInformation("トークン更新成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "トークン更新中にエラーが発生");
                return false;
            }
        }

        /// <summary>
        /// 必要な権限を確認
        /// </summary>
        public bool HasRequiredPermissions()
        {
            if (_authResult?.Scopes == null)
            {
                return false;
            }

            var requiredScopes = _configuration.GetSection("AzureAd:Scopes").Get<string[]>() ?? Array.Empty<string>();
            return requiredScopes.All(scope => _authResult.Scopes.Contains(scope));
        }

        /// <summary>
        /// 親ウィンドウのハンドルを取得
        /// </summary>
        private IntPtr GetParentWindow()
        {
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(mainWindow);
                return windowInteropHelper.Handle;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// MSALログコールバック
        /// </summary>
        private void LogCallback(LogLevel level, string message, bool containsPii)
        {
            if (containsPii)
            {
                return; // PII情報を含む場合はログに出力しない
            }

            switch (level)
            {
                case LogLevel.Error:
                    _logger.LogError("MSAL: {Message}", message);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning("MSAL: {Message}", message);
                    break;
                case LogLevel.Info:
                    _logger.LogInformation("MSAL: {Message}", message);
                    break;
                case LogLevel.Verbose:
                    _logger.LogDebug("MSAL: {Message}", message);
                    break;
            }
        }
    }
}