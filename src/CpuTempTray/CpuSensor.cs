using LibreHardwareMonitor.Hardware;

namespace CpuTempTray;

internal readonly record struct TempReading(string Name, float Value);

/// <summary>Чтение температурных датчиков процессора через LibreHardwareMonitor.</summary>
internal sealed class CpuSensor : IDisposable
{
    // Intel: "CPU Package", AMD: "Core (Tctl/Tdie)"
    static readonly string[] PreferredSensors =
        { "CPU Package", "Core (Tctl/Tdie)", "Core (Tdie)", "Core (Tctl)", "Core Max" };

    readonly Computer _computer = new() { IsCpuEnabled = true };

    public CpuSensor() => _computer.Open();

    public IEnumerable<string> CpuNames => _computer.Hardware.Select(h => h.Name);

    public List<TempReading> ReadAll()
    {
        var list = new List<TempReading>();
        foreach (var hw in _computer.Hardware) Collect(hw, list);
        return list;
    }

    /// <summary>Выбирает основное значение для иконки.</summary>
    public static float? PickMain(IReadOnlyList<TempReading> temps)
    {
        if (temps.Count == 0) return null;
        foreach (var key in PreferredSensors)
            foreach (var t in temps)
                if (t.Name == key) return t.Value;
        return temps.Max(t => t.Value);
    }

    static void Collect(IHardware hw, List<TempReading> list)
    {
        hw.Update();
        foreach (var s in hw.Sensors)
        {
            if (s.SensorType != SensorType.Temperature || s.Value is not float v) continue;
            // "Distance to TjMax" имеет тип Temperature, но это запас до предела, а не температура
            if (s.Name.Contains("Distance", StringComparison.OrdinalIgnoreCase)) continue;
            if (v <= 0 || v >= 150) continue;
            list.Add(new TempReading(s.Name, v));
        }
        foreach (var sub in hw.SubHardware) Collect(sub, list);
    }

    public void Dispose() => _computer.Close();
}
