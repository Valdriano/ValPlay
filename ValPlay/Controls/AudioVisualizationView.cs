using ValPlay.Models;

namespace ValPlay.Controls;

public sealed class AudioVisualizationView : ContentView
{
  public static readonly BindableProperty IsPlayingProperty =
    BindableProperty.Create(nameof(IsPlaying), typeof(bool), typeof(AudioVisualizationView), false,
      propertyChanged: OnAnimationPropertyChanged);

  public static readonly BindableProperty ModeProperty =
    BindableProperty.Create(nameof(Mode), typeof(VisualizationMode), typeof(AudioVisualizationView),
      VisualizationMode.Off, propertyChanged: OnAnimationPropertyChanged);

  private readonly AudioVisualizationDrawable _drawable = new();
  private readonly GraphicsView _graphicsView;
  private readonly IDispatcherTimer _timer;
  private double _phase;

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

  private static void OnAnimationPropertyChanged(BindableObject bindable, object _, object __)
  {
    if (bindable is AudioVisualizationView view)
      view.UpdateTimerState();
  }

  private void OnTimerTick(object? sender, EventArgs e)
  {
    if (Mode == VisualizationMode.Off)
      return;

    _phase += IsPlaying ? 0.22 : 0.06;
    _drawable.Phase = _phase;
    _drawable.IsPlaying = IsPlaying;
    _drawable.Mode = Mode;
    _graphicsView.Invalidate();
  }

  private void UpdateTimerState()
  {
    var shouldRun = Mode != VisualizationMode.Off && IsVisible;

    if (shouldRun)
    {
      if (!_timer.IsRunning)
        _timer.Start();

      _drawable.Mode = Mode;
      _drawable.IsPlaying = IsPlaying;
      _graphicsView.Invalidate();
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
