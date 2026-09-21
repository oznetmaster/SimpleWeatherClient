// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

namespace SimpleWeather.Tests;

[TestFixture]
public sealed class WeatherModelTests
	{
	[TestCase (true)]
	[TestCase (false)]
	public void CurrentResponse_MapsBothApiFormats (bool oneCall)
		{
		var weather = new CurrentWeather (oneCall ? Payloads.Current : Payloads.FreeCurrent);
		using (Assert.EnterMultipleScope ())
			{
			Assert.That (weather.Coordinates.Latitude, Is.EqualTo (55.5));
			Assert.That (weather.Coordinates.Longitude, Is.EqualTo (-3.25));
			Assert.That (weather.Main!.Temperature, Is.EqualTo (12.5));
			Assert.That (weather.Main.Humidity, Is.EqualTo (73));
			Assert.That (weather.Wind!.Speed, Is.EqualTo (4.5));
			Assert.That (weather.Wind.Degree, Is.EqualTo (90));
			Assert.That (weather.Wind.Gust, Is.EqualTo (6.5));
			Assert.That (weather.Sys!.Sunrise, Is.EqualTo (DateTimeOffset.FromUnixTimeSeconds (1699948800).LocalDateTime));
			Assert.That (weather.Visibility, Is.EqualTo (9000));
			Assert.That (weather.StatusCode, Is.Null);
			}
		}

	[Test]
	public void FreeCurrent_PreservesStationAndLocationMetadata ()
		{
		var weather = new CurrentWeather (Payloads.FreeCurrent);
		Assert.That (weather.Base, Is.EqualTo ("stations"));
		Assert.That (weather.City, Is.EqualTo ("Synthetic City"));
		Assert.That (weather.CityID, Is.EqualTo (42));
		Assert.That (weather.Sys!.Country, Is.EqualTo ("GB"));
		}

	[TestCase ("{\"cod\":401}", 401)]
	[TestCase ("{\"cod\":\"404\"}", 404)]
	public void CurrentError_PreservesStatusWithoutInventingReadings (string json, int status)
		{
		var weather = new CurrentWeather (json);
		Assert.That (weather.StatusCode, Is.EqualTo (status));
		Assert.That (weather.Main, Is.Null);
		}

	[TestCase (true)]
	[TestCase (false)]
	public void ForecastResponse_MapsBothApiFormats (bool oneCall)
		{
		var forecast = new WeatherForecast (oneCall ? Payloads.Forecast : Payloads.FreeForecast);
		Assert.That (forecast.Hourly, Has.Count.EqualTo (1));
		Assert.That (forecast.Daily, Has.Count.EqualTo (1));
		Hourly hour = forecast.Hourly[0];
		using (Assert.EnterMultipleScope ())
			{
			Assert.That (forecast.Latitude, Is.EqualTo (55.5));
			Assert.That (hour.Temperature, Is.EqualTo (12.5));
			Assert.That (hour.WindSpeed, Is.EqualTo (4.5));
			Assert.That (hour.WindDegree, Is.EqualTo (90));
			Assert.That (hour.WindDirectionShort, Is.EqualTo ("E"));
			Assert.That (hour.PrecipitationProbability, Is.EqualTo (65));
			Assert.That (forecast.Daily[0].PrecipitationProbability, Is.EqualTo (65));
			Assert.That (hour.DT, Is.EqualTo (DateTimeOffset.FromUnixTimeSeconds (1700000000).LocalDateTime));
			}
		}

	[TestCase (0, 0)]
	[TestCase (0.01, 1)]
	[TestCase (0.65, 65)]
	[TestCase (1, 100)]
	public void PrecipitationProbability_UsesPercent (double probability, double percent)
		{
		var json = new JsonObject { ["pop"] = probability };
		Assert.That (Hourly.FromJson (json.ToJsonString ()).PrecipitationProbability, Is.EqualTo (percent));
		Assert.That (Daily.FromJson (json.ToJsonString ()).PrecipitationProbability, Is.EqualTo (percent));
		}

	[TestCase ("en-US")]
	[TestCase ("fr-FR")]
	[TestCase ("de-DE")]
	public void JsonNumbers_AreIndependentOfCurrentCulture (string culture)
		{
		using var scope = new CultureScope (culture);
		var main = Main.FromJson ("{\"temp\":12.5,\"feels_like\":\"11.25\"}");
		Assert.That (main.Temperature, Is.EqualTo (12.5));
		Assert.That (main.FeelsLike, Is.EqualTo (11.25));
		}

	[TestCase (null)]
	[TestCase ("{}")]
	[TestCase ("{\"temp\":null}")]
	[TestCase ("{\"temp\":\"unavailable\"}")]
	public void MissingOrUnavailableMeasurements_RemainNull (string? json)
		{
		Assert.That (Main.FromJson (json).Temperature, Is.Null);
		}

	[TestCase ("{}", "Hourly")]
	[TestCase ("{\"hourly\":[]}", "Daily")]
	public void IncompleteOneCallForecast_ReportsMissingSection (string json, string section)
		{
		Assert.That (() => new WeatherForecast (json), Throws.InvalidOperationException.With.Message.Contains (section));
		}

	[Test]
	public void OptionalForecastSections_CanBeAbsent ()
		{
		var forecast = new WeatherForecast ("{\"hourly\":[],\"daily\":[]}");
		Assert.That (forecast.Hourly, Is.Empty);
		Assert.That (forecast.Daily, Is.Empty);
		Assert.That (forecast.Alerts, Is.Empty);
		}

	[Test]
	public void Forecast5_DailySummaryUsesExtremesAndClosestToNoon ()
		{
		var list = new JsonArray (Entry (6, 5, 10, 0.1, 500), Entry (12, 8, 18, 0.8, 800), Entry (15, 7, 14, 0.3, 801));
		var forecast = new WeatherForecast (new JsonObject { ["city"] = new JsonObject { ["timezone"] = 0 }, ["list"] = list }.ToString ());
		Daily day = forecast.Daily.Single ();
		Assert.That (day.Temperature!.Min, Is.EqualTo (5));
		Assert.That (day.Temperature.Max, Is.EqualTo (18));
		Assert.That (day.PrecipitationProbability, Is.EqualTo (80));
		Assert.That (day.Weather!.ID, Is.EqualTo (800));
		}

	[Test]
	public void Forecast5_GroupsUsingFullTimeZoneOffset ()
		{
		var first = Entry (18, 5, 10, 0, 800);
		first["dt"] = new DateTimeOffset (2026, 1, 1, 18, 45, 0, TimeSpan.Zero).ToUnixTimeSeconds ();
		var second = Entry (21, 5, 10, 0, 800);
		var forecast = new WeatherForecast (new JsonObject { ["city"] = new JsonObject { ["timezone"] = 19800 }, ["list"] = new JsonArray (first, second) }.ToString ());
		Assert.That (forecast.Daily, Has.Count.EqualTo (1), "Both entries fall on January 2 at UTC+05:30.");
		}

	[TestCase (0, "N")]
	[TestCase (90, "E")]
	[TestCase (180, "S")]
	[TestCase (270, "W")]
	[TestCase (360, "N")]
	public void Wind_UsesCompassBearing (double degrees, string expected)
		{
		Assert.That (Wind.GetWindDirectionShort (degrees), Is.EqualTo (expected));
		}

	[TestCase ("25", 25)]
	[TestCase ("{\"all\":25}", 25)]
	[TestCase ("null", null)]
	public void HourlyClouds_AcceptFlatNestedAndUnavailableValues (string cloudJson, double? expected)
		{
		var hour = Hourly.FromJson (new JsonObject { ["clouds"] = JsonNode.Parse (cloudJson) }.ToJsonString ());
		Assert.That (hour.Clouds, Is.EqualTo (expected));
		}

	[Test]
	public void Forecast_PreservesAlertContentAndTimeWindow ()
		{
		Alerts alert = new WeatherForecast (Payloads.Forecast).Alerts.Single ();
		Assert.That (alert.SenderName, Is.EqualTo ("Synthetic agency"));
		Assert.That (alert.Event, Is.EqualTo ("Wind"));
		Assert.That (alert.Description, Is.EqualTo ("Synthetic test alert"));
		Assert.That (alert.End - alert.Start, Is.EqualTo (TimeSpan.FromHours (1)));
		Assert.That (JsonTest.ParseArray (alert.Tags!).Select (value => value!.GetValue<string> ()), Is.EqualTo (new[] { "Wind" }));
		}

	[TestCase (WeatherIconResolution.Standard, "02d.png")]
	[TestCase (WeatherIconResolution.DoubleScale, "02d@2x.png")]
	[TestCase (WeatherIconResolution.Quadruple, "02d@4x.png")]
	public void WeatherIcon_UsesRequestedResolutionAndFirstCondition (WeatherIconResolution resolution, string image)
		{
		var weather = Weather.FromJson (JsonTest.ParseArray ("[{\"id\":801,\"icon\":\"02d\"},{\"id\":800,\"icon\":\"01d\"}]").ToJsonString ());
		Assert.That (weather.ID, Is.EqualTo (801));
		Assert.That (weather.GetIconUrl (resolution), Is.EqualTo ("https://openweathermap.org/img/wn/" + image));
		}

	[TestCase (null)]
	[TestCase ("")]
	[TestCase ("  ")]
	public void MissingWeatherIcon_DoesNotProduceBrokenUrl (string? icon) => Assert.That (new Weather { Icon = icon }.GetIconUrl (), Is.Null);

	private static JsonObject Entry (int hour, double min, double max, double pop, int weatherId) => new ()
		{
		["dt"] = new DateTimeOffset (2026, 1, 1, hour, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds (),
		["main"] = new JsonObject { ["temp_min"] = min, ["temp_max"] = max },
		["pop"] = pop,
		["weather"] = new JsonArray (new JsonObject { ["id"] = weatherId })
		};
	}