using ValPlay.Models;

namespace ValPlay.Controls;

public sealed class AudioVisualizationDrawable : IDrawable
{
  private static readonly Color Accent = Color.FromArgb("#00B4D8");
  private static readonly Color AccentSoft = Color.FromArgb("#6600B4D8");
  private static readonly Color Glow = Color.FromArgb("#3300B4D8");
  private static readonly Color GridColor = Color.FromArgb("#33FFFFFF");
  private static readonly Color LabelColor = Color.FromArgb("#88F0F6FC");

  private const float MinDb = -48f;
  private const float MaxDb = 0f;
  private static readonly float[] GridDb = [0f, -6f, -12f, -24f, -36f, -48f];

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

    var speed = IsPlaying ? 0.35 : 0.12;
    var phase = Phase * speed;

    switch (Mode)
    {
      case VisualizationMode.Bars:
        DrawEqualizerBars(canvas, dirtyRect, phase);
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
      if (bands[i] > 0.08f)
        return true;
    }

    return false;
  }

  private static float NormalizedToDb(float normalized) =>
    MinDb + normalized * (MaxDb - MinDb);

  private static float DbToHeight(float db, float plotHeight) =>
    plotHeight * (db - MinDb) / (MaxDb - MinDb);

  private void DrawEqualizerBars(ICanvas canvas, RectF rect, double phase)
  {
    const int barCount = 22;
    var showScale = rect.Width >= 180f;
    var leftPad = showScale ? 34f : 6f;
    var plot = new RectF(rect.Left + leftPad, rect.Top + 8f, rect.Right - 6f, rect.Bottom - 8f);
    var slot = plot.Width / barCount;
    var barWidth = slot * 0.52f;
    var useSpectrum = HasActiveSpectrum(Bands) && IsPlaying;

    DrawDbGrid(canvas, plot, showScale);

    for (var i = 0; i < barCount; i++)
    {
      float normalized;
      if (useSpectrum)
      {
        var index = Math.Min(i, Bands!.Length - 1);
        normalized = Bands[index];
      }
      else
      {
        var t = phase + i * 0.35;
        normalized = (float)(0.18 + Math.Abs(Math.Sin(t)) * 0.42);
      }

      var db = NormalizedToDb(normalized);
      var height = MathF.Max(4f, DbToHeight(db, plot.Height));
      var x = plot.Left + i * slot + (slot - barWidth) / 2f;
      var y = plot.Bottom - height;
      var intensity = (db - MinDb) / (MaxDb - MinDb);

      canvas.FillColor = Accent.WithAlpha(0.25f + intensity * 0.65f);
      canvas.FillRoundedRectangle(x, y, barWidth, height, 2);

      canvas.FillColor = AccentSoft.WithAlpha(0.15f + intensity * 0.25f);
      canvas.FillRoundedRectangle(x, y, barWidth, MathF.Min(4f, height), 2);
    }
  }

  private static void DrawDbGrid(ICanvas canvas, RectF plot, bool showLabels)
  {
    canvas.StrokeColor = GridColor;
    canvas.StrokeSize = 1f;
    canvas.FontSize = 10f;
    canvas.FontColor = LabelColor;

    foreach (var db in GridDb)
    {
      var y = plot.Bottom - DbToHeight(db, plot.Height);
      canvas.DrawLine(plot.Left, y, plot.Right, y);

      if (showLabels && db is 0f or -12f or -24f or -48f)
        canvas.DrawString($"{db:0}", plot.Left - 32f, y + 4f, HorizontalAlignment.Left);
    }
  }

  private void DrawWaves(ICanvas canvas, RectF rect, double phase)
  {
    DrawWaveLine(canvas, rect, phase, 3, 0.22f, Accent, 2.2f, 0);
    DrawWaveLine(canvas, rect, phase + 0.8, 5, 0.14f, AccentSoft, 1.8f, 1);
    DrawWaveLine(canvas, rect, phase + 1.6, 2, 0.18f, Glow, 1.4f, 2);
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
    var step = Math.Max(2f, rect.Width / 100f);
    var hasBands = HasActiveSpectrum(Bands) && IsPlaying;

    for (var x = rect.Left; x <= rect.Right; x += step)
    {
      var normalized = (x - rect.Left) / rect.Width;
      var amplitude = baseAmplitude;

      if (hasBands)
      {
        var index = (int)(normalized * (Bands!.Length - 1));
        amplitude *= 0.25f + Bands[index] * 0.9f;
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
        (0.25, 0.45, 0.22, 0.6, 2),
        (0.55, 0.35, 0.18, 0.75, 6),
        (0.72, 0.62, 0.16, 0.55, 12),
        (0.38, 0.68, 0.14, 0.65, 16),
        (0.62, 0.52, 0.12, 0.85, 20)
      };

    var hasBands = HasActiveSpectrum(Bands) && IsPlaying;

    foreach (var (xRatio, yRatio, sizeRatio, speed, bandIndex) in orbs)
    {
      var bandEnergy = hasBands
        ? Bands![Math.Min(bandIndex, Bands.Length - 1)]
        : (float)(0.35 + Math.Abs(Math.Sin(phase * speed)) * 0.35);

      var pulse = 0.6f + bandEnergy * 0.5f;
      var radius = rect.Width * sizeRatio * pulse;
      var cx = rect.Left + rect.Width * xRatio + Math.Sin(phase * speed * 0.4) * rect.Width * 0.02;
      var cy = rect.Top + rect.Height * yRatio + Math.Cos(phase * speed * 0.35) * rect.Height * 0.02;

      canvas.FillColor = Glow;
      canvas.FillCircle((float)cx, (float)cy, (float)(radius * 1.25));

      canvas.FillColor = Accent.WithAlpha(0.3f + bandEnergy * 0.5f);
      canvas.FillCircle((float)cx, (float)cy, (float)radius);
    }
  }
}
