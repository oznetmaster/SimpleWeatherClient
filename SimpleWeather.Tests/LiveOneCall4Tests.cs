// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

namespace SimpleWeather.Tests;

[TestFixture, Category ("Live"), NonParallelizable]
public sealed class LiveOneCall4Tests
	{
	private LiveTestSettings _settings = null!;
	private WeatherController? _controller;

	[OneTimeSetUp]
	public void LoadPrivateAccount ()
		{
		_settings = LiveTestSupport.LoadForRun ();
		if (!bool.TryParse (TestContext.Parameters.Get ("EnableOneCall4Tests", _settings.EnableOneCall4Tests.ToString ()), out bool enabled) || !enabled)
			Assert.Ignore ("Set EnableOneCall4Tests=true with a One Call 4.0 subscription to test its live endpoints.");
		_controller = new WeatherController (_settings.ApiKey, OpenWeatherService.OneCall4);
		}

	[OneTimeTearDown]
	public void DisposeClient () => _controller?.Dispose ();

	[Test]
	public async Task CurrentWeather_ReadsLiveVersion4Measurements ()
		{
		CurrentWeather weather = await LiveTestSupport.Request (() => _controller!.GetCurrentWeatherAsync (_settings.Latitude!.Value, _settings.Longitude!.Value, _settings.Units));
		Assert.That (weather.Coordinates.Latitude, Is.EqualTo (_settings.Latitude!.Value).Within (0.1));
		Assert.That (weather.Main?.Temperature, Is.Not.Null);
		Assert.That (weather.Main?.Humidity, Is.InRange (0, 100));
		Assert.That (weather.Wind?.Speed, Is.GreaterThanOrEqualTo (0));
		}

	[Test]
	public async Task Forecast_ReadsLiveVersion4DailyAndPaginatedHourlyData ()
		{
		WeatherForecast forecast = await LiveTestSupport.Request (() => _controller!.GetWeatherForecastAsync (_settings.Latitude!.Value, _settings.Longitude!.Value, _settings.Units));
		Assert.That (forecast.Daily, Has.Count.EqualTo (8));
		Assert.That (forecast.Hourly, Has.Count.EqualTo (48));
		Assert.That (forecast.Hourly.Select (hour => hour.DT), Is.Ordered.Ascending.And.Unique);
		Assert.That (forecast.Daily.Select (day => day.DT), Is.Ordered.Ascending.And.Unique);
		Assert.That (forecast.Hourly[0].DT, Is.InRange (DateTime.Now.AddHours (-12), DateTime.Now.AddHours (12)));
		foreach (Hourly hour in forecast.Hourly)
			{
			Assert.That (hour.Temperature, Is.Not.Null);
			Assert.That (hour.PrecipitationProbability, Is.InRange (0, 100));
			}
		}

	[Test]
	public async Task DailySnapshot_ReadsLiveCurrentAndDailyWithTwoVersion4Requests ()
		{
		using var handler = new Version4OnlyHandler ();
		using var controller = new WeatherController (_settings.ApiKey, handler, OpenWeatherService.OneCall4);
		WeatherSnapshot snapshot = await LiveTestSupport.Request (() => controller.GetWeatherSnapshotAsync (new LatLong (_settings.Latitude!.Value, _settings.Longitude!.Value), includeHourly: false, units: _settings.Units));
		Assert.That (snapshot.CurrentWeather.Main?.Temperature, Is.Not.Null);
		Assert.That (snapshot.Forecast.Daily, Has.Count.EqualTo (8));
		Assert.That (snapshot.Forecast.Hourly, Is.Empty);
		Assert.That (handler.LiveRequests, Is.EqualTo (2));
		}

	private sealed class Version4OnlyHandler : DelegatingHandler
		{
		internal int LiveRequests { get; private set; }
		internal Version4OnlyHandler () : base (new HttpClientHandler ()) { }

		protected override Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
			{
			// Count real 4.0 requests and forbid any subscription probe or free fallback.
			if (!request.RequestUri!.AbsolutePath.StartsWith ("/data/4.0/onecall/", StringComparison.Ordinal))
				throw new InvalidOperationException ("The live One Call 4.0 test attempted a different endpoint.");
			LiveRequests++;
			return base.SendAsync (request, cancellationToken);
			}
		}
	}
