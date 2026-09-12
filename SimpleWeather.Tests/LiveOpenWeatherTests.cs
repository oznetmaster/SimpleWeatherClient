// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

namespace SimpleWeather.Tests;

[TestFixture, Category ("Live"), NonParallelizable]
public sealed class LiveOpenWeatherTests
	{
	private LiveTestSettings _settings = null!;
	private WeatherController? _controller;
	private LatLong Location => new (_settings.Latitude!.Value, _settings.Longitude!.Value);

	[OneTimeSetUp]
	public void LoadPrivateAccount ()
		{
		_settings = LiveTestSupport.LoadForRun ();
		_controller = new WeatherController (_settings.ApiKey);
		}

	[OneTimeTearDown]
	public void DisposeClient () => _controller?.Dispose ();

	[Test]
	public async Task CurrentWeather_ReturnsObservedMeasurements ()
		{
		CurrentWeather weather = await LiveTestSupport.Request (() => _controller!.GetCurrentWeatherAsync (Location, _settings.Units));
		Assert.That (weather.StatusCode, Is.Null);
		Assert.That (weather.Coordinates.Latitude, Is.EqualTo (Location.Latitude).Within (0.1));
		Assert.That (weather.Coordinates.Longitude, Is.EqualTo (Location.Longitude).Within (0.1));
		Assert.That (weather.Main?.Temperature, Is.Not.Null);
		Assert.That (weather.Main?.Humidity, Is.InRange (0, 100));
		Assert.That (weather.Wind?.Speed, Is.GreaterThanOrEqualTo (0));
		Assert.That (weather.Weather, Is.Not.Null);
		}

	[Test]
	public async Task Forecast_ReturnsCurrentChronologicalReadings ()
		{
		WeatherForecast forecast = await LiveTestSupport.Request (() => _controller!.GetWeatherForecastAsync (Location, _settings.Units));
		Assert.That (forecast.Hourly, Is.Not.Empty);
		Assert.That (forecast.Daily, Is.Not.Empty);
		Assert.That (forecast.Hourly.Select (hour => hour.DT), Is.Ordered.Ascending);
		Assert.That (forecast.Hourly[0].DT, Is.InRange (DateTime.Now.AddHours (-12), DateTime.Now.AddHours (12)));
		foreach (Hourly hour in forecast.Hourly)
			{
			Assert.That (hour.Temperature, Is.Not.Null);
			Assert.That (hour.PrecipitationProbability, Is.InRange (0, 100));
			Assert.That (hour.WindSpeed, Is.GreaterThanOrEqualTo (0));
			}
		}

	[Test]
	public async Task ReverseGeocoding_ResolvesConfiguredLocation ()
		{
		var locator = new GeoLocator (_settings.ApiKey);
		string? name = await LiveTestSupport.Request (() => locator.GetCityNameByCoordinatesAsync (Location));
		Assert.That (name, Is.Not.Null.And.Not.Empty, "Choose coordinates near a populated place for geocoding tests.");
		}

	[Test]
	public async Task DirectGeocoding_ResolvesConfiguredCity ()
		{
		if (string.IsNullOrWhiteSpace (_settings.CityName))
			Assert.Ignore ("Set cityName in private live settings to enable direct geocoding.");
		var locator = new GeoLocator (_settings.ApiKey);
		List<string> cities = await LiveTestSupport.Request (() => locator.GetCitiesByNameAsync (_settings.CityName!, countryCode: _settings.CountryCode));
		Assert.That (cities, Is.Not.Empty);
		foreach (string city in cities)
			{
			var payload = JObject.Parse (city);
			Assert.That (payload["name"], Is.Not.Null);
			Assert.That (payload["lat"]!.Value<double> (), Is.InRange (-90, 90));
			Assert.That (payload["lon"]!.Value<double> (), Is.InRange (-180, 180));
			}
		}
	}