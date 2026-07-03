using System.Windows;
using System.Windows.Media;
using PrinterMonitor.Models;
using PrinterMonitor.Services;

namespace PrinterMonitor;

public partial class MainWindow : Window
{
    private readonly SpoolerService _spooler = new();
    private bool _monitoring;
    private int _conteoOffline, _conteoOnline;
    private bool _estableOffline;

    public MainWindow()
    {
        InitializeComponent();
        var ver = System.Reflection.Assembly.GetEntryAssembly()?.GetName()?.Version;
        LblVersion.Text = ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.1.0";
        LoadPrinters();
    }

    private void LoadPrinters()
    {
        var list = _spooler.ListPrinters();
        CboPrinters.ItemsSource = list;
        if (list.Count > 0)
            CboPrinters.SelectedIndex = 0;
    }

    private void BtnIniciar_Click(object sender, RoutedEventArgs e)
    {
        if (_monitoring)
        {
            StopMonitoring();
        }
        else
        {
            string? name = CboPrinters.SelectedItem as string;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Selecciona una impresora.", "Aviso",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            StartMonitoring(name);
        }
    }

    private void StartMonitoring(string name)
    {
        _monitoring = true;
        _conteoOffline = _conteoOnline = 0;
        _estableOffline = false;
        BtnIniciar.Content = "Detener";
        LblSub.Text = name;
        _spooler.StateChanged += OnStateChanged;
        _spooler.Start(name);
    }

    private void StopMonitoring()
    {
        _monitoring = false;
        _spooler.StateChanged -= OnStateChanged;
        _spooler.Stop();
        BtnIniciar.Content = "Iniciar";
        LblSub.Text = "Monitoreo detenido";
    }

    private void OnStateChanged(PrinterState state)
    {
        Dispatcher.Invoke(() => UpdateUI(state));
    }

    private void UpdateUI(PrinterState s)
    {
        // Histeresis
        if (s.Offline)
        {
            _conteoOffline++;
            _conteoOnline = 0;
        }
        else
        {
            _conteoOnline++;
            _conteoOffline = 0;
        }

        if (_conteoOffline >= 3 && !_estableOffline)
            _estableOffline = true;
        else if (_conteoOnline >= 3 && _estableOffline)
            _estableOffline = false;

        bool offline = _estableOffline;

        // Icono y estado
        IconoGrande.Text = offline ? "\u26D4" : s.Icono;
        LblEstado.Text = offline ? "IMPRESORA FUERA DE LINEA" : s.EstadoTexto;
        LblTimestamp.Text = $"{s.EstadoSub}  |  {s.Timestamp}";

        // Colores
        Color color;
        if (offline)
            color = Color.FromRgb(0xD6, 0x30, 0x31);
        else if (s.Error || s.SinPapel || s.Atasco)
            color = Color.FromRgb(0xE1, 0x70, 0x55);
        else if (s.IsReady)
            color = Color.FromRgb(0x00, 0xB8, 0x94);
        else if (s.Imprimiendo)
            color = Color.FromRgb(0x09, 0x84, 0xE3);
        else
            color = Color.FromRgb(0xFD, 0xCB, 0x6E);

        LblEstado.Foreground = new SolidColorBrush(color);

        // Detalles
        SetDot(DotOffline, offline, "#D63031");
        SetDot(DotError, s.Error, "#D63031");
        SetDot(DotSinPapel, s.SinPapel, "#E17055");
        SetDot(DotAtasco, s.Atasco, "#E17055");
        SetDot(DotProblemaPapel, s.ProblemaPapel, "#E17055");
        SetDot(DotSinToner, s.SinToner, "#D63031");
        SetDot(DotTonerBajo, s.TonerBajo, "#FDCB6E");
        SetDot(DotPuertaAbierta, s.PuertaAbierta, "#E17055");
        SetDot(DotPausada, s.Pausada, "#FDCB6E");
        SetDot(DotOcupada, s.Ocupada, "#FDCB6E");
        SetDot(DotImprimiendo, s.Imprimiendo, "#0984E3");
        SetDot(DotIntervencion, s.Intervencion, "#D63031");

        // Conexion
        if (s.Conectada)
        {
            LblConexion.Text = "\u2713 Conexion estable";
            LblConexion.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xB8, 0x94));
        }
        else
        {
            LblConexion.Text = "\u2718 Sin conexion";
            LblConexion.Foreground = new SolidColorBrush(Color.FromRgb(0xD6, 0x30, 0x31));
        }

        // Debug
        LblDebug.Text = s.DebugLine;

        // Histeresis indicator
        if (_monitoring)
        {
            if (offline)
                LblHist.Text = $"Offline: {_conteoOffline}/3";
            else
                LblHist.Text = $"Online: {_conteoOnline}/3";
        }
    }

    private static void SetDot(System.Windows.Controls.TextBlock dot, bool active, string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        dot.Text = active ? "\u25CF" : "\u25CB";
        dot.Foreground = active
            ? new SolidColorBrush(color)
            : new SolidColorBrush(Color.FromRgb(0xB2, 0xBE, 0xC3));
    }

    private void BtnSimular_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Modo simulacion disponible en la version Python.",
                        "Simular", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _spooler.Dispose();
        base.OnClosing(e);
    }
}
