using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TeamsAccountManager.Services;
using TeamsAccountManager.ViewModels;
using TeamsAccountManager.Views;

namespace TeamsAccountManager
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        public IServiceProvider Services => _serviceProvider!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // グローバルエラーハンドラーの設定
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // 設定ファイルの読み込み
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true);

            IConfiguration configuration = builder.Build();

            // DIコンテナの設定
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, configuration);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            // メインウィンドウの表示
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // 設定
            services.AddSingleton(configuration);

            // ログ
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.AddDebug();
            });

            // サービス
            services.AddSingleton<AuthenticationService>();
            services.AddSingleton<GraphApiService>();
            services.AddSingleton<ExcelService>();
            services.AddSingleton<CsvService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<UserListViewModel>();

            // Views
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginView>();
            services.AddTransient<UserListView>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogError(ex, "未処理の例外が発生しました");
                ShowErrorDialog(ex);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError(e.Exception, "UIスレッドで未処理の例外が発生しました");
            ShowErrorDialog(e.Exception);
            e.Handled = true;
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogError(e.Exception, "タスクで未処理の例外が発生しました");
            e.SetObserved();
        }

        private void LogError(Exception ex, string message)
        {
            try
            {
                var logger = _serviceProvider?.GetService<ILogger<App>>();
                logger?.LogError(ex, message);
            }
            catch
            {
                // ログ出力自体が失敗した場合は無視
            }
        }

        private void ShowErrorDialog(Exception ex)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var message = $"予期しないエラーが発生しました。\n\n{ex.Message}\n\nアプリケーションを再起動してください。";
                    MessageBox.Show(
                        message,
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            catch
            {
                // ダイアログ表示も失敗した場合は無視
            }
        }
    }
}