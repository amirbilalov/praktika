namespace SystemInfoApp.Services;

public interface ISystemInfoCollector
{
    Task<string> GetFreeDiskSpaceAsync(CancellationToken ct = default);
    Task<string> GetRamAsync(CancellationToken ct = default);
    Task<string> GetCpuAsync(CancellationToken ct = default);
    Task<string> GetPathVariableAsync(CancellationToken ct = default);
    Task<string> GetScreenResolutionAsync(CancellationToken ct = default);
    Task<string> GetOpenGlVersionAsync(CancellationToken ct = default);
}
