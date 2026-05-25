using System.Windows;
using Microsoft.Extensions.Configuration;
using SystemInfoApp.Models;
using SystemInfoApp.Services;
using SystemInfoApp.ViewModels;

namespace SystemInfoApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var settings = new AppSettings
        {
            SpeedTest  = config.GetSection("SpeedTest").Get<SpeedTestSettings>()   ?? new(),
            Traceroute = config.GetSection("Traceroute").Get<TracerouteSettings>() ?? new(),
            Pkzi       = config.GetSection("Pkzi").Get<PkziSettings>()             ?? new(),
        };

        ISystemInfoCollector collector  = new SystemInfoCollector();
        ISpeedTestService    speedTest  = new SpeedTestService(settings);
        TracerouteService    traceroute = new TracerouteService();

        var viewModel  = new MainViewModel(collector, speedTest, traceroute, settings);
        var mainWindow = new MainWindow(viewModel);

        mainWindow.Show();
    }
}
