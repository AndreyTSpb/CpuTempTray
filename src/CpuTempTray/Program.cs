namespace CpuTempTray;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Не даём запустить вторую копию
        using var mutex = new Mutex(true, "CpuTempTray_SingleInstance", out bool isNew);
        if (!isNew) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
