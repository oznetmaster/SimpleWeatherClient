// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

namespace SimpleWeather.Tests;

[TestFixture]
public sealed class GeoLocatorTests
	{
	[TestCase (null)]
	[TestCase ("")]
	[TestCase ("  ")]
	public void EmptyCity_IsRejectedBeforeSending (string? city)
		{
		using var http = new ScriptedWeather ();
		var geo = http.Locator ();
		Assert.ThrowsAsync<ArgumentException> (() => geo.GetCitiesByNameAsync (city!));
		Assert.ThrowsAsync<ArgumentException> (() => geo.GetCoordinatesByCityNameAsync (city!));
		Assert.That (http.Requests, Is.Empty);
		}
	[TestCase ("", "GB")]
	[TestCase ("  ", "GB")]
	[TestCase ("AB1 2CD", "")]
	[TestCase ("AB1 2CD", " ")]
	public void IncompletePostalLocation_IsRejected (string postal, string country)
		{
		using var http = new ScriptedWeather ();
		Assert.ThrowsAsync<ArgumentException> (() => http.Locator ().GetCoordinatesByPostCodeAsync (postal, country));
		Assert.That (http.Requests, Is.Empty);
		}
	[Test]
	public async Task Cities_ReturnRawObjectsAndPreserveUnicode ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("[{\"name\":\"München\",\"lat\":48,\"lon\":11,\"local_names\":{\"ja\":\"ミュンヘン\"}},{\"name\":\"Other\",\"lat\":49.5,\"lon\":12.5}]");
		var cities = await http.Locator ().GetCitiesByNameAsync ("München");
		Assert.That (cities, Has.Count.EqualTo (2));
		Assert.That (JObject.Parse (cities[0])["local_names"]!["ja"]!.Value<string> (), Is.EqualTo ("ミュンヘン"));
		Assert.That (http.Contents[0].Disposed, Is.True);
		}
	[TestCase (false, "55", "-3")]
	[TestCase (false, "55.5", "-3.25")]
	[TestCase (true, "55", "-3")]
	[TestCase (true, "55.5", "-3.25")]
	public async Task Coordinates_AcceptIntegralAndFractionalJson (bool postal, string lat, string lon)
		{
		using var http = new ScriptedWeather ();
		string body = "{\"lat\":" + lat + ",\"lon\":" + lon + "}";
		http.Reply (postal ? body : "[" + body + ", {\"lat\":0,\"lon\":0}]");
		var coordinates = postal ? await http.Locator ().GetCoordinatesByPostCodeAsync ("AB1", "GB") : await http.Locator ().GetCoordinatesByCityNameAsync ("Synthetic City");
		Assert.That (coordinates!.Value.Latitude, Is.EqualTo (double.Parse (lat, System.Globalization.CultureInfo.InvariantCulture)));
		Assert.That (coordinates.Value.Longitude, Is.EqualTo (double.Parse (lon, System.Globalization.CultureInfo.InvariantCulture)));
		}
	[TestCase ("[]")]
	[TestCase ("null")]
	public async Task NoCityMatch_ReturnsNull (string body)
		{
		using var http = new ScriptedWeather ();
		http.Reply (body);
		Assert.That (await http.Locator ().GetCoordinatesByCityNameAsync ("Missing"), Is.Null);
		}
	[TestCase ("{}")]
	[TestCase ("null")]
	public async Task NoPostalMatch_ReturnsNull (string body)
		{
		using var http = new ScriptedWeather ();
		http.Reply (body);
		Assert.That (await http.Locator ().GetCoordinatesByPostCodeAsync ("Missing", "GB"), Is.Null);
		}
	[TestCase ("en-US")]
	[TestCase ("fr-FR")]
	public async Task ReverseLookup_UsesInvariantCoordinates (string culture)
		{
		using var scope = new CultureScope (culture);
		using var http = new ScriptedWeather ();
		http.Reply ("[{\"name\":\"Synthetic City\"}]");
		Assert.That (await http.Locator ().GetCityNameByCoordinatesAsync (new LatLong (55.5, -3.25)), Is.EqualTo ("Synthetic City"));
		Assert.That (ScriptedWeather.Query (http.Requests[0])["lat"], Is.EqualTo ("55.5"));
		}
	[Test]
	public async Task LocationFiltersAndKey_AreEscapedAsValues ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("[]");
		await http.Locator ("key&?#").GetCoordinatesByCityNameAsync ("A&B", "X", "GB");
		var query = ScriptedWeather.Query (http.Requests[0]);
		Assert.That (query["q"], Is.EqualTo ("A&B,X,GB"));
		Assert.That (query["appid"], Is.EqualTo ("key&?#"));
		}
	[TestCase (401)]
	[TestCase (403)]
	[TestCase (429)]
	[TestCase (500)]
	public void HttpFailure_IsReportedAndDisposed (int status)
		{
		using var http = new ScriptedWeather ();
		http.Reply ("{}", (HttpStatusCode)status);
		Assert.ThrowsAsync<HttpRequestException> (() => http.Locator ().GetCoordinatesByCityNameAsync ("Synthetic City"));
		Assert.That (http.Contents[0].Disposed, Is.True);
		}
	[Test]
	public void MalformedJson_ReportsParsingFailure ()
		{
		using var http = new ScriptedWeather ();
		http.Reply ("not-json");
		Assert.CatchAsync<Newtonsoft.Json.JsonException> (() => http.Locator ().GetCoordinatesByCityNameAsync ("Synthetic City"));
		}
	}

internal sealed class CultureScope : IDisposable
	{
	private readonly System.Globalization.CultureInfo _original = System.Globalization.CultureInfo.CurrentCulture;
	internal CultureScope (string culture) => System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo (culture);
	public void Dispose () => System.Globalization.CultureInfo.CurrentCulture = _original;
	}