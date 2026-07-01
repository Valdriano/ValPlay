using ValPlay.Models;

namespace ValPlay.Controls;

public sealed class AudioVisualizationView : ContentView
{
  public static readonly BindableProperty IsPlayingProperty =
    BindableProperty.Create(nameof(IsPlaying), typeof(bool), typeof(AudioVisualizationView), false,
      propertyChanged: OnRenderPropertyChanged);

  public static readonly BindableProperty ModeProperty =
    BindableProperty.Create(nameof(Mode), typeof(VisualizationMode), typeof(AudioVisualizationView),
      VisualizationMode.Off, propertyChanged: OnRenderPropertyChanged);

  public static readonly BindableProperty BandsProperty =
    BindableProperty.Create(nameof(Bands), typeof(float[]), typeof(AudioVisualizationView),
      Array.Empty<float>(), propertyChanged: OnBandsChanged);

  public static readonly BindableProperty BandsVersionProperty =
    BindableProperty.Create(nameof(BandsVersion), typeof(int), typeof(AudioVisualizationView),
      0, propertyChanged: OnBandsChanged);

  private readonly AudioVisualizationDrawable _drawable = new();
  private readonly GraphicsView _graphicsView;
  private readonly IDispatcherTimer _timer;
  private readonly float[] _displayBands = new float[22];

  public AudioVisualizationView()
  {
    _graphicsView = new GraphicsView
    {
      Drawable = _drawable,
      BackgroundColor = Colors.Transparent,
      HorizontalOptions = LayoutOptions.Fill,
      VerticalOptions = LayoutOptions.Fill
    };

    Content = _graphicsView;

    _timer = Dispatcher.CreateTimer();
    _timer.Interval = TimeSpan.FromMilliseconds(33);
    _timer.Tick += OnTimerTick;
  }

  public bool IsPlaying
  {
    get => (bool)GetValue(IsPlayingProperty);
    set => SetValue(IsPlayingProperty, value);
  }

  public VisualizationMode Mode
  {
    get => (VisualizationMode)GetValue(ModeProperty);
    set => SetValue(ModeProperty, value);
  }

  public float[] Bands
  {
    get => (float[])GetValue(BandsProperty);
    set => SetValue(BandsProperty, value);
  }

  public int BandsVersion
  {
    get => (int)GetValue(BandsVersionProperty);
    set => SetValue(BandsVersionProperty, value);
  }

  private static void OnRenderPropertyChanged(BindableObject bindable, object _, object __)
  {
    if (bindable is not AudioVisualizationView view)
      return;

    view.SyncDrawable();
    view._graphicsView.Invalidate();
  }

  private static void OnBandsChanged(BindableObject bindable, object _, object __)
  {
    if (bindable is not AudioVisualizationView view)
      return;

    view.CopyBandsToDisplay();
    view.SyncDrawable();
    view._graphicsView.Invalidate();
  }

  private void CopyBandsToDisplay()
  {
    var source = Bands;
    if (source is not { Length: > 0 })
      return;

    var count = Math.Min(source.Length, _displayBands.Length);
    Array.Copy(source, _displayBands, count);
  }

  private void OnTimerTick(object? sender, EventArgs e)
  {
    if (Mode == VisualizationMode.Off)
      return;

    if (!IsPlaying)
    {
      for (var i = 0; i < _displayBands.Length; i++)
        _displayBands[i] *= 0.9f;
    }

    SyncDrawable();
    _graphicsView.Invalidate();
  }

  private void SyncDrawable()
  {
    _drawable.IsPlaying = IsPlaying;
    _drawable.Mode = Mode;
    _drawable.Bands = _displayBands;
    UpdateTimerState();
  }

  private void UpdateTimerState()
  {
    var shouldRun = Mode != VisualizationMode.Off && IsVisible;

    if (shouldRun)
    {
      if (!_timer.IsRunning)
        _timer.Start();
      return;
    }

    if (_timer.IsRunning)
      _timer.Stop();
  }

  protected override void OnHandlerChanged()
  {
    base.OnHandlerChanged();
    UpdateTimerState();
  }

  protected override void OnParentSet()
  {
    base.OnParentSet();
    UpdateTimerState();
  }
}
