// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace SimpleWeather;

/// <summary>
/// Describes feature flags representing OpenWeather API surface areas that may be available
/// depending on the user's plan and configuration.
/// </summary>
[Flags]
public enum OpenWeatherFeatures
	{
	/// <summary>
	/// No features are available.
	/// </summary>
	None = 0,

	/// <summary>
	/// Access to current weather endpoints.
	/// </summary>
	CurrentWeather = 1 << 0,

	/// <summary>
	/// Access to the 5 day / 3 hour forecast endpoint.
	/// </summary>
	Forecast5 = 1 << 1,

	/// <summary>
	/// Access to One Call endpoints.
	/// </summary>
	OneCall = 1 << 2,

	/// <summary>
	/// Access to One Call hourly forecast data.
	/// </summary>
	OneCallHourly = 1 << 3,

	/// <summary>
	/// Access to One Call daily forecast data.
	/// </summary>
	OneCallDaily = 1 << 4,

	/// <summary>
	/// Access to weather alerts.
	/// </summary>
	Alerts = 1 << 5,

	/// <summary>
	/// Access to geocoding endpoints.
	/// </summary>
	Geocoding = 1 << 6,
	}

/// <summary>
/// Represents the effective OpenWeather capabilities available to the application.
/// This includes which API features can be used and any associated limits.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OpenWeatherCapabilities"/> class.
/// </remarks>
/// <param name="features">The set of available OpenWeather feature flags.</param>
/// <param name="maxForecastDays">The maximum number of forecast days supported by the plan.</param>
/// <param name="forecastStep">The time-step between forecast entries supported by the plan.</param>
public sealed class OpenWeatherCapabilities (OpenWeatherFeatures features, int maxForecastDays, TimeSpan forecastStep)
    {

    /// <summary>
    /// Gets the set of OpenWeather feature flags available to the application.
    /// </summary>
    public OpenWeatherFeatures Features { get; } = features;

    /// <summary>
    /// Gets the maximum number of forecast days supported by the current plan.
    /// </summary>
    public int MaxForecastDays { get; } = maxForecastDays;

    /// <summary>
    /// Gets the time-step between forecast entries provided by the supported forecast endpoint.
    /// </summary>
    public TimeSpan ForecastStep { get; } = forecastStep;

    /// <summary>
    /// Returns a value indicating whether the specified <paramref name="feature"/> flag(s) are available.
    /// </summary>
    /// <param name="feature">The feature flag(s) to test for.</param>
    /// <returns><see langword="true"/> if all requested feature flag(s) are available; otherwise, <see langword="false"/>.</returns>
    public bool Has (OpenWeatherFeatures feature) => (Features & feature) == feature;

	/// <summary>
	/// Gets the capability preset for paid plans using One Call.
	/// </summary>
	public static OpenWeatherCapabilities PaidOneCall => new (
		OpenWeatherFeatures.CurrentWeather |
		OpenWeatherFeatures.Geocoding |
		OpenWeatherFeatures.OneCall |
		OpenWeatherFeatures.OneCallHourly |
		OpenWeatherFeatures.OneCallDaily |
		OpenWeatherFeatures.Alerts,
		maxForecastDays: 7,
		forecastStep: TimeSpan.FromHours (1));

	/// <summary>
	/// Gets the capability preset for the free plan using the 5 day / 3 hour forecast endpoint.
	/// </summary>
	public static OpenWeatherCapabilities Free => new (
		OpenWeatherFeatures.CurrentWeather |
		OpenWeatherFeatures.Geocoding |
		OpenWeatherFeatures.Forecast5,
		maxForecastDays: 5,
		forecastStep: TimeSpan.FromHours (3));
	}
