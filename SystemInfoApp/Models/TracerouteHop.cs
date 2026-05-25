namespace SystemInfoApp.Models;

public sealed record TracerouteHop(
    int HopNumber,
    string Address,
    long RttMs
);
