using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using SimpleAnimate.Core.Interfaces;
using SimpleAnimate.Core.Services;
using SimpleAnimate.Plugins;
using SimpleAnimate.ViewModels;
using SimpleAnimate.Views;

namespace SimpleAnimate;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Show splash immediately
            var splash = new SplashWindow();
            splash.Show();

            // Run DI & plugin initialization on a background thread
            var initTask = Task.Run(() =>
            {
                var services = new ServiceCollection();

                // Core services
                services.AddSingleton<IProjectService, ProjectService>();
                services.AddSingleton<IUndoService, UndoService>();
                services.AddSingleton<IElementService, ElementService>();

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<CanvasViewModel>();
                services.AddSingleton<TimelineViewModel>();
                services.AddSingleton<ToolbarViewModel>();

                // Views
                services.AddSingleton<MainWindow>();

                // Load plugins
                var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
                var plugins = PluginLoader.LoadPlugins(pluginDir);
                PluginLoader.RegisterAll(services, plugins);

                return services;
            });

            // Wait for both: init complete AND minimum splash display time
            var minSplashTask = Task.Delay(TimeSpan.FromSeconds(2));
            var services = await initTask;
            await minSplashTask;

            // Build provider and create main window on UI thread
            _serviceProvider = services.BuildServiceProvider();
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();

            splash.Close();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
