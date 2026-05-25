namespace SystemInfoApp.Services;

public sealed record SpeedTestResult(
    double DownloadMbps,
    double UploadMbps,
    double LatencyMs
);

public interface ISpeedTestService
{
    Task<double> MeasureLatencyAsync(CancellationToken ct = default);
    Task<double> MeasureDownloadAsync(CancellationToken ct = default);
    Task<double> MeasureUploadAsync(CancellationToken ct = default);
    Task<SpeedTestResult> MeasureAllAsync(CancellationToken ct = default);
}
