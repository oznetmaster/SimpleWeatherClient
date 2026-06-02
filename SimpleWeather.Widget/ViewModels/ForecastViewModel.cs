using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.UI.Dispatching;

using SimpleWeather.Widget.Services;

namespace SimpleWeather.Widget.ViewModels;

public sealed partial class ForecastViewModel : INotifyPropertyChanged, IDisposable
	{
	private const int FORECAST_MAX_DAYS_PAID = 7;
	private const int FORECAST_MAX_DAYS_FREE = 5;
	private static readonly TimeSpan _minRefreshInterval = TimeSpan.FromMinutes (30);
	private readonly DispatcherQueue _dispatcherQueue;
	private readonly DispatcherQueueTimer _timer;
	private WeatherController? _weatherController;
	private bool _isVisible;
	private bool _refreshPending;
	private string? _lastApiKey;

	public event PropertyChangedEventHandler? PropertyChanged;

	public ForecastViewModel (WidgetSettingsStore settingsStore)
		{
		SettingsStore = settingsStore;
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread ();
		_timer = _dispatcherQueue.CreateTimer ();
		_timer.Interval = _minRefreshInterval;
		_timer.IsRepeating = true;
		_timer.Tick += OnTimerTick;
		}

	public string Title { get; private set; } = "Forecast";
	public string StatusText { get; private set; } = "";
	public IReadOnlyList<ForecastDayItem> Days { get; private set; } = [];

	public DateTime LastRefreshTimeUtc { get; private set; } = DateTime.MinValue;

	public string DisplayName { get; private set; } = "";
	public int ForecastDaysCount { get; private set; } = FORECAST_MAX_DAYS_FREE;

	internal WidgetSettingsStore SettingsStore { get; }

	public void SetVisible (bool isVisible)
		{
		if (_isVisible == isVisible)
			{
			return;
			}

		_isVisible = isVisible;

		if (_isVisible)
			{
			_timer.Interval = _minRefreshInterval;
			_timer.Start ();

			if (_refreshPending || ShouldRefreshNow ())
				{
				_refreshPending = false;
				_ = _dispatcherQueue.TryEnqueue (async () => await RefreshAsync ());
				}
			}
		else
			{
			// Keep timer running, but only refresh when visible.
			}
		}

	public bool ShouldRefreshNow () => (DateTime.UtcNow - LastRefreshTimeUtc) >= _minRefreshInterval;

	public async Task EnsureInitializedAndRefreshedAsync ()
		{
		var apiKey = SettingsStore.CurrentSettings.ApiKey;
		if (!string.Equals (_lastApiKey, apiKey, StringComparison.Ordinal))
			{
			_weatherController?.Dispose ();
			_weatherController = string.IsNullOrWhiteSpace (apiKey) ? null : new WeatherController (apiKey);
			_lastApiKey = apiKey;
			}

		if (_weatherController is null)
			{
			StatusText = "Add your OpenWeather API key in Settings.";
			OnPropertyChanged (nameof (StatusText));
			Days = [];
			OnPropertyChanged (nameof (Days));
			return;
			}

		if (ShouldRefreshNow () || Days.Count == 0)
			{
			await RefreshAsync ();
			}
		}

	private async void OnTimerTick (DispatcherQueueTimer sender, object args)
		{
		try
			{
			if (!_isVisible)
				{
				_refreshPending = true;
				return;
				}

			await RefreshAsync ();
			}
		catch (Exception ex)
			{
			Debug.WriteLine ($"[Forecast][TimerTick] {ex}");
			}
		}

	public async Task RefreshAsync ()
		{
		if (_weatherController is null)
			{
			return;
			}

		try
			{
			WidgetSettings settings = SettingsStore.CurrentSettings;
			DisplayName = BuildCityStateLabel (settings.City, settings.State);
			OnPropertyChanged (nameof (DisplayName));

			StatusText = "Updating...";
			OnPropertyChanged (nameof (StatusText));

			WeatherForecast forecast = await _weatherController.GetWeatherForecastAsync (settings.City, settings.State, settings.Country, settings.Units, CancellationToken.None).ConfigureAwait (false);

			ForecastDaysCount = forecast.Current is null ? FORECAST_MAX_DAYS_FREE : FORECAST_MAX_DAYS_PAID;
			OnPropertyChanged (nameof (ForecastDaysCount));

			ForecastDayItem[] dayItems = [.. forecast.Daily
				.Take (ForecastDaysCount)
				.Select (d => ForecastDayItem.FromDaily (d, settings.Units))];

			void Apply ()
				{
				Days = dayItems;
				OnPropertyChanged (nameof (Days));
				LastRefreshTimeUtc = DateTime.UtcNow;
				StatusText = $"Updated {DateTime.Now:t}";
				OnPropertyChanged (nameof (StatusText));
				}

			if (_dispatcherQueue.HasThreadAccess)
				{
				Apply ();
				}
			else
				{
				_ = _dispatcherQueue.TryEnqueue (Apply);
				}
			}
		catch (Exception ex)
			{
			void ApplyError ()
				{
				StatusText = ex is UnauthorizedAccessException
					? "Enter a valid OpenWeather API key in Settings."
					: "Unable to load forecast.";
				OnPropertyChanged (nameof (StatusText));
				}

			if (_dispatcherQueue.HasThreadAccess)
				{
				ApplyError ();
				}
			else
				{
				_ = _dispatcherQueue.TryEnqueue (ApplyError);
				}

			Debug.WriteLine ($"[Forecast][Refresh] {ex}");
			}
		}

	private static string BuildCityStateLabel (string? city, string? state)
		{
		var parts = new List<string> (capacity: 2);
		if (!string.IsNullOrWhiteSpace (city))
			{
			parts.Add (city.Trim ());
			}

		if (!string.IsNullOrWhiteSpace (state))
			{
			parts.Add (state.Trim ());
			}

		return string.Join (", ", parts);
		}

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

	public sealed record ForecastDayItem (string DayLabel, string Description, string HighLow, string? IconUrl)
		{
		public static ForecastDayItem FromDaily (Daily d, string units)
			{
			DateTime dt = d.DT ?? DateTime.Now;
			var isToday = dt.Date == DateTime.Now.Date;
			var dayLabel = isToday ? "Today" : dt.ToString ("ddd");

			var baseDesc = d.Weather?.Description ?? d.Weather?.Main ?? string.Empty;
			var precip = BuildPrecipText (d, units);
			var desc = string.IsNullOrWhiteSpace (precip) ? baseDesc : string.IsNullOrWhiteSpace (baseDesc) ? precip : $"{baseDesc} ({precip})";

			var hi = FormatTemp (d.Temperature?.Max, units);
			var lo = FormatTemp (d.Temperature?.Min, units);
			var icon = d.Weather?.Icon;
			if (!string.IsNullOrWhiteSpace (icon) && icon.Length == 3)
				{
				icon = icon[..2] + "d";
				}

			var iconUrl = string.IsNullOrWhiteSpace (icon) ? null : $"https://openweathermap.org/img/wn/{icon}.png";
			return new ForecastDayItem (dayLabel, desc, $"{hi} / {lo}", iconUrl);
			}

		private static string? BuildPrecipText (Daily d, string units)
			{
			try
				{
				// Probability (already 0-100)
				var popPct = d.PrecipitationProbability;
				var popText = popPct.HasValue && popPct.Value > 0 ? $"{Math.Round (Math.Clamp (popPct.Value, 0, 100)):0}%" : null;

				double? amountMm = null;
				string? kind = null;

				if (d.Snow.HasValue && d.Snow.Value > 0)
					{
					amountMm = d.Snow.Value;
					kind = "snow";
					}
				else if (d.Rain.HasValue && d.Rain.Value > 0)
					{
					amountMm = d.Rain.Value;
					kind = "rain";
					}

				string? amountText = null;
				if (amountMm.HasValue)
					{
					// OWM daily rain/snow is typically in mm; convert for imperial.
					if (string.Equals (units, "imperial", StringComparison.OrdinalIgnoreCase))
						{
						var inches = amountMm.Value / 25.4;
						amountText = $"{inches:0.#} in";
						}
					else
						{
						amountText = $"{amountMm.Value:0.#} mm";
						}
					}

				// Only show something if there's meaningful precip info.
				if (kind is null && string.IsNullOrWhiteSpace (popText))
					{
					return null;
					}

				if (kind is null)
					{
					return $"precip {popText}";
					}

				return !string.IsNullOrWhiteSpace (amountText) && !string.IsNullOrWhiteSpace (popText)
					 ? $"{kind} {popText}, {amountText}"
					 : !string.IsNullOrWhiteSpace (popText)
					  ? $"{kind} {popText}"
					  : !string.IsNullOrWhiteSpace (amountText) ? $"{kind} {amountText}" : null;
				}
			catch
				{
				return null;
				}
			}

		private static string FormatTemp (double? t, string units)
			{
			if (!t.HasValue)
				{
				return "?";
				}

			var suffix = units switch
				{
					"imperial" => "F",
					"standard" => "K",
					_ => "C"
					};

			var rounded = Math.Round (t.Value, 0, MidpointRounding.AwayFromZero);
			return $"{rounded:0}°{suffix}";
			}
		}
	}
