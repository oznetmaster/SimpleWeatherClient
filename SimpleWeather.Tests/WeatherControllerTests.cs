// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

namespace SimpleWeather.Tests;

[TestFixture]
public sealed class WeatherControllerTests
	{
	private static async Task Fetch (WeatherController controller, bool forecast)
		{
		if (forecast) await controller.GetWeatherForecastAsync (55.5, -3.25);
		else await controller.GetCurrentWeatherAsync (55.5, -3.25);
		}
	[TestCase (false)]
	[TestCase (true)]
	public async Task OneCallSuccess_DoesNotUseFallback (bool forecast)
		{
		using var http = new ScriptedWeather ();
		http.Reply (forecast ? Payloads.Forecast : Payloads.Current);
		using var controller = new WeatherController ("synthetic-key", http);
		await Fetch (controller, forecast);
		Assert.That (http.Requests, Has.Count.EqualTo (1));
		Assert.That (http.Requests[0].AbsolutePath, Is.EqualTo ("/data/3.0/onecall"));
		Assert.That (http.Methods.Single (), Is.EqualTo (HttpMethod.Get));
		Assert.That (http.AcceptHeaders.Single (), Does.Contain ("application/json"));
		Assert.That (http.Contents.All (content => content.Disposed), Is.True, "Responses should be disposed after reading.");
		}
	[TestCase (false, 401, "{}")]
	[TestCase (false, 403, "{}")]
	[TestCase (false, 200, "{\"cod\":401}")]
	[TestCase (false, 200, "{\"cod\":\"403\"}")]
	[TestCase (true, 401, "{}")]
	[TestCase (true, 403, "{}")]
	[TestCase (true, 200, "{\"cod\":401}")]
	[TestCase (true, 200, "{\"cod\":\"403\"}")]
	public async Task OneCallAccessDenied_UsesFreeEndpoint (bool forecast, int status, string body)
		{
		using var http = new ScriptedWeather ();
		http.Reply (body, (HttpStatusCode)status);
		http.Reply (forecast ? Payloads.FreeForecast : Payloads.FreeCurrent);
		using var controller = new WeatherController ("synthetic-key", http);
		await Fetch (controller, forecast);
		Assert.That (http.Requests, Has.Count.EqualTo (2));
		Assert.That (http.Requests[1].AbsolutePath, Is.EqualTo (forecast ? "/data/2.5/forecast" : "/data/2.5/weather"));
		Assert.That (http.Contents.All (content => content.Disposed), Is.True);
		}
	[TestCase (false, 401)]
	[TestCase (false, 403)]
	[TestCase (true, 401)]
	[TestCase (true, 403)]
	public void BothEndpointsDenyAccess_ReportsAuthenticationFailure (bool forecast, int status)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", HttpStatusCode.Unauthorized);
		http.Reply ("{}", (HttpStatusCode)status);
		using var controller = new WeatherController ("synthetic-key", http);
		Assert.ThrowsAsync<UnauthorizedAccessException> (() => Fetch (controller, forecast));
		}
	[TestCase (false, 404)]
	[TestCase (false, 429)]
	[TestCase (false, 500)]
	[TestCase (true, 404)]
	[TestCase (true, 429)]
	[TestCase (true, 500)]
	public void OtherHttpFailures_DoNotTriggerFallback (bool forecast, int status)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", (HttpStatusCode)status);
		using var controller = new WeatherController ("synthetic-key", http);
		var exception = Assert.ThrowsAsync<HttpRequestException> (() => Fetch (controller, forecast));
		Assert.That (exception!.Message, Does.Contain (status.ToString ()));
		Assert.That (http.Requests, Has.Count.EqualTo (1));
		}
	[TestCase (false)]
	[TestCase (true)]
	public void TransportFailure_IsPreserved (bool forecast)
		{
		using var http = new ScriptedWeather ();
		var failure = new HttpRequestException ("Synthetic transport failure");
		http.Throw (failure);
		using var controller = new WeatherController ("synthetic-key", http);
		Assert.That (Assert.ThrowsAsync<HttpRequestException> (() => Fetch (controller, forecast)), Is.SameAs (failure));
		}
	[TestCase (false)]
	[TestCase (true)]
	public void Cancellation_IsPropagated (bool forecast)
		{
		using var http = new ScriptedWeather ();
		http.WaitForCancellation ();
		using var controller = new WeatherController ("synthetic-key", http);
		using var cancellation = new CancellationTokenSource ();
		Task operation = forecast ? controller.GetWeatherForecastAsync (1, 2, cancellationToken: cancellation.Token) : controller.GetCurrentWeatherAsync (1, 2, cancellationToken: cancellation.Token);
		cancellation.Cancel ();
		Assert.CatchAsync<OperationCanceledException> (async () => await operation);
		Assert.That (http.Requests, Has.Count.EqualTo (1));
		}
	[TestCase ("en-US")]
	[TestCase ("fr-FR")]
	[TestCase ("de-DE")]
	public async Task CoordinatesAndQueryValues_AreCultureIndependentAndEscaped (string culture)
		{
		using var scope = new CultureScope (culture);
		using var http = new ScriptedWeather ();
		http.Reply (Payloads.Current);
		using var controller = new WeatherController ("key+&=#?", http);
		await controller.GetCurrentWeatherAsync (55.5, -3.25, "metric&extra=yes");
		var query = ScriptedWeather.Query (http.Requests.Single ());
		Assert.That (query["lat"], Is.EqualTo ("55.5"));
		Assert.That (query["lon"], Is.EqualTo ("-3.25"));
		Assert.That (query["appid"], Is.EqualTo ("key+&=#?"));
		Assert.That (query["units"], Is.EqualTo ("metric&extra=yes"));
		Assert.That (query.ContainsKey ("extra"), Is.False);
		}
	[TestCase (false, false)]
	[TestCase (false, true)]
	[TestCase (true, false)]
	[TestCase (true, true)]
	public async Task NamedLocation_UsesGeocodingThenWeather (bool forecast, bool postal)
		{
		using var http = new ScriptedWeather ();
		http.Reply (postal ? "{\"lat\":55.5,\"lon\":-3.25}" : "[{\"lat\":55.5,\"lon\":-3.25}]");
		http.Reply (forecast ? Payloads.Forecast : Payloads.Current);
		using var controller = new WeatherController ("synthetic-key", http);
		if (postal && forecast) await controller.GetWeatherForecastByPostCodeAsync ("AB1 2CD", "GB");
		else if (postal) await controller.GetCurrentWeatherByPostCodeAsync ("AB1 2CD", "GB");
		else if (forecast) await controller.GetWeatherForecastAsync ("Synthetic City", countryCode: "GB");
		else await controller.GetCurrentWeatherAsync ("Synthetic City", countryCode: "GB");
		Assert.That (http.Requests, Has.Count.EqualTo (2));
		Assert.That (http.Requests[0].AbsolutePath, Does.StartWith ("/geo/1.0/"));
		Assert.That (ScriptedWeather.Query (http.Requests[1])["lat"], Is.EqualTo ("55.5"));
		Assert.That (http.Disposed, Is.False, "Geocoding must not dispose the controller transport.");
		}
	[TestCase (false)]
	[TestCase (true)]
	public void UnknownCity_StopsBeforeWeatherRequest (bool forecast)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("[]");
		using var controller = new WeatherController ("synthetic-key", http);
		Assert.ThrowsAsync<InvalidOperationException> (async () => { if (forecast) await controller.GetWeatherForecastAsync ("Missing"); else await controller.GetCurrentWeatherAsync ("Missing"); });
		Assert.That (http.Requests, Has.Count.EqualTo (1));
		}
	[TestCase (200, true)]
	[TestCase (401, false)]
	[TestCase (403, false)]
	[TestCase (503, false)]
	public async Task CapabilityProbe_CachesItsResult (int status, bool oneCall)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", (HttpStatusCode)status);
		using var controller = new WeatherController ("synthetic-key", http);
		var first = await controller.ProbeCapabilitiesAsync ();
		Assert.That (first.Has (OpenWeatherFeatures.OneCall), Is.EqualTo (oneCall));
		Assert.That (await controller.ProbeCapabilitiesAsync (), Is.SameAs (first));
		Assert.That (http.Requests, Has.Count.EqualTo (1));
		}
	[Test]
	public async Task DisposedController_RejectsCachedCapabilityProbe ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}");
		var controller = new WeatherController ("synthetic-key", http);
		await controller.ProbeCapabilitiesAsync ();
		controller.Dispose ();
		Assert.ThrowsAsync<ObjectDisposedException> (() => controller.ProbeCapabilitiesAsync ());
		Assert.That (http.Requests, Has.Count.EqualTo (1));
		}

	[TestCase (false)]
	[TestCase (true)]
	public void MalformedSuccessResponse_IsDisposed (bool forecast)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("not-json");
		using var controller = new WeatherController ("synthetic-key", http);
		Assert.CatchAsync<Newtonsoft.Json.JsonException> (() => Fetch (controller, forecast));
		Assert.That (http.Contents.Single ().Disposed, Is.True);
		Assert.That (http.Requests, Has.Count.EqualTo (1));
		}

	[TestCase (false)]
	[TestCase (true)]
	public void DisposedController_RejectsRequests (bool forecast)
		{
		using var http = new ScriptedWeather ();
		var controller = new WeatherController ("synthetic-key", http);
		controller.Dispose ();
		controller.Dispose ();
		Assert.That (http.Disposed, Is.True);
		Assert.ThrowsAsync<ObjectDisposedException> (() => Fetch (controller, forecast));
		Assert.That (http.Requests, Is.Empty);
		}
	}