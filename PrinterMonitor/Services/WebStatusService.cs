using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using PrinterMonitor.Models;

namespace PrinterMonitor.Services;

internal static class WebStatusService
{
    private static string? _cachedIp;
    private static DateTime _lastDiscovery = DateTime.MinValue;
    private static readonly TimeSpan DiscoveryCooldown = TimeSpan.FromMinutes(5);
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };

    public static async Task<string?> ApplyStatus(PrinterState state, string? portName = null)
    {
        try
        {
            string? ip = await DiscoverPrinterIp(portName);
            if (ip == null) return "Web: no encontrada";

            string url = $"http://{ip}/general/status.html";
            string html;
            try
            {
                html = await _http.GetStringAsync(url);
            }
            catch
            {
                url = $"http://{ip}/";
                html = await _http.GetStringAsync(url);
            }

            var statuses = new List<string>();

            // Brother status: <dt>Device Status</dt><dd><span class="moni moniXxx">...</span>
            var statusMatch = Regex.Match(html,
                @"Device\s*Status.*?<span[^>]*class=""moni\s*moni(\w+)""[^>]*>(.*?)</span>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (statusMatch.Success)
            {
                string statusClass = statusMatch.Groups[1].Value.Trim();
                string statusText = statusMatch.Groups[2].Value.Trim();

                statuses.Add($"Web: {statusClass}={statusText}");

                if (statusClass.Contains("Warning", StringComparison.OrdinalIgnoreCase) ||
                    statusClass.Contains("Error", StringComparison.OrdinalIgnoreCase))
                {
                    state.Error = true;

                    if (statusText.Contains("Paper", StringComparison.OrdinalIgnoreCase) ||
                        statusText.Contains("Empty", StringComparison.OrdinalIgnoreCase))
                    {
                        if (statusText.Contains("Jam", StringComparison.OrdinalIgnoreCase))
                            state.Atasco = true;
                        else
                            state.SinPapel = true;
                    }
                    if (statusText.Contains("Toner", StringComparison.OrdinalIgnoreCase))
                    {
                        if (statusText.Contains("Low", StringComparison.OrdinalIgnoreCase))
                            state.TonerBajo = true;
                        else
                            state.SinToner = true;
                    }
                    if (statusText.Contains("Door", StringComparison.OrdinalIgnoreCase) ||
                        statusText.Contains("Cover", StringComparison.OrdinalIgnoreCase) ||
                        statusText.Contains("Open", StringComparison.OrdinalIgnoreCase))
                        state.PuertaAbierta = true;
                    if (statusText.Contains("Offline", StringComparison.OrdinalIgnoreCase) ||
                        statusText.Contains("Disconnect", StringComparison.OrdinalIgnoreCase))
                        state.Offline = true;
                }
            }

            // Toner level section
            var tonerMatch = Regex.Match(html,
                @"Toner.*?Level.*?<img[^>]*alt=""(\w+)""[^>]*class=""tonerremain""",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (tonerMatch.Success)
            {
                string level = tonerMatch.Groups[1].Value.Trim();
                if (level.Equals("Low", StringComparison.OrdinalIgnoreCase))
                    state.TonerBajo = true;
                else if (level.Equals("Empty", StringComparison.OrdinalIgnoreCase))
                    state.SinToner = true;
            }

            return statuses.Count > 0 ? string.Join(" | ", statuses) : $"Web: OK (ip={ip})";
        }
        catch (Exception ex)
        {
            return $"Web: error={ex.Message}";
        }
    }

    private static async Task<string?> DiscoverPrinterIp(string? portName)
    {
        // Usar cache temporal
        if (_cachedIp != null && DateTime.Now - _lastDiscovery < DiscoveryCooldown)
            return _cachedIp;

        // 1. Intentar resolver hostname de Brother
        string[] hosts = ["MFC-L2700DW", "MFC-L2700", "Brother", "BROTHER"];
        foreach (var h in hosts)
        {
            try
            {
                var addr = await Dns.GetHostAddressesAsync(h);
                if (addr.Length > 0)
                {
                    _cachedIp = addr[0].ToString();
                    _lastDiscovery = DateTime.Now;
                    return _cachedIp;
                }
            }
            catch { }
        }

        // 2. Escanear IPs locales en busca de puerto 80 + pagina Brother
        var localIp = GetLocalIp();
        if (localIp == null) return null;

        var subnet = GetSubnet(localIp);
        if (subnet == null) return null;

        var tasks = new List<Task<string?>>();
        for (int i = 1; i < 255; i++)
        {
            string ip = $"{subnet}.{i}";
            tasks.Add(CheckBrotherWeb(ip));
        }

        var results = await Task.WhenAll(tasks);
        foreach (var ip in results)
        {
            if (ip != null)
            {
                _cachedIp = ip;
                _lastDiscovery = DateTime.Now;
                return ip;
            }
        }

        return null;
    }

    private static async Task<string?> CheckBrotherWeb(string ip)
    {
        try
        {
            using var tcp = new TcpClient();
            var conn = tcp.BeginConnect(ip, 80, null, null);
            if (!conn.AsyncWaitHandle.WaitOne(500, false))
                return null;
            try { tcp.EndConnect(conn); } catch { return null; }
            tcp.Close();

            // HTTP GET para verificar si es Brother
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                string html = await client.GetStringAsync($"http://{ip}/");

            if (html.Contains("Brother", StringComparison.OrdinalIgnoreCase))
                return ip;
        }
        catch { }
        return null;
    }

    private static string? GetLocalIp()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint ep)
                return ep.Address.ToString();
        }
        catch { }
        return null;
    }

    private static string? GetSubnet(string ip)
    {
        var parts = ip.Split('.');
        if (parts.Length == 4)
            return $"{parts[0]}.{parts[1]}.{parts[2]}";
        return null;
    }
}
