using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using SimpleWeather.Widget.Services;
using SimpleWeather.Widget.Utilities;

using Windows.UI;

namespace SimpleWeather.Widget.ViewModels;

public sealed partial class WeatherWidgetViewModel : INotifyPropertyChanged, IDisposable
	{
	private readonly WidgetSettingsStore _settingsStore;
	private readonly DispatcherQueueTimer _timer;
	private readonly DispatcherQueue _dispatcherQueue;
	private WeatherController? _weatherController;
	private string? _lastApiKey;
	private string? _lastIconCode;
	private bool _isVisible;
	private bool _refreshPending;

	public event PropertyChangedEventHandler? PropertyChanged;

	public WeatherWidgetViewModel (WidgetSettingsStore settingsStore)
		{
		_settingsStore = settingsStore;
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread ();
		WeatherBackgrounds.Initialize ();
		_timer = _dispatcherQueue.CreateTimer ();
		_timer.Tick += OnTimerTick;
		_timer.IsRepeating = true;
		ApplySettings (_settingsStore.CurrentSettings);
		}

	public void OnVisibilityChanged (bool isVisible)
		{
		if (_isVisible == isVisible)
			{
			return;
			}

		_isVisible = isVisible;

#if DEBUG
		Debug.WriteLine ($"[{DebugTs}] [Widget][AutoRefresh] Visibility changed: Visible={_isVisible}, Pending={_refreshPending}");
#endif

		if (_isVisible && _refreshPending)
			{
			_refreshPending = false;
#if DEBUG
			Debug.WriteLine ($"[{DebugTs}] [Widget][AutoRefresh] Pending refresh consumed -> refreshing now and restarting interval.");
#endif
			_ = _dispatcherQueue.TryEnqueue (async () =>
				{
					await RefreshAsync (triggeredByTimer: true);
					_timer.Interval = RefreshInterval;
					_timer.Start ();
				});
			}
		}

	public bool AutoRefreshEnabled
		{
		get;
		set
			{
			if (field == value)
				{
				return;
				}

			field = value;
			OnPropertyChanged ();

#if DEBUG
			Debug.WriteLine ($"[{DebugTs}] [Widget][AutoRefresh] Enabled={field}");
#endif

			if (!field)
				{
				_timer.Stop ();
				}
			}
		} = true;

	private async void OnTimerTick (DispatcherQueueTimer sender, object args)
		{
		if (!AutoRefreshEnabled)
			{
#if DEBUG
			Debug.WriteLine ($"[{DebugTs}] [Widget][TimerTick] Suppressed (auto-refresh disabled).");
#endif
			return;
			}

		try
			{
			if (!_isVisible)
				{
				if (!_refreshPending)
					{
					_refreshPending = true;
#if DEBUG
					Debug.WriteLine ($"[{DebugTs}] [Widget][TimerTick] Hidden -> marked refresh pending (no API call).");
#endif
					}

				return;
				}

			sender.Stop ();
			await RefreshAsync (true).ConfigureAwait (false);

#if DEBUG
			Debug.WriteLine ($"[{DebugTs}] [Widget][TimerTick] Refreshed -> restarting interval.");
#endif

			sender.Interval = RefreshInterval;
			sender.Start ();
			}
		catch (Exception ex)
			{
			Debug.WriteLine ($"[{DebugTs}] [Widget][TimerTick] {ex}");
			}
		}

	public string Location { get; private set; } = "—";
	public string Temperature { get; private set; } = "—";
	public string Condition { get; private set; } = "Waiting for data";
	public string HighLow { get; private set; } = string.Empty;
	public string Wind { get; private set; } = "—";
	public string Humidity { get; private set; } = "—";
	public string StatusMessage { get; private set; } = "Configure your widget.";
	public string UpdatedDisplay { get; private set; } = string.Empty;
	public string? IconUrl { get; private set; }
	public BitmapImage? CompactBackground { get; private set; }
	public SolidColorBrush CompactTemperatureForeground { get; } = new SolidColorBrush (Colors.White);
	public SolidColorBrush CompactLocationForeground { get; } = new SolidColorBrush (Colors.White);
	public string? CompactBackgroundImage { get; private set; }
	public string? DebugCompactBackground { get; private set; }
	public string CompactLocation { get; private set; } = "—";
	public string CompactTemperature { get; private set; } = "—";
	public Visibility FullLayoutVisibility => IsCompactMode ? Visibility.Collapsed : Visibility.Visible;
	public Visibility CompactLayoutVisibility => IsCompactMode ? Visibility.Visible : Visibility.Collapsed;

	public bool IsCompactMode
		{
		get;
		private set
			{
			if (field == value)
				{
				return;
				}

			field = value;
			OnPropertyChanged ();
			OnPropertyChanged (nameof (FullLayoutVisibility));
			OnPropertyChanged (nameof (CompactLayoutVisibility));
			}
		}

	public bool IsBusy
		{
		get;
		private set
			{
			if (field != value)
				{
				field = value;
				OnPropertyChanged ();
				}
			}
		}

	public DateTime LastRefreshTimeUtc { get; private set; } = DateTime.MinValue;

	public TimeSpan RefreshInterval
		{
		get
			{
			var minutes = Math.Clamp (_settingsStore.CurrentSettings.RefreshMinutes, 5, 240);
			return TimeSpan.FromMinutes (minutes);
			}
		}

	public bool ShouldRefreshNow ()
		{
		if (_weatherController is null)
			{
			return false;
			}

		var due = (DateTime.UtcNow - LastRefreshTimeUtc) >= RefreshInterval;
		return due;
		}

    public Task RefreshIfDueAsync (bool triggeredByTimer = false) =>
		ShouldRefreshNow () ? RefreshAsync (triggeredByTimer) : Task.CompletedTask;

    public async Task InitializeAsync ()
		{
		try
			{
#if DEBUG
			Debug.WriteLine ($"[{DebugTs}] [Widget][Initialize] Begin. Interval={RefreshInterval.TotalMinutes:0.##} min");
#endif
			var apiKey = ResolveApiKey (_settingsStore.CurrentSettings);
			if (string.IsNullOrWhiteSpace (apiKey))
				{
				_weatherController?.Dispose ();
				_weatherController = null;
				_lastApiKey = null;
				StatusMessage = "Add your OpenWeather API key in Settings.";
				OnPropertyChanged (nameof (StatusMessage));
				return;
				}

			if (!string.Equals (_lastApiKey, apiKey, StringComparison.Ordinal))
				{
				_weatherController?.Dispose ();
				_weatherController = new WeatherController (apiKey);
				_lastApiKey = apiKey;
				}

			await RefreshAsync ().ConfigureAwait (false);
			}
		catch (Exception ex)
			{
			StatusMessage = ex.Message;
			OnPropertyChanged (nameof (StatusMessage));
			Debug.WriteLine ($"[Widget][Initialize] {ex}");
			}
		}

	public async Task RefreshAsync (bool triggeredByTimer = false)
		{
		if (_weatherController is null)
			{
			return;
			}

		try
			{
			IsBusy = true;
			StatusMessage = triggeredByTimer ? "Auto updating…" : "Updating weather…";
			OnPropertyChanged (nameof (StatusMessage));

			WidgetSettings settings = _settingsStore.CurrentSettings;
			CurrentWeather weather = await _weatherController.GetCurrentWeatherAsync (settings.City, settings.State, settings.Country, settings.Units, CancellationToken.None).ConfigureAwait (false);
			UpdateFromResult (weather, settings);
			LastRefreshTimeUtc = DateTime.UtcNow;
			StatusMessage = $"Updated {DateTime.Now:t}";
			OnPropertyChanged (nameof (StatusMessage));
			}
		catch (Exception ex)
			{
			var message = ex switch
				{
					UnauthorizedAccessException => "Enter a valid OpenWeather API key in Settings.",
					HttpRequestException hre when hre.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => "Enter a valid OpenWeather API key in Settings.",
					_ => "Unable to update weather."
					};

			StatusMessage = message;
			OnPropertyChanged (nameof (StatusMessage));
#if DEBUG
			Debug.WriteLine ($"[Widget][Refresh] {ex}");
#endif
			}
		finally
			{
			IsBusy = false;
			}
		}

	public void ApplySettings (WidgetSettings settings)
		{
		_settingsStore.Save (settings);
		Location = BuildLocationLabel (settings);
		OnPropertyChanged (nameof (Location));
		CompactLocation = BuildCompactLocationLabel (settings.City, settings.Country);
		OnPropertyChanged (nameof (CompactLocation));
		SetCompactMode (settings.IsCompactMode);

		_timer.Interval = RefreshInterval;
		_timer.Start ();
#if DEBUG
		Debug.WriteLine ($"[{DebugTs}] [Widget][Timer] Started. Interval={_timer.Interval.TotalMinutes:0.##} min; Visible={_isVisible}; Pending={_refreshPending}");
#endif
		}

	private void UpdateFromResult (CurrentWeather weather, WidgetSettings settings)
		{
		// Keep full layout location consistent with user selection (city/state/country).
		// The API typically only returns city name.
		Location = BuildLocationLabel (settings);
		Temperature = weather.Main?.Temperature.HasValue == true
			 ? $"{weather.Main.Temperature.Value:0.#}°{ResolveUnitSuffix (settings.Units)}"
			 : "—";
		CompactTemperature = weather.Main?.Temperature.HasValue == true
			 ? $"{Math.Round (weather.Main.Temperature.Value):0}°"
			 : "—";

		// Keep compact label stable and user-friendly: prefer configured values (which may be full country name)
		// and avoid swapping to ISO country codes from the API.
		var compactCity = string.IsNullOrWhiteSpace (weather.City) ? settings.City : weather.City;
		var compactCountry = settings.Country;
		CompactLocation = BuildCompactLocationLabel (compactCity, compactCountry);

		Condition = string.IsNullOrWhiteSpace (weather.Weather?.Description) ? weather.Weather?.Main ?? "Unknown" : weather.Weather.Description;
		HighLow = $"Feels like {FormatTemperature (weather.Main?.FeelsLike, settings.Units)}";
		Wind = FormatWind (weather.Wind, settings.Units);
		Humidity = FormatPercentage (weather.Main?.Humidity);
		UpdatedDisplay = DateTime.Now.ToString ("ddd • h:mm tt");
		IconUrl = weather.Weather?.GetIconUrl (WeatherIconResolution.DoubleScale);
		var iconCode = weather.Weather?.Icon;
		_lastIconCode = iconCode;
		CompactBackground = WeatherBackgrounds.FromIcon (iconCode);
		Color fgColor = ResolveCompactForegroundColor (iconCode);
		DebugCompactBackground = $"id={weather.Weather?.ID}; icon={iconCode}; bg=bg_{(string.IsNullOrWhiteSpace (iconCode) ? "01d" : iconCode.Trim ())}.png";

		void ApplyUi ()
			{
			CompactTemperatureForeground.Color = fgColor;
			CompactLocationForeground.Color = fgColor;
			OnPropertyChanged (nameof (Location));
			OnPropertyChanged (nameof (Temperature));
			OnPropertyChanged (nameof (CompactTemperature));
			OnPropertyChanged (nameof (Condition));
			OnPropertyChanged (nameof (HighLow));
			OnPropertyChanged (nameof (Wind));
			OnPropertyChanged (nameof (Humidity));
			OnPropertyChanged (nameof (UpdatedDisplay));
			OnPropertyChanged (nameof (IconUrl));
			OnPropertyChanged (nameof (CompactLocation));
			OnPropertyChanged (nameof (CompactBackground));
			}

		if (_dispatcherQueue.HasThreadAccess)
			{
			ApplyUi ();
			}
		else
			{
            _ = _dispatcherQueue.TryEnqueue (ApplyUi);
			}

		Debug.WriteLine ($"[Widget] {DebugCompactBackground}");
		}

	private static Color ResolveCompactForegroundColor (string? iconCode)
		{
		var isNight = !string.IsNullOrWhiteSpace (iconCode) && iconCode.Trim ().EndsWith ("n", StringComparison.OrdinalIgnoreCase);
		return isNight ? Colors.White : Colors.Black;
		}

    private static string ResolveUnitSuffix (string units) => units switch
		{
			"imperial" => "F",
			"standard" => "K",
			_ => "C"
			};

	private static string BuildLocationLabel (WidgetSettings settings)
		{
		var parts = new List<string> { settings.City };
		if (!string.IsNullOrWhiteSpace (settings.State))
			{
			parts.Add (settings.State);
			}

		if (!string.IsNullOrWhiteSpace (settings.Country))
			{
			parts.Add (settings.Country);
			}

		return string.Join (", ", parts);
		}

	private static string BuildCompactLocationLabel (string? city, string? country)
		{
		var parts = new List<string> (capacity: 2);
		if (!string.IsNullOrWhiteSpace (city))
			{
			parts.Add (city.Trim ());
			}

		if (!string.IsNullOrWhiteSpace (country))
			{
			parts.Add (country.Trim ());
			}

		return string.Join (", ", parts);
		}

	private static string FormatTemperature (double? value, string units) =>
		 !value.HasValue ? "—" : $"{value:0.#}°{ResolveUnitSuffix (units)}";

	private static string FormatPercentage (double? value) => value.HasValue ? $"{value:0.#}%" : "—";

	private static string FormatWind (Wind? wind, string units)
		{
		if (wind?.Speed is null)
			{
			return "—";
			}

		var suffix = units == "imperial" ? "mph" : "m/s";
		var direction = string.IsNullOrWhiteSpace (wind.WindDirectionShort) ? string.Empty : $" ({wind.WindDirectionShort})";
		return $"{wind.Speed:0.#} {suffix}{direction}";
		}

    private static string? ResolveApiKey (WidgetSettings settings) =>
		string.IsNullOrWhiteSpace (settings.ApiKey) ? null : settings.ApiKey;

    private void OnPropertyChanged ([CallerMemberName] string? name = null)
		{
		void Raise () => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));

		if (_dispatcherQueue.HasThreadAccess)
			{
			Raise ();
			}
		else
			{
            _ = _dispatcherQueue.TryEnqueue (Raise);
			}
		}

	public void Dispose ()
		{
		_timer.Stop ();
		_timer.Tick -= OnTimerTick;
		_weatherController?.Dispose ();
		_weatherController = null;
		}

	public void SetCompactMode (bool isCompactMode)
		{
		IsCompactMode = isCompactMode;
		if (!string.IsNullOrWhiteSpace (_lastIconCode))
			{
			Color fgColor = ResolveCompactForegroundColor (_lastIconCode);
			CompactTemperatureForeground.Color = fgColor;
			CompactLocationForeground.Color = fgColor;
			}
		}

	private static string DebugTs => DateTime.Now.ToString ("HH:mm:ss.fff");
	}
