// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

using System.IO;
using Newtonsoft.Json;

namespace SimpleWeather.Tests;

internal sealed class LiveTestSettings
	{
	public bool Enabled { get; set; }
	public string? ApiKey { get; set; }
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }
	public string Units { get; set; } = "metric";
	public string? CityName { get; set; }
	public string? CountryCode { get; set; }
	}

internal static class LiveTestSupport
	{
	internal static LiveTestSettings LoadForRun () => LoadForRun (FindSettingsPath (), TestContext.Parameters.Get ("EnableLiveTests", ""));

	internal static string FindSettingsPath ()
		{
		string directory = TestContext.Parameters.Get ("TestDataDirectory", "");
		if (!string.IsNullOrWhiteSpace (directory))
			return Path.Combine (directory, "LiveTestSettings.json");
		for (DirectoryInfo? parent = new (TestContext.CurrentContext.TestDirectory); parent != null; parent = parent.Parent)
			{
			string path = Path.Combine (parent.FullName, "LiveTestSettings.json");
			if (File.Exists (path))
				return path;
			if (File.Exists (Path.Combine (parent.FullName, "SimpleWeather.sln")))
				break;
			}
		return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "SimpleWeather", "LiveTestSettings.json");
		}

	internal static LiveTestSettings LoadForRun (string path, string enableOverride)
		{
		bool? enabled = null;
		if (!string.IsNullOrWhiteSpace (enableOverride))
			{
			if (!bool.TryParse (enableOverride, out bool value))
				throw new InvalidDataException ("EnableLiveTests must be true or false.");
			enabled = value;
			}
		if (enabled == false)
			Assert.Ignore ("Live OpenWeather tests are disabled by EnableLiveTests=false.");
		LiveTestSettings? settings = null;
		if (File.Exists (path))
			{
			try
				{
				settings = JsonConvert.DeserializeObject<LiveTestSettings> (File.ReadAllText (path));
				}
			catch (JsonException)
				{
				// JSON exceptions can include secret values; deliberately omit the original exception.
				throw new InvalidDataException ("LiveTestSettings.json is not valid settings JSON.");
				}
			}
		if (enabled != true && settings?.Enabled != true)
			Assert.Ignore ("Live OpenWeather tests are disabled. Use live.runsettings or set enabled in private LiveTestSettings.json.");
		if (settings == null)
			throw new InvalidDataException ("Enabled live tests require LiveTestSettings.json beside the test project or in TestDataDirectory.");
		if (string.IsNullOrWhiteSpace (settings.ApiKey))
			throw new InvalidDataException ("Enabled live tests require an apiKey.");
		if (!ValidCoordinate (settings.Latitude, 90) || !ValidCoordinate (settings.Longitude, 180))
			throw new InvalidDataException ("Enabled live tests require valid latitude and longitude coordinates.");
		if (settings.Units is not ("metric" or "imperial" or "standard"))
			throw new InvalidDataException ("Live test units must be metric, imperial or standard.");
		return settings;
		}

	private static bool ValidCoordinate (double? value, double limit) => value.HasValue && !double.IsNaN (value.Value) && !double.IsInfinity (value.Value) && Math.Abs (value.Value) <= limit;

	internal static async Task<T> Request<T> (Func<Task<T>> request)
		{
		try
			{
			return await request ();
			}
		catch (Exception exception)
			{
			// Request URIs and deserialization errors can contain the account key or private responses.
			throw new InvalidOperationException ($"Live OpenWeather request failed ({exception.GetType ().Name}). Check account access, connectivity and private settings.");
			}
		}
	}