using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using TeamsAccountManager.Services;
using TeamsAccountManager.ViewModels;
using TeamsAccountManager.Views;

namespace TeamsAccountManager
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        private static string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TeamsAccountManager", "startup.log");
        
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // ログディレクトリ作成
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.WriteAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 起動開始\n");
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] base.OnStartup呼び出し\n");
                base.OnStartup(e);
                
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] リソース読み込み開始\n");
                // リソースをコードで読み込み
                LoadResourceDictionary("ja-JP");
                
                // リソースの存在チェック
                try
                {
                    var testResource = Application.Current.TryFindResource("DisplayName");
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] リソースチェック: {(testResource != null ? "OK" : "NG")}\n");
                }
                catch (Exception resEx)
                {
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] リソースエラー: {resEx.Message}\n");
                }
                
                // コンソールウィンドウを表示（デバッグ時のみ）
                #if DEBUG
                AllocConsole();
                Console.WriteLine("=== Teams Account Manager - Debug Console ===");
                Console.WriteLine($"起動時刻: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
                Console.WriteLine($"ビルド構成: Debug");
                #endif
                
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DIコンテナ設定開始\n");
                // DIコンテナの設定
                ConfigureServices();
                
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainWindow作成\n");
                var window = new MainWindow();
                
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] LoginView作成\n");
                // LoginViewを初期コンテンツとして設定
                var loginView = new Views.LoginView();
                window.NavigateToContent(loginView);
                
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Window表示\n");
                window.Show();
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 起動完了\n");
            }
            catch (Exception ex)
            {
                var errorMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 起動エラー: {ex.Message}\n{ex.StackTrace}\n";
                try
                {
                    File.AppendAllText(LogPath, errorMsg);
                }
                catch { }
                
                MessageBox.Show($"エラー: {ex.Message}\n\nログファイル: {LogPath}", "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
        
        private void ConfigureServices()
        {
            var services = new ServiceCollection();
            
            // 設定ファイルの読み込み
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();
            
            services.AddSingleton<IConfiguration>(configuration);
            
            // ロギングの設定
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            // サービスの登録
            services.AddSingleton<AuthenticationService>();
            services.AddSingleton<GraphApiService>();
            services.AddTransient<ExcelService>();
            services.AddTransient<CsvService>();
            
            // ViewModelの登録
            services.AddTransient<LoginViewModel>();
            services.AddTransient<UserListViewModel>();
            
            _serviceProvider = services.BuildServiceProvider();
        }
        
        public static T GetService<T>() where T : class
        {
            var app = (App)Current;
            if (app._serviceProvider == null)
            {
                throw new InvalidOperationException("ServiceProvider is not initialized");
            }
            
            return app._serviceProvider.GetRequiredService<T>();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
        
        private void LoadResourceDictionary(string culture)
        {
            try
            {
                // 実行ファイルのディレクトリからリソースファイルを読み込み
                var exeDir = AppContext.BaseDirectory;
                var resourceFile = Path.Combine(exeDir, "Resources", "Languages", $"Resources.{culture}.xaml");
                
                if (File.Exists(resourceFile))
                {
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] リソースファイル発見: {resourceFile}\n");
                    var resourceDict = new ResourceDictionary
                    {
                        Source = new Uri(resourceFile, UriKind.Absolute)
                    };
                    Application.Current.Resources.MergedDictionaries.Add(resourceDict);
                }
                else
                {
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] リソースファイルが見つかりません: {resourceFile}\n");
                }
            }
            catch (Exception ex)
            {
                // リソースが読み込めない場合は続行
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] リソース読み込みエラー: {ex.Message}\n");
            }
        }
    }
}