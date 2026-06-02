using System.Text.Json;

using Windows.Storage;

namespace SimpleWeather.Widget.Services;

public sealed class WidgetSettingsStore
	{
	private const string LEGACY_FILE_NAME = "widget-settings.json";
	private const string SETTINGS_KEY = "WidgetSettingsJson";
	private readonly ApplicationDataContainer? _localSettings;
	private readonly string _legacyFolder;
	private readonly string _legacyPath;

	public WidgetSettingsStore ()
		{
		_legacyFolder = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "SimpleWeather");
		_legacyPath = Path.Combine (_legacyFolder, LEGACY_FILE_NAME);
		_localSettings = InitializeLocalSettings ();
		CurrentSettings = Load ();
		}

	public WidgetSettings CurrentSettings { get; private set; }

   private static readonly JsonSerializerOptions _options = new () { WriteIndented = false };
	public void Save (WidgetSettings settings)
		{
		CurrentSettings = Sanitize (settings);
      var json = JsonSerializer.Serialize (CurrentSettings, _options);

		if (_localSettings is not null)
			{
			_localSettings.Values[SETTINGS_KEY] = json;
			}
		else
			{
			SaveToLegacyFile (json);
			}
		}

	private WidgetSettings Load ()
		{
		WidgetSettings? settings = null;

		if (_localSettings is not null && _localSettings.Values.TryGetValue (SETTINGS_KEY, out var stored) && stored is string storedJson)
			{
			settings = Deserialize (storedJson);
			}

		if (settings is null)
			{
			settings = LoadFromLegacyFile ();
			if (settings is not null && _localSettings is not null)
				{
				Save (settings);
				DeleteLegacyFile ();
				}
			}

		return settings is null ? new WidgetSettings () : Sanitize (settings);
		}

	private static ApplicationDataContainer? InitializeLocalSettings ()
		{
		try
			{
			return ApplicationData.Current.LocalSettings;
			}
		catch
			{
			return null;
			}
		}

	private static WidgetSettings? Deserialize (string json)
		{
		try
			{
			return JsonSerializer.Deserialize<WidgetSettings> (json);
			}
		catch
			{
			return null;
			}
		}

	private WidgetSettings? LoadFromLegacyFile ()
		{
		try
			{
			if (!File.Exists (_legacyPath))
				{
				return null;
				}

			var json = File.ReadAllText (_legacyPath);
			return Deserialize (json);
			}
		catch
			{
			return null;
			}
		}

	private void SaveToLegacyFile (string json)
		{
		try
			{
            _ = Directory.CreateDirectory (_legacyFolder);
			File.WriteAllText (_legacyPath, json);
			}
		catch
			{
			// ignore file persistence errors
			}
		}

	private void DeleteLegacyFile ()
		{
		try
			{
			if (File.Exists (_legacyPath))
				{
				File.Delete (_legacyPath);
				}
			}
		catch
			{
			// ignore cleanup failures
			}
		}

	private static WidgetSettings Sanitize (WidgetSettings settings)
		{
		settings.City = string.IsNullOrWhiteSpace (settings.City) ? "Seattle" : settings.City.Trim ();
		settings.State = string.IsNullOrWhiteSpace (settings.State) ? null : settings.State.Trim ();
		settings.Country = string.IsNullOrWhiteSpace (settings.Country) ? "US" : settings.Country.Trim ();
		settings.Units = string.IsNullOrWhiteSpace (settings.Units) ? "metric" : settings.Units;
		settings.RefreshMinutes = Math.Clamp (settings.RefreshMinutes, 5, 240);
		settings.ApiKey = string.IsNullOrWhiteSpace (settings.ApiKey) ? null : settings.ApiKey.Trim ();
		return settings;
		}
	}

public sealed class WidgetSettings
	{
	public string City { get; set; } = "Seattle";
	public string? State { get; set; }
	public string? Country { get; set; } = "US";
	public string Units { get; set; } = "metric";
	public int RefreshMinutes { get; set; } = 30;
	public string? ApiKey { get; set; }
	public double? WindowLeft { get; set; }
	public double? WindowTop { get; set; }
	public double? CompactWindowLeft { get; set; }
	public double? CompactWindowTop { get; set; }
	public double? ForecastWindowLeft { get; set; }
	public double? ForecastWindowTop { get; set; }
	public double? ForecastWindowWidth { get; set; }
	public double? ForecastWindowHeight { get; set; }
	public bool IsLocationFixed { get; set; }
	public bool IsAlwaysOnTop { get; set; }
	public bool IsCompactMode { get; set; }
	}
