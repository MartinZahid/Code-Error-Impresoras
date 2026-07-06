using System.Runtime.InteropServices;
using System.Text;

namespace PrinterMonitor.Services;

internal static class NativeMethods
{
    public const uint PRINTER_STATUS_PAUSED           = 0x00000001;
    public const uint PRINTER_STATUS_ERROR            = 0x00000002;
    public const uint PRINTER_STATUS_PAPER_JAM        = 0x00000008;
    public const uint PRINTER_STATUS_PAPER_OUT        = 0x00000010;
    public const uint PRINTER_STATUS_PAPER_PROBLEM    = 0x00000040;
    public const uint PRINTER_STATUS_OFFLINE          = 0x00000080;
    public const uint PRINTER_STATUS_IO_ACTIVE        = 0x00000100;
    public const uint PRINTER_STATUS_BUSY             = 0x00000200;
    public const uint PRINTER_STATUS_PRINTING         = 0x00000400;
    public const uint PRINTER_STATUS_NOT_AVAILABLE    = 0x00001000;
    public const uint PRINTER_STATUS_WAITING          = 0x00002000;
    public const uint PRINTER_STATUS_PROCESSING       = 0x00004000;
    public const uint PRINTER_STATUS_INITIALIZING     = 0x00008000;
    public const uint PRINTER_STATUS_WARMING_UP       = 0x00010000;
    public const uint PRINTER_STATUS_TONER_LOW        = 0x00020000;
    public const uint PRINTER_STATUS_NO_TONER         = 0x00040000;
    public const uint PRINTER_STATUS_USER_INTERVENTION = 0x00100000;
    public const uint PRINTER_STATUS_OUT_OF_MEMORY    = 0x00200000;
    public const uint PRINTER_STATUS_DOOR_OPEN        = 0x00400000;
    public const uint PRINTER_STATUS_POWER_SAVE       = 0x01000000;

    public const uint PRINTER_ATTRIBUTE_WORK_OFFLINE  = 0x00000400;
    public const uint PRINTER_CHANGE_SET_PRINTER      = 0x00000002;

    public const uint PRINTER_ACCESS_USE = 0x00000008;

    public const int WAIT_OBJECT_0  = 0;
    public const int WAIT_TIMEOUT   = 0x00000102;
    public const uint INFINITE      = 0xFFFFFFFF;

    public const int PRINTER_ENUM_LOCAL       = 0x00000002;
    public const int PRINTER_ENUM_CONNECTIONS = 0x00000004;

    public const uint JOB_STATUS_PAUSED             = 0x00000001;
    public const uint JOB_STATUS_ERROR              = 0x00000002;
    public const uint JOB_STATUS_DELETING           = 0x00000004;
    public const uint JOB_STATUS_SPOOLING           = 0x00000008;
    public const uint JOB_STATUS_PRINTING           = 0x00000010;
    public const uint JOB_STATUS_OFFLINE            = 0x00000020;
    public const uint JOB_STATUS_PAPEROUT           = 0x00000040;
    public const uint JOB_STATUS_PRINTED            = 0x00000080;
    public const uint JOB_STATUS_DELETED            = 0x00000100;
    public const uint JOB_STATUS_BLOCKED            = 0x00000200;
    public const uint JOB_STATUS_USER_INTERVENTION  = 0x00000400;

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEMTIME
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct JOB_INFO_1
    {
        public uint JobId;
        public IntPtr pPrinterName;
        public IntPtr pMachineName;
        public IntPtr pUserName;
        public IntPtr pDocument;
        public IntPtr pDatatype;
        public IntPtr pStatus;
        public uint Status;
        public uint Priority;
        public uint Position;
        public uint TotalPages;
        public uint Size;
        public SYSTEMTIME Submitted;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DOC_INFO_1
    {
        public IntPtr pDocName;
        public IntPtr pOutputFile;
        public IntPtr pDatatype;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PRINTER_INFO_2
    {
        public IntPtr pServerName;
        public IntPtr pPrinterName;
        public IntPtr pShareName;
        public IntPtr pPortName;
        public IntPtr pDriverName;
        public IntPtr pComment;
        public IntPtr pLocation;
        public IntPtr pDevMode;
        public IntPtr pSepFile;
        public IntPtr pPrintProcessor;
        public IntPtr pDatatype;
        public IntPtr pParameters;
        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint cJobs;
        public uint AveragePPM;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool GetPrinter(IntPtr hPrinter, int Level, IntPtr pPrinter, int cbBuf, out int pcbNeeded);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int EnumPrinters(int Flags, string? Name, int Level, IntPtr pPrinterEnum, int cbBuf, out int pcbNeeded, out int pcReturned);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr FindFirstPrinterChangeNotification(IntPtr hPrinter, uint fdwFlags, uint fdwOptions, IntPtr pPrinterNotifyOptions);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool FindNextPrinterChangeNotification(IntPtr hChange, IntPtr pdwChange, IntPtr pPrinterNotifyOptions, IntPtr ppPrinterNotifyInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool FindClosePrinterChangeNotification(IntPtr hChange);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int StartDocPrinter(IntPtr hPrinter, int Level, IntPtr pDocInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBuf, int cbBuf, out int pcWritten);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool SetJob(IntPtr hPrinter, int JobId, int Level, IntPtr pJob, int Command);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool EnumJobs(IntPtr hPrinter, int FirstJob, int NoJobs, int Level, IntPtr pJob, int cbBuf, out int pcbNeeded, out int pcReturned);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool GetJob(IntPtr hPrinter, int JobId, int Level, IntPtr pJob, int cbBuf, out int pcbNeeded);

    public static string? GetPrinterString(IntPtr hPrinter)
    {
        int needed;
        GetPrinter(hPrinter, 2, IntPtr.Zero, 0, out needed);
        if (needed <= 0) return null;

        IntPtr buf = Marshal.AllocHGlobal(needed);
        try
        {
            if (!GetPrinter(hPrinter, 2, buf, needed, out needed))
                return null;

            PRINTER_INFO_2 info = Marshal.PtrToStructure<PRINTER_INFO_2>(buf);
            return Marshal.PtrToStringUni(info.pPrinterName);
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    public static uint GetPrinterStatus(IntPtr hPrinter)
    {
        int needed;
        GetPrinter(hPrinter, 2, IntPtr.Zero, 0, out needed);
        if (needed <= 0) return 0;

        IntPtr buf = Marshal.AllocHGlobal(needed);
        try
        {
            if (!GetPrinter(hPrinter, 2, buf, needed, out needed))
                return 0;

            PRINTER_INFO_2 info = Marshal.PtrToStructure<PRINTER_INFO_2>(buf);
            return info.Status;
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    public static uint GetPrinterAttributes(IntPtr hPrinter)
    {
        int needed;
        GetPrinter(hPrinter, 2, IntPtr.Zero, 0, out needed);
        if (needed <= 0) return 0;

        IntPtr buf = Marshal.AllocHGlobal(needed);
        try
        {
            if (!GetPrinter(hPrinter, 2, buf, needed, out needed))
                return 0;

            PRINTER_INFO_2 info = Marshal.PtrToStructure<PRINTER_INFO_2>(buf);
            return info.Attributes;
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    public static uint GetJobStatusFlags(IntPtr hPrinter)
    {
        int needed;
        GetPrinter(hPrinter, 2, IntPtr.Zero, 0, out needed);
        if (needed <= 0) return 0;

        IntPtr buf = Marshal.AllocHGlobal(needed);
        try
        {
            if (!GetPrinter(hPrinter, 2, buf, needed, out needed))
                return 0;

            PRINTER_INFO_2 info = Marshal.PtrToStructure<PRINTER_INFO_2>(buf);
            if (info.cJobs == 0) return 0;

            int cbBuf;
            int pcReturned;
            if (!EnumJobs(hPrinter, 0, (int)info.cJobs, 1, IntPtr.Zero, 0, out cbBuf, out pcReturned))
            {
                if (cbBuf <= 0) return 0;
            }
            if (cbBuf <= 0) return 0;

            IntPtr jobsBuf = Marshal.AllocHGlobal(cbBuf);
            try
            {
                if (!EnumJobs(hPrinter, 0, (int)info.cJobs, 1, jobsBuf, cbBuf, out cbBuf, out pcReturned))
                    return 0;

                uint aggStatus = 0;
                IntPtr ptr = jobsBuf;
                for (int i = 0; i < pcReturned; i++)
                {
                    JOB_INFO_1 job = Marshal.PtrToStructure<JOB_INFO_1>(ptr);
                    aggStatus |= job.Status;
                    ptr = IntPtr.Add(ptr, Marshal.SizeOf<JOB_INFO_1>());
                }
                return aggStatus;
            }
            finally
            {
                Marshal.FreeHGlobal(jobsBuf);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    public const int JOB_CONTROL_CANCEL = 3;
    public const int JOB_CONTROL_DELETE = 5;

    public static bool SubmitTestJob(IntPtr hPrinter)
    {
        try
        {
            IntPtr pDocName = Marshal.StringToHGlobalUni("__healthcheck__");
            IntPtr pDatatype = Marshal.StringToHGlobalUni("RAW");
            DOC_INFO_1 doc = new()
            {
                pDocName = pDocName,
                pOutputFile = IntPtr.Zero,
                pDatatype = pDatatype
            };
            IntPtr pDoc = Marshal.AllocHGlobal(Marshal.SizeOf<DOC_INFO_1>());
            try
            {
                Marshal.StructureToPtr(doc, pDoc, false);
                int jobId = StartDocPrinter(hPrinter, 1, pDoc);
                if (jobId <= 0) return false;

                // Intentar cancelar el trabajo; si falla, forzar eliminación
                if (!SetJob(hPrinter, jobId, 0, IntPtr.Zero, JOB_CONTROL_CANCEL))
                    SetJob(hPrinter, jobId, 0, IntPtr.Zero, JOB_CONTROL_DELETE);

                EndDocPrinter(hPrinter);
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(pDoc);
                Marshal.FreeHGlobal(pDocName);
                Marshal.FreeHGlobal(pDatatype);
            }
        }
        catch
        {
            return false;
        }
    }

    public static List<string> EnumPrinters()
    {
        var result = new List<string>();
        int needed, returned;
        int ret = EnumPrinters(
            PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS,
            null, 2, IntPtr.Zero, 0, out needed, out returned);

        if (needed <= 0) return result;

        IntPtr buf = Marshal.AllocHGlobal(needed);
        try
        {
            ret = EnumPrinters(
                PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS,
                null, 2, buf, needed, out needed, out returned);

            if (ret <= 0) return result;

            IntPtr ptr = buf;
            for (int i = 0; i < returned; i++)
            {
                PRINTER_INFO_2 info = Marshal.PtrToStructure<PRINTER_INFO_2>(ptr);
                string? name = Marshal.PtrToStringUni(info.pPrinterName);
                if (!string.IsNullOrEmpty(name))
                    result.Add(name);
                ptr = IntPtr.Add(ptr, Marshal.SizeOf<PRINTER_INFO_2>());
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
        return result;
    }

    // === Lee Status + cJobs + PortName en una sola llamada a GetPrinter ===
    public static (uint Status, uint cJobs, string? PortName) GetPrinterDetails(IntPtr hPrinter)
    {
        int needed;
        GetPrinter(hPrinter, 2, IntPtr.Zero, 0, out needed);
        if (needed <= 0) return (0, 0, null);

        IntPtr buf = Marshal.AllocHGlobal(needed);
        try
        {
            if (!GetPrinter(hPrinter, 2, buf, needed, out needed))
                return (0, 0, null);

            PRINTER_INFO_2 info = Marshal.PtrToStructure<PRINTER_INFO_2>(buf);
            return (info.Status, info.cJobs, Marshal.PtrToStringUni(info.pPortName));
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    // === Puerto (PORT_INFO_2) ===
    public const uint PORT_STATUS_OFFLINE = 1;
    public const uint PORT_STATUS_PAPER_OUT = 2;
    public const uint PORT_STATUS_PAPER_JAM = 3;
    public const uint PORT_STATUS_OUTPUT_BIN_FULL = 4;
    public const uint PORT_STATUS_PAPER_PROBLEM = 5;
    public const uint PORT_STATUS_NO_TONER = 6;
    public const uint PORT_STATUS_DOOR_OPEN = 7;
    public const uint PORT_STATUS_USER_INTERVENTION = 8;
    public const uint PORT_STATUS_OUT_OF_MEMORY = 9;

    public const uint PORT_TYPE_WRITE = 0x0001;
    public const uint PORT_TYPE_READ = 0x0002;
    public const uint PORT_TYPE_REDIRECTED = 0x0004;
    public const uint PORT_TYPE_NET_ATTACHED = 0x0008;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PORT_INFO_2
    {
        public IntPtr pPortName;
        public uint fPortType;
        public uint Reserved;
        public uint Severity;
        public uint Status;
        public uint dwStatusSize;
        public IntPtr pStatus;
        public uint dwProviderVersion;
        public IntPtr pMonitorName;
        public IntPtr pDescription;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool EnumPorts(string? pName, int Level, IntPtr pPorts, int cbBuf, out int pcbNeeded, out int pcReturned);

    public static string? GetPortMonitorStatus(string portName)
    {
        int needed, returned;
        bool ok = EnumPorts(null, 2, IntPtr.Zero, 0, out needed, out returned);
        if (!ok && needed <= 0) return null;

        IntPtr buf = Marshal.AllocHGlobal(needed);
        try
        {
            ok = EnumPorts(null, 2, buf, needed, out needed, out returned);
            if (!ok) return null;

            IntPtr ptr = buf;
            for (int i = 0; i < returned; i++)
            {
                PORT_INFO_2 port = Marshal.PtrToStructure<PORT_INFO_2>(ptr);
                string? name = Marshal.PtrToStringUni(port.pPortName);
                if (!string.IsNullOrEmpty(name) && name.Equals(portName, StringComparison.OrdinalIgnoreCase))
                {
                    string? statusStr = Marshal.PtrToStringUni(port.pStatus);
                    if (port.Status != 0 || !string.IsNullOrEmpty(statusStr))
                        return $"port=0x{port.Status:X8} '{statusStr}'";
                    return null;
                }
                ptr = IntPtr.Add(ptr, Marshal.SizeOf<PORT_INFO_2>());
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
        return null;
    }

    // === SetupAPI / USB directo ===
    public const uint DIGCF_PRESENT = 0x00000002;
    public const uint DIGCF_DEVICEINTERFACE = 0x00000010;

    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;
    public const uint OPEN_EXISTING = 3;
    public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
    public const uint INVALID_HANDLE_VALUE = unchecked((uint)-1);

    public const uint IOCTL_USBPRINT_GET_STATUS = 0x002B0008;

    public static readonly Guid GUID_DEVINTERFACE_USBPRINT = new("28D78FAD-5A12-11D1-AE5B-0000F803A8C2");

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVICE_INTERFACE_DATA
    {
        public uint cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    [DllImport("setupapi.dll", SetLastError = true)]
    public static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

    public static List<string> EnumUsbPrinterPaths()
    {
        var paths = new List<string>();
        Guid guid = GUID_DEVINTERFACE_USBPRINT;
        IntPtr devInfo = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (devInfo == new IntPtr(-1)) return paths;

        try
        {
            uint index = 0;
            while (true)
            {
                SP_DEVICE_INTERFACE_DATA diData = new SP_DEVICE_INTERFACE_DATA
                {
                    cbSize = (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>()
                };
                if (!SetupDiEnumDeviceInterfaces(devInfo, IntPtr.Zero, ref guid, index, ref diData))
                    break;

                uint required;
                SetupDiGetDeviceInterfaceDetail(devInfo, ref diData, IntPtr.Zero, 0, out required, IntPtr.Zero);
                if (required == 0) { index++; continue; }

                IntPtr buffer = Marshal.AllocHGlobal((int)required);
                try
                {
                    uint cbSize = IntPtr.Size == 8 ? 8u : 6u;
                    Marshal.WriteInt32(buffer, 0, (int)cbSize);

                    if (SetupDiGetDeviceInterfaceDetail(devInfo, ref diData, buffer, required, out required, IntPtr.Zero))
                    {
                        IntPtr strPtr = IntPtr.Add(buffer, (int)cbSize);
                        string? path = Marshal.PtrToStringUni(strPtr);
                        if (!string.IsNullOrEmpty(path))
                            paths.Add(path);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
                index++;
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(devInfo);
        }
        return paths;
    }

    public static (byte? Status, string? Diagnostic) ReadUsbPrinterStatus(string devicePath)
    {
        // Intentar con GENERIC_READ | GENERIC_WRITE primero
        IntPtr hDevice = CreateFile(devicePath, GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
        int lastError = Marshal.GetLastWin32Error();

        // Fallback: solo GENERIC_READ
        if (hDevice == new IntPtr(-1))
        {
            hDevice = CreateFile(devicePath, GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
            if (hDevice == new IntPtr(-1))
                return (null, $"CreateFile fail (RW err={lastError}, RO err={Marshal.GetLastWin32Error()})");
        }

        try
        {
            IntPtr pOut = Marshal.AllocHGlobal(1);
            try
            {
                uint returned;
                if (DeviceIoControl(hDevice, IOCTL_USBPRINT_GET_STATUS, IntPtr.Zero, 0, pOut, 1, out returned, IntPtr.Zero)
                    && returned >= 1)
                    return (Marshal.ReadByte(pOut), null);

                // Si falla GET_STATUS, probar GET_1284_ID (0x002B0004) como diagnstico
                IntPtr p1284 = Marshal.AllocHGlobal(1024);
                try
                {
                    if (DeviceIoControl(hDevice, 0x002B0004, IntPtr.Zero, 0, p1284, 1024, out returned, IntPtr.Zero)
                        && returned > 0)
                    {
                        string? id = Marshal.PtrToStringUni(p1284, (int)returned / 2);
                        return (null, $"1284: {(id ?? "null").Trim('\0').Replace('\n', '|')}");
                    }
                }
                finally { Marshal.FreeHGlobal(p1284); }

                return (null, $"IOCTL fail (GET_STATUS err={Marshal.GetLastWin32Error()})");
            }
            finally
            {
                Marshal.FreeHGlobal(pOut);
            }
        }
        finally
        {
            CloseHandle(hDevice);
        }
    }
}
