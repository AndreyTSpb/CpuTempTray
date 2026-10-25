using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace CpuTempTray;

/// <summary>
/// Автозапуск через Планировщик заданий: программе нужны права администратора,
/// а ключ реестра Run такие программы при входе не запускает.
/// </summary>
internal static class Autostart
{
    const string TaskName = "CpuTempTray";

    public static bool IsEnabled() => RunSchtasks("/query", "/tn", TaskName) == 0;

    public static bool Disable() => RunSchtasks("/delete", "/tn", TaskName, "/f") == 0;

    public static bool Enable()
    {
        string exe = Environment.ProcessPath!;
        string user = WindowsIdentity.GetCurrent().Name;

        // XML вместо простых флагов schtasks — чтобы снять запрет "запускать только от сети" на ноутбуках
        string xml = $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <Triggers>
                <LogonTrigger>
                  <Enabled>true</Enabled>
                  <UserId>{SecurityElement.Escape(user)}</UserId>
                  <Delay>PT10S</Delay>
                </LogonTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <UserId>{SecurityElement.Escape(user)}</UserId>
                  <LogonType>InteractiveToken</LogonType>
                  <RunLevel>HighestAvailable</RunLevel>
                </Principal>
              </Principals>
              <Settings>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
              </Settings>
              <Actions Context="Author">
                <Exec>
                  <Command>{SecurityElement.Escape(exe)}</Command>
                </Exec>
              </Actions>
            </Task>
            """;

        string file = Path.Combine(Path.GetTempPath(), "CpuTempTray_task.xml");
        File.WriteAllText(file, xml, Encoding.Unicode);
        try { return RunSchtasks("/create", "/tn", TaskName, "/xml", file, "/f") == 0; }
        finally { File.Delete(file); }
    }

    static int RunSchtasks(params string[] args)
    {
        var psi = new ProcessStartInfo("schtasks.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        return p.ExitCode;
    }
}
