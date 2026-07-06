using System.Diagnostics;
using PrinterMonitor.Models;

namespace PrinterMonitor.Services;

internal class SpoolerService : IDisposable
{
    private IntPtr _hPrinter = IntPtr.Zero;
    private IntPtr _hChange = IntPtr.Zero;
    private Thread? _thread;
    private volatile bool _running;

    // Health-check: se limita a 30s entre envios y requiere 2 fallos
    // consecutivos para marcar error, evitando saturar el spooler
    // y falsos positivos por fallos transitorios.
    private static readonly TimeSpan TestJobInterval = TimeSpan.FromSeconds(30);
    private const int TestJobFailThreshold = 2;
    private DateTime _lastTestJobTime = DateTime.MinValue;
    private int _testJobFailStreak;

    public event Action<PrinterState>? StateChanged;

    public List<string> ListPrinters() => NativeMethods.EnumPrinters();

    public void Start(string printerName)
    {
        Stop();
        _running = true;

        _thread = new Thread(() => {
            try
            {
                int ret = NativeMethods.OpenPrinter(printerName, out _hPrinter, IntPtr.Zero);
                if (ret == 0 || _hPrinter == IntPtr.Zero)
                {
                    // Reportar error via UI
                    NotifyError(printerName, $"No se pudo abrir la impresora: error={ret}");
                    _running = false;
                    return;
                }

                _hChange = NativeMethods.FindFirstPrinterChangeNotification(
                    _hPrinter, NativeMethods.PRINTER_CHANGE_SET_PRINTER, 0, IntPtr.Zero);

                if (_hChange == IntPtr.Zero || _hChange == new IntPtr(-1))
                {
                    int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    NativeMethods.ClosePrinter(_hPrinter);
                    _hPrinter = IntPtr.Zero;
                    NotifyError(printerName, $"FindFirstPrinterChangeNotification fallo: error={err}");
                    _running = false;
                    return;
                }

                // Estado inicial
                NotifyState(printerName);
                PollingLoop(printerName);
            }
            catch (Exception ex)
            {
                NotifyError(printerName, ex.Message);
                _running = false;
            }
        })
        {
            IsBackground = true,
            Name = "PrinterMonitor"
        };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;

        // Cerrar notificaciones primero para que WaitForSingleObject retorne inmediatamente
        if (_hChange != IntPtr.Zero)
        {
            NativeMethods.FindClosePrinterChangeNotification(_hChange);
            _hChange = IntPtr.Zero;
        }

        _thread?.Join(2000);
        _thread = null;

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
            }
            // Refrescar siempre, incluso con timeout, para capturar cambios
            // que el driver no notifica (papel, puerta, toner, etc.)
            NotifyState(printerName);
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

    private void NotifyError(string printerName, string error)
    {
        var state = new PrinterState
        {
            Offline = true,
            Error = true,
            Conectada = false
        };
        state.JobDebug = $"Error: {error}";
        state.Timestamp = DateTime.Now.ToString("HH:mm:ss");
        StateChanged?.Invoke(state);
    }

    private static ushort? ToUshort(object? val)
    {
        if (val is ushort u) return u;
        if (val is short s) return (ushort)s;
        if (val is int i) return (ushort)i;
        if (val is uint ui) return (ushort)ui;
        if (val is byte b) return b;
        if (val is string str && ushort.TryParse(str, out var p)) return p;
        return null;
    }

    private static int? ToInt(object? val)
    {
        if (val is int i) return i;
        if (val is short s) return s;
        if (val is ushort u) return u;
        if (val is uint ui) return (int)ui;
        if (val is string str && int.TryParse(str, out var p)) return p;
        return null;
    }

    private static bool ToBool(object? val)
    {
        if (val is bool b) return b;
        if (val is string s) return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";
        if (val is int i) return i != 0;
        if (val is uint ui) return ui != 0;
        return false;
    }

    private PrinterState ReadState(string printerName)
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

            // Releer PRINTER_INFO_2 solo si hay flags activos, por si el driver
            // no actualiza los flags en la primera lectura
            if (mask != 0 || cJobs > 0)
            {
                Thread.Sleep(50);
                var details2 = NativeMethods.GetPrinterDetails(h);
                uint mask2 = details2.Status;
                if (mask2 != mask)
                {
                    mask = mask2;
                    res.RawMask = mask;
                    debugParts.Add("mask-refresh");
                }
            }

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

            // 1c. Health-check: enviar trabajo fantasma.
            // Limitado a TestJobInterval para no saturar el spooler/driver,
            // y requiere TestJobFailThreshold fallos consecutivos para
            // marcar error, evitando falsos positivos transitorios.
            if (!res.Offline)
            {
                bool due = (DateTime.Now - _lastTestJobTime) >= TestJobInterval;
                if (due)
                {
                    _lastTestJobTime = DateTime.Now;
                    if (NativeMethods.SubmitTestJob(h))
                    {
                        _testJobFailStreak = 0;
                        debugParts.Add("test-job:OK");
                    }
                    else
                    {
                        _testJobFailStreak++;
                        debugParts.Add($"test-job:FAIL(x{_testJobFailStreak})");
                    }
                }
                else
                {
                    debugParts.Add("test-job:skip(throttled)");
                }

                if (_testJobFailStreak >= TestJobFailThreshold)
                {
                    res.Intervencion = true;
                    res.Error = true;
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
            var usbPaths = new List<string>();

            // (a) Desde el portName tipo USB001, USB002, etc.
            if (!string.IsNullOrEmpty(portName) &&
                portName.StartsWith("USB", StringComparison.OrdinalIgnoreCase))
            {
                usbPaths.Add(@"\\.\" + portName);
                // Tambin variante USBP<N> en caso de que exista
                if (portName.Length > 3 && int.TryParse(portName.AsSpan(3), out int portNum))
                {
                    usbPaths.Add($@"\\.\USBP{portNum:D3}");
                    usbPaths.Add($@"\\.\USBPRINT{portNum:D2}");
                }
            }

            // (b) Desde SetupAPI (GUID_DEVINTERFACE_USBPRINT) — slo detecta presentes
            usbPaths.AddRange(NativeMethods.EnumUsbPrinterPaths());

            // Quitar duplicados conservando orden
            usbPaths = usbPaths.Distinct().ToList();

            bool usbFound = false;
            foreach (string path in usbPaths)
            {
                var (usbStatus, diag) = NativeMethods.ReadUsbPrinterStatus(path);
                if (!usbStatus.HasValue)
                {
                    if (diag != null)
                        debugParts.Add($"USB: {diag} ({path})");
                    continue;
                }

                usbFound = true;
                // Bits segun estandar USB Printer Class 1.1 (Port Status byte):
                // bit 3 (0x08) = Not-Error, bit 4 (0x10) = Selected/Online,
                // bit 5 (0x20) = Paper Empty
                bool noError = (usbStatus.Value & 0x08) != 0;
                bool selected = (usbStatus.Value & 0x10) != 0;
                bool paperEmpty = (usbStatus.Value & 0x20) != 0;
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
            else if (!usbFound)
                debugParts.Add("USB: no-device");
        }

        // 4. Event Log — solo aplicar si la impresora ya reporta problemas
        // desde otras fuentes, para evitar falsos positivos con eventos viejos
        string? evtLog = PrintEventQuerier.GetRecentError(printerName);
        if (evtLog != null && !res.IsReady)
        {
            debugParts.Add(evtLog);
            res.Error = true;
            res.Intervencion = true;
        }
        else if (evtLog != null)
        {
            debugParts.Add(evtLog + " (ignorado, estado actual OK)");
        }

        // 5. WMI — fuente mas confiable para estado de papel/puerta/toner
        // Usa conversion robusta de tipos (WMI puede devolver ushort, int, short, etc.)
        var wmi = WmiService.Query(printerName);
        if (wmi.Count > 0)
        {
            bool wmiWorkOffline = ToBool(wmi.GetValueOrDefault("WorkOffline"));
            ushort? wmiState = ToUshort(wmi.GetValueOrDefault("PrinterState"));
            ushort? wmiStatus = ToUshort(wmi.GetValueOrDefault("PrinterStatus"));
            int? wmiErr = ToInt(wmi.GetValueOrDefault("DetectedErrorState"));
            int? wmiExt = ToInt(wmi.GetValueOrDefault("ExtendedPrinterStatus"));

            // Offline
            if (wmiWorkOffline || wmiState == 8 || wmiStatus == 7)
                res.Offline = true;

            // PrinterStatus: 3=ready, 4=printing, 6=stopped, 7=offline,
            // 8=jam, 9=paper out, 10=paper problem, 11=paused,
            // 12=toner low, 13=no toner, etc.
            if (wmiStatus.HasValue && wmiStatus > 3)
            {
                switch (wmiStatus.Value)
                {
                    case 6: res.Intervencion = true; break;
                    case 8: res.Atasco = true; break;
                    case 9: res.SinPapel = true; break;
                    case 10: res.ProblemaPapel = true; break;
                    case 11: res.Pausada = true; break;
                    case 12: res.TonerBajo = true; break;
                    case 13: res.SinToner = true; break;
                }
            }

            // DetectedErrorState: 2=jam, 3=paper out, 7=toner low,
            // 8=no toner, 9=door open, 10=intervention
            if (wmiErr == 2) res.Atasco = true;
            else if (wmiErr == 3) res.SinPapel = true;
            else if (wmiErr == 7) res.TonerBajo = true;
            else if (wmiErr == 8) res.SinToner = true;
            else if (wmiErr == 9) res.PuertaAbierta = true;
            else if (wmiErr == 10) res.Intervencion = true;

            // ExtendedPrinterStatus: 17=intervention, 18=jam, 19=paper out
            if (wmiExt == 17) res.Intervencion = true;
            else if (wmiExt == 18) res.Atasco = true;
            else if (wmiExt == 19) res.SinPapel = true;

            // Si WMI dice que esta lista (PrinterStatus=3) y no hay flags activos,
            // resetear cualquier flag fantasma que otras fuentes hayan puesto
            if (wmiStatus == 3 && res.IsReady)
            {
                res.Error = false;
                res.Intervencion = false;
            }

            string wmiName = wmi.GetValueOrDefault("_matched_by") as string ?? "";
            string extra = !string.IsNullOrEmpty(wmiName) ? $" [{wmiName}]" : "";
            res.WmiInfo = $"WMI: S={wmiStatus} St={wmiState} Err={wmiErr}{extra}";
        }
        else
        {
            res.WmiInfo = "WMI: no encontrada";
        }

        // 6. PrintQueue (System.Printing) — estado detallado del spooler
        // Funciona con impresoras WSD/red que los metodos anteriores no cubren
        string? pqInfo = PrintSystemService.ApplyStatus(printerName, res);
        if (pqInfo != null)
            debugParts.Add(pqInfo);

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
