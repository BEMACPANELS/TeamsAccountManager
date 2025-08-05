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
        
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                
                // コンソールウィンドウを表示（デバッグ時のみ）
                #if DEBUG
                AllocConsole();
                Console.WriteLine("=== Teams Account Manager - Debug Console ===");
                Console.WriteLine($"起動時刻: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
                Console.WriteLine($"ビルド構成: Debug");
                #endif
                
                // DIコンテナの設定
                ConfigureServices();
                
                var window = new MainWindow();
                
                // LoginViewを初期コンテンツとして設定
                var loginView = new Views.LoginView();
                window.NavigateToContent(loginView);
                
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラー: {ex.Message}", "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}