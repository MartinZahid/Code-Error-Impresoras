using System.Diagnostics.Eventing.Reader;

namespace PrinterMonitor.Services;

internal static class PrintEventQuerier
{
    public static string? GetRecentError(string printerName)
    {
        try
        {
            var query = new EventLogQuery("Microsoft-Windows-PrintService/Operational", PathType.LogName)
            {
                ReverseDirection = true
            };
            using var reader = new EventLogReader(query);
            DateTime cutoff = DateTime.Now.AddSeconds(-15);
            int checkedCount = 0;
            EventRecord? evt;
            while ((evt = reader.ReadEvent()) != null && checkedCount < 50)
            {
                checkedCount++;
                using (evt)
                {
                    if (evt.TimeCreated.HasValue && evt.TimeCreated.Value < cutoff)
                        break;

                    if (evt.Id == 22 || evt.Id == 21)
                    {
                        var props = evt.Properties;
                        if (props != null && props.Count >= 2)
                        {
                            string? msg = props[1]?.Value?.ToString();
                            if (!string.IsNullOrEmpty(msg) && msg.Contains(printerName, StringComparison.OrdinalIgnoreCase))
                            {
                                string? doc = props.Count >= 3 ? props[2]?.Value?.ToString() : null;
                                return $"EventLog: Job#{props[0]?.Value} \"{doc}\" error at {evt.TimeCreated:HH:mm:ss}";
                            }
                        }
                    }
                }
            }
        }
        catch
        {
        }
        return null;
    }
}
