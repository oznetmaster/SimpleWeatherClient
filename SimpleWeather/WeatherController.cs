// Copyright (c) 2026 Neil Colvin.
// Copyright (c)2022 Ivan Gechev
// Copyright (c)2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWeather;

/// <summary>
/// Provides high level weather APIs backed by the OpenWeather One Call endpoints.
/// </summary>
public class WeatherController : IDisposable
	{
	private readonly string _oneCallBaseUrl = "https://api.openweathermap.org/data/3.0/";
	private readonly string _currentWeatherBaseUrl = "https://api.openweathermap.org/data/2.5/";
	private readonly string? _apiKey;
	private HttpClient? _client;
	private readonly HttpMessageHandler _handler;
	private OpenWeatherCapabilities? _capabilities;

	/// <summary>
	/// Initializes a new instance of the <see cref="WeatherController"/> class.
	/// </summary>
	/// <param name="apiKey">Optional OpenWeather API key used for authenticated requests.</param>
	public WeatherController (string? apiKey = null) : this (apiKey, new HttpClientHandler ())
		{
		}

	internal WeatherController (string? apiKey, HttpMessageHandler handler)
		{
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
		var oneCallUri = new Uri (FormattableString.Invariant ($"{_oneCallBaseUrl}onecall?lat={lat}&lon={lon}&exclude=minutely,hourly,daily&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));
		var currentUri = new Uri (FormattableString.Invariant ($"{_currentWeatherBaseUrl}weather?lat={lat}&lon={lon}&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));

		(ResponseInfo oneCallResponse, var oneCallContent) = await GetAsyncWithContent (oneCallUri, cancellationToken).ConfigureAwait (false);
		if (oneCallResponse.IsSuccessStatusCode && !IsAuthFailure (oneCallResponse.StatusCode, oneCallContent))
			{
			return new CurrentWeather (oneCallContent);
			}

		// If the key does not have access to One Call, fall back to the free current weather endpoint.
		if (IsAuthFailure (oneCallResponse.StatusCode, oneCallContent))
			{
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
	public async Task<WeatherForecast> GetWeatherForecastAsync (string cityName, string? stateCode = null, string? countryCode = null, string units = "metric", CancellationToken cancellationToken = default)
		{
		try
			{
			var geoLocator = CreateGeoLocator ();
			LatLong? latlong = await geoLocator.GetCoordinatesByCityNameAsync (cityName, stateCode, countryCode, cancellationToken).ConfigureAwait (false)
					?? throw new InvalidOperationException ($"Could not find coordinates for city: {cityName}, {stateCode}, {countryCode}");
			return await GetWeatherForecastAsync (latlong.Value, units, cancellationToken).ConfigureAwait (false);
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
	public async Task<WeatherForecast> GetWeatherForecastAsync (double lat, double lon, string units = "metric", CancellationToken cancellationToken = default)
		{
		var oneCallUri = new Uri (FormattableString.Invariant ($"{_oneCallBaseUrl}onecall?lat={lat}&lon={lon}&exclude=current,minutely,alerts&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));
		var forecast5Uri = new Uri (FormattableString.Invariant ($"{_currentWeatherBaseUrl}forecast?lat={lat}&lon={lon}&appid={Uri.EscapeDataString (_apiKey ?? "")}&units={Uri.EscapeDataString (units)}"));

		(ResponseInfo oneCallResponse, var oneCallContent) = await GetAsyncWithContent (oneCallUri, cancellationToken).ConfigureAwait (false);
		if (oneCallResponse.IsSuccessStatusCode && !IsAuthFailure (oneCallResponse.StatusCode, oneCallContent))
			{
			return new WeatherForecast (oneCallContent);
			}

		if (IsAuthFailure (oneCallResponse.StatusCode, oneCallContent))
			{
			(ResponseInfo forecastResponse, var forecastContent) = await GetAsyncWithContent (forecast5Uri, cancellationToken).ConfigureAwait (false);
			if (forecastResponse.IsSuccessStatusCode && !IsAuthFailure (forecastResponse.StatusCode, forecastContent))
				{
				return new WeatherForecast (forecastContent);
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
			_capabilities = OpenWeatherCapabilities.Free;
			return _capabilities;
			}

		// Unknown (network/proxy/etc). Default to Free-like constraints so UIs can degrade.
		_capabilities = OpenWeatherCapabilities.Free;
		return _capabilities;
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
			var json = Newtonsoft.Json.Linq.JObject.Parse (body);
			var codToken = json.SelectToken ("cod")?.ToString ();
			return codToken is "401" or "403";
			}
		catch
			{
			return false;
			}
		}
	}
