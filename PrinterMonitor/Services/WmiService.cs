using System.Diagnostics;
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

            // 1. Acceso directo por path WMI (mas seguro, sin inyeccion)
            try
            {
                string escaped = printerName.Replace("'", "''");
                var mo = new ManagementObject(scope,
                    new ManagementPath($"Win32_Printer.DeviceID='{escaped}'"),
                    new ObjectGetOptions());
                mo.Get();
                FillResult(mo, result);
                if (result.Count > 0)
                    return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WMI path access failed: {ex.Message}");
            }

            // 2. WQL exacto (escapando caracteres especiales)
            string sanitized = printerName
                .Replace("'", "''")
                .Replace("\\", "\\\\")
                .Replace("%", "[%]")
                .Replace("_", "[_]");

            string[] queries = [
                $"SELECT * FROM Win32_Printer WHERE Name = '{sanitized}'",
                $"SELECT * FROM Win32_Printer WHERE DeviceID = '{sanitized}'",
            ];

            foreach (var q in queries)
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(q));
                    foreach (ManagementObject p in searcher.Get())
                    {
                        FillResult(p, result);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WMI query failed: {ex.Message}");
                }
            }

            // 3. Fallback: busqueda parcial por nombre
            try
            {
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

                    string? wmiDeviceId = p["DeviceID"]?.ToString();
                    if (!string.IsNullOrEmpty(wmiDeviceId) &&
                        (wmiDeviceId.IndexOf(printerName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         printerName.IndexOf(wmiDeviceId, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        FillResult(p, result, wmiDeviceId);
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WMI fallback search failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WMI scope connection failed: {ex.Message}");
        }

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
