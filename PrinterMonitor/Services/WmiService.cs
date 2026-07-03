using System.Management;

namespace PrinterMonitor.Services;

internal static class WmiService
{
    public static Dictionary<string, object?> Query(string printerName)
    {
        var result = new Dictionary<string, object?>();

        try
        {
            var scope = new ManagementScope(@"\\.\root\cimv2");
            scope.Connect();

            string[] queries = [
                $"SELECT * FROM Win32_Printer WHERE Name = '{printerName.Replace("'", "''")}'",
                $"SELECT * FROM Win32_Printer WHERE Name = \"{printerName.Replace("\"", "\"\"")}\"",
                $"SELECT * FROM Win32_Printer WHERE DeviceID = '{printerName.Replace("'", "''")}'",
            ];

            foreach (var q in queries)
            {
                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(q));
                foreach (ManagementObject p in searcher.Get())
                {
                    FillResult(p, result);
                    return result;
                }
            }

            // Fallback: busqueda parcial por nombre
            using var allSearch = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT * FROM Win32_Printer"));
            foreach (ManagementObject p in allSearch.Get())
            {
                string? wmiName = p["Name"]?.ToString();
                if (string.IsNullOrEmpty(wmiName)) continue;

                if (wmiName.IndexOf(printerName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    printerName.IndexOf(wmiName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    FillResult(p, result, wmiName);
                    return result;
                }

                // Tambien buscar por DeviceID
                string? wmiDeviceId = p["DeviceID"]?.ToString();
                if (!string.IsNullOrEmpty(wmiDeviceId) &&
                    (wmiDeviceId.IndexOf(printerName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     printerName.IndexOf(wmiDeviceId, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    FillResult(p, result, wmiDeviceId);
                    return result;
                }
            }

            // Fallback final: acceso directo via path WMI
            try
            {
                string escaped = printerName.Replace("'", "''");
                var mo = new ManagementObject(scope,
                    new ManagementPath($"Win32_Printer.DeviceID='{escaped}'"),
                    new ObjectGetOptions());
                mo.Get();
                FillResult(mo, result);
            }
            catch { }
        }
        catch { }

        return result;
    }

    private static void FillResult(ManagementBaseObject p, Dictionary<string, object?> result, string? matchedBy = null)
    {
        result["PrinterStatus"] = p["PrinterStatus"];
        result["PrinterState"] = p["PrinterState"];
        result["WorkOffline"] = p["WorkOffline"];
        result["DetectedErrorState"] = p["DetectedErrorState"];
        result["ExtendedPrinterStatus"] = p["ExtendedPrinterStatus"];
        if (matchedBy != null)
            result["_matched_by"] = matchedBy;
    }
}
