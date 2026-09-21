// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

using System.Text.Json.Serialization;

namespace SimpleWeather;

// Provider wire formats are separate from the stable public weather models.

internal class MainResponse
	{
	public MainResponse () { }
	[JsonPropertyName ("temp")]
	public double? Temperature { get; set; }
	[JsonPropertyName ("feels_like")]
	public double? FeelsLike { get; set; }
	[JsonPropertyName ("temp_min")]
	public double? TemperatureMin { get; set; }
	[JsonPropertyName ("temp_max")]
	public double? TemperatureMax { get; set; }
	[JsonPropertyName ("pressure")]
	public double? Pressure { get; set; }
	[JsonPropertyName ("humidity")]
	public double? Humidity { get; set; }
	[JsonPropertyName ("sea_level")]
	public double? SeaLevel { get; set; }
	[JsonPropertyName ("grnd_level")]
	public double? GroundLevel { get; set; }
	[JsonPropertyName ("dew_point")]
	public double? DewPoint { get; set; }
	}

internal class CoordinatesResponse
	{
	public CoordinatesResponse () { }
	[JsonPropertyName ("lat")]
	public double? Latitude { get; set; }
	[JsonPropertyName ("lon")]
	public double? Longitude { get; set; }
	}

internal class WindResponse
	{
	public WindResponse () { }
	[JsonPropertyName ("speed")]
	public double? Speed { get; set; }
	[JsonPropertyName ("deg")]
	public double? Degree { get; set; }
	[JsonPropertyName ("gust")]
	public double? Gust { get; set; }
	}

internal class CloudResponse
	{
	public CloudResponse () { }
	[JsonPropertyName ("all")]
	public double? Cloudiness { get; set; }
	}

internal class PrecipitationResponse
	{
	public PrecipitationResponse () { }
	[JsonPropertyName ("1h")]
	public double? OneHour { get; set; }
	[JsonPropertyName ("3h")]
	public double? ThreeHours { get; set; }
	}

internal class SysResponse
	{
	public SysResponse () { }
	[JsonPropertyName ("type")]
	public double? Type { get; set; }
	[JsonPropertyName ("id")]
	public double? ID { get; set; }
	[JsonPropertyName ("sunrise")]
	public double? Sunrise { get; set; }
	[JsonPropertyName ("sunset")]
	public double? Sunset { get; set; }
	[JsonPropertyName ("country")]
	public string? Country { get; set; }
	}

internal class ConditionResponse
	{
	public ConditionResponse () { }
	[JsonPropertyName ("id")]
	public int ID { get; set; }
	[JsonPropertyName ("main")]
	public string? Main { get; set; }
	[JsonPropertyName ("description")]
	public string? Description { get; set; }
	[JsonPropertyName ("icon")]
	public string? Icon { get; set; }
	}

internal class TemperatureResponse
	{
	public TemperatureResponse () { }
	[JsonPropertyName ("morn")]
	public double? Morning { get; set; }
	[JsonPropertyName ("day")]
	public double? Day { get; set; }
	[JsonPropertyName ("eve")]
	public double? Evening { get; set; }
	[JsonPropertyName ("night")]
	public double? Night { get; set; }
	[JsonPropertyName ("min")]
	public double? Min { get; set; }
	[JsonPropertyName ("max")]
	public double? Max { get; set; }
	}

internal class InstantResponse : MainResponse, ITimedResponse
	{
	public InstantResponse () { }
	[JsonPropertyName ("dt")]
	public double? DT { get; set; }
	[JsonPropertyName ("sunrise")]
	public double? Sunrise { get; set; }
	[JsonPropertyName ("sunset")]
	public double? Sunset { get; set; }
	[JsonPropertyName ("uvi")]
	public double? Uvi { get; set; }
	[JsonPropertyName ("visibility")]
	public double? Visibility { get; set; }
	[JsonPropertyName ("wind_speed")]
	public double? WindSpeed { get; set; }
	[JsonPropertyName ("wind_deg")]
	public double? WindDegree { get; set; }
	[JsonPropertyName ("wind_gust")]
	public double? WindGust { get; set; }
	[JsonPropertyName ("pop")]
	public double? PrecipitationProbability { get; set; }
	[JsonPropertyName ("clouds")]
	[JsonConverter (typeof (CloudinessConverter))]
	public double? Clouds { get; set; }
	[JsonPropertyName ("weather")]
	public ConditionResponse[]? Weather { get; set; }
	[JsonPropertyName ("main")]
	public MainResponse? Main { get; set; }
	[JsonPropertyName ("wind")]
	public WindResponse? Wind { get; set; }
	[JsonPropertyName ("sys")]
	public SysResponse? Sys { get; set; }
	[JsonPropertyName ("rain")]
	public PrecipitationResponse? Rain { get; set; }
	[JsonPropertyName ("snow")]
	public PrecipitationResponse? Snow { get; set; }
	}

internal class DailyResponse : ITimedResponse
	{
	public DailyResponse () { }
	[JsonPropertyName ("dt")]
	public double? DT { get; set; }
	[JsonPropertyName ("sunrise")]
	public double? Sunrise { get; set; }
	[JsonPropertyName ("sunset")]
	public double? Sunset { get; set; }
	[JsonPropertyName ("pressure")]
	public double? Pressure { get; set; }
	[JsonPropertyName ("humidity")]
	public double? Humidity { get; set; }
	[JsonPropertyName ("dew_point")]
	public double? DewPoint { get; set; }
	[JsonPropertyName ("uvi")]
	public double? Uvi { get; set; }
	[JsonPropertyName ("visibility")]
	public double? Visibility { get; set; }
	[JsonPropertyName ("wind_speed")]
	public double? WindSpeed { get; set; }
	[JsonPropertyName ("wind_deg")]
	public double? WindDegree { get; set; }
	[JsonPropertyName ("wind_gust")]
	public double? WindGust { get; set; }
	[JsonPropertyName ("pop")]
	public double? PrecipitationProbability { get; set; }
	[JsonPropertyName ("clouds")]
	[JsonConverter (typeof (CloudinessConverter))]
	public double? Clouds { get; set; }
	[JsonPropertyName ("weather")]
	public ConditionResponse[]? Weather { get; set; }
	[JsonPropertyName ("moonrise")]
	public double? Moonrise { get; set; }
	[JsonPropertyName ("moonset")]
	public double? Moonset { get; set; }
	[JsonPropertyName ("moon_phase")]
	public double? MoonPhase { get; set; }
	[JsonPropertyName ("rain")]
	public double? Rain { get; set; }
	[JsonPropertyName ("snow")]
	public double? Snow { get; set; }
	[JsonPropertyName ("temp")]
	public TemperatureResponse? Temperature { get; set; }
	[JsonPropertyName ("feels_like")]
	public TemperatureResponse? FeelsLike { get; set; }
	}

internal class AlertResponse
	{
	public AlertResponse () { }
	[JsonPropertyName ("start")]
	public double? Start { get; set; }
	[JsonPropertyName ("end")]
	public double? End { get; set; }
	[JsonPropertyName ("sender_name")]
	public string? SenderName { get; set; }
	[JsonPropertyName ("event")]
	public string? Event { get; set; }
	[JsonPropertyName ("description")]
	public string? Description { get; set; }
	[JsonPropertyName ("tags")]
	public string[]? Tags { get; set; }
	}

internal class ForecastCityResponse
	{
	public ForecastCityResponse () { }
	[JsonPropertyName ("coord")]
	public CoordinatesResponse? Coordinates { get; set; }
	[JsonPropertyName ("name")]
	public string? Name { get; set; }
	[JsonPropertyName ("timezone")]
	public double? Timezone { get; set; }
	}

internal class WeatherResponse : InstantResponse
	{
	public WeatherResponse () { }
	[JsonPropertyName ("lat")]
	public double? Latitude { get; set; }
	[JsonPropertyName ("lon")]
	public double? Longitude { get; set; }
	[JsonPropertyName ("timezone_offset")]
	public double? TimezoneOffset { get; set; }
	[JsonPropertyName ("timezone")]
	[JsonConverter (typeof (TextOrNumberConverter))]
	public string? Timezone { get; set; }
	[JsonPropertyName ("coord")]
	public CoordinatesResponse? Coordinates { get; set; }
	[JsonPropertyName ("current")]
	public InstantResponse? Current { get; set; }
	[JsonPropertyName ("hourly")]
	public InstantResponse[]? Hourly { get; set; }
	[JsonPropertyName ("daily")]
	public DailyResponse[]? Daily { get; set; }
	[JsonPropertyName ("alerts")]
	public AlertResponse[]? Alerts { get; set; }
	[JsonPropertyName ("list")]
	public InstantResponse[]? List { get; set; }
	[JsonPropertyName ("city")]
	public ForecastCityResponse? City { get; set; }
	[JsonPropertyName ("cod")]
	[JsonConverter (typeof (TextOrNumberConverter))]
	public string? StatusCode { get; set; }
	[JsonPropertyName ("base")]
	public string? Base { get; set; }
	[JsonPropertyName ("id")]
	public int? CityID { get; set; }
	[JsonPropertyName ("name")]
	public string? CityName { get; set; }
	}

internal class OneCall4Response<T>
	{
	public OneCall4Response () { }
	[JsonPropertyName ("lat")]
	public double? Latitude { get; set; }
	[JsonPropertyName ("lon")]
	public double? Longitude { get; set; }
	[JsonPropertyName ("timezone_offset")]
	public double? TimezoneOffset { get; set; }
	[JsonPropertyName ("timezone")]
	[JsonConverter (typeof (TextOrNumberConverter))]
	public string? Timezone { get; set; }
	[JsonPropertyName ("data")]
	public T[]? Data { get; set; }
	[JsonPropertyName ("next")]
	public string? Next { get; set; }
	}

internal class LocationResponse : CoordinatesResponse
	{
	public LocationResponse () { }
	[JsonPropertyName ("name")]
	public string? Name { get; set; }
	}

internal class ErrorResponse
	{
	public ErrorResponse () { }
	[JsonPropertyName ("cod")]
	[JsonConverter (typeof (TextOrNumberConverter))]
	public string? Code { get; set; }
	}

internal interface ITimedResponse
	{
	double? DT { get; }
	}
