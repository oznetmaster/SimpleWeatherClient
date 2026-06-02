using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using SimpleWeather.Widget.Services;
using SimpleWeather.Widget.ViewModels;

using Windows.Graphics;

using WinRT.Interop;

namespace SimpleWeather.Widget;

public sealed partial class ForecastWindow : Window
	{
	public ForecastViewModel ViewModel { get; }

	private readonly AppWindow? _appWindow;

	private static string DeployedIconPath => Path.Combine (AppContext.BaseDirectory, "Resources", "OpenWeather-Master-Logo-RGB.ico");

	public ForecastWindow (ForecastViewModel viewModel)
		{
		ViewModel = viewModel;

		InitializeComponent ();

		if (Content is FrameworkElement fe)
			{
			fe.DataContext = ViewModel;
			}

		_appWindow = InitializeAppWindow ();
		if (_appWindow?.TitleBar is AppWindowTitleBar titleBar)
			{
			titleBar.ExtendsContentIntoTitleBar = false;
			titleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
			}

		if (_appWindow?.Presenter is OverlappedPresenter presenter)
			{
			presenter.SetBorderAndTitleBar (true, true);
			}

		ViewModel.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName is nameof (ForecastViewModel.DisplayName) or nameof (ForecastViewModel.ForecastDaysCount))
					{
					UpdateTitle ();
					}
			};

		RestoreWindowPlacement ();
		UpdateTitle ();
		TryApplyAppWindowIconsFromDeployedFile ();

		Closed += (_, __) =>
			{
				PersistWindowPlacement ();
				ViewModel.SetVisible (false);
			};

		Activated += (_, __) =>
			{
				ViewModel.SetVisible (true);
				TryApplyAppWindowIconsFromDeployedFile ();
			};
		}

	private void TryApplyAppWindowIconsFromDeployedFile ()
		{
		try
			{
			if (_appWindow is null)
				{
				return;
				}

			var path = DeployedIconPath;
			if (!File.Exists (path))
				{
				return;
				}

			_appWindow.SetTaskbarIcon (path);
			_appWindow.SetTitleBarIcon (path);
			}
		catch
			{
			}
		}

	private AppWindow? InitializeAppWindow ()
		{
		try
			{
			var hwnd = WindowNative.GetWindowHandle (this);
			if (hwnd == IntPtr.Zero)
				{
				return null;
				}

			WindowId windowId = Win32Interop.GetWindowIdFromWindow (hwnd);
			return AppWindow.GetFromWindowId (windowId);
			}
		catch
			{
			return null;
			}
		}

	private void UpdateTitle ()
		{
		var location = string.IsNullOrWhiteSpace (ViewModel.DisplayName) ? "Forecast" : ViewModel.DisplayName;
		var days = ViewModel.ForecastDaysCount <= 0 ? 0 : ViewModel.ForecastDaysCount;
		Title = days > 0 ? $"{location} - {days} Day Forecast" : $"{location} - Forecast";

		_appWindow?.Title = Title ?? string.Empty;
		}

	private void RestoreWindowPlacement ()
		{
		try
			{
			if (_appWindow is null)
				{
				return;
				}

         WidgetSettings settings = ViewModel.SettingsStore.CurrentSettings;
			if (settings.ForecastWindowLeft is null || settings.ForecastWindowTop is null || settings.ForecastWindowWidth is null || settings.ForecastWindowHeight is null)
				{
				return;
				}

			_appWindow.MoveAndResize (new RectInt32 (
				(int)Math.Round (settings.ForecastWindowLeft.Value),
				(int)Math.Round (settings.ForecastWindowTop.Value),
				Math.Max (1, (int)Math.Round (settings.ForecastWindowWidth.Value)),
				Math.Max (1, (int)Math.Round (settings.ForecastWindowHeight.Value))));
			}
		catch
			{
			}
		}

	private void PersistWindowPlacement ()
		{
		try
			{
			if (_appWindow is null)
				{
				return;
				}

            PointInt32 pos = _appWindow.Position;
            SizeInt32 size = _appWindow.Size;

			var left = (double)pos.X;
			var top = (double)pos.Y;
			var width = (double)Math.Max (0, size.Width);
			var height = (double)Math.Max (0, size.Height);

            WidgetSettingsStore store = ViewModel.SettingsStore;
            WidgetSettings snapshot = store.CurrentSettings;
			snapshot.ForecastWindowLeft = left;
			snapshot.ForecastWindowTop = top;
			snapshot.ForecastWindowWidth = width;
			snapshot.ForecastWindowHeight = height;
			store.Save (snapshot);
			}
		catch
			{
			}
		}
	}
