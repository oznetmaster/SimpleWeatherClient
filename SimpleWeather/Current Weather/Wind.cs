// Copyright (c) 2026 Neil Colvin.
// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)




namespace SimpleWeather;

	/// <summary>
	/// Represents the wind section of the current weather response, including derived directions.
	/// </summary>
	public class Wind
		{
	/// <summary>Creates this model from its OpenWeather JSON fragment.</summary>
	/// <param name="json">The JSON fragment, or null for an empty model.</param>
	/// <returns>The parsed weather model.</returns>
	public static Wind FromJson (string? json) => new (ResponseJson.ReadOptional<WindResponse> (json));

	internal Wind (WindResponse? data)
		{
		if (data == null) return;
		Speed = data.Speed;
		Degree = data.Degree;
		Gust = data.Gust;
		WindDirectionShort = GetWindDirectionShort (Degree);
		WindDirectionLong = GetWindDirectionLong (Degree);
		}
		/// <summary>
		/// Wind speed. Default Unit: meter/sec
		/// </summary>
		public double? Speed
			{
			get;
			}
		/// <summary>
		/// Wind direction, degrees (meteorological)
		/// </summary>
		public double? Degree
			{
			get;
			}
		/// <summary>
		/// Short wind direction represented by a string (N, E, S, W, etc.)
		/// </summary>
		public string? WindDirectionShort
			{
			get;
			}
		/// <summary>
		/// Long wind direction represented by a string (North, East, South, West, etc.)
		/// </summary>
		public string? WindDirectionLong
			{
			get;
			}
		/// <summary>
		/// Wind gust. Default Unit: meter/sec
		/// </summary>
		public double? Gust
			{
			get;
			}

		/// <summary>
		/// Converts a numeric wind bearing into the corresponding short compass representation.
		/// </summary>
		/// <param name="windDegree">The meteorological wind degree.</param>
		/// <returns>The short direction token (e.g., N, NW).</returns>
		public static string GetWindDirectionShort (double? windDegree)
			{
			var windDirectionShort = string.Empty;

			if (!windDegree.HasValue)
				{
				return "??";
				}

			if (windDegree is (>= 0 and <= 11.25) or (> 348.75 and <= 360))
				{
				windDirectionShort = "N";
				}
			else if (windDegree is > 11.25 and <= 33.75)
				{
				windDirectionShort = "NNE";
				}
			else if (windDegree is > 33.75 and <= 56.25)
				{
				windDirectionShort = "NE";
				}
			else if (windDegree is > 56.25 and <= 78.75)
				{
				windDirectionShort = "ENE";
				}
			else if (windDegree is > 78.75 and <= 101.25)
				{
				windDirectionShort = "E";
				}
			else if (windDegree is > 101.25 and <= 123.75)
				{
				windDirectionShort = "ESE";
				}
			else if (windDegree is > 123.75 and <= 146.25)
				{
				windDirectionShort = "SE";
				}
			else if (windDegree is > 146.25 and <= 168.75)
				{
				windDirectionShort = "SSE";
				}
			else if (windDegree is > 168.75 and <= 191.25)
				{
				windDirectionShort = "S";
				}
			else if (windDegree is > 191.25 and <= 213.75)
				{
				windDirectionShort = "SSW";
				}
			else if (windDegree is > 213.75 and <= 236.25)
				{
				windDirectionShort = "SW";
				}
			else if (windDegree is > 236.25 and <= 258.75)
				{
				windDirectionShort = "WSW";
				}
			else if (windDegree is > 258.75 and <= 281.25)
				{
				windDirectionShort = "W";
				}
			else if (windDegree is > 281.25 and <= 303.75)
				{
				windDirectionShort = "WNW";
				}
			else if (windDegree is > 303.75 and <= 326.25)
				{
				windDirectionShort = "NW";
				}
			else if (windDegree is > 326.25 and <= 348.75)
				{
				windDirectionShort = "NNW";
				}

			return windDirectionShort;
			}

		/// <summary>
		/// Converts a numeric wind bearing into the corresponding long compass representation.
		/// </summary>
		/// <param name="windDegree">The meteorological wind degree.</param>
		/// <returns>The long direction token (e.g., North, Northwest).</returns>
		public static string GetWindDirectionLong (double? windDegree)
			{
			var windDirectionLong = string.Empty;

			if (!windDegree.HasValue)
				{
				return "Enknown";
				}

			if (windDegree is (>= 0 and <= 11.25) or (> 348.75 and <= 360))
				{
				windDirectionLong = "North";
				}
			else if (windDegree is > 11.25 and <= 33.75)
				{
				windDirectionLong = "North-northeast";
				}
			else if (windDegree is > 33.75 and <= 56.25)
				{
				windDirectionLong = "Northeast";
				}
			else if (windDegree is > 56.25 and <= 78.75)
				{
				windDirectionLong = "East-northeast";
				}
			else if (windDegree is > 78.75 and <= 101.25)
				{
				windDirectionLong = "East";
				}
			else if (windDegree is > 101.25 and <= 123.75)
				{
				windDirectionLong = "East-southeast";
				}
			else if (windDegree is > 123.75 and <= 146.25)
				{
				windDirectionLong = "Southeast";
				}
			else if (windDegree is > 146.25 and <= 168.75)
				{
				windDirectionLong = "South-southeast";
				}
			else if (windDegree is > 168.75 and <= 191.25)
				{
				windDirectionLong = "South";
				}
			else if (windDegree is > 191.25 and <= 213.75)
				{
				windDirectionLong = "South-southwest";
				}
			else if (windDegree is > 213.75 and <= 236.25)
				{
				windDirectionLong = "Southwest";
				}
			else if (windDegree is > 236.25 and <= 258.75)
				{
				windDirectionLong = "West-southwest";
				}
			else if (windDegree is > 258.75 and <= 281.25)
				{
				windDirectionLong = "West";
				}
			else if (windDegree is > 281.25 and <= 303.75)
				{
				windDirectionLong = "West-northwest";
				}
			else if (windDegree is > 303.75 and <= 326.25)
				{
				windDirectionLong = "Northwest";
				}
			else if (windDegree is > 326.25 and <= 348.75)
				{
				windDirectionLong = "North-northwest";
				}

			return windDirectionLong;
			}
		}

