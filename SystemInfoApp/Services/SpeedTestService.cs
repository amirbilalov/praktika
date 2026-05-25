using System.Diagnostics;
using System.Net.Http;
using SystemInfoApp.Models;

namespace SystemInfoApp.Services;

public sealed class SpeedTestService : ISpeedTestService, IDisposable
{
    private readonly AppSettings _settings;
    private readonly HttpClient  _http;

    public SpeedTestService(AppSettings settings)
    {
        _settings = settings;
        _http = new HttpClient
        {
            BaseAddress = new Uri(settings.SpeedTest.ServerUrl),
            Timeout     = TimeSpan.FromSeconds(settings.SpeedTest.TimeoutSeconds)
        };
    }

    public async Task<double> MeasureLatencyAsync(CancellationToken ct = default)
    {
        int count = _settings.SpeedTest.PingCount;
        var rtts  = new List<double>(count);

        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var sw = Stopwatch.StartNew();
            using var resp = await _http.GetAsync("/ping", HttpCompletionOption.ResponseContentRead, ct);
            resp.EnsureSuccessStatusCode();
            sw.Stop();

            rtts.Add(sw.Elapsed.TotalMilliseconds / 2.0);
        }

        rtts.Sort();
        return rtts[count / 2];
    }

    public async Task<double> MeasureDownloadAsync(CancellationToken ct = default)
    {
        long bytes = _settings.SpeedTest.DownloadSizeKb * 1024L;

        var sw = Stopwatch.StartNew();
        using var resp = await _http.GetAsync($"/download?bytes={bytes}", HttpCompletionOption.ResponseContentRead, ct);
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadAsByteArrayAsync(ct);
        sw.Stop();

        if (sw.Elapsed.TotalSeconds < 1e-9) return 0;

        return data.Length * 8.0 / (sw.Elapsed.TotalSeconds * 1_000_000.0);
    }

    public async Task<double> MeasureUploadAsync(CancellationToken ct = default)
    {
        long byteCount = _settings.SpeedTest.UploadSizeKb * 1024L;
        var  data      = new byte[byteCount];
        Random.Shared.NextBytes(data);

        using var content = new ByteArrayContent(data);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var sw = Stopwatch.StartNew();
        using var resp = await _http.PostAsync("/upload", content, ct);
        resp.EnsureSuccessStatusCode();
        sw.Stop();

        double elapsedSec = sw.Elapsed.TotalSeconds;
        try
        {
            var json = await resp.Content.ReadAsStringAsync(ct);
            var doc  = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("ms", out var msProp))
            {
                double serverMs = msProp.GetDouble();
                if (serverMs > 0)
                    elapsedSec = serverMs / 1000.0;
            }
        }
        catch { }

        if (elapsedSec < 1e-9) return 0;

        return byteCount * 8.0 / (elapsedSec * 1_000_000.0);
    }

    public async Task<SpeedTestResult> MeasureAllAsync(CancellationToken ct = default)
    {
        double latency = await MeasureLatencyAsync(ct);

        var dlTask = MeasureDownloadAsync(ct);
        var ulTask = MeasureUploadAsync(ct);
        await Task.WhenAll(dlTask, ulTask);

        return new SpeedTestResult(
            DownloadMbps: await dlTask,
            UploadMbps:   await ulTask,
            LatencyMs:    latency
        );
    }

    public void Dispose() => _http.Dispose();
}
