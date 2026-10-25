using System.Text;

namespace CpuTempTray;

internal sealed class TrayApp : ApplicationContext
{
    const string AppTitle = "Температура CPU";

    readonly CpuSensor _sensor = new();
    readonly NotifyIcon _tray = new();
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 2000 };
    readonly ToolStripMenuItem _autostartItem = new("Запускать при входе в Windows");

    Icon? _icon;
    float? _max;
    bool _detailsOpen;

    public TrayApp()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Подробно…", null, (_, _) => ShowDetails());
        _autostartItem.Checked = Autostart.IsEnabled();
        _autostartItem.Click += (_, _) => ToggleAutostart();
        menu.Items.Add(_autostartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitThread());

        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ShowDetails();
        _tray.Visible = true;

        _timer.Tick += (_, _) => UpdateTemperature();
        _timer.Start();
        UpdateTemperature();
    }

    void UpdateTemperature()
    {
        float? t = CpuSensor.PickMain(_sensor.ReadAll());
        if (t is float cur) _max = Math.Max(_max ?? cur, cur);

        var old = _icon;
        _icon = TrayIconRenderer.Create(t);
        _tray.Icon = _icon;
        old?.Dispose();

        string tip = t is float v
            ? $"CPU: {v:F0} °C\nМаксимум: {_max:F0} °C"
            : "CPU: датчик не найден";
        _tray.Text = tip.Length > 127 ? tip[..127] : tip; // ограничение Windows
    }

    void ShowDetails()
    {
        if (_detailsOpen) return;
        _detailsOpen = true;
        try
        {
            var sb = new StringBuilder();
            foreach (var name in _sensor.CpuNames) sb.AppendLine(name);
            sb.AppendLine();

            var temps = _sensor.ReadAll();
            if (temps.Count == 0)
            {
                sb.AppendLine("Датчики температуры не найдены.");
                sb.AppendLine();
                sb.AppendLine("Установите драйвер PawnIO:");
                sb.AppendLine("winget install namazso.PawnIO");
            }
            foreach (var r in temps)
                sb.AppendLine($"{r.Name}: {r.Value:F1} °C");
            if (_max is float m)
                sb.AppendLine().AppendLine($"Максимум за сеанс: {m:F1} °C");

            MessageBox.Show(sb.ToString(), AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally { _detailsOpen = false; }
    }

    void ToggleAutostart()
    {
        bool ok = _autostartItem.Checked ? Autostart.Disable() : Autostart.Enable();
        if (!ok)
            MessageBox.Show("Не удалось изменить автозапуск.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        _autostartItem.Checked = Autostart.IsEnabled();
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        _icon?.Dispose();
        _sensor.Dispose();
        base.ExitThreadCore();
    }
}
