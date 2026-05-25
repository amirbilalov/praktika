namespace SystemInfoApp.Models;

public sealed class AppSettings
{
    public SpeedTestSettings SpeedTest { get; init; } = new();
    public TracerouteSettings Traceroute { get; init; } = new();
    public PkziSettings Pkzi { get; init; } = new();
}

public sealed class SpeedTestSettings
{
    public string ServerUrl { get; init; } = "http://localhost:8000";
    public int DownloadSizeKb { get; init; } = 2048;
    public int UploadSizeKb { get; init; } = 1024;
    public int PingCount { get; init; } = 5;
    public int TimeoutSeconds { get; init; } = 30;
}

public sealed class TracerouteSettings
{
    public string Host { get; init; } = "8.8.8.8";
    public int MaxHops { get; init; } = 30;
    public int TimeoutMs { get; init; } = 3000;
}

public sealed class PkziSettings
{
    public List<string> KnownAddresses { get; init; } = ["10.205.110.129", "10.205.110.130"];
}
