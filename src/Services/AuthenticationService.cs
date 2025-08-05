using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.IO;
using System.Linq;
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
        private string? _userRoles;

        public AuthenticationService(ILogger<AuthenticationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // MSAL設定の読み込み
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            var redirectUri = _configuration["AzureAd:RedirectUri"];

            // モックモードの判定
            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId))
            {
                _logger.LogWarning("Azure AD設定が未設定のため、モックモードで動作します");
                // モックモードでは_appはnullのまま
                _app = null!;
            }
            else
            {
                // PublicClientApplicationの作成
                _app = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                    .WithRedirectUri(redirectUri)
                    .WithLogging(LogCallback, Microsoft.Identity.Client.LogLevel.Verbose, true)
                    .Build();
                    
                // トークンキャッシュの設定
                _ = EnableTokenCacheAsync();
            }
        }
        
        /// <summary>
        /// トークンキャッシュを有効化
        /// </summary>
        private async Task EnableTokenCacheAsync()
        {
            try
            {
                // キャッシュファイルの保存先
                var cacheFileName = "teams_account_manager_cache.dat";
                var cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "TeamsAccountManager");
                
                Directory.CreateDirectory(cacheDir);
                
                var storageProperties = new StorageCreationPropertiesBuilder(
                    cacheFileName,
                    cacheDir)
                    .WithUnprotectedFile() // 簡単のため暗号化なし（本番環境では暗号化推奨）
                    .Build();
                
                var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
                cacheHelper.RegisterCache(_app.UserTokenCache);
                
                _logger.LogInformation("トークンキャッシュを有効化しました");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "トークンキャッシュの有効化に失敗しました");
            }
        }

        /// <summary>
        /// 認証状態を取得
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

        /// <summary>
        /// 現在のユーザー名を取得
        /// </summary>
        public string? CurrentUserName => _authResult?.Account?.Username ?? (_app == null ? "テストユーザー" : null);

        /// <summary>
        /// 現在のユーザーの権限を取得・設定
        /// </summary>
        public string? CurrentUserRoles 
        { 
            get => _userRoles;
            set => _userRoles = value;
        }

        /// <summary>
        /// アクセストークンを取得
        /// </summary>
        public string? AccessToken => _accessToken;

        /// <summary>
        /// サイレント認証を試行
        /// </summary>
        public async Task<bool> TrySignInSilentlyAsync()
        {
            // モックモードの場合
            if (_app == null)
            {
                return false; // サイレント認証は失敗扱い
            }
            
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
        /// ログイン
        /// </summary>
        public async Task<bool> LoginAsync()
        {
            // モックモードの場合
            if (_app == null)
            {
                _logger.LogInformation("モックモードでログイン");
                _accessToken = "mock-access-token";
                _authResult = null; // モックモードではnull
                await Task.Delay(1000); // 認証処理のシミュレーション
                return true;
            }
            
            // まずサイレント認証を試行
            if (await TrySignInSilentlyAsync())
            {
                return true;
            }

            // サイレント認証が失敗したらインタラクティブ認証
            return await SignInAsync();
        }

        /// <summary>
        /// インタラクティブ認証を実行
        /// </summary>
        private async Task<bool> SignInAsync()
        {
            // モックモードの場合
            if (_app == null)
            {
                _logger.LogWarning("モックモードではインタラクティブ認証はサポートされていません");
                return false;
            }
            
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
                _logger.LogInformation($"テナントID: {result.TenantId}");
                _logger.LogInformation($"ユーザーID: {result.UniqueId}");
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
        private void LogCallback(Microsoft.Identity.Client.LogLevel level, string message, bool containsPii)
        {
            if (containsPii)
            {
                return; // PII情報を含む場合はログに出力しない
            }

            switch (level)
            {
                case Microsoft.Identity.Client.LogLevel.Error:
                    _logger.LogError("MSAL: {Message}", message);
                    break;
                case Microsoft.Identity.Client.LogLevel.Warning:
                    _logger.LogWarning("MSAL: {Message}", message);
                    break;
                case Microsoft.Identity.Client.LogLevel.Info:
                    _logger.LogInformation("MSAL: {Message}", message);
                    break;
                case Microsoft.Identity.Client.LogLevel.Verbose:
                    _logger.LogDebug("MSAL: {Message}", message);
                    break;
            }
        }
    }
}