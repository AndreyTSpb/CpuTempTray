using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace CpuTempTray;

/// <summary>Рисует иконку трея с числом температуры.</summary>
internal static class TrayIconRenderer
{
    public const float WarmThreshold = 60f; // от этой температуры — оранжевый
    public const float HotThreshold = 80f;  // от этой температуры — красный

    public static Color ColorFor(float? t) => t switch
    {
        null => Color.FromArgb(110, 110, 110),
        < WarmThreshold => Color.FromArgb(46, 125, 50),
        < HotThreshold => Color.FromArgb(230, 110, 0),
        _ => Color.FromArgb(198, 40, 40),
    };

    public static Icon Create(float? temperature)
    {
        string text = temperature is float v ? Math.Round(v).ToString("0") : "--";
        return Create(text, ColorFor(temperature));
    }

    public static Icon Create(string text, Color background)
    {
        var size = SystemInformation.SmallIconSize; // 16/20/24/32 в зависимости от масштаба экрана
        using var bmp = new Bitmap(size.Width, size.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using (var brush = new SolidBrush(background))
            using (var path = RoundedRect(new RectangleF(0, 0, size.Width - 1, size.Height - 1), size.Width / 5f))
                g.FillPath(brush, path);

            float fontPx = size.Height * (text.Length > 2 ? 0.52f : 0.72f);
            using var font = new Font("Segoe UI", fontPx, FontStyle.Bold, GraphicsUnit.Pixel);
            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };
            g.DrawString(text, font, Brushes.White, new RectangleF(-2, 0, size.Width + 4, size.Height), sf);
        }

        IntPtr handle = bmp.GetHicon();
        try { return (Icon)Icon.FromHandle(handle).Clone(); }
        finally { DestroyIcon(handle); }
    }

    static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr handle);
}
