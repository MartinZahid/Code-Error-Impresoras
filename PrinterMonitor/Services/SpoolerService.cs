using System.Diagnostics;
using PrinterMonitor.Models;

namespace PrinterMonitor.Services;

internal class SpoolerService : IDisposable
{
    private IntPtr _hPrinter = IntPtr.Zero;
    private IntPtr _hChange = IntPtr.Zero;
    private Thread? _thread;
    private volatile bool _running;

    public event Action<PrinterState>? StateChanged;

    public List<string> ListPrinters() => NativeMethods.EnumPrinters();

    public void Start(string printerName)
    {
        Stop();
        _running = true;

        int ret = NativeMethods.OpenPrinter(printerName, out _hPrinter, IntPtr.Zero);
        if (ret == 0 || _hPrinter == IntPtr.Zero)
        {
            _running = false;
            return;
        }

        _hChange = NativeMethods.FindFirstPrinterChangeNotification(
            _hPrinter, NativeMethods.PRINTER_CHANGE_SET_PRINTER, 0, IntPtr.Zero);

        if (_hChange == IntPtr.Zero || _hChange == new IntPtr(-1))
        {
            NativeMethods.ClosePrinter(_hPrinter);
            _hPrinter = IntPtr.Zero;
            _running = false;
            return;
        }

        // Estado inicial
        NotifyState(printerName);

        _thread = new Thread(() => PollingLoop(printerName))
        {
            IsBackground = true,
            Name = "PrinterMonitor"
        };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join(2000);
        _thread = null;

        if (_hChange != IntPtr.Zero)
        {
            NativeMethods.FindClosePrinterChangeNotification(_hChange);
            _hChange = IntPtr.Zero;
        }
        if (_hPrinter != IntPtr.Zero)
        {
            NativeMethods.ClosePrinter(_hPrinter);
            _hPrinter = IntPtr.Zero;
        }
    }

    private void PollingLoop(string printerName)
    {
        while (_running)
        {
            int ret = NativeMethods.WaitForSingleObject(_hChange, 1000);
            if (ret == NativeMethods.WAIT_OBJECT_0)
            {
                NativeMethods.FindNextPrinterChangeNotification(_hChange, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                NotifyState(printerName);
            }
        }
    }

    private void NotifyState(string printerName)
    {
        try
        {
            var state = ReadState(printerName);
            StateChanged?.Invoke(state);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading state: {ex.Message}");
        }
    }

    private static PrinterState ReadState(string printerName)
    {
        var res = new PrinterState();
        var debugParts = new List<string>();

        // 1. Abrir impresora y leer todo de una
        int ret = NativeMethods.OpenPrinter(printerName, out IntPtr h, IntPtr.Zero);
        bool connected = ret != 0 && h != IntPtr.Zero;

        string? portName = null;
        if (connected)
        {
            var details = NativeMethods.GetPrinterDetails(h);
            uint mask = details.Status;
            uint cJobs = details.cJobs;
            portName = details.PortName;

            res.RawMask = mask;
            res.Offline       = (mask & NativeMethods.PRINTER_STATUS_OFFLINE) != 0;
            res.SinPapel      = (mask & NativeMethods.PRINTER_STATUS_PAPER_OUT) != 0;
            res.Atasco        = (mask & NativeMethods.PRINTER_STATUS_PAPER_JAM) != 0;
            res.Error         = (mask & NativeMethods.PRINTER_STATUS_ERROR) != 0;
            res.Pausada       = (mask & NativeMethods.PRINTER_STATUS_PAUSED) != 0;
            res.Ocupada       = (mask & NativeMethods.PRINTER_STATUS_BUSY) != 0;
            res.Imprimiendo   = (mask & NativeMethods.PRINTER_STATUS_PRINTING) != 0;
            res.PuertaAbierta = (mask & NativeMethods.PRINTER_STATUS_DOOR_OPEN) != 0;
            res.SinToner      = (mask & NativeMethods.PRINTER_STATUS_NO_TONER) != 0;
            res.TonerBajo     = (mask & NativeMethods.PRINTER_STATUS_TONER_LOW) != 0;
            res.ProblemaPapel = (mask & NativeMethods.PRINTER_STATUS_PAPER_PROBLEM) != 0;
            res.Intervencion  = (mask & NativeMethods.PRINTER_STATUS_USER_INTERVENTION) != 0;

            // 1b. Trabajos en cola
            uint jobStatus = NativeMethods.GetJobStatusFlags(h);
            if (jobStatus != 0)
            {
                debugParts.Add($"Jobs: 0x{jobStatus:X8}");
                if ((jobStatus & NativeMethods.JOB_STATUS_ERROR) != 0)
                    res.Error = true;
                if ((jobStatus & NativeMethods.JOB_STATUS_PAPEROUT) != 0)
                    res.SinPapel = true;
                if ((jobStatus & NativeMethods.JOB_STATUS_OFFLINE) != 0)
                    res.Offline = true;
                if ((jobStatus & NativeMethods.JOB_STATUS_USER_INTERVENTION) != 0)
                    res.Intervencion = true;
            }

            // 1c. Health-check: enviar trabajo fantasma
            if (!res.Offline)
            {
                bool ok = NativeMethods.SubmitTestJob(h);
                if (!ok)
                {
                    res.Intervencion = true;
                    res.Error = true;
                    debugParts.Add("test-job:FAIL");
                }
                else
                {
                    debugParts.Add("test-job:OK");
                }
            }
            else
            {
                debugParts.Add("test-job:skip(offline)");
            }

            NativeMethods.ClosePrinter(h);
        }
        res.Conectada = connected;

        // 2. Puerto (PORT_INFO_2) — monitor de puerto
        if (!string.IsNullOrEmpty(portName))
        {
            string? portStatus = NativeMethods.GetPortMonitorStatus(portName);
            if (portStatus != null)
                debugParts.Add(portStatus);
        }

        // 3. USB directo via IOCTL
        {
            // Coleccion de rutas a probar
            var usbPaths = new List<string>();

            if (!string.IsNullOrEmpty(portName) && portName.StartsWith("USB", StringComparison.OrdinalIgnoreCase))
                usbPaths.Add(@"\\.\" + portName);

            // Respaldo: enumerar todas las interfaces USBPRINT via SetupAPI
            usbPaths.AddRange(NativeMethods.EnumUsbPrinterPaths());

            bool usbFound = false;
            foreach (string path in usbPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var (usbStatus, diag) = NativeMethods.ReadUsbPrinterStatus(path);
                if (!usbStatus.HasValue)
                {
                    if (diag != null)
                        debugParts.Add($"USB[{path}]: {diag}");
                    continue;
                }

                usbFound = true;
                bool noError = (usbStatus.Value & 0x04) != 0;
                bool paperEmpty = (usbStatus.Value & 0x01) != 0;
                bool selected = (usbStatus.Value & 0x02) != 0;
                debugParts.Add($"USB: 0x{usbStatus.Value:X2} paper={(paperEmpty?1:0)} online={(selected?1:0)} ok={(noError?1:0)}");

                if (!noError)
                {
                    res.Error = true;
                    if (paperEmpty)
                        res.SinPapel = true;
                    else
                        res.Intervencion = true;
                }
                if (!selected && !res.Offline)
                    res.Offline = true;

                break;
            }
            if (!usbFound && usbPaths.Count > 0)
                debugParts.Add("USB: sin-acceso");
        }

        // 4. Event Log
        string? evtLog = PrintEventQuerier.GetRecentError(printerName);
        if (evtLog != null)
        {
            debugParts.Add(evtLog);
            res.Error = true;
            res.Intervencion = true;
        }

        // 5. WMI
        var wmi = WmiService.Query(printerName);
        if (wmi.Count > 0)
        {
            bool wmiOffline =
                (wmi.GetValueOrDefault("WorkOffline") as bool?) == true ||
                (wmi.GetValueOrDefault("PrinterState") as ushort?) == 8 ||
                (wmi.GetValueOrDefault("PrinterStatus") as ushort?) == 7;
            if (wmiOffline) res.Offline = true;

            int? err = wmi.GetValueOrDefault("DetectedErrorState") as int? ??
                       (wmi.GetValueOrDefault("DetectedErrorState") as ushort?);
            if (err == 2) res.Atasco = true;
            else if (err == 3) res.SinPapel = true;
            else if (err == 7) res.TonerBajo = true;
            else if (err == 8) res.SinToner = true;
            else if (err == 9) res.PuertaAbierta = true;
            else if (err == 10) res.Intervencion = true;

            int? ext = wmi.GetValueOrDefault("ExtendedPrinterStatus") as int? ??
                       (wmi.GetValueOrDefault("ExtendedPrinterStatus") as ushort?);
            if (ext == 17) res.Intervencion = true;
            else if (ext == 18) res.Atasco = true;
            else if (ext == 19) res.SinPapel = true;

            string wmiName = wmi.GetValueOrDefault("_matched_by") as string ?? "";
            string extra = !string.IsNullOrEmpty(wmiName) ? $" [{wmiName}]" : "";
            res.WmiInfo = $"WMI: S={wmi.GetValueOrDefault("PrinterStatus")} " +
                           $"St={wmi.GetValueOrDefault("PrinterState")} " +
                           $"Err={wmi.GetValueOrDefault("DetectedErrorState")}{extra}";
        }
        else
        {
            res.WmiInfo = "WMI: no encontrada";
        }

        // Error generico sin detalle -> intervencion
        if (res.Error && !res.SinPapel && !res.Atasco && !res.PuertaAbierta &&
            !res.SinToner && !res.TonerBajo && !res.ProblemaPapel && !res.Intervencion)
        {
            res.Intervencion = true;
        }

        res.JobDebug = string.Join(" | ", debugParts);
        res.Timestamp = DateTime.Now.ToString("HH:mm:ss");
        return res;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
