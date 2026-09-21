// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

using System.IO;

namespace SimpleWeather.Tests;

[TestFixture]
public sealed class OneCall4Tests
	{
	private const string Api = "https://api.openweathermap.org/data/4.0/onecall/";

	private static string Current ()
		{
		JsonObject value = JsonTest.ParseObject (Payloads.Current);
		value["data"] = new JsonArray (value["current"]!.DeepClone ());
		value.Remove ("current");
		return value.ToString ();
		}

	private static string Timeline (bool daily, int first, int count, string? next = null)
		{
		var data = new JsonArray ();
		for (int index = first; index < first + count; index++)
			{
			JsonObject item = (JsonObject)JsonTest.ParseObject (Payloads.Forecast)[daily ? "daily" : "hourly"]![0]!.DeepClone ();
			item["dt"] = 1700000000L + index * (daily ? 86400L : 3600L);
			data.Add (item);
			}
		return new JsonObject { ["lat"] = 55.5, ["lon"] = -3.25, ["timezone"] = "Europe/London", ["timezone_offset"] = 3600, ["data"] = data, ["next"] = next }.ToString ();
		}

	[Test]
	public async Task Current_UsesVersion4AfterVersion3DeniesAccess ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply (Current ());
		using var controller = new WeatherController ("synthetic-key", http);
		CurrentWeather result = await controller.GetCurrentWeatherAsync (55.5, -3.25);
		Assert.That (result.Main!.Temperature, Is.EqualTo (12.5));
		Assert.That (result.Wind!.Speed, Is.EqualTo (4.5));
		Assert.That (result.Timezone, Is.EqualTo ("Europe/London"));
		Assert.That (http.Requests.Select (uri => uri.AbsolutePath), Is.EqualTo (new[] { "/data/3.0/onecall", "/data/4.0/onecall/current" }));
		Assert.That (http.Contents.All (item => item.Disposed), Is.True);
		}

	[TestCase (OpenWeatherService.OneCall3, "One Call 3.0")]
	[TestCase (OpenWeatherService.OneCall4, "One Call 4.0")]
	[TestCase (OpenWeatherService.Free, "free")]
	public async Task SelectedServicesAreIndependentWithTheSameKey (OpenWeatherService service, string label)
		{
		using var http = new ScriptedWeather ();
		http.Reply (service == OpenWeatherService.OneCall4 ? Current () : service == OpenWeatherService.Free ? Payloads.FreeCurrent : Payloads.Current);
		using var controller = new WeatherController ("same-account-key", http, service);
		await controller.GetCurrentWeatherAsync (55.5, -3.25);
		string path = service == OpenWeatherService.OneCall3 ? "/data/3.0/onecall" : service == OpenWeatherService.OneCall4 ? "/data/4.0/onecall/current" : "/data/2.5/weather";
		Assert.That (controller.Service, Is.EqualTo (service), label);
		Assert.That (http.Requests.Select (uri => uri.AbsolutePath), Is.EqualTo (new[] { path }));
		Assert.That (ScriptedWeather.Query (http.Requests[0])["appid"], Is.EqualTo ("same-account-key"));
		}

	[Test]
	public void SelectedServiceDenialNeverFallsBack (
		[Values (OpenWeatherService.OneCall3, OpenWeatherService.OneCall4, OpenWeatherService.Free)] OpenWeatherService service,
		[Values ("current", "forecast", "snapshot", "capabilities")] string operation,
		[Values (HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden)] HttpStatusCode status)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", status);
		using var controller = new WeatherController ("synthetic-key", http, service);
		Task Request () => operation switch
			{
			"current" => controller.GetCurrentWeatherAsync (55.5, -3.25),
			"forecast" => controller.GetWeatherForecastAsync (55.5, -3.25),
			"snapshot" => controller.GetWeatherSnapshotAsync (new LatLong (55.5, -3.25)),
			_ => controller.ProbeCapabilitiesAsync ()
			};
		var error = Assert.ThrowsAsync<UnauthorizedAccessException> (async () => await Request ());
		Assert.That (error!.Message, Does.Contain (service == OpenWeatherService.OneCall3 ? "3.0" : service == OpenWeatherService.OneCall4 ? "4.0" : "free"));
		Assert.That (error.Message, Does.Not.Contain ("synthetic-key"));
		Assert.That (http.Requests, Has.Count.EqualTo (1));
		}

	[TestCase (true)]
	[TestCase (false)]
	public async Task Snapshot_Version3UsesOneRequestForCurrentAndForecast (bool includeHourly)
		{
		using var http = new ScriptedWeather ();
		JsonObject data = JsonTest.ParseObject (Payloads.Forecast);
		data["current"] = JsonTest.ParseObject (Payloads.Current)["current"]!.DeepClone ();
		http.Reply (data.ToString ());
		using var controller = new WeatherController ("synthetic-key", http);
		WeatherSnapshot result = await controller.GetWeatherSnapshotAsync (new LatLong (55.5, -3.25), includeHourly);
		Assert.That (http.Requests, Has.Count.EqualTo (1));
		Assert.That (result.CurrentWeather.Main!.Temperature, Is.EqualTo (12.5));
		Assert.That (result.Forecast.Daily, Is.Not.Empty);
		Assert.That (result.Forecast.Hourly.Count > 0, Is.EqualTo (includeHourly));
		}

	[Test]
	public async Task Snapshot_Version4DailyOnlyDoesNotRequestHourlyPages ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply (Timeline (true, 0, 10));
		http.Reply (Current ());
		using var controller = new WeatherController ("synthetic-key", http);
		WeatherSnapshot result = await controller.GetWeatherSnapshotAsync (new LatLong (55.5, -3.25), includeHourly: false);
		Assert.That (http.Requests.Select (uri => uri.AbsolutePath), Is.EqualTo (new[] { "/data/3.0/onecall", "/data/4.0/onecall/timeline/1day", "/data/4.0/onecall/current" }));
		Assert.That (result.Forecast.Daily, Has.Count.EqualTo (8));
		Assert.That (result.Forecast.Hourly, Is.Empty);
		Assert.That (result.CurrentWeather.Main!.Temperature, Is.EqualTo (12.5));
		}

	[Test]
	public async Task DailyForecast_DoesNotFetchUnusedHourlyData (
		[Values (OpenWeatherService.OneCall3, OpenWeatherService.OneCall4, OpenWeatherService.Free)] OpenWeatherService service,
		[Values (true, false)] bool city)
		{
		using var http = new ScriptedWeather ();
		if (city) http.Reply ("[{\"lat\":55.5,\"lon\":-3.25}]");
		JsonObject dailyOnly = JsonTest.ParseObject (Payloads.Forecast);
		dailyOnly.Remove ("hourly");
		http.Reply (service == OpenWeatherService.OneCall4 ? Timeline (true, 0, 8)
			: service == OpenWeatherService.Free ? Payloads.FreeForecast : dailyOnly.ToString ());
		using var controller = new WeatherController ("synthetic-key", http, service);
		WeatherForecast result = city
			? await controller.GetWeatherForecastAsync ("Synthetic City", includeHourly: false, countryCode: "GB")
			: await controller.GetWeatherForecastAsync (55.5, -3.25, includeHourly: false);
		Assert.That (result.Daily, Is.Not.Empty);
		Assert.That (result.Hourly, Is.Empty);
		Assert.That (http.Requests, Has.Count.EqualTo (city ? 2 : 1));
		string expectedPath = service == OpenWeatherService.OneCall4 ? "/data/4.0/onecall/timeline/1day"
			: service == OpenWeatherService.OneCall3 ? "/data/3.0/onecall" : "/data/2.5/forecast";
		Assert.That (http.Requests.Last ().AbsolutePath, Is.EqualTo (expectedPath));
		if (service == OpenWeatherService.OneCall3)
			Assert.That (ScriptedWeather.Query (http.Requests.Last ())["exclude"].Split (','), Does.Contain ("hourly"));
		}

	[TestCase (true)]
	[TestCase (false)]
	public async Task Snapshot_FreeAccountDoesNotRepeatSubscriptionProbes (bool includeHourly)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply (Payloads.FreeForecast);
		http.Reply (Payloads.FreeCurrent);
		using var controller = new WeatherController ("synthetic-key", http);
		WeatherSnapshot result = await controller.GetWeatherSnapshotAsync (new LatLong (55.5, -3.25), includeHourly);
		Assert.That (http.Requests.Select (uri => uri.AbsolutePath), Is.EqualTo (new[] { "/data/3.0/onecall", "/data/4.0/onecall/timeline/1day", "/data/2.5/forecast", "/data/2.5/weather" }));
		Assert.That (result.CurrentWeather.Main!.Temperature, Is.Not.Null);
		Assert.That (result.Forecast.Daily, Is.Not.Empty);
		Assert.That (result.Forecast.Hourly.Count > 0, Is.EqualTo (includeHourly));
		}

	[Test]
	public async Task Snapshot_FullVersion4UsesFiveWeatherRequests ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply (Timeline (true, 0, 10));
		http.Reply (Timeline (false, 0, 20, Api + "timeline/1h?start=2"));
		http.Reply (Timeline (false, 20, 20, Api + "timeline/1h?start=3"));
		http.Reply (Timeline (false, 40, 20));
		http.Reply (Current ());
		using var controller = new WeatherController ("synthetic-key", http);
		WeatherSnapshot result = await controller.GetWeatherSnapshotAsync (new LatLong (55.5, -3.25));
		Assert.That (http.Requests, Has.Count.EqualTo (6), "Five weather requests plus the denied 3.0 probe.");
		Assert.That (result.Forecast.Hourly, Has.Count.EqualTo (48));
		}

	[TestCase (HttpStatusCode.Unauthorized, (HttpStatusCode)429)]
	[TestCase (HttpStatusCode.InternalServerError, HttpStatusCode.OK)]
	public void Snapshot_ServiceFailuresDoNotTriggerMoreWeatherRequests (HttpStatusCode version3, HttpStatusCode version4)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", version3);
		if (version3 == HttpStatusCode.Unauthorized) http.Reply ("{}", version4);
		using var controller = new WeatherController ("synthetic-key", http);
		Assert.ThrowsAsync<HttpRequestException> (() => controller.GetWeatherSnapshotAsync (new LatLong (55.5, -3.25)));
		Assert.That (http.Requests, Has.Count.EqualTo (version3 == HttpStatusCode.Unauthorized ? 2 : 1));
		}

	[Test]
	public async Task Forecast_Reads48HoursAndEightDaysWithoutFollowingTheLongRangeTimeline ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply (Timeline (true, 0, 10, Api + "timeline/1day?start=1800000000"));
		http.Reply (Timeline (false, 0, 20, Api.Replace ("https:", "http:") + "timeline/1h?start=1700072000"));
		http.Reply (Timeline (false, 20, 20, Api + "timeline/1h?start=1700144000&units=standard"));
		http.Reply (Timeline (false, 40, 20, Api + "timeline/1h?start=1700216000"));
		using var controller = new WeatherController ("key+&=#?", http);
		WeatherForecast result = await controller.GetWeatherForecastAsync (55.5, -3.25, "imperial");
		Assert.That (result.Daily, Has.Count.EqualTo (8));
		Assert.That (result.Hourly, Has.Count.EqualTo (48));
		Assert.That (result.Daily[0].Temperature!.Min, Is.EqualTo (10));
		Assert.That (result.Latitude, Is.EqualTo (55.5));
		Assert.That (http.Requests, Has.Count.EqualTo (5));
		foreach (Uri uri in http.Requests.Skip (1))
			{
			Assert.That (uri.Scheme, Is.EqualTo (Uri.UriSchemeHttps));
			var query = ScriptedWeather.Query (uri);
			Assert.That (query["units"], Is.EqualTo ("imperial"));
			Assert.That (query["appid"], Is.EqualTo ("key+&=#?"));
			Assert.That (query["lat"], Is.EqualTo ("55.5"));
			Assert.That (query["lon"], Is.EqualTo ("-3.25"));
			}
		Assert.That (http.Contents.All (item => item.Disposed), Is.True);
		}

	[Test]
	public async Task Forecast_ReadsShortDailyPagesAndStopsWhenNoContinuationExists ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Forbidden);
		http.Reply (Timeline (true, 0, 4, Api + "timeline/1day?start=1700345600"));
		http.Reply (Timeline (true, 4, 4));
		http.Reply (Timeline (false, 0, 2));
		using var controller = new WeatherController ("synthetic-key", http);
		WeatherForecast result = await controller.GetWeatherForecastAsync (55.5, -3.25);
		Assert.That (result.Daily, Has.Count.EqualTo (8));
		Assert.That (result.Hourly, Has.Count.EqualTo (2));
		}

	[Test]
	public async Task Capabilities_RecognizeVersion4Account ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply (Current ());
		using var controller = new WeatherController ("synthetic-key", http);
		var capabilities = await controller.ProbeCapabilitiesAsync ();
		Assert.That (capabilities.Has (OpenWeatherFeatures.OneCallDaily), Is.True);
		Assert.That (await controller.ProbeCapabilitiesAsync (), Is.SameAs (capabilities));
		Assert.That (http.Requests, Has.Count.EqualTo (2));
		}

	[TestCase (429)]
	[TestCase (500)]
	public void Version4Failure_DoesNotSilentlyDowngradeToFree (int status)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply ("{}", (HttpStatusCode)status);
		using var controller = new WeatherController ("synthetic-key", http);
		Assert.ThrowsAsync<HttpRequestException> (() => controller.GetCurrentWeatherAsync (1, 2));
		Assert.That (http.Requests, Has.Count.EqualTo (2));
		}

	[TestCase ("{}")]
	[TestCase ("{\"data\":[]}")]
	[TestCase ("{\"data\":{\"temp\":12}}")]
	public void InvalidCurrentPayload_IsNotSuccessfulWeather (string payload)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply (payload);
		using var controller = new WeatherController ("synthetic-key", http);
		Assert.ThrowsAsync<InvalidDataException> (() => controller.GetCurrentWeatherAsync (1, 2));
		}

	[TestCase ("https://example.test/data/4.0/onecall/timeline/1day?start=2")]
	[TestCase ("http://api.openweathermap.org:8080/data/4.0/onecall/timeline/1day?start=2")]
	[TestCase ("https://api.openweathermap.org/data/4.0/onecall/current?start=2")]
	public void InvalidPagination_DoesNotSendAnotherRequest (string next)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply (Timeline (true, 0, 1, next));
		using var controller = new WeatherController ("synthetic-key", http);
		Assert.ThrowsAsync<InvalidDataException> (() => controller.GetWeatherForecastAsync (1, 2));
		Assert.That (http.Requests, Has.Count.EqualTo (2));
		}

	[Test]
	public void RepeatedTimelineRecord_StopsInsteadOfLooping ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply (Timeline (true, 0, 1, Api + "timeline/1day?start=2"));
		http.Reply (Timeline (true, 0, 1, Api + "timeline/1day?start=3"));
		using var controller = new WeatherController ("synthetic-key", http);
		Assert.ThrowsAsync<InvalidDataException> (() => controller.GetWeatherForecastAsync (1, 2));
		Assert.That (http.Requests, Has.Count.EqualTo (3));
		}

	[Test]
	public void CancellationDuringPagination_IsPropagated ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply (Timeline (true, 0, 1, Api + "timeline/1day?start=2"));
		http.WaitForCancellation ();
		using var controller = new WeatherController ("synthetic-key", http);
		using var cancellation = new CancellationTokenSource ();
		Task operation = controller.GetWeatherForecastAsync (1, 2, cancellationToken: cancellation.Token);
		cancellation.Cancel ();
		Assert.CatchAsync<OperationCanceledException> (async () => await operation);
		}
	}
