using System.Printing;
using PrinterMonitor.Models;

namespace PrinterMonitor.Services;

internal static class PrintSystemService
{
    public static string? ApplyStatus(string printerName, PrinterState state)
    {
        try
        {
            using var ps = new PrintServer();
            using var queue = ps.GetPrintQueue(printerName);
            var qs = queue.QueueStatus;

            if (qs.HasFlag(PrintQueueStatus.PaperOut))
                state.SinPapel = true;
            if (qs.HasFlag(PrintQueueStatus.PaperJam))
                state.Atasco = true;
            if (qs.HasFlag(PrintQueueStatus.DoorOpen))
                state.PuertaAbierta = true;
            if (qs.HasFlag(PrintQueueStatus.TonerLow))
                state.TonerBajo = true;
            if (qs.HasFlag(PrintQueueStatus.NoToner))
                state.SinToner = true;
            if (qs.HasFlag(PrintQueueStatus.Offline))
                state.Offline = true;
            if (qs.HasFlag(PrintQueueStatus.Error) || qs.HasFlag(PrintQueueStatus.PaperProblem))
                state.Error = true;

            return $"PQ: 0x{(uint)qs:X08}";
        }
        catch (Exception ex)
        {
            return $"PQ: error={ex.Message}";
        }
    }
}
