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
  public float[]? Bands { get; set; }

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

  private static bool HasActiveSpectrum(float[]? bands)
  {
    if (bands is not { Length: > 0 })
      return false;

    for (var i = 0; i < bands.Length; i++)
    {
      if (bands[i] > 0.12f)
        return true;
    }

    return false;
  }

  private void DrawBars(ICanvas canvas, RectF rect, double phase)
  {
    const int barCount = 22;
    var slot = rect.Width / barCount;
    var barWidth = slot * 0.55f;
    var useSpectrum = HasActiveSpectrum(Bands) && IsPlaying;

    for (var i = 0; i < barCount; i++)
    {
      float energy;
      if (useSpectrum)
      {
        var index = i * Bands!.Length / barCount;
        energy = 0.1f + Bands[index] * 0.9f;
      }
      else
      {
        var t = phase + i * 0.55;
        energy = (float)(Math.Abs(Math.Sin(t)) * 0.55 + Math.Abs(Math.Sin(t * 2.3 + 0.4)) * 0.45);
      }

      var height = rect.Height * (0.08f + energy * 0.88f);
      var x = rect.Left + i * slot + (slot - barWidth) / 2f;
      var y = rect.Bottom - height;

      canvas.FillColor = Accent.WithAlpha(0.35f + energy * 0.55f);
      canvas.FillRoundedRectangle(x, y, barWidth, height, 3);
    }
  }

  private void DrawWaves(ICanvas canvas, RectF rect, double phase)
  {
    DrawWaveLine(canvas, rect, phase, 4, 0.28f, Accent, 2.5f, 0);
    DrawWaveLine(canvas, rect, phase + 1.2, 6, 0.18f, AccentSoft, 2f, 1);
    DrawWaveLine(canvas, rect, phase + 2.4, 3, 0.22f, Glow, 1.5f, 2);
  }

  private void DrawWaveLine(
    ICanvas canvas,
    RectF rect,
    double phase,
    int cycles,
    float baseAmplitude,
    Color color,
    float strokeWidth,
    int bandOffset)
  {
    var path = new PathF();
    var midY = rect.Top + rect.Height / 2f;
    var step = Math.Max(2f, rect.Width / 120f);
    var hasBands = HasActiveSpectrum(Bands) && IsPlaying;

    for (var x = rect.Left; x <= rect.Right; x += step)
    {
      var normalized = (x - rect.Left) / rect.Width;
      var amplitude = baseAmplitude;

      if (hasBands && IsPlaying)
      {
        var index = (int)(normalized * (Bands!.Length - 1));
        amplitude *= 0.35f + Bands[index] * 1.4f;
      }

      var y = midY + Math.Sin(normalized * Math.PI * cycles + phase + bandOffset) * rect.Height * amplitude;

      if (x <= rect.Left)
        path.MoveTo(x, (float)y);
      else
        path.LineTo(x, (float)y);
    }

    canvas.StrokeColor = color;
    canvas.StrokeSize = strokeWidth;
    canvas.DrawPath(path);
  }

  private void DrawOrbs(ICanvas canvas, RectF rect, double phase)
  {
    var orbs =
      new (double X, double Y, double Size, double Speed, int BandIndex)[]
      {
        (0.25, 0.45, 0.22, 1.0, 2),
        (0.55, 0.35, 0.18, 1.3, 6),
        (0.72, 0.62, 0.16, 0.9, 12),
        (0.38, 0.68, 0.14, 1.1, 16),
        (0.62, 0.52, 0.12, 1.5, 20)
      };

    var hasBands = HasActiveSpectrum(Bands) && IsPlaying;

    foreach (var (xRatio, yRatio, sizeRatio, speed, bandIndex) in orbs)
    {
      var bandEnergy = hasBands && IsPlaying
        ? Bands![Math.Min(bandIndex, Bands.Length - 1)]
        : (float)(0.5 + Math.Abs(Math.Sin(phase * speed)) * 0.5);

      var pulse = 0.65f + bandEnergy * 0.55f;
      var radius = rect.Width * sizeRatio * pulse;
      var cx = rect.Left + rect.Width * xRatio + Math.Sin(phase * speed * 0.6) * rect.Width * 0.04 * bandEnergy;
      var cy = rect.Top + rect.Height * yRatio + Math.Cos(phase * speed * 0.5) * rect.Height * 0.04 * bandEnergy;

      canvas.FillColor = Glow;
      canvas.FillCircle((float)cx, (float)cy, (float)(radius * 1.35));

      canvas.FillColor = Accent.WithAlpha(0.35f + bandEnergy * 0.55f);
      canvas.FillCircle((float)cx, (float)cy, (float)radius);
    }
  }
}
