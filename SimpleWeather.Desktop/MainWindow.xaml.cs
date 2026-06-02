using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SimpleWeather.Desktop;

public partial class MainWindow : Window
    {
    private readonly DispatcherTimer _autoRefreshTimer = new () { Interval = TimeSpan.FromMinutes (5) };
    private WeatherRequest? _lastRequest;
    private bool _isFetching;
    private readonly WeatherController? _weatherController;
    private readonly ObservableCollection<string> _cityOptions = [];
    private readonly ObservableCollection<string> _stateOptions = [];
    private readonly ObservableCollection<string> _countryOptions = [];
    private readonly LocationSettingsStore _settingsStore = new ();
 
    private CancellationTokenSource _refreshCts = new ();

    public MainWindow ()
        {
        InitializeComponent ();
        UnitsComboBox.ItemsSource = new[] { "metric", "imperial", "standard" };
        UnitsComboBox.SelectedIndex = 0;

        var apiKey = ResolveApiKey ();
        if (string.IsNullOrWhiteSpace (apiKey))
            {
            StatusText.Text = "Set OpenWeather:ApiKey in App.config or create .local\\openweather-api-key.txt in the solution root.";
            RefreshButton.IsEnabled = false;
            return;
            }

        _weatherController = new WeatherController (apiKey);
        _autoRefreshTimer.Tick += AutoRefreshTimerOnTick;
        InitializeCachedSettings ();
        ApplySavedWindowPlacement ();
        }

    private static string? ResolveApiKey ()
        {
        var apiKey = ConfigurationManager.AppSettings["OpenWeather:ApiKey"];
        if (string.IsNullOrWhiteSpace (apiKey) || IsPlaceholderApiKey (apiKey))
            {
            apiKey = TryReadLocalApiKeyFile ();
            }

        return string.IsNullOrWhiteSpace (apiKey) ? null : apiKey;
        }

    private static bool IsPlaceholderApiKey (string apiKey) =>
        string.Equals (apiKey, "YOUR_OPENWEATHER_API_KEY_HERE", StringComparison.Ordinal) ||
        string.Equals (apiKey, "YOUR_API_KEY_HERE", StringComparison.Ordinal);

    private static string? TryReadLocalApiKeyFile ()
        {
        foreach (var localConfigPath in EnumerateLocalApiKeyFilePaths ())
            {
            try
                {
                if (!File.Exists (localConfigPath))
                    {
                    continue;
                    }

                var apiKey = File.ReadAllText (localConfigPath).Trim ();
                if (!string.IsNullOrWhiteSpace (apiKey) && !IsPlaceholderApiKey (apiKey))
                    {
                    return apiKey;
                    }
                }
            catch
                {
                }
            }

        return null;
        }

    private static IEnumerable<string> EnumerateLocalApiKeyFilePaths ()
        {
        yield return Path.GetFullPath (Path.Combine (AppContext.BaseDirectory, "..", "..", "..", ".local", "openweather-api-key.txt"));
        yield return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "SimpleWeather", "desktop-api-key.txt");
        }

    private async void RefreshButton_OnClick (object sender, RoutedEventArgs e)
        {
        if (_weatherController is null)
            {
            return;
            }

        var city = CityComboBox.Text?.Trim ();
        if (string.IsNullOrWhiteSpace (city))
            {
            StatusText.Text = "Enter at least a city name.";
            return;
            }
 
        var state = string.IsNullOrWhiteSpace (StateComboBox.Text) ? null : StateComboBox.Text.Trim ();
        var country = string.IsNullOrWhiteSpace (CountryComboBox.Text) ? null : CountryComboBox.Text.Trim ();
        var units = (UnitsComboBox.SelectedItem as string) ?? "metric";
        var locationLabel = BuildLocationLabel (city, state, country);

        var request = new WeatherRequest (city, state, country, units, locationLabel);
        await FetchWeatherAsync (request).ConfigureAwait (true);
        }

    private async void AutoRefreshTimerOnTick (object? sender, EventArgs e)
        {
        if (_lastRequest is null)
            {
            return;
            }

        WeatherRequest cachedRequest = _lastRequest;
        await FetchWeatherAsync (cachedRequest, triggeredByTimer: true).ConfigureAwait (true);
        }

    private async Task FetchWeatherAsync (WeatherRequest request, bool triggeredByTimer = false)
        {
        if (_weatherController is null)
            {
            return;
            }

        if (triggeredByTimer && _isFetching)
            {
            return;
            }

        try
            {
            _isFetching = true;
            var busyMessage = triggeredByTimer ? $"Auto-updating {DateTime.Now:t}" : "Fetching weather…";
            SetBusy (true, busyMessage);
            _refreshCts.Cancel ();
            _refreshCts.Dispose ();
            _refreshCts = new CancellationTokenSource ();

            CurrentWeather current = await _weatherController.GetCurrentWeatherAsync (request.City, request.State, request.Country, request.Units, _refreshCts.Token).ConfigureAwait (true);
            UpdateDisplay (current, request.Units, request.LocationLabel);
            StatusText.Text = triggeredByTimer ? $"Auto-updated {DateTime.Now:t}" : $"Updated {DateTime.Now:t}";
            _lastRequest = request;
            UpdateOptionCollections (request);
            _settingsStore.RecordSelection (request.City, request.State, request.Country, request.Units);
            if (!_autoRefreshTimer.IsEnabled)
                {
                _autoRefreshTimer.Start ();
                }
            }
        catch (OperationCanceledException)
            {
            StatusText.Text = triggeredByTimer ? "Auto-refresh skipped." : "Request canceled.";
            }
        catch (Exception ex)
            {
            ClearDisplay ();
            StatusText.Text = ex.Message;
            }
        finally
            {
            _isFetching = false;
            SetBusy (false);
            }
        }

    private void UpdateDisplay (CurrentWeather weather, string units, string requestedLocation)
        {
        CityValueText.Text = ResolveCityName (weather.City, requestedLocation);
        CoordinatesValueText.Text = $"{weather.Coordinates.Latitude:F2}, {weather.Coordinates.Longitude:F2}";
        var description = string.IsNullOrWhiteSpace (weather.Weather?.Description)
            ? weather.Weather?.Main
            : weather.Weather?.Description;
        ConditionValueText.Text = description ?? "Unknown";
        TemperatureValueText.Text = FormatTemperature (weather.Main?.Temperature, units);
        FeelsLikeValueText.Text = FormatTemperature (weather.Main?.FeelsLike, units);
        HumidityValueText.Text = FormatPercentage (weather.Main?.Humidity);
        PressureValueText.Text = FormatPressure (weather.Main?.Pressure);
        WindValueText.Text = FormatWind (weather.Wind, units);
        VisibilityValueText.Text = FormatVisibility (weather.Visibility);
        TimezoneValueText.Text = FormatTimezone (weather);
        }

    private static string FormatTemperature (double? value, string units) =>
		!value.HasValue ? "—" : $"{value:0.#}°{ResolveUnitSuffix (units)}";

    private static string FormatPercentage (double? value) => value.HasValue ? $"{value:0.#}%" : "—";

    private static string FormatPressure (double? value) => value.HasValue ? $"{value:0.#} hPa" : "—";

    private static string FormatVisibility (double? value) =>
		!value.HasValue ? "—" : value >= 1000 ? $"{value / 1000:0.#} km" : $"{value:0} m";

    private static string FormatWind (Wind? wind, string units)
        {
        if (wind?.Speed is null)
            {
            return "—";
            }

        var speedSuffix = units switch
            {
            "imperial" => "mph",
            _ => "m/s"
            };

        var direction = string.IsNullOrWhiteSpace (wind.WindDirectionShort) ? string.Empty : $" ({wind.WindDirectionShort})";
        return $"{wind.Speed:0.#} {speedSuffix}{direction}";
        }

    private static string FormatTimezone (CurrentWeather weather)
        {
        if (!string.IsNullOrWhiteSpace (weather.Timezone))
            {
            return weather.Timezone;
            }

        var offset = weather.TimezoneOffset ?? 0;
        return offset == 0 ? "UTC" : $"UTC{offset:+#;-#}";
        }

    private static string ResolveUnitSuffix (string units) => units switch
        {
        "imperial" => "F",
        "standard" => "K",
        _ => "C"
        };

    private static string ResolveCityName (string? apiCity, string requestedLocation) =>
		!string.IsNullOrWhiteSpace (apiCity)
            ? apiCity
            : !string.IsNullOrWhiteSpace (requestedLocation) ? requestedLocation : "Unknown city";

    private static string BuildLocationLabel (string city, string? state, string? country)
        {
        var parts = new List<string> (capacity: 3) { city };
        if (!string.IsNullOrWhiteSpace (state))
            {
            parts.Add (state);
            }

        if (!string.IsNullOrWhiteSpace (country))
            {
            parts.Add (country);
            }

        return string.Join (", ", parts.Where (part => !string.IsNullOrWhiteSpace (part)));
        }

    private void InitializeCachedSettings ()
        {
        CityComboBox.ItemsSource = _cityOptions;
        StateComboBox.ItemsSource = _stateOptions;
        CountryComboBox.ItemsSource = _countryOptions;

        SeedOptions (_cityOptions, _settingsStore.Cities);
        SeedOptions (_stateOptions, _settingsStore.States);
        SeedOptions (_countryOptions, _settingsStore.Countries);

        if (!string.IsNullOrWhiteSpace (_settingsStore.LastCity))
            {
            CityComboBox.Text = _settingsStore.LastCity;
            }

        if (!string.IsNullOrWhiteSpace (_settingsStore.LastState))
            {
            StateComboBox.Text = _settingsStore.LastState;
            }

        if (!string.IsNullOrWhiteSpace (_settingsStore.LastCountry))
            {
            CountryComboBox.Text = _settingsStore.LastCountry;
            }

        UnitsComboBox.SelectedItem = _settingsStore.LastUnits;
        if (UnitsComboBox.SelectedItem == null)
            {
            UnitsComboBox.SelectedIndex = 0;
            }
        }

    private void CityComboBox_OnSelectionChanged (object sender, SelectionChangedEventArgs e)
        {
        var selectedCity = CityComboBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace (selectedCity))
            {
            return;
            }

        if (_settingsStore.TryGetUniqueLocationByCity (selectedCity, out LocationSettingsStore.LocationEntry? location))
            {
            if (!string.IsNullOrWhiteSpace (location?.State))
                {
                StateComboBox.Text = location.State;
                }

            if (!string.IsNullOrWhiteSpace (location?.Country))
                {
                CountryComboBox.Text = location.Country;
                }
            }
        }
 
     private static void SeedOptions (ObservableCollection<string> target, IEnumerable<string> values)
         {
        foreach (var value in values)
            {
            AddOption (target, value);
            }
        }

    private static void AddOption (ObservableCollection<string> target, string? value)
        {
        if (string.IsNullOrWhiteSpace (value))
            {
            return;
            }

        if (target.Any (existing => string.Equals (existing, value, StringComparison.OrdinalIgnoreCase)))
            {
            return;
            }

        target.Add (value);
        }

    private void UpdateOptionCollections (WeatherRequest request)
        {
        AddOption (_cityOptions, request.City);
        AddOption (_stateOptions, request.State);
        AddOption (_countryOptions, request.Country);

        CityComboBox.Text = request.City;
        StateComboBox.Text = request.State ?? StateComboBox.Text;
        CountryComboBox.Text = request.Country ?? CountryComboBox.Text;
        UnitsComboBox.SelectedItem = request.Units;
        }

    private void ClearDisplay ()
        {
        CityValueText.Text = "-";
        CoordinatesValueText.Text = "-";
        ConditionValueText.Text = "-";
        TemperatureValueText.Text = "-";
        FeelsLikeValueText.Text = "-";
        HumidityValueText.Text = "-";
        PressureValueText.Text = "-";
        WindValueText.Text = "-";
        VisibilityValueText.Text = "-";
        TimezoneValueText.Text = "-";
        }

    private void SetBusy (bool isBusy, string? message = null)
        {
        BusyIndicator.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        RefreshButton.IsEnabled = !isBusy;
        if (!string.IsNullOrWhiteSpace (message))
            {
            StatusText.Text = message;
            }
        }

    protected override void OnClosed (EventArgs e)
        {
        _refreshCts.Cancel ();
        _refreshCts.Dispose ();
        _weatherController?.Dispose ();
        _autoRefreshTimer.Stop ();
        _autoRefreshTimer.Tick -= AutoRefreshTimerOnTick;
        _settingsStore.RecordWindowPlacement (CaptureWindowPlacement ());
        base.OnClosed (e);
        }
 
     private sealed record WeatherRequest (string City, string? State, string? Country, string Units, string LocationLabel);

    private void ApplySavedWindowPlacement ()
        {
        if (!_settingsStore.TryGetWindowPlacement (out LocationSettingsStore.WindowPlacement placement))
            {
            return;
            }

        (var left, var top, var width, var height) = CoerceBounds (placement.Left, placement.Top, placement.Width, placement.Height);
        Left = left;
        Top = top;
        Width = width;
        Height = height;

        if (placement.State == WindowState.Maximized)
            {
            Loaded += (_, _) => WindowState = WindowState.Maximized;
            }
        }

    private static (double left, double top, double width, double height) CoerceBounds (double left, double top, double width, double height)
        {
        var minWidth = 600d;
        var minHeight = 360d;
        var screenLeft = SystemParameters.VirtualScreenLeft;
        var screenTop = SystemParameters.VirtualScreenTop;
        var screenWidth = Math.Max (SystemParameters.VirtualScreenWidth, minWidth);
        var screenHeight = Math.Max (SystemParameters.VirtualScreenHeight, minHeight);

        width = double.IsNaN (width) || width <= 0 ? minWidth : width;
        height = double.IsNaN (height) || height <= 0 ? minHeight : height;
        width = Math.Min (width, screenWidth);
        height = Math.Min (height, screenHeight);

        var maxLeft = screenLeft + screenWidth - width;
        var maxTop = screenTop + screenHeight - height;
        left = double.IsNaN (left) ? screenLeft : Math.Min (Math.Max (left, screenLeft), maxLeft);
        top = double.IsNaN (top) ? screenTop : Math.Min (Math.Max (top, screenTop), maxTop);

        return (left, top, width, height);
        }

    private LocationSettingsStore.WindowPlacement CaptureWindowPlacement ()
        {
        WindowState state = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;
        Rect bounds = state == WindowState.Normal ? new Rect (Left, Top, Width, Height) : RestoreBounds;
        return new LocationSettingsStore.WindowPlacement (bounds.Width, bounds.Height, bounds.Left, bounds.Top, state);
        }
     }
