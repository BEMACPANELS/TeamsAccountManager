namespace TeamsAccountManager.Models
{
    /// <summary>
    /// アプリケーション設定
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Azure AD設定
        /// </summary>
        public AzureAdSettings AzureAd { get; set; } = new();

        /// <summary>
        /// アプリケーション設定
        /// </summary>
        public ApplicationSettings Application { get; set; } = new();

        /// <summary>
        /// ログ設定
        /// </summary>
        public LoggingSettings Logging { get; set; } = new();
    }

    /// <summary>
    /// Azure AD設定
    /// </summary>
    public class AzureAdSettings
    {
        /// <summary>
        /// テナントID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// クライアントID（アプリケーションID）
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// リダイレクトURI
        /// </summary>
        public string RedirectUri { get; set; } = "http://localhost";

        /// <summary>
        /// 権限スコープ
        /// </summary>
        public string[] Scopes { get; set; } = new[]
        {
            "User.Read.All",
            "User.ReadWrite.All",
            "Directory.Read.All"
        };
    }

    /// <summary>
    /// アプリケーション設定
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// デフォルト言語
        /// </summary>
        public string DefaultLanguage { get; set; } = "ja-JP";

        /// <summary>
        /// ページサイズ（ユーザー一覧取得時）
        /// </summary>
        public int PageSize { get; set; } = 100;

        /// <summary>
        /// タイムアウト（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// リトライ回数
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// エクスポートファイルの既定パス
        /// </summary>
        public string DefaultExportPath { get; set; } = "exports";

        /// <summary>
        /// インポートファイルの既定パス
        /// </summary>
        public string DefaultImportPath { get; set; } = "imports";

        /// <summary>
        /// ログファイルの保存パス
        /// </summary>
        public string LogPath { get; set; } = "logs";
    }

    /// <summary>
    /// ログ設定
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// ログレベル
        /// </summary>
        public LogLevel LogLevel { get; set; } = new();
    }

    /// <summary>
    /// ログレベル設定
    /// </summary>
    public class LogLevel
    {
        /// <summary>
        /// デフォルトレベル
        /// </summary>
        public string Default { get; set; } = "Information";

        /// <summary>
        /// Microsoftのログレベル
        /// </summary>
        public string Microsoft { get; set; } = "Warning";

        /// <summary>
        /// Microsoft.Hostingのログレベル
        /// </summary>
        public string MicrosoftHosting { get; set; } = "Information";
    }
}