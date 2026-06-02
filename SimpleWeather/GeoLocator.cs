// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace SimpleWeather;

/// <summary>
/// Provides helper methods for interacting with the OpenWeather geocoding APIs.
/// </summary>
/// <param name="apiKey">Optional API key used when issuing HTTP requests.</param>
public class GeoLocator (string? apiKey = null)
	{
	private const string BASE_URL = $"https://api.openweathermap.org/geo/1.0/";

	private static Task<string> ReadContentAsStringAsync (HttpContent content, CancellationToken cancellationToken)
		{
#if NET10_0_OR_GREATER
		return content.ReadAsStringAsync (cancellationToken);
#else
		_ = cancellationToken;
		return content.ReadAsStringAsync ();
#endif
		}

	// Replace all instances of 'throw new Exception' with 'throw new HttpRequestException' for API call failures

	/// <summary>
	/// Retrieves up to five matching cities based on name, state, and country filters.
	/// </summary>
	/// <param name="cityName">The city name to look up.</param>
	/// <param name="stateCode">Optional ISO state code used for disambiguation.</param>
	/// <param name="countryCode">Optional ISO country code used for disambiguation.</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A list of raw city payloads returned by the API.</returns>
	public async Task<List<string>> GetCitiesByNameAsync (string cityName, string? stateCode = null, string? countryCode = null, CancellationToken cancellationToken = default)
		{
		if (string.IsNullOrWhiteSpace (cityName))
			{
			throw new ArgumentException ("City name cannot be null or empty.", nameof (cityName));
			}

		var query = cityName;
		if (!string.IsNullOrWhiteSpace (stateCode))
			{
			query += $",{stateCode}";
			}

		if (!string.IsNullOrWhiteSpace (countryCode))
			{
			query += $",{countryCode}";
			}

		var baseAddress = new Uri ($"{BASE_URL}direct?q={query}&limit=5&appid={apiKey}");
		using var client = new HttpClient ();
		HttpResponseMessage response = await client.GetAsync (baseAddress, cancellationToken).ConfigureAwait (false);
		if (!response.IsSuccessStatusCode)
			{
#if NET10_0_OR_GREATER
			throw new HttpRequestException ($"Error fetching data from OpenWeatherMap API: {response.ReasonPhrase}", null, response.StatusCode);
#else
			throw new HttpRequestException ($"Error fetching data from OpenWeatherMap API: {(int)response.StatusCode} {response.ReasonPhrase}");
#endif
			}

		var jsonContent = await ReadContentAsStringAsync (response.Content, cancellationToken).ConfigureAwait (false);
		List<string>? cities = JsonConvert.DeserializeObject<List<string>> (jsonContent);
		return cities ?? [];
		}

	/// <summary>
	/// Retrieves the first set of coordinates that matches the supplied city filters.
	/// </summary>
	/// <param name="cityName">The city name to look up.</param>
	/// <param name="stateCode">Optional ISO state code used for disambiguation.</param>
	/// <param name="countryCode">Optional ISO country code used for disambiguation.</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A <see cref="LatLong"/> when a city is found; otherwise <c>null</c>.</returns>
	public async Task<LatLong?> GetCoordinatesByCityNameAsync (string cityName, string? stateCode = null, string? countryCode = null, CancellationToken cancellationToken = default)
		{
		if (string.IsNullOrWhiteSpace (cityName))
			{
			throw new ArgumentException ("City name cannot be null or empty.", nameof (cityName));
			}

		var query = cityName;
		if (!string.IsNullOrWhiteSpace (stateCode))
			{
			query += $",{stateCode}";
			}

		if (!string.IsNullOrWhiteSpace (countryCode))
			{
			query += $",{countryCode}";
			}

		var url = $"{BASE_URL}direct?q={query}&limit=1&appid={apiKey}";
		using var client = new HttpClient ();
		HttpResponseMessage response = await client.GetAsync (url, cancellationToken).ConfigureAwait (false);
		if (!response.IsSuccessStatusCode)
			{
#if NET10_0_OR_GREATER
			throw new HttpRequestException ($"Error fetching data from OpenWeatherMap API: {response.ReasonPhrase}", null, response.StatusCode);
#else
			throw new HttpRequestException ($"Error fetching data from OpenWeatherMap API: {(int)response.StatusCode} {response.ReasonPhrase}");
#endif
			}

		var jsonContent = await ReadContentAsStringAsync (response.Content, cancellationToken).ConfigureAwait (false);
		Dictionary<string, object>[]? results = JsonConvert.DeserializeObject<Dictionary<string, object>[]> (jsonContent);
		if (results != null && results.Length > 0)
			{
			Dictionary<string, object> firstResult = results[0];
			return new LatLong ((double)(firstResult["lat"] ?? 0), (double)(firstResult["lon"] ?? 0));
			}

		return null;
		}

	/// <summary>
	/// Retrieves coordinates by postal or ZIP code.
	/// </summary>
	/// <param name="postCode">The postal or ZIP code.</param>
	/// <param name="countryCode">The ISO country code the postal code belongs to.</param>
	/// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
	/// <returns>A <see cref="LatLong"/> when a match is found; otherwise <c>null</c>.</returns>
	public async Task<LatLong?> GetCoordinatesByPostCodeAsync (string postCode, string countryCode, CancellationToken cancellationToken = default)
		{
		if (string.IsNullOrWhiteSpace (postCode))
			{
			throw new ArgumentException ("Post code cannot be null or empty.", nameof (postCode));
			}

		if (string.IsNullOrWhiteSpace (countryCode))
			{
			throw new ArgumentException ("Country code cannot be null or empty.", nameof (countryCode));
			}

		var url = $"{BASE_URL}zip?zip={postCode},{countryCode}&appid={apiKey}";
		using var client = new HttpClient ();
		HttpResponseMessage response = await client.GetAsync (url, cancellationToken).ConfigureAwait (false);
		if (!response.IsSuccessStatusCode)
			{
#if NET10_0_OR_GREATER
			throw new HttpRequestException ($"Error fetching data from OpenWeatherMap API: {response.ReasonPhrase}", null, response.StatusCode);
#else
			throw new HttpRequestException ($"Error fetching data from OpenWeatherMap API: {(int)response.StatusCode} {response.ReasonPhrase}");
#endif
			}

		var jsonContent = await ReadContentAsStringAsync (response.Content, cancellationToken).ConfigureAwait (false);
		Dictionary<string, object>? results = JsonConvert.DeserializeObject<Dictionary<string, object>> (jsonContent);
		return results != null && results.Count > 0 ? new LatLong ((double)(results["lat"] ?? 0), (double)(results["lon"] ?? 0)) : null;
		}

    /// <summary>
    /// Looks up the city name for the supplied coordinate pair.
    /// </summary>
    /// <param name="latLong">The latitude and longitude to reverse geocode.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>The city name when available; otherwise <c>null</c>.</returns>
    public Task<string?> GetCityNameByCoordinatesAsync (LatLong latLong, CancellationToken cancellationToken = default) =>
		 GetCityNameByCoordinatesAsync (latLong.Latitude, latLong.Longitude, cancellationToken);

    /// <summary>
    /// Looks up the city name for the supplied latitude/longitude pair.
    /// </summary>
    /// <param name="lat">The latitude to reverse geocode.</param>
    /// <param name="lon">The longitude to reverse geocode.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>The city name when available; otherwise <c>null</c>.</returns>
    public async Task<string?> GetCityNameByCoordinatesAsync (double lat, double lon, CancellationToken cancellationToken = default)
		{
		var url = $"{BASE_URL}reverse?lat={lat}&lon={lon}&limit=1&appid={apiKey}";
		using var client = new HttpClient ();
		HttpResponseMessage response = await client.GetAsync (url, cancellationToken).ConfigureAwait (false);
		if (!response.IsSuccessStatusCode)
			{
#if NET10_0_OR_GREATER
			throw new HttpRequestException ($"Error fetching data from OpenWeatherMap API: {response.ReasonPhrase}", null, response.StatusCode);
#else
			throw new HttpRequestException ($"Error fetching data from OpenWeatherMap API: {(int)response.StatusCode} {response.ReasonPhrase}");
#endif
			}

		var jsonContent = await ReadContentAsStringAsync (response.Content, cancellationToken).ConfigureAwait (false);
		Dictionary<string, object>[]? results = JsonConvert.DeserializeObject<Dictionary<string, object>[]> (jsonContent);
		if (results != null && results.Length > 0)
			{
			Dictionary<string, object> firstResult = results[0];
			return firstResult["name"]?.ToString ();
			}

		return null;
		}
	}

// Use ValueTuple for coordinates
/// <summary>
/// Contains helpers for extracting coordinate data from JSON payloads.
/// </summary>
public static class GeoUtils
	{
	/// <summary>
	/// Converts a JSON token containing <c>lat</c> and <c>lon</c> fields into a <see cref="LatLong"/> instance.
	/// </summary>
	/// <param name="data">The JSON token that includes latitude and longitude.</param>
	/// <returns>The converted <see cref="LatLong"/> value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <c>null</c>.</exception>
	public static LatLong GetCoordinatesFromJToken (Newtonsoft.Json.Linq.JToken data)
		{
#if NET10_0_OR_GREATER
		ArgumentNullException.ThrowIfNull (data);
#else
		if (data == null)
			{
			throw new ArgumentNullException (nameof (data));
			}
#endif

		var latitude = (double)(data["lat"] ?? 0);
		var longitude = (double)(data["lon"] ?? 0);
		return new LatLong (latitude, longitude);
		}
	}

