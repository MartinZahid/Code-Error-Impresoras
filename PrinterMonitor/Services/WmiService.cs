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
            ];

            foreach (var q in queries)
            {
                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(q));
                foreach (ManagementObject p in searcher.Get())
                {
                    result["PrinterStatus"] = p["PrinterStatus"];
                    result["PrinterState"] = p["PrinterState"];
                    result["WorkOffline"] = p["WorkOffline"];
                    result["DetectedErrorState"] = p["DetectedErrorState"];
                    result["ExtendedPrinterStatus"] = p["ExtendedPrinterStatus"];
                    return result;
                }
            }

            // Fallback: busqueda parcial
            using var allSearch = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT * FROM Win32_Printer"));
            foreach (ManagementObject p in allSearch.Get())
            {
                string? wmiName = p["Name"]?.ToString();
                if (string.IsNullOrEmpty(wmiName)) continue;

                if (wmiName.IndexOf(printerName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    printerName.IndexOf(wmiName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result["PrinterStatus"] = p["PrinterStatus"];
                    result["PrinterState"] = p["PrinterState"];
                    result["WorkOffline"] = p["WorkOffline"];
                    result["DetectedErrorState"] = p["DetectedErrorState"];
                    result["ExtendedPrinterStatus"] = p["ExtendedPrinterStatus"];
                    result["_matched_by"] = wmiName;
                    return result;
                }
            }
        }
        catch { }

        return result;
    }
}
