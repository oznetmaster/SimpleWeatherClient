using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;

using SimpleWeather.Widget.Services;
using SimpleWeather.Widget.ViewModels;

using Windows.Foundation;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

using WinRT.Interop;

namespace SimpleWeather.Widget;

public sealed partial class MainWindow : Window
	{
	private static string DebugTs => DateTime.Now.ToString ("HH:mm:ss.fff");
	private const double MINIMUM_DEFAULT_WIDTH = 320;
	private const double MINIMUM_DEFAULT_HEIGHT = 180;
	private const double COMPACT_CLIENT_WIDTH = 140;
	private const double COMPACT_CLIENT_HEIGHT = 76;

	public WeatherWidgetViewModel ViewModel { get; }
	private readonly WidgetSettingsStore _settingsStore = new ();

	private AppWindow? _appWindow;
	private OverlappedPresenter? _presenter;

	private double _minClientWidth;
	private double _minClientHeight;
	private double _fullLayoutWidth = MINIMUM_DEFAULT_WIDTH;
	private double _fullLayoutHeight = MINIMUM_DEFAULT_HEIGHT;
	private bool _sizeInitialized;
	private DateTime _lastLocationSaveTime = DateTime.MinValue;
	private bool _locationRestored;
	private bool _isLocationFixed;
	private bool _isAlwaysOnTop;
	private bool _isCompactMode;

	private bool _isVisibleForRefresh;
   private DispatcherQueueTimer? _visibilityTimer;
	private bool _sessionLocked;

	private ForecastWindow? _forecastWindow;
	private ForecastViewModel? _forecastViewModel;

	public bool IsLocationFixed
		{
		get => _isLocationFixed;
		private set
			{
			if (_isLocationFixed == value)
				{
				return;
				}

			_isLocationFixed = value;
			UpdateSettings (s => s.IsLocationFixed = value);
			}
		}

	public bool IsAlwaysOnTop
		{
		get => _isAlwaysOnTop;
		private set
			{
			if (_isAlwaysOnTop == value)
				{
				return;
				}

			_isAlwaysOnTop = value;
			ApplyAlwaysOnTop ();
			UpdateSettings (s => s.IsAlwaysOnTop = value);
			}
		}

	public bool IsCompactMode
		{
		get => _isCompactMode;
		private set
			{
			if (_isCompactMode == value)
				{
				return;
				}

			PersistWindowLocation (force: true, modeOverride: _isCompactMode);
			_isCompactMode = value;
			ViewModel?.SetCompactMode (value);
			UpdateSettings (s => s.IsCompactMode = value);

			void ApplySize ()
				{
				RestoreWindowLocation (modeOverride: value, allowRepeat: true);

				var hwndRaw = WindowNative.GetWindowHandle (this);
				if (hwndRaw != IntPtr.Zero)
					{
					ApplyCornerPreference (new HWND (hwndRaw));
					}

				if (value)
					{
					ApplyCompactWindowSize ();
					}
				else
					{
					_minClientWidth = Math.Max (_fullLayoutWidth, MINIMUM_DEFAULT_WIDTH);
					_minClientHeight = Math.Max (_fullLayoutHeight, MINIMUM_DEFAULT_HEIGHT);
					UpdateRootMinSize ();
					ResizeWindow (_minClientWidth, _minClientHeight);
					EnsureContentFits ();
					}
				}

			if (!DispatcherQueue.TryEnqueue (ApplySize))
				{
				ApplySize ();
				}
			}
		}

	public MainWindow ()
		{
		InitializeComponent ();
		Title = "SimpleWeather Widget";

		InitializeAppWindow ();
		ConfigureForWidgetChrome ();
		RemoveWindowBorderStyles ();

		ViewModel = new WeatherWidgetViewModel (_settingsStore);
		_isLocationFixed = _settingsStore.CurrentSettings.IsLocationFixed;
		_isAlwaysOnTop = _settingsStore.CurrentSettings.IsAlwaysOnTop;
		_isCompactMode = _settingsStore.CurrentSettings.IsCompactMode;
		ViewModel.SetCompactMode (_isCompactMode);

		AttachDataContext ();
		Closed += OnClosed;
		SizeChanged += OnWindowSizeChanged;

		CompositionTarget.Rendering += OnCompositionRendering;
		InitializeVisibilityTimer ();

		RestoreWindowLocation ();
		ApplyAlwaysOnTop ();

		SystemEvents.SessionSwitch += OnSessionSwitch;
		}

	private void RemoveWindowBorderStyles ()
		{
		var hwndRaw = WindowNative.GetWindowHandle (this);
		if (hwndRaw == IntPtr.Zero)
			{
			return;
			}

		var hwnd = new HWND (hwndRaw);

		var style = (uint)PInvoke.GetWindowLong (hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
		style &= ~((uint)WINDOW_STYLE.WS_CAPTION | (uint)WINDOW_STYLE.WS_THICKFRAME | (uint)WINDOW_STYLE.WS_BORDER);
		_ = PInvoke.SetWindowLong (hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)style);

		_ = PInvoke.SetWindowPos (
			hwnd,
			HWND.Null,
			0,
			0,
			0,
			0,
			SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
			SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
			SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
			SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);

		ApplyCornerPreference (hwnd);
		}

	private void ApplyCornerPreference (HWND hwnd)
		{
		try
			{
            // Widget UI is rounded in both modes; keep the window corners rounded too.
            DWM_WINDOW_CORNER_PREFERENCE pref = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;

			Span<byte> buf = stackalloc byte[sizeof (DWM_WINDOW_CORNER_PREFERENCE)];
			MemoryMarshal.Write (buf, in pref);
			_ = PInvoke.DwmSetWindowAttribute (hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, buf);
			}
		catch
			{
			}
		}

	private void InitializeAppWindow ()
		{
		try
			{
			var hwnd = WindowNative.GetWindowHandle (this);
			if (hwnd == IntPtr.Zero)
				{
				return;
				}

         WindowId windowId = Win32Interop.GetWindowIdFromWindow (hwnd);
			_appWindow = AppWindow.GetFromWindowId (windowId);
			_presenter = _appWindow?.Presenter as OverlappedPresenter;
			}
		catch
			{
			_appWindow = null;
			_presenter = null;
			}
		}

	private void ConfigureForWidgetChrome ()
		{
		if (_appWindow?.TitleBar is AppWindowTitleBar titleBar)
			{
			titleBar.ExtendsContentIntoTitleBar = true;
			titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
			titleBar.BackgroundColor = Colors.Transparent;
			titleBar.InactiveBackgroundColor = Colors.Transparent;
			titleBar.ButtonBackgroundColor = Colors.Transparent;
			titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		 titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
			titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
			titleBar.ButtonForegroundColor = Colors.Transparent;
			titleBar.ButtonInactiveForegroundColor = Colors.Transparent;
			}

		if (_presenter is not null)
			{
			_presenter.SetBorderAndTitleBar (false, false);
			_presenter.IsResizable = false;
			_presenter.IsMaximizable = false;
			_presenter.IsMinimizable = false;
			}
		}

	private void InitializeVisibilityTimer ()
		{
		_visibilityTimer = DispatcherQueue.CreateTimer ();
		_visibilityTimer.IsRepeating = true;
		_visibilityTimer.Interval = TimeSpan.FromMilliseconds (250);
		_visibilityTimer.Tick += (_, __) => EvaluateVisibilityForRefresh (triggerImmediateRefresh: false);
		_visibilityTimer.Start ();

		// First evaluation will likely be "not visible" until first render.
		EvaluateVisibilityForRefresh (triggerImmediateRefresh: false);
		}

	private void OnSessionSwitch (object? sender, SessionSwitchEventArgs e)
		{
		switch (e.Reason)
			{
			case SessionSwitchReason.SessionLock:
				_sessionLocked = true;
				if (_isVisibleForRefresh)
					{
					_isVisibleForRefresh = false;
					ViewModel.OnVisibilityChanged (false);
#if DEBUG
					Debug.WriteLine ($"[{DebugTs}] [Widget][Session] Lock -> Visible -> Hidden");
#endif
					}

				break;
			case SessionSwitchReason.SessionUnlock:
				_sessionLocked = false;
#if DEBUG
				Debug.WriteLine ($"[{DebugTs}] [Widget][Session] Unlock -> re-evaluate visibility");
#endif
				EvaluateVisibilityForRefresh (triggerImmediateRefresh: false);
				break;
			}
		}

	private void OnCompositionRendering (object? sender, object e)
		{
		}

	private void EvaluateVisibilityForRefresh (bool triggerImmediateRefresh)
		{
		_ = triggerImmediateRefresh;

		if (_sessionLocked)
			{
			if (_isVisibleForRefresh)
				{
				_isVisibleForRefresh = false;
				ViewModel.OnVisibilityChanged (false);
#if DEBUG
				Debug.WriteLine ($"[{DebugTs}] [Widget][Visibility] Visible -> Hidden | SessionLocked=true");
#endif
				}

			return;
			}

		var xamlVisible = Content is FrameworkElement fe && fe.Visibility == Visibility.Visible;
		var visibleNow = xamlVisible;
		if (_isVisibleForRefresh == visibleNow)
			{
			return;
			}

		var previous = _isVisibleForRefresh;
		_isVisibleForRefresh = visibleNow;
		ViewModel.OnVisibilityChanged (_isVisibleForRefresh);

#if DEBUG
		Debug.WriteLine ($"[{DebugTs}] [Widget][Visibility] {(previous ? "Visible" : "Hidden")} -> {(visibleNow ? "Visible" : "Hidden")} | XamlVisible={xamlVisible}");
#endif
		}

	private void AttachDataContext ()
		{
		if (Content is FrameworkElement root)
			{
			root.DataContext = ViewModel;
			root.Loaded += OnRootLoaded;
			}
		}

	private async void OnRootLoaded (object sender, RoutedEventArgs e)
		{
		if (sender is FrameworkElement root)
			{
			root.Loaded -= OnRootLoaded;
			EnsureDefaultSize (root);
			}

		AdjustWindowSizeForCurrentMode ();
		await ViewModel.InitializeAsync ();
		AdjustWindowSizeForCurrentMode ();

		// After initialization the first frame should have rendered; re-evaluate visibility.
		EvaluateVisibilityForRefresh (triggerImmediateRefresh: false);
		}

	private void EnsureDefaultSize (FrameworkElement root)
		{
		if (_sizeInitialized)
			{
			return;
			}

		_sizeInitialized = true;
		root.Measure (new Size (double.PositiveInfinity, double.PositiveInfinity));
		Size desired = root.DesiredSize;
		_minClientWidth = Math.Max (MINIMUM_DEFAULT_WIDTH, Math.Ceiling (desired.Width));
		_minClientHeight = Math.Max (MINIMUM_DEFAULT_HEIGHT, Math.Ceiling (desired.Height));
		_fullLayoutWidth = _minClientWidth;
		_fullLayoutHeight = _minClientHeight;

		UpdateRootMinSize (root);
		ResizeWindow (_minClientWidth, _minClientHeight);
		}

	private void EnsureContentFits ()
		{
		if (Content is not FrameworkElement root)
			{
			return;
			}

		root.UpdateLayout ();
		root.Measure (new Size (double.PositiveInfinity, double.PositiveInfinity));
		Size desired = root.DesiredSize;
		var desiredWidth = Math.Ceiling (desired.Width);
		var desiredHeight = Math.Ceiling (desired.Height);
		var updated = false;

		if (desiredWidth > _minClientWidth)
			{
			_minClientWidth = desiredWidth;
			updated = true;
			}

		if (desiredHeight > _minClientHeight)
			{
			_minClientHeight = desiredHeight;
			updated = true;
			}

		if (updated)
			{
			UpdateRootMinSize (root);
			ResizeWindow (_minClientWidth, _minClientHeight);
			_fullLayoutWidth = _minClientWidth;
			_fullLayoutHeight = _minClientHeight;
			}
		}

	private void OnClosed (object sender, WindowEventArgs args)
		{
		SystemEvents.SessionSwitch -= OnSessionSwitch;

		_visibilityTimer?.Stop ();
		_visibilityTimer = null;
		CompositionTarget.Rendering -= OnCompositionRendering;

		try
			{
			_forecastWindow?.Close ();
			}
		catch
			{
			}

		_forecastWindow = null;
		_forecastViewModel?.Dispose ();
		_forecastViewModel = null;

		PersistWindowLocation (force: true);
		ViewModel.Dispose ();
		}

	private async Task RefreshWidgetAsync ()
		{
		await ViewModel.RefreshAsync ();
		AdjustWindowSizeForCurrentMode ();
		}

	private async Task ShowSettingsDialogAsync ()
		{
		var settingsWindow = new WidgetSettingsWindow (_settingsStore.CurrentSettings);
		WidgetSettings? updated = await settingsWindow.ShowAsync ();
		if (updated is null)
			{
			return;
			}

		updated.WindowLeft = _settingsStore.CurrentSettings.WindowLeft;
		updated.WindowTop = _settingsStore.CurrentSettings.WindowTop;
		updated.CompactWindowLeft = _settingsStore.CurrentSettings.CompactWindowLeft;
		updated.CompactWindowTop = _settingsStore.CurrentSettings.CompactWindowTop;
		updated.IsLocationFixed = IsLocationFixed;
		updated.IsAlwaysOnTop = IsAlwaysOnTop;
		ViewModel.ApplySettings (updated);
		IsCompactMode = updated.IsCompactMode;
		await ViewModel.InitializeAsync ();
		AdjustWindowSizeForCurrentMode ();
		}

	private void UpdateRootMinSize (FrameworkElement? root = null)
		{
		if (root is null && Content is FrameworkElement current)
			{
			root = current;
			}

		if (root is null)
			{
			return;
			}

		root.MinWidth = _minClientWidth;
		root.MinHeight = _minClientHeight;
		}

	private void OnWindowSizeChanged (object sender, WindowSizeChangedEventArgs args)
		{
		if (!_sizeInitialized)
			{
			return;
			}

		var width = args.Size.Width;
		var height = args.Size.Height;

		if (IsCompactMode)
			{
			const double tolerance = 0.5;
			if (Math.Abs (width - _minClientWidth) > tolerance || Math.Abs (height - _minClientHeight) > tolerance)
				{
				ResizeWindow (_minClientWidth, _minClientHeight);
				}

			return;
			}

		if (width > _minClientWidth)
			{
			_minClientWidth = width;
			}

		if (height > _minClientHeight)
			{
			_minClientHeight = height;
			}

		if (width + 0.5 < _minClientWidth || height + 0.5 < _minClientHeight)
			{
			ResizeWindow (_minClientWidth, _minClientHeight);
			}
		}

	private async void RefreshMenuItem_OnClick (object sender, RoutedEventArgs e) => await RefreshWidgetAsync ();
	private async void SettingsMenuItem_OnClick (object sender, RoutedEventArgs e) => await ShowSettingsDialogAsync ();

	private void MenuFlyout_OnOpening (object sender, object e)
		{
		if (sender is not MenuFlyout flyout)
			{
			return;
			}

		foreach (MenuFlyoutItemBase? item in flyout.Items)
			{
			if (item is not ToggleMenuFlyoutItem toggle || toggle.Tag is null)
				{
				continue;
				}

			switch (toggle.Tag)
				{
				case "FixLocation":
					toggle.IsChecked = IsLocationFixed;
					break;
				case "AlwaysOnTop":
					toggle.IsChecked = IsAlwaysOnTop;
					break;
				case "CompactMode":
					toggle.IsChecked = IsCompactMode;
					break;
				}
			}
		}

	private void CloseMenuItem_OnClick (object sender, RoutedEventArgs e) => Close ();

	private void AlwaysOnTopMenuItem_OnClick (object sender, RoutedEventArgs e)
		{
		if (sender is ToggleMenuFlyoutItem item)
			{
			IsAlwaysOnTop = item.IsChecked;
			}
		}

	private void CompactModeMenuItem_OnClick (object sender, RoutedEventArgs e)
		{
		if (sender is ToggleMenuFlyoutItem item)
			{
			IsCompactMode = item.IsChecked;
			}
		}

	private void FixLocationMenuItem_OnClick (object sender, RoutedEventArgs e)
		{
		if (sender is ToggleMenuFlyoutItem item)
			{
			IsLocationFixed = item.IsChecked;
			}
		}

	private async void Location_Tapped (object sender, TappedRoutedEventArgs e)
		{
		e.Handled = true;

		_forecastViewModel ??= new ForecastViewModel (_settingsStore);
		if (_forecastWindow is null)
			{
			_forecastWindow = new ForecastWindow (_forecastViewModel);
			_forecastWindow.Closed += (_, __) => _forecastWindow = null;
			}

		await _forecastViewModel.EnsureInitializedAndRefreshedAsync ();
		_forecastWindow.Activate ();
		}

	private void Card_OnPointerPressed (object sender, PointerRoutedEventArgs e)
		{
		if (IsLocationFixed || !e.GetCurrentPoint (Card).Properties.IsLeftButtonPressed)
			{
			return;
			}

		// AppWindow does not expose a direct "start dragging" API; rely on normal window move
		// behavior via title bar extension.
		}

	private void RestoreWindowLocation () => RestoreWindowLocation (modeOverride: null, allowRepeat: false);

	private void RestoreWindowLocation (bool? modeOverride, bool allowRepeat)
		{
		if (_appWindow is null)
			{
			return;
			}

		if (!allowRepeat && _locationRestored)
			{
			return;
			}

		WidgetSettings settings = _settingsStore.CurrentSettings;
		var useCompact = modeOverride ?? IsCompactMode;
		var left = useCompact ? settings.CompactWindowLeft : settings.WindowLeft;
		var top = useCompact ? settings.CompactWindowTop : settings.WindowTop;

		if (!left.HasValue || !top.HasValue)
			{
			var fallbackLeft = useCompact ? settings.WindowLeft : settings.CompactWindowLeft;
			var fallbackTop = useCompact ? settings.WindowTop : settings.CompactWindowTop;
			left = fallbackLeft;
			top = fallbackTop;
			}

		if (!left.HasValue || !top.HasValue)
			{
			if (!allowRepeat)
				{
				_locationRestored = true;
				}

			return;
			}

		_appWindow.Move (new PointInt32 ((int)Math.Round (left.Value), (int)Math.Round (top.Value)));

		if (!allowRepeat)
			{
			_locationRestored = true;
			}
		}

	private void PersistWindowLocation (bool force = false) => PersistWindowLocation (force, modeOverride: null);

	private void PersistWindowLocation (bool force, bool? modeOverride)
		{
		if (_appWindow is null)
			{
			return;
			}

		if (!force && (DateTime.UtcNow - _lastLocationSaveTime) < TimeSpan.FromMilliseconds (500))
			{
			return;
			}

        PointInt32 pos = _appWindow.Position;

		WidgetSettings settings = _settingsStore.CurrentSettings;
		var left = (double)pos.X;
		var top = (double)pos.Y;
		var useCompact = modeOverride ?? IsCompactMode;
		var existingLeft = useCompact ? settings.CompactWindowLeft : settings.WindowLeft;
		var existingTop = useCompact ? settings.CompactWindowTop : settings.WindowTop;

		if (existingLeft.HasValue && existingTop.HasValue)
			{
			if (Math.Abs (existingLeft.Value - left) < 1 && Math.Abs (existingTop.Value - top) < 1)
				{
				return;
				}
			}

		if (useCompact)
			{
			settings.CompactWindowLeft = left;
			settings.CompactWindowTop = top;
			}
		else
			{
			settings.WindowLeft = left;
			settings.WindowTop = top;
			}

		_settingsStore.Save (settings);
		_lastLocationSaveTime = DateTime.UtcNow;
		}

	private void ApplyAlwaysOnTop ()
		{
		if (_presenter is null)
			{
			return;
			}

		_presenter.IsAlwaysOnTop = IsAlwaysOnTop;
		}

	private void ResizeWindow (double width, double height)
		{
		if (_appWindow is null || width <= 0 || height <= 0)
			{
			return;
			}

		_appWindow.Resize (new SizeInt32 ((int)Math.Ceiling (width), (int)Math.Ceiling (height)));
		}

	private void ApplyCompactWindowSize ()
		{
		_minClientWidth = COMPACT_CLIENT_WIDTH;
		_minClientHeight = COMPACT_CLIENT_HEIGHT;
		UpdateRootMinSize ();
		ResizeWindow (_minClientWidth, _minClientHeight);
		}

	private void AdjustWindowSizeForCurrentMode ()
		{
		if (IsCompactMode)
			{
			ApplyCompactWindowSize ();
			}
		else
			{
			_minClientWidth = Math.Max (_fullLayoutWidth, MINIMUM_DEFAULT_WIDTH);
			_minClientHeight = Math.Max (_fullLayoutHeight, MINIMUM_DEFAULT_HEIGHT);
			UpdateRootMinSize ();
			ResizeWindow (_minClientWidth, _minClientHeight);
			EnsureContentFits ();
			}
		}

	private void UpdateSettings (Action<WidgetSettings> updateAction)
		{
		WidgetSettings snapshot = _settingsStore.CurrentSettings;
		updateAction (snapshot);
		_settingsStore.Save (snapshot);
		}
	}
