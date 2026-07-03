using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrinterMonitor.Models;

public class PrinterState : INotifyPropertyChanged
{
    private bool _offline;
    private bool _sinPapel;
    private bool _atasco;
    private bool _error;
    private bool _pausada;
    private bool _ocupada;
    private bool _imprimiendo;
    private bool _puertaAbierta;
    private bool _sinToner;
    private bool _tonerBajo;
    private bool _problemaPapel;
    private bool _intervencion;
    private bool _conectada;
    private uint _rawMask;
    private string _wmiInfo = "";
    private string _jobDebug = "";
    private string _timestamp = "";

    public bool Offline       { get => _offline;       set => Set(ref _offline, value); }
    public bool SinPapel      { get => _sinPapel;      set => Set(ref _sinPapel, value); }
    public bool Atasco        { get => _atasco;        set => Set(ref _atasco, value); }
    public bool Error         { get => _error;         set => Set(ref _error, value); }
    public bool Pausada       { get => _pausada;       set => Set(ref _pausada, value); }
    public bool Ocupada       { get => _ocupada;       set => Set(ref _ocupada, value); }
    public bool Imprimiendo   { get => _imprimiendo;   set => Set(ref _imprimiendo, value); }
    public bool PuertaAbierta { get => _puertaAbierta; set => Set(ref _puertaAbierta, value); }
    public bool SinToner      { get => _sinToner;      set => Set(ref _sinToner, value); }
    public bool TonerBajo     { get => _tonerBajo;     set => Set(ref _tonerBajo, value); }
    public bool ProblemaPapel { get => _problemaPapel; set => Set(ref _problemaPapel, value); }
    public bool Intervencion  { get => _intervencion;  set => Set(ref _intervencion, value); }
    public bool Conectada     { get => _conectada;     set => Set(ref _conectada, value); }
    public uint RawMask       { get => _rawMask;       set => Set(ref _rawMask, value); }
    public string WmiInfo     { get => _wmiInfo;       set => Set(ref _wmiInfo, value); }
    public string JobDebug    { get => _jobDebug;      set => Set(ref _jobDebug, value); }
    public string Timestamp   { get => _timestamp;     set => Set(ref _timestamp, value); }

    public bool IsReady => !(Offline || Error || SinPapel || Atasco || Pausada ||
                             PuertaAbierta || SinToner || ProblemaPapel || Intervencion);

    public string Icono =>
        Offline       ? "\u2718" :   // ✘
        Error || SinPapel || Atasco ? "\u26A0" :   // ! de advertencia
        IsReady       ? "\u2713" :   // ✓
        Imprimiendo   ? "\u21BB" :   // ↻
                        "\u25CB";    // ○

    public string EstadoTexto =>
        Offline       ? "IMPRESORA FUERA DE LINEA" :
        Error || SinPapel || Atasco ? "IMPRESORA CON ERRORES" :
        IsReady       ? "IMPRESORA LISTA (Ready)" :
        Imprimiendo   ? "IMPRIMIENDO..." :
                        "IMPRESORA OCUPADA";

    public string EstadoSub =>
        Offline       ? "Sin conexion con la impresora" :
        Error || SinPapel || Atasco ? "Revisa los detalles abajo" :
        IsReady       ? "Funcionando correctamente" :
        Imprimiendo   ? "Trabajo en progreso" :
                        "Esperando turno";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? prop = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            if (prop is not nameof(IsReady) and not nameof(Icono)
                and not nameof(EstadoTexto) and not nameof(EstadoSub))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReady)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icono)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EstadoTexto)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EstadoSub)));
            }
        }
    }

    public string DebugLine
    {
        get
        {
            var activos = new List<string>();
            if (SinPapel) activos.Add("sin_papel");
            if (Atasco) activos.Add("atasco");
            if (Error) activos.Add("error");
            if (PuertaAbierta) activos.Add("puerta_abierta");
            if (SinToner) activos.Add("sin_toner");
            if (TonerBajo) activos.Add("toner_bajo");
            if (Intervencion) activos.Add("intervencion");
            var act = activos.Count > 0 ? " | Activos: " + string.Join(", ", activos) : "";
            string job = !string.IsNullOrEmpty(JobDebug) ? " | " + JobDebug : "";
            return $"mask=0x{RawMask:X08}  {WmiInfo}{job}{act}";
        }
    }
}
