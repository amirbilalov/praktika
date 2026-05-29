using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using SystemInfoApp.Models;
using SystemInfoApp.Services;
using System.IO;
using System.Text.Json;

namespace SystemInfoApp.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISystemInfoCollector _collector;
    private readonly ISpeedTestService    _speedTest;
    private readonly TracerouteService    _traceroute;
    private readonly AppSettings          _settings;

    private CancellationTokenSource _cts = new();
    private bool   _isBusy;
    private string _statusMessage = "Готово";

    public ObservableCollection<SystemInfoItem> Items { get; } = [];

    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); RefreshCommand.RaiseCanExecuteChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public RelayCommand RefreshCommand { get; }

    private readonly SystemInfoItem _diskSpace = new() { Name = "Свободное место на диске, Гб" };
    private readonly SystemInfoItem _ram       = new() { Name = "ОЗУ, Гб" };
    private readonly SystemInfoItem _cpu       = new() { Name = "Процессор" };
    private readonly SystemInfoItem _path      = new() { Name = "PATH (переменные окружения)" };
    private readonly SystemInfoItem _screen    = new() { Name = "Разрешение экрана" };
    private readonly SystemInfoItem _openGl    = new() { Name = "Версия OpenGL" };
    private readonly SystemInfoItem _download  = new() { Name = "Скорость входящая, Мбит/с" };
    private readonly SystemInfoItem _upload    = new() { Name = "Скорость исходящая, Мбит/с" };
    private readonly SystemInfoItem _latency   = new() { Name = "Задержка (Latency), мс" };
    private readonly SystemInfoItem _tracert   = new() { Name = "Tracert до сервера" };
    private readonly SystemInfoItem _pkzi      = new() { Name = "Использование ключа ПКЗИ" };

    public MainViewModel(
        ISystemInfoCollector collector,
        ISpeedTestService    speedTest,
        TracerouteService    traceroute,
        AppSettings          settings)
    {
        _collector  = collector;
        _speedTest  = speedTest;
        _traceroute = traceroute;
        _settings   = settings;

        foreach (var item in new[]
        {
            _diskSpace, _ram, _cpu, _path, _screen,
            _openGl, _download, _upload, _latency, _tracert, _pkzi
        })
        {
            Items.Add(item);
        }

        RefreshCommand = new RelayCommand(
            async () => await CollectAllAsync(),
            () => !IsBusy);
    }

    public Task InitializeAsync() => CollectAllAsync();

    private async Task CollectAllAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsBusy = true;
        StatusMessage = "Сбор данных…";

        foreach (var item in Items)
        {
            item.Value = string.Empty;
            item.State = LoadingState.Loading;
        }

        var tasks = new List<Task>
        {
            UpdateItemAsync(_diskSpace, () => _collector.GetFreeDiskSpaceAsync(ct),    ct),
            UpdateItemAsync(_ram,       () => _collector.GetRamAsync(ct),              ct),
            UpdateItemAsync(_cpu,       () => _collector.GetCpuAsync(ct),              ct),
            UpdateItemAsync(_path,      () => _collector.GetPathVariableAsync(ct),     ct),
            UpdateItemAsync(_screen,    () => _collector.GetScreenResolutionAsync(ct), ct),
            UpdateItemAsync(_openGl,    () => _collector.GetOpenGlVersionAsync(ct),    ct),
            RunSpeedTestAsync(ct),
            RunTracertAndPkziAsync(ct),
        };

        try
        {
            await Task.WhenAll(tasks);
            await Task.WhenAll(tasks);
            await SaveToFileAsync();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Отменено";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveToFileAsync()
    {
        var lines = Items
            .Select(item => $"{item.Name}: {item.Value}")
            .ToArray();

        var outputPath = Path.Combine(
            Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName,
            "system_info.json");

        var json = JsonSerializer.Serialize(lines, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        await File.WriteAllTextAsync(outputPath, json);
    }

    private static async Task UpdateItemAsync(
        SystemInfoItem     item,
        Func<Task<string>> getter,
        CancellationToken  ct)
    {
        try
        {
            string value = await getter();
            SetItem(item, value, LoadingState.Done);
        }
        catch (OperationCanceledException)
        {
            SetItem(item, "Отменено", LoadingState.Error);
        }
        catch (Exception ex)
        {
            SetItem(item, $"Ошибка: {ex.Message}", LoadingState.Error);
        }
    }

    private async Task RunSpeedTestAsync(CancellationToken ct)
    {
        try
        {
            double latency = await _speedTest.MeasureLatencyAsync(ct);
            SetItem(_latency, $"{latency:F1}", LoadingState.Done);
        }
        catch (OperationCanceledException) { SetItem(_latency, "Отменено", LoadingState.Error); return; }
        catch (Exception ex)               { SetItem(_latency, $"Ошибка: {ex.Message}", LoadingState.Error); }

        var dlTask = Task.Run(async () =>
        {
            try
            {
                double dl = await _speedTest.MeasureDownloadAsync(ct);
                SetItem(_download, $"{dl:F2}", LoadingState.Done);
            }
            catch (OperationCanceledException) { SetItem(_download, "Отменено", LoadingState.Error); }
            catch (Exception ex)               { SetItem(_download, $"Ошибка: {ex.Message}", LoadingState.Error); }
        }, ct);

        var ulTask = Task.Run(async () =>
        {
            try
            {
                double ul = await _speedTest.MeasureUploadAsync(ct);
                SetItem(_upload, $"{ul:F2}", LoadingState.Done);
            }
            catch (OperationCanceledException) { SetItem(_upload, "Отменено", LoadingState.Error); }
            catch (Exception ex)               { SetItem(_upload, $"Ошибка: {ex.Message}", LoadingState.Error); }
        }, ct);

        await Task.WhenAll(dlTask, ulTask);
    }

    private async Task RunTracertAndPkziAsync(CancellationToken ct)
    {
        List<Models.TracerouteHop>? hops = null;

        try
        {
            var progressItems = new List<Models.TracerouteHop>();

            var progress = new Progress<Models.TracerouteHop>(hop =>
            {
                progressItems.Add(hop);
                SetItem(_tracert, TracerouteService.Format(progressItems), LoadingState.Loading);
            });

            hops = await _traceroute.TraceAsync(
                _settings.Traceroute.Host,
                _settings.Traceroute.MaxHops,
                _settings.Traceroute.TimeoutMs,
                progress,
                ct);

            SetItem(_tracert, TracerouteService.Format(hops), LoadingState.Done);
        }
        catch (OperationCanceledException)
        {
            SetItem(_tracert, "Отменено", LoadingState.Error);
            SetItem(_pkzi,    "Отменено", LoadingState.Error);
            return;
        }
        catch (Exception ex)
        {
            SetItem(_tracert, $"Ошибка: {ex.Message}", LoadingState.Error);
            SetItem(_pkzi,    "Н/Д (ошибка tracert)",  LoadingState.Error);
            return;
        }

        try
        {
            var known = _settings.Pkzi.KnownAddresses
                            .Select(a => a.Trim())
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var matched = hops
                .Where(h => known.Contains(h.Address))
                .Select(h => h.Address)
                .Distinct()
                .ToList();

            string value = matched.Count > 0
                ? $"Да  (адреса в маршруте: {string.Join(", ", matched)})"
                : "Нет";

            SetItem(_pkzi, value, LoadingState.Done);
        }
        catch (Exception ex)
        {
            SetItem(_pkzi, $"Ошибка: {ex.Message}", LoadingState.Error);
        }
    }

    private static void SetItem(SystemInfoItem item, string value, LoadingState state)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            item.Value = value;
            item.State = state;
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        (_speedTest as IDisposable)?.Dispose();
    }
}
