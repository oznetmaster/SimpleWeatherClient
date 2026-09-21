// Copyright (c) 2026 Neil Colvin.
// Copyright (c)2022 Ivan Gechev
// Copyright (c)2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using System.Text.Json;

namespace SimpleWeather;

/// <summary>
/// Provides high level weather APIs backed by the OpenWeather One Call endpoints.
/// </summary>
public class WeatherController : IDisposable
	{
	private readonly string _oneCallBaseUrl = "https://api.openweathermap.org/data/3.0/";
	private const string OneCall4BaseUrl = "https://api.openweathermap.org/data/4.0/onecall/";
	private readonly string _currentWeatherBaseUrl = "https://api.openweathermap.org/data/2.5/";
	private readonly string? _apiKey;
	private HttpClient? _client;
	private readonly HttpMessageHandler _handler;
	private OpenWeatherCapabilities? _capabilities;

	/// <summary>The service selected for this controller. Other controllers using the same key are independent.</summary>
	public OpenWeatherService Service { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="WeatherController"/> class.
	/// </summary>
	/// <param name="apiKey">Optional OpenWeather API key used for authenticated requests.</param>
	public WeatherController (string? apiKey = null) : this (apiKey, new HttpClientHandler ())
		{
		}

	/// <summary>Creates a client for an explicitly selected service, or automatic fallback.</summary>
	/// <param name="apiKey">The account key. The same key can be used by controllers selecting different services.</param>
	/// <param name="service">Service selection. Explicit selections never fall back to another service.</param>
	public WeatherController (string? apiKey, OpenWeatherService service) : this (apiKey, new HttpClientHandler (), service)
		{
		}

	internal WeatherController (string? apiKey, HttpMessageHandler handler, OpenWeatherService service = OpenWeatherService.Automatic)
		{
		if (service is not (OpenWeatherService.Automatic or OpenWeatherService.OneCall3 or OpenWeatherService.OneCall4 or OpenWeatherService.Free))
			throw new ArgumentOutOfRangeException (nameof (service));
		Service = service;
		_apiKey = apiKey;

		_handler = handler ?? throw new ArgumentNullException (nameof (handler));
		_client = new HttpClient (handler);
		_client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));
		_client.Timeout = TimeSpan.FromMilliseconds (5000);
		}

	/// <summary>
	/// Returns an object that contains all the relevant data for the current weather in a given city.
	/// </summary>
	/// <param name="cityName">The name of the city</param>
	/// <param name="stateCode">The state code (optional, for US cities)</param>
	/// <param name="countryCode">The country code (optional, e.g. "US", "GB")</param>
	/// <param name="units">Units of measurement. Standard, metric, and imperial units are available. Default is "metric"</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A <see cref="CurrentWeather"/> object for the specified city</returns>
	public async Task<CurrentWeather> GetCurrentWeatherAsync (string cityName, string? stateCode = null, string? countryCode = null, string units = "metric", CancellationToken cancellationToken = default)
		{
		try
			{
			var geoLocator = CreateGeoLocator ();
			LatLong? latlong = await geoLocator.GetCoordinatesByCityNameAsync (cityName, stateCode, countryCode, cancellationToken).ConfigureAwait (false)
					?? throw new InvalidOperationException ($"Could not find coordinates for city: {cityName}, {stateCode}, {countryCode}");
			return await GetCurrentWeatherAsync (latlong.Value, units, cancellationToken).ConfigureAwait (false);
			}
		catch (Exception ex) when (IsAuthFailure (ex))
			{
			throw new UnauthorizedAccessException ("Invalid OpenWeather API key or the key does not have access to required endpoints.", ex);
			}
		}

	/// <summary>
	/// Returns an object that contains all the relevant data for the current weather at the specified latitude and longitude.
	/// </summary>
	/// <param name="lat">Latitude of the location</param>
	/// <param name="lon">Longitude of the location</param>
	/// <param name="units">Units of measurement. Standard, metric, and imperial units are available. Default is "metric"</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A <see cref="CurrentWeather"/> object for the specified coordinates</returns>
	public async Task<CurrentWeather> GetCurrentWeatherAsync (double lat, double lon, string units = "metric", CancellationToken cancellationToken = default)
		{
		if (Service == OpenWeatherService.Free)
			return new CurrentWeather (await ReadFreeAsync ("weather", lat, lon, units, cancellationToken).ConfigureAwait (false));
		if (Service == OpenWeatherService.OneCall4)
			return await ReadOneCall4CurrentAsync (lat, lon, units, cancellationToken).ConfigureAwait (false);
		var oneCallUri = new Uri (FormattableString.Invariant ($"{_oneCallBaseUrl}onecall?lat={lat}&lon={lon}&exclude=minutely,hourly,daily&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));
		var currentUri = new Uri (FormattableString.Invariant ($"{_currentWeatherBaseUrl}weather?lat={lat}&lon={lon}&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));

		(ResponseInfo oneCallResponse, var oneCallContent) = await GetAsyncWithContent (oneCallUri, cancellationToken).ConfigureAwait (false);
		if (oneCallResponse.IsSuccessStatusCode && !IsAuthFailure (oneCallResponse.StatusCode, oneCallContent))
			{
			return new CurrentWeather (oneCallContent);
			}

		// New subscriptions may only have One Call 4.0 access. Keep existing 3.0 accounts working.
		if (IsAuthFailure (oneCallResponse.StatusCode, oneCallContent))
			{
			RequireAutomaticFallback ();
			OneCall4Response<InstantResponse>? version4 = await TryGetOneCall4Async<InstantResponse> (CreateOneCall4Uri ("current", lat, lon, units), cancellationToken).ConfigureAwait (false);
			if (version4 != null)
				{
				return MapOneCall4Current (version4);
				}

			(ResponseInfo currentResponse, var currentContent) = await GetAsyncWithContent (currentUri, cancellationToken).ConfigureAwait (false);
			if (currentResponse.IsSuccessStatusCode && !IsAuthFailure (currentResponse.StatusCode, currentContent))
				{
				return new CurrentWeather (currentContent);
				}

			if (IsAuthFailure (currentResponse.StatusCode, currentContent))
				{
				throw new UnauthorizedAccessException ("Invalid OpenWeather API key or the key does not have access to required endpoints.");
				}

			throw new HttpRequestException ($"Error fetching current weather data: {(int)currentResponse.StatusCode} {currentResponse.ReasonPhrase}");
			}

		throw new HttpRequestException ($"Error fetching current weather data: {(int)oneCallResponse.StatusCode} {oneCallResponse.ReasonPhrase}");
		}

	/// <summary>
	/// Asynchronously gets the current weather for the specified coordinates.
	/// </summary>
	/// <param name="coordinates">The coordinates of the location</param>
	/// <param name="units">Units of measurement. Standard, metric, and imperial units are available. Default is "metric"</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A task representing the asynchronous operation whose result is a <see cref="CurrentWeather"/> for the specified coordinates.</returns>
	public Task<CurrentWeather> GetCurrentWeatherAsync (LatLong coordinates, string units = "metric", CancellationToken cancellationToken = default) =>
		 GetCurrentWeatherAsync (coordinates.Latitude, coordinates.Longitude, units, cancellationToken);

	/// <summary>
	/// Returns an object that contains all the relevant weather forecast data for a given city.
	/// </summary>
	/// <param name="cityName">The name of the city</param>
	/// <param name="stateCode">The state code (optional, for US cities)</param>
	/// <param name="countryCode">The country code (optional, e.g. "US", "GB")</param>
	/// <param name="units">Units of measurement. Standard, metric, and imperial units are available. Default is "metric"</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A <see cref="WeatherForecast"/> object for the specified city</returns>
	public Task<WeatherForecast> GetWeatherForecastAsync (string cityName, string? stateCode = null, string? countryCode = null, string units = "metric", CancellationToken cancellationToken = default) =>
		GetWeatherForecastAsync (cityName, true, stateCode, countryCode, units, cancellationToken);

	/// <summary>Gets a city forecast, optionally omitting hourly data and its One Call 4.0 requests.</summary>
	public async Task<WeatherForecast> GetWeatherForecastAsync (string cityName, bool includeHourly, string? stateCode = null, string? countryCode = null, string units = "metric", CancellationToken cancellationToken = default)
		{
		try
			{
			var geoLocator = CreateGeoLocator ();
			LatLong? latlong = await geoLocator.GetCoordinatesByCityNameAsync (cityName, stateCode, countryCode, cancellationToken).ConfigureAwait (false)
					?? throw new InvalidOperationException ($"Could not find coordinates for city: {cityName}, {stateCode}, {countryCode}");
			return await GetWeatherForecastAsync (latlong.Value.Latitude, latlong.Value.Longitude, includeHourly, units, cancellationToken).ConfigureAwait (false);
			}
		catch (Exception ex) when (IsAuthFailure (ex))
			{
			throw new UnauthorizedAccessException ("Invalid OpenWeather API key or the key does not have access to required endpoints.", ex);
			}
		}

	/// <summary>
	/// Returns an object that contains all the relevant weather forecast data for a given latitude and longitude.
	/// </summary>
	/// <param name="lat">Latitude of the location</param>
	/// <param name="lon">Longitude of the location</param>
	/// <param name="units">Units of measurement. Standard, metric, and imperial units are available. Default is "metric"</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A <see cref="WeatherForecast"/> object for the specified coordinates</returns>
	public Task<WeatherForecast> GetWeatherForecastAsync (double lat, double lon, string units = "metric", CancellationToken cancellationToken = default) =>
		GetWeatherForecastAsync (lat, lon, true, units, cancellationToken);

	/// <summary>Gets a coordinate forecast, optionally omitting hourly data and its One Call 4.0 requests.</summary>
	public async Task<WeatherForecast> GetWeatherForecastAsync (double lat, double lon, bool includeHourly, string units = "metric", CancellationToken cancellationToken = default)
		{
		if (Service == OpenWeatherService.Free)
			return new WeatherForecast (ResponseJson.Read<WeatherResponse> (await ReadFreeAsync ("forecast", lat, lon, units, cancellationToken).ConfigureAwait (false)), includeHourly);
		if (Service == OpenWeatherService.OneCall4)
			return await TryGetOneCall4ForecastAsync (lat, lon, units, cancellationToken, includeHourly).ConfigureAwait (false)
				?? throw AccessDenied ("One Call 4.0");
		string exclude = includeHourly ? "current,minutely,alerts" : "current,minutely,alerts,hourly";
		var oneCallUri = new Uri (FormattableString.Invariant ($"{_oneCallBaseUrl}onecall?lat={lat}&lon={lon}&exclude={exclude}&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));
		var forecast5Uri = new Uri (FormattableString.Invariant ($"{_currentWeatherBaseUrl}forecast?lat={lat}&lon={lon}&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));

		(ResponseInfo oneCallResponse, var oneCallContent) = await GetAsyncWithContent (oneCallUri, cancellationToken).ConfigureAwait (false);
		if (oneCallResponse.IsSuccessStatusCode && !IsAuthFailure (oneCallResponse.StatusCode, oneCallContent))
			{
			return new WeatherForecast (ResponseJson.Read<WeatherResponse> (oneCallContent), includeHourly);
			}

		if (IsAuthFailure (oneCallResponse.StatusCode, oneCallContent))
			{
			RequireAutomaticFallback ();
			WeatherForecast? version4 = await TryGetOneCall4ForecastAsync (lat, lon, units, cancellationToken, includeHourly).ConfigureAwait (false);
			if (version4 != null) return version4;

			(ResponseInfo forecastResponse, var forecastContent) = await GetAsyncWithContent (forecast5Uri, cancellationToken).ConfigureAwait (false);
			if (forecastResponse.IsSuccessStatusCode && !IsAuthFailure (forecastResponse.StatusCode, forecastContent))
				{
				return new WeatherForecast (ResponseJson.Read<WeatherResponse> (forecastContent), includeHourly);
				}

			if (IsAuthFailure (forecastResponse.StatusCode, forecastContent))
				{
				throw new UnauthorizedAccessException ("Invalid OpenWeather API key or the key does not have access to required endpoints.");
				}

			throw new HttpRequestException ($"Error fetching weather forecast data: {(int)forecastResponse.StatusCode} {forecastResponse.ReasonPhrase}");
			}

		throw new HttpRequestException ($"Error fetching weather forecast data: {(int)oneCallResponse.StatusCode} {oneCallResponse.ReasonPhrase}");
		}

	/// <summary>
	/// Asynchronously gets the weather forecast for the specified coordinates.
	/// </summary>
	/// <param name="coordinates">The coordinates of the location</param>
	/// <param name="units">Units of measurement. Standard, metric, and imperial units are available. Default is "metric"</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A task representing the asynchronous operation whose result is a <see cref="WeatherForecast"/> for the specified coordinates.</returns>
	public Task<WeatherForecast> GetWeatherForecastAsync (LatLong coordinates, string units = "metric", CancellationToken cancellationToken = default) =>
		GetWeatherForecastAsync (coordinates.Latitude, coordinates.Longitude, units, cancellationToken);

	/// <summary>
	/// Returns an object that contains all the relevant data for the current weather at the specified postal/zip code and country code.
	/// </summary>
	/// <param name="postCode">The postal or zip code</param>
	/// <param name="countryCode">The country code (e.g. "US", "GB")</param>
	/// <param name="units">Units of measurement. Standard, metric, and imperial units are available. Default is "metric"</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A <see cref="CurrentWeather"/> object for the specified postal/zip code</returns>
	public async Task<CurrentWeather> GetCurrentWeatherByPostCodeAsync (string postCode, string countryCode, string units = "metric", CancellationToken cancellationToken = default)
		{
		var geoLocator = CreateGeoLocator ();
		LatLong? latlong = await geoLocator.GetCoordinatesByPostCodeAsync (postCode, countryCode, cancellationToken).ConfigureAwait (false) 
				?? throw new InvalidOperationException ($"Could not find coordinates for postal/zip code: {postCode}, {countryCode}");
		return await GetCurrentWeatherAsync (latlong.Value, units, cancellationToken).ConfigureAwait (false);
		}

	/// <summary>
	/// Returns an object that contains all the relevant weather forecast data for the specified postal/zip code and country code.
	/// </summary>
	/// <param name="postCode">The postal or zip code</param>
	/// <param name="countryCode">The country code (e.g. "US", "GB")</param>
	/// <param name="units">Units of measurement. Standard, metric, and imperial units are available. Default is "metric"</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A <see cref="WeatherForecast"/> object for the specified postal/zip code</returns>
	public async Task<WeatherForecast> GetWeatherForecastByPostCodeAsync (string postCode, string countryCode, string units = "metric", CancellationToken cancellationToken = default)
		{
		var geoLocator = CreateGeoLocator ();
		LatLong? latlong = await geoLocator.GetCoordinatesByPostCodeAsync (postCode, countryCode, cancellationToken).ConfigureAwait (false) 
				?? throw new InvalidOperationException ($"Could not find coordinates for postal/zip code: {postCode}, {countryCode}");
		return await GetWeatherForecastAsync (latlong.Value, units, cancellationToken).ConfigureAwait (false);
		}

	/// <summary>
	/// Disposes the underlying <see cref="HttpClient"/> instance and suppresses finalization.
	/// </summary>
	public void Dispose ()
		{
        // Dispose of the HttpClient instance if it is not null
        _client?.Dispose ();
        _client = null;
        // Additional cleanup can be added here if necessary
        GC.SuppressFinalize (this);
		}

	/// <summary>
	/// Probes the available capabilities of the OpenWeather API for the configured API key.
	/// </summary>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A task representing the asynchronous operation whose result is an <see cref="OpenWeatherCapabilities"/> instance indicating the available capabilities.</returns>
	public async Task<OpenWeatherCapabilities> ProbeCapabilitiesAsync (CancellationToken cancellationToken = default)
		{
		ThrowIfDisposed ();
		if (Service == OpenWeatherService.Free)
			{
			if (_capabilities != null) return _capabilities;
			_ = await ReadFreeAsync ("weather", 0, 0, "metric", cancellationToken).ConfigureAwait (false);
			return _capabilities = OpenWeatherCapabilities.Free;
			}
		if (Service == OpenWeatherService.OneCall4)
			{
			if (_capabilities != null) return _capabilities;
			_ = await ReadOneCall4CurrentAsync (0, 0, "metric", cancellationToken).ConfigureAwait (false);
			return _capabilities = OpenWeatherCapabilities.PaidOneCall;
			}
		if (_capabilities != null)
			{
			return _capabilities;
			}

		// Prefer One Call when available.
		var probeLat = 0d;
		var probeLon = 0d;
		var oneCallUri = new Uri (FormattableString.Invariant ($"{_oneCallBaseUrl}onecall?lat={probeLat}&lon={probeLon}&exclude=minutely&appid={Uri.EscapeDataString (_apiKey ?? "")}&units=metric"));
		(ResponseInfo resp, var body) = await GetAsyncWithContent (oneCallUri, cancellationToken).ConfigureAwait (false);

		if (resp.IsSuccessStatusCode && !IsAuthFailure (resp.StatusCode, body))
			{
			_capabilities = OpenWeatherCapabilities.PaidOneCall;
			return _capabilities;
			}

		if (IsAuthFailure (resp.StatusCode, body))
			{
			RequireAutomaticFallback ();
			var version4Uri = CreateOneCall4Uri ("current", probeLat, probeLon, "metric");
			(ResponseInfo version4Response, string version4Body) = await GetAsyncWithContent (version4Uri, cancellationToken).ConfigureAwait (false);
			if (version4Response.IsSuccessStatusCode && !IsAuthFailure (version4Response.StatusCode, version4Body))
				{
				_capabilities = OpenWeatherCapabilities.PaidOneCall;
				return _capabilities;
				}
			_capabilities = OpenWeatherCapabilities.Free;
			return _capabilities;
			}

		// Unknown (network/proxy/etc). Default to Free-like constraints so UIs can degrade.
		if (Service == OpenWeatherService.OneCall3)
			throw new HttpRequestException ($"Error probing One Call 3.0: {(int)resp.StatusCode} {resp.ReasonPhrase}");
		_capabilities = OpenWeatherCapabilities.Free;
		return _capabilities;
		}

	private Uri CreateOneCall4Uri (string endpoint, double lat, double lon, string units) =>
		new (FormattableString.Invariant ($"{OneCall4BaseUrl}{endpoint}?lat={lat}&lon={lon}&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));

	/// <summary>
	/// Retrieves current conditions and forecast together. Prefers existing One Call 3.0 access,
	/// then tries 4.0, then the free endpoints when subscription access is denied.
	/// </summary>
	/// <param name="coordinates">The location to query.</param>
	/// <param name="includeHourly">Whether to request hourly forecasts. Set false to avoid hourly pagination charges in 4.0.</param>
	/// <param name="units">OpenWeather units: metric, imperial or standard.</param>
	/// <param name="cancellationToken">Cancels requests, including pagination.</param>
	/// <returns>Current weather and forecast. The 3.0 route uses one request; 4.0 normally uses two without hourly data or five with it, plus the denied 3.0 probe.</returns>
	public async Task<WeatherSnapshot> GetWeatherSnapshotAsync (LatLong coordinates, bool includeHourly = true, string units = "metric", CancellationToken cancellationToken = default)
		{
		double lat = coordinates.Latitude;
		double lon = coordinates.Longitude;
		if (Service == OpenWeatherService.Free)
			return await ReadFreeSnapshotAsync (lat, lon, units, includeHourly, cancellationToken).ConfigureAwait (false);
		if (Service != OpenWeatherService.OneCall4)
			{
			string exclude = includeHourly ? "minutely,alerts" : "minutely,alerts,hourly";
			var uri = new Uri (FormattableString.Invariant ($"{_oneCallBaseUrl}onecall?lat={lat}&lon={lon}&exclude={exclude}&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));
			(ResponseInfo response, string content) = await GetAsyncWithContent (uri, cancellationToken).ConfigureAwait (false);
			if (response.IsSuccessStatusCode && !IsAuthFailure (response.StatusCode, content))
				{
				WeatherResponse data = ResponseJson.Read<WeatherResponse> (content);
				return new WeatherSnapshot (new CurrentWeather (data), new WeatherForecast (data, includeHourly));
				}
			if (!IsAuthFailure (response.StatusCode, content))
				throw new HttpRequestException ($"Error fetching weather snapshot: {(int)response.StatusCode} {response.ReasonPhrase}");
			RequireAutomaticFallback ();
			}

		WeatherForecast? forecast = await TryGetOneCall4ForecastAsync (lat, lon, units, cancellationToken, includeHourly).ConfigureAwait (false);
		if (forecast != null)
			{
			return new WeatherSnapshot (await ReadOneCall4CurrentAsync (lat, lon, units, cancellationToken).ConfigureAwait (false), forecast);
			}
		if (Service == OpenWeatherService.OneCall4) throw AccessDenied ("One Call 4.0");
		return await ReadFreeSnapshotAsync (lat, lon, units, includeHourly, cancellationToken).ConfigureAwait (false);
		}

	private async Task<WeatherSnapshot> ReadFreeSnapshotAsync (double lat, double lon, string units, bool includeHourly, CancellationToken cancellationToken)
		{
		var forecast = new WeatherForecast (await ReadFreeAsync ("forecast", lat, lon, units, cancellationToken).ConfigureAwait (false));
		if (!includeHourly) forecast.Hourly.Clear ();
		return new WeatherSnapshot (new CurrentWeather (await ReadFreeAsync ("weather", lat, lon, units, cancellationToken).ConfigureAwait (false)), forecast);
		}

	private async Task<string> ReadFreeAsync (string endpoint, double lat, double lon, string units, CancellationToken cancellationToken)
		{
		var uri = new Uri (FormattableString.Invariant ($"{_currentWeatherBaseUrl}{endpoint}?lat={lat}&lon={lon}&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));
		(ResponseInfo response, string body) = await GetAsyncWithContent (uri, cancellationToken).ConfigureAwait (false);
		if (IsAuthFailure (response.StatusCode, body)) throw AccessDenied ("free current-weather/forecast service");
		if (!response.IsSuccessStatusCode) throw new HttpRequestException ($"Error fetching free weather data: {(int)response.StatusCode} {response.ReasonPhrase}");
		return body;
		}

	private async Task<CurrentWeather> ReadOneCall4CurrentAsync (double lat, double lon, string units, CancellationToken cancellationToken)
		{
		OneCall4Response<InstantResponse> data = await TryGetOneCall4Async<InstantResponse> (CreateOneCall4Uri ("current", lat, lon, units), cancellationToken).ConfigureAwait (false)
			?? throw AccessDenied ("One Call 4.0");
		return MapOneCall4Current (data);
		}

	private static CurrentWeather MapOneCall4Current (OneCall4Response<InstantResponse> data)
		{
		InstantResponse[] readings = GetOneCall4Data (data);
		if (readings.Length != 1 || readings[0] == null) throw new InvalidDataException ("One Call 4.0 current weather must contain one reading.");
		return new CurrentWeather (new WeatherResponse
			{
			Latitude = data.Latitude, Longitude = data.Longitude,
			Timezone = data.Timezone, TimezoneOffset = data.TimezoneOffset,
			Current = readings[0]
			});
		}

	private static UnauthorizedAccessException AccessDenied (string service) =>
		new ($"OpenWeather {service} access was denied. Check the account subscription and API key.");

	private void RequireAutomaticFallback ()
		{
		if (Service != OpenWeatherService.Automatic) throw AccessDenied ("One Call 3.0");
		}

	private async Task<OneCall4Response<T>?> TryGetOneCall4Async<T> (Uri uri, CancellationToken cancellationToken)
		{
		(ResponseInfo response, string content) = await GetAsyncWithContent (uri, cancellationToken).ConfigureAwait (false);
		if (IsAuthFailure (response.StatusCode, content)) return null;
		if (!response.IsSuccessStatusCode)
			throw new HttpRequestException ($"Error fetching One Call 4.0 weather data: {(int)response.StatusCode} {response.ReasonPhrase}");
		try { return ResponseJson.Read<OneCall4Response<T>> (content); }
		catch (JsonException exception) { throw new InvalidDataException ("One Call 4.0 returned an invalid weather response.", exception); }
		}

	private static T[] GetOneCall4Data<T> (OneCall4Response<T> response) =>
		response.Data ?? throw new InvalidDataException ("One Call 4.0 response does not contain a data array.");

	private async Task<WeatherForecast?> TryGetOneCall4ForecastAsync (double lat, double lon, string units, CancellationToken cancellationToken, bool includeHourly = true)
		{
		Uri dailyUri = CreateOneCall4Uri ("timeline/1day", lat, lon, units);
		OneCall4Response<DailyResponse>? daily = await TryGetOneCall4Async<DailyResponse> (dailyUri, cancellationToken).ConfigureAwait (false);
		if (daily == null) return null;

		// Match the existing forecast contract: up to eight daily and 48 hourly records.
		DailyResponse[] days = await ReadOneCall4TimelineAsync (dailyUri, daily, 8, cancellationToken).ConfigureAwait (false);
		InstantResponse[] hours = [];
		if (includeHourly)
			{
			Uri hourlyUri = CreateOneCall4Uri ("timeline/1h", lat, lon, units);
			OneCall4Response<InstantResponse>? hourly = await TryGetOneCall4Async<InstantResponse> (hourlyUri, cancellationToken).ConfigureAwait (false);
			if (hourly == null) return null;
			hours = await ReadOneCall4TimelineAsync (hourlyUri, hourly, 48, cancellationToken).ConfigureAwait (false);
			}
		var combined = new WeatherResponse
			{
			Latitude = daily.Latitude, Longitude = daily.Longitude,
			Timezone = daily.Timezone, TimezoneOffset = daily.TimezoneOffset,
			Daily = days, Hourly = hours
			};
		return new WeatherForecast (combined);
		}

	private async Task<T[]> ReadOneCall4TimelineAsync<T> (Uri originalUri, OneCall4Response<T> page, int recordLimit, CancellationToken cancellationToken) where T : class, ITimedResponse
		{
		var records = new List<T> ();
		long? lastTimestamp = null;
		var visited = new HashSet<string> (StringComparer.Ordinal) { originalUri.AbsoluteUri };
		while (true)
			{
			T[] data = GetOneCall4Data (page);
			foreach (T record in data)
				{
				if (record?.DT == null || record.DT.Value != Math.Truncate (record.DT.Value) || record.DT.Value < long.MinValue || record.DT.Value >= 9223372036854775808d)
					throw new InvalidDataException ("One Call 4.0 timeline record is missing its timestamp.");
				long timestamp = (long)record.DT.Value;
				if (lastTimestamp.HasValue && timestamp <= lastTimestamp.Value)
					throw new InvalidDataException ("One Call 4.0 timeline did not advance.");
				lastTimestamp = timestamp;
				records.Add (record);
				if (records.Count == recordLimit) return records.ToArray ();
				}

			string? next = page.Next;
			if (string.IsNullOrWhiteSpace (next)) return records.ToArray ();
			if (data.Length == 0) throw new InvalidDataException ("One Call 4.0 returned an empty page with a continuation.");
			if (!Uri.TryCreate (next, UriKind.Absolute, out Uri? nextUri) ||
				(nextUri.Scheme != Uri.UriSchemeHttps && nextUri.Scheme != Uri.UriSchemeHttp) ||
				nextUri.Host != originalUri.Host || !nextUri.IsDefaultPort ||
				nextUri.AbsolutePath != originalUri.AbsolutePath || !string.IsNullOrEmpty (nextUri.UserInfo))
				throw new InvalidDataException ("One Call 4.0 returned an invalid timeline continuation.");

			// Provider links may omit units. Preserve the caller's location, units and key on every page.
			var query = ParseQuery (nextUri.Query);
			foreach (KeyValuePair<string, string> pair in ParseQuery (originalUri.Query)) query[pair.Key] = pair.Value;
			var builder = new UriBuilder (nextUri)
				{
				// The live service returns HTTP continuation links; never send the key over HTTP.
				Scheme = Uri.UriSchemeHttps,
				Port = -1,
				Query = string.Join ("&", query.Select (pair => Uri.EscapeDataString (pair.Key) + "=" + Uri.EscapeDataString (pair.Value))),
				Fragment = string.Empty
				};
			if (!visited.Add (builder.Uri.AbsoluteUri)) throw new InvalidDataException ("One Call 4.0 repeated a timeline continuation.");
			page = await TryGetOneCall4Async<T> (builder.Uri, cancellationToken).ConfigureAwait (false)
				?? throw new UnauthorizedAccessException ("One Call 4.0 access was denied while reading a forecast page.");
			}
		}

	private static Dictionary<string, string> ParseQuery (string query)
		{
		var values = new Dictionary<string, string> (StringComparer.Ordinal);
		foreach (string part in query.TrimStart ('?').Split ('&'))
			{
			string[] pair = part.Split (['='], 2);
			if (pair.Length == 2) values[Uri.UnescapeDataString (pair[0])] = Uri.UnescapeDataString (pair[1]);
			}
		return values;
		}

	private GeoLocator CreateGeoLocator ()
		{
		ThrowIfDisposed ();
		return new GeoLocator (_apiKey, () => new HttpClient (_handler, false));
		}

	private async Task<(ResponseInfo Response, string Content)> GetAsyncWithContent (Uri uri, CancellationToken cancellationToken)
		{
		ThrowIfDisposed ();
		using HttpResponseMessage response = await _client!.GetAsync (uri, cancellationToken).ConfigureAwait (false);
		var content = await ReadContentAsStringAsync (response.Content, cancellationToken).ConfigureAwait (false);
		return (new ResponseInfo (response.StatusCode, response.ReasonPhrase), content);
		}

	private void ThrowIfDisposed ()
		{
#if NET10_0_OR_GREATER
		ObjectDisposedException.ThrowIf (_client == null, this);
#else
		if (_client == null)
			throw new ObjectDisposedException (nameof (WeatherController));
#endif
		}

	private sealed class ResponseInfo (HttpStatusCode statusCode, string? reasonPhrase)
		{
		internal HttpStatusCode StatusCode { get; } = statusCode;
		internal string? ReasonPhrase { get; } = reasonPhrase;
		internal bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;
		}

	private static Task<string> ReadContentAsStringAsync (HttpContent content, CancellationToken cancellationToken)
		{
#if NET10_0_OR_GREATER
		return content.ReadAsStringAsync (cancellationToken);
#else
		_ = cancellationToken;
		return content.ReadAsStringAsync ();
#endif
		}

	private static bool IsAuthFailure (Exception ex)
		{
		if (ex is UnauthorizedAccessException)
			{
			return true;
			}

		if (ex is HttpRequestException hre)
			{
#if NET10_0_OR_GREATER
			if (hre.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
				{
				return true;
				}
#endif
			var msg = hre.Message;
			return msg.Contains (" 401 ") || msg.EndsWith (" 401", StringComparison.Ordinal) ||
				msg.Contains (" 403 ") || msg.EndsWith (" 403", StringComparison.Ordinal) ||
				msg.Contains ("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
				msg.Contains ("Forbidden", StringComparison.OrdinalIgnoreCase);
			}

		return false;
		}

	private static bool IsAuthFailure (HttpStatusCode statusCode, string body)
		{
		if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
			{
			return true;
			}

		try
			{
			var codToken = ResponseJson.Read<ErrorResponse> (body).Code;
			return codToken is "401" or "403";
			}
		catch
			{
			return false;
			}
		}
	}
