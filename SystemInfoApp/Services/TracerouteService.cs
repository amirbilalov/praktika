using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using SystemInfoApp.Models;

namespace SystemInfoApp.Services;

public sealed class TracerouteService
{
    public async Task<List<TracerouteHop>> TraceAsync(
        string host,
        int    maxHops   = 30,
        int    timeoutMs = 3000,
        IProgress<TracerouteHop>? progress = null,
        CancellationToken ct = default)
    {
        var result = new List<TracerouteHop>();

        IPAddress target;
        try
        {
            var addrs = await Dns.GetHostAddressesAsync(host, ct);
            target = addrs.First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                     ?? addrs.First();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Не удалось разрешить хост '{host}': {ex.Message}", ex);
        }

        var pingData    = new byte[32];
        var pingOptions = new PingOptions(1, true);

        for (int ttl = 1; ttl <= maxHops; ttl++)
        {
            ct.ThrowIfCancellationRequested();

            pingOptions.Ttl = ttl;
            PingReply reply;

            try
            {
                using var ping = new Ping();
                reply = await ping.SendPingAsync(target, timeoutMs, pingData, pingOptions);
            }
            catch (PingException)
            {
                var timeoutHop = new TracerouteHop(ttl, "*", -1);
                result.Add(timeoutHop);
                progress?.Report(timeoutHop);
                continue;
            }

            bool isTimeout = reply.Status == IPStatus.TimedOut;
            var  hop = new TracerouteHop(
                HopNumber: ttl,
                Address:   isTimeout ? "*" : (reply.Address?.ToString() ?? "*"),
                RttMs:     isTimeout ? -1  : reply.RoundtripTime
            );

            result.Add(hop);
            progress?.Report(hop);

            if (reply.Status == IPStatus.Success)
                break;
        }

        return result;
    }

    public static string Format(List<TracerouteHop> hops)
    {
        if (hops.Count == 0) return "(нет данных)";

        var sb = new StringBuilder();
        foreach (var h in hops)
        {
            string rtt = h.RttMs >= 0 ? $"{h.RttMs} мс" : "таймаут";
            sb.AppendLine($"  {h.HopNumber,2}  {h.Address,-20}  {rtt}");
        }
        return sb.ToString().TrimEnd();
    }
}
