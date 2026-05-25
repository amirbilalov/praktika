using System.IO;
using System.Management;
using System.Windows;
using System.Windows.Interop;

namespace SystemInfoApp.Services;

public sealed class SystemInfoCollector : ISystemInfoCollector
{
    public Task<string> GetFreeDiskSpaceAsync(CancellationToken ct = default) =>
        Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var exePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
            var root = Path.GetPathRoot(exePath)
                       ?? Path.GetPathRoot(AppContext.BaseDirectory)
                       ?? "C:\\";

            var drive = new DriveInfo(root);
            double freeGb  = drive.AvailableFreeSpace / 1_073_741_824.0;
            double totalGb = drive.TotalSize / 1_073_741_824.0;

            return $"{freeGb:F2} Гб свободно из {totalGb:F2} Гб  (диск {drive.Name})";
        }, ct);

    public Task<string> GetRamAsync(CancellationToken ct = default) =>
        Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");

            foreach (ManagementObject obj in searcher.Get())
            {
                var totalKb = Convert.ToInt64(obj["TotalVisibleMemorySize"]);
                var freeKb  = Convert.ToInt64(obj["FreePhysicalMemory"]);
                double totalGb = totalKb / 1_048_576.0;
                double freeGb  = freeKb  / 1_048_576.0;
                return $"{totalGb:F2} Гб (свободно {freeGb:F2} Гб)";
            }

            return "Н/Д";
        }, ct);

    public Task<string> GetCpuAsync(CancellationToken ct = default) =>
        Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var results = new List<string>();

            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed " +
                "FROM Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                var name    = obj["Name"]?.ToString()?.Trim() ?? "Unknown";
                var cores   = obj["NumberOfCores"]?.ToString() ?? "?";
                var logical = obj["NumberOfLogicalProcessors"]?.ToString() ?? "?";
                var mhz     = obj["MaxClockSpeed"] is uint u ? u : 0u;
                var speed   = mhz > 0 ? $"{mhz / 1000.0:F2} ГГц" : "?";

                results.Add($"{name}\n  Ядра: {cores} физ. / {logical} лог., {speed}");
            }

            return results.Count > 0 ? string.Join("\n", results) : "Н/Д";
        }, ct);

    public Task<string> GetPathVariableAsync(CancellationToken ct = default) =>
        Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(path))
                return "(переменная PATH не задана)";

            var entries = path.Split(';', StringSplitOptions.RemoveEmptyEntries);
            return string.Join("\n", entries);
        }, ct);

    public Task<string> GetScreenResolutionAsync(CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<string>();

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var window = Application.Current.MainWindow;
                var source = PresentationSource.FromVisual(window);
                double dpiX = 1.0, dpiY = 1.0;

                if (source?.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformToDevice.M11;
                    dpiY = source.CompositionTarget.TransformToDevice.M22;
                }

                int physW = (int)(SystemParameters.PrimaryScreenWidth  * dpiX);
                int physH = (int)(SystemParameters.PrimaryScreenHeight * dpiY);

                tcs.SetResult(
                    $"{physW}×{physH} пикселей  " +
                    $"(DPI: {dpiX * 96:F0}×{dpiY * 96:F0}, масштаб: {dpiX * 100:F0}%)");
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    public Task<string> GetOpenGlVersionAsync(CancellationToken ct = default) =>
        Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            return OpenGLService.GetVersion();
        }, ct);
}
