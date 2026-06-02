using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace SimpleWeather.Desktop;

internal sealed class LocationSettingsStore
	{
	private const string FileName = "location-settings.json";
	private readonly string _filePath;
	private SettingsSnapshot _snapshot;

	public LocationSettingsStore ()
		{
		var folder = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "SimpleWeather");
		Directory.CreateDirectory (folder);
		_filePath = Path.Combine (folder, FileName);
		_snapshot = Load ();
		}

	public IReadOnlyList<string> Cities => _snapshot.Cities;
	public IReadOnlyList<string> States => _snapshot.States;
	public IReadOnlyList<string> Countries => _snapshot.Countries;
	public IReadOnlyList<LocationEntry> Locations => _snapshot.Locations;
	public string? LastCity => _snapshot.LastCity;
	public string? LastState => _snapshot.LastState;
	public string? LastCountry => _snapshot.LastCountry;
	public string LastUnits => string.IsNullOrWhiteSpace (_snapshot.LastUnits) ? "metric" : _snapshot.LastUnits!;
	public bool TryGetWindowPlacement (out WindowPlacement placement)
		{
		placement = default;
		if (_snapshot.WindowWidth is > 0 and var width &&
		    _snapshot.WindowHeight is > 0 and var height &&
		    _snapshot.WindowLeft is not null &&
		    _snapshot.WindowTop is not null)
			{
            WindowState state = _snapshot.WindowState.HasValue ? (WindowState)_snapshot.WindowState.Value : WindowState.Normal;
			placement = new WindowPlacement (width, height, _snapshot.WindowLeft.Value, _snapshot.WindowTop.Value, state);
			return true;
			}

		return false;
		}

	public void RecordSelection (string city, string? state, string? country, string units)
		{
		AddUnique (_snapshot.Cities, city);
		AddUnique (_snapshot.States, state);
		AddUnique (_snapshot.Countries, country);
		AddOrUpdateLocationEntry (city, state, country);

		_snapshot.LastCity = city;
		_snapshot.LastState = state;
		_snapshot.LastCountry = country;
		_snapshot.LastUnits = string.IsNullOrWhiteSpace (units) ? "metric" : units;

		Save ();
		}

	public void RecordWindowPlacement (WindowPlacement placement)
		{
		_snapshot.WindowWidth = placement.Width;
		_snapshot.WindowHeight = placement.Height;
		_snapshot.WindowLeft = placement.Left;
		_snapshot.WindowTop = placement.Top;
		_snapshot.WindowState = (int)placement.State;
		Save ();
		}

	public bool TryGetUniqueLocationByCity (string city, out LocationEntry? entry)
		{
		entry = default;
		if (string.IsNullOrWhiteSpace (city))
			{
			return false;
			}

        List<LocationEntry> matches = _snapshot.Locations.FindAll (l => string.Equals (l.City, city, StringComparison.OrdinalIgnoreCase));
		if (matches.Count == 1)
			{
			entry = matches[0];
			return true;
			}

		return false;
		}

	private SettingsSnapshot Load ()
		{
		try
			{
			if (File.Exists (_filePath))
				{
				var json = File.ReadAllText (_filePath);
                SettingsSnapshot? snapshot = JsonSerializer.Deserialize<SettingsSnapshot> (json);
				if (snapshot != null)
					{
					snapshot.Cities ??= [];
					snapshot.States ??= [];
					snapshot.Countries ??= [];
					snapshot.Locations ??= [];
					return snapshot;
					}
				}
			}
		catch
			{
			// Ignore corrupt files and fall back to defaults.
			}

		return new SettingsSnapshot
			{
			Cities = [],
			States = [],
			Countries = [],
			Locations = [],
			LastUnits = "metric"
			};
		}

	private void Save ()
		{
		try
			{
			var options = new JsonSerializerOptions { WriteIndented = true };
			var json = JsonSerializer.Serialize (_snapshot, options);
			File.WriteAllText (_filePath, json);
			}
		catch
			{
			// Swallow IO exceptions; persistence is best-effort.
			}
		}

	private static void AddUnique (List<string> target, string? value)
		{
		if (string.IsNullOrWhiteSpace (value))
			{
			return;
			}

		if (target.Exists (existing => string.Equals (existing, value, StringComparison.OrdinalIgnoreCase)))
			{
			return;
			}

		target.Add (value);
		}

	private void AddOrUpdateLocationEntry (string city, string? state, string? country)
		{
		if (string.IsNullOrWhiteSpace (city))
			{
			return;
			}

		var existingIndex = _snapshot.Locations.FindIndex (l => string.Equals (l.City, city, StringComparison.OrdinalIgnoreCase) && string.Equals (l.State ?? string.Empty, state ?? string.Empty, StringComparison.OrdinalIgnoreCase) && string.Equals (l.Country ?? string.Empty, country ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		if (existingIndex >= 0)
			{
			_snapshot.Locations[existingIndex] = new LocationEntry (city, state, country);
			return;
			}

		_snapshot.Locations.Add (new LocationEntry (city, state, country));
		}

	private sealed class SettingsSnapshot
		{
		public List<string> Cities { get; set; } = [];
		public List<string> States { get; set; } = [];
		public List<string> Countries { get; set; } = [];
		public List<LocationEntry> Locations { get; set; } = [];
		public string? LastCity { get; set; }
		public string? LastState { get; set; }
		public string? LastCountry { get; set; }
		public string? LastUnits { get; set; }
		public double? WindowWidth { get; set; }
		public double? WindowHeight { get; set; }
		public double? WindowLeft { get; set; }
		public double? WindowTop { get; set; }
		public int? WindowState { get; set; }
		}

	internal readonly record struct WindowPlacement (double Width, double Height, double Left, double Top, WindowState State);
	internal sealed record LocationEntry (string City, string? State, string? Country);
	}
