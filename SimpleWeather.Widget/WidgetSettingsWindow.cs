using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using SimpleWeather.Widget.Services;

using Windows.Graphics;

using WinRT.Interop;

namespace SimpleWeather.Widget;

public sealed class WidgetSettingsWindow : Window
	{
	private const int DEFAULT_WIDTH = 420;
	private const int DEFAULT_HEIGHT = 660;
	private const int MIN_WINDOW_WIDTH = 360;
	private const int MIN_WINDOW_HEIGHT = 620;

	private readonly TaskCompletionSource<WidgetSettings?> _tcs = new ();
	private readonly AppWindow? _appWindow;

	public WidgetSettingsWindow (WidgetSettings settings)
		{
		Title = "SimpleWeather Widget Settings";

		_appWindow = InitializeAppWindow ();
		if (_appWindow != null)
			{
			_appWindow.Title = Title;
			_appWindow.Resize (new SizeInt32 (DEFAULT_WIDTH, DEFAULT_HEIGHT));
			}

		var view = new WidgetSettingsDialog (settings)
			{
			MinWidth = MIN_WINDOW_WIDTH,
			MinHeight = MIN_WINDOW_HEIGHT
			};
		view.SaveRequested += OnSaveRequested;
		view.CancelRequested += OnCancelRequested;
		Content = view;

		SizeChanged += EnforceMinWindowSize;
		Closed += (_, _) => _tcs.TrySetResult (null);
		}

	public Task<WidgetSettings?> ShowAsync ()
		{
		Activate ();
		return _tcs.Task;
		}

	private void OnSaveRequested (object? sender, WidgetSettings updated)
		{
		_ = _tcs.TrySetResult (updated);
		Close ();
		}

	private void OnCancelRequested (object? sender, object e)
		{
		_ = _tcs.TrySetResult (null);
		Close ();
		}

	private AppWindow? InitializeAppWindow ()
		{
		var hWnd = WindowNative.GetWindowHandle (this);
		if (hWnd == IntPtr.Zero)
			{
			return null;
			}

		WindowId windowId = Win32Interop.GetWindowIdFromWindow (hWnd);
		return AppWindow.GetFromWindowId (windowId);
		}

	private void EnforceMinWindowSize (object sender, WindowSizeChangedEventArgs args)
		{
		if (_appWindow is null)
			{
			return;
			}

		var desiredWidth = Math.Max ((int)Math.Round (args.Size.Width), MIN_WINDOW_WIDTH);
		var desiredHeight = Math.Max ((int)Math.Round (args.Size.Height), MIN_WINDOW_HEIGHT);

		if (desiredWidth != (int)Math.Round (args.Size.Width) || desiredHeight != (int)Math.Round (args.Size.Height))
			{
			_appWindow.Resize (new SizeInt32 (desiredWidth, desiredHeight));
			}
		}
	}
