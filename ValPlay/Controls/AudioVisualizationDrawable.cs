using ValPlay.Models;

namespace ValPlay.Controls;

public sealed class AudioVisualizationDrawable : IDrawable
{
  private static readonly Color Accent = Color.FromArgb("#00B4D8");
  private static readonly Color AccentSoft = Color.FromArgb("#6600B4D8");
  private static readonly Color Glow = Color.FromArgb("#3300B4D8");

  public double Phase { get; set; }
  public bool IsPlaying { get; set; }
  public VisualizationMode Mode { get; set; } = VisualizationMode.Bars;

  public void Draw(ICanvas canvas, RectF dirtyRect)
  {
    canvas.SaveState();
    canvas.FillColor = Color.FromArgb("#0D1117");
    canvas.FillRectangle(dirtyRect);

    if (Mode == VisualizationMode.Off)
    {
      canvas.RestoreState();
      return;
    }

    var speed = IsPlaying ? 1.0 : 0.25;
    var phase = Phase * speed;

    switch (Mode)
    {
      case VisualizationMode.Bars:
        DrawBars(canvas, dirtyRect, phase);
        break;
      case VisualizationMode.Waves:
        DrawWaves(canvas, dirtyRect, phase);
        break;
      case VisualizationMode.Orbs:
        DrawOrbs(canvas, dirtyRect, phase);
        break;
    }

    canvas.RestoreState();
  }

  private static void DrawBars(ICanvas canvas, RectF rect, double phase)
  {
    const int barCount = 22;
    var slot = rect.Width / barCount;
    var barWidth = slot * 0.55f;

    for (var i = 0; i < barCount; i++)
    {
      var t = phase + i * 0.55;
      var energy = Math.Abs(Math.Sin(t)) * 0.55 + Math.Abs(Math.Sin(t * 2.3 + 0.4)) * 0.45;
      var height = rect.Height * (0.08f + (float)energy * 0.88f);
      var x = rect.Left + i * slot + (slot - barWidth) / 2f;
      var y = rect.Bottom - height;

      canvas.FillColor = Accent.WithAlpha((float)(0.35 + energy * 0.55));
      canvas.FillRoundedRectangle(x, y, barWidth, height, 3);
    }
  }

  private static void DrawWaves(ICanvas canvas, RectF rect, double phase)
  {
    DrawWaveLine(canvas, rect, phase, 4, 0.28f, Accent, 2.5f);
    DrawWaveLine(canvas, rect, phase + 1.2, 6, 0.18f, AccentSoft, 2f);
    DrawWaveLine(canvas, rect, phase + 2.4, 3, 0.22f, Glow, 1.5f);
  }

  private static void DrawWaveLine(
    ICanvas canvas,
    RectF rect,
    double phase,
    int cycles,
    float amplitude,
    Color color,
    float strokeWidth)
  {
    var path = new PathF();
    var midY = rect.Top + rect.Height / 2f;
    var step = Math.Max(2f, rect.Width / 120f);

    for (var x = rect.Left; x <= rect.Right; x += step)
    {
      var normalized = (x - rect.Left) / rect.Width;
      var y = midY + Math.Sin(normalized * Math.PI * cycles + phase) * rect.Height * amplitude;

      if (x <= rect.Left)
        path.MoveTo(x, (float)y);
      else
        path.LineTo(x, (float)y);
    }

    canvas.StrokeColor = color;
    canvas.StrokeSize = strokeWidth;
    canvas.DrawPath(path);
  }

  private static void DrawOrbs(ICanvas canvas, RectF rect, double phase)
  {
    var orbs =
      new (double X, double Y, double Size, double Speed)[]
      {
        (0.25, 0.45, 0.22, 1.0),
        (0.55, 0.35, 0.18, 1.3),
        (0.72, 0.62, 0.16, 0.9),
        (0.38, 0.68, 0.14, 1.1),
        (0.62, 0.52, 0.12, 1.5)
      };

    foreach (var (xRatio, yRatio, sizeRatio, speed) in orbs)
    {
      var pulse = 0.75 + Math.Sin(phase * speed) * 0.25;
      var radius = rect.Width * sizeRatio * pulse;
      var cx = rect.Left + rect.Width * xRatio + Math.Sin(phase * speed * 0.6) * rect.Width * 0.04;
      var cy = rect.Top + rect.Height * yRatio + Math.Cos(phase * speed * 0.5) * rect.Height * 0.04;

      canvas.FillColor = Glow;
      canvas.FillCircle((float)cx, (float)cy, (float)(radius * 1.35));

      canvas.FillColor = Accent.WithAlpha(0.55f);
      canvas.FillCircle((float)cx, (float)cy, (float)radius);
    }
  }
}
