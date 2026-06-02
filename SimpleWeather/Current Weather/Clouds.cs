// Copyright (c) 2022 Ivan Gechev
// Copyright (c) 2025 Nivloc Enterprises Ltd
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// This file is adapted from Banovvv/SimpleWeather (https://github.com/Banovvv/SimpleWeather)

using System.Globalization;

using Newtonsoft.Json.Linq;

namespace SimpleWeather;

/// <summary>
/// Represents the cloud coverage portion of a current weather response.
/// </summary>
public class Clouds
	{
	/// <summary>
	/// Initializes a new <see cref="Clouds"/> instance from the supplied JSON token.
	/// </summary>
	/// <param name="data">The JSON fragment that contains the cloudiness value.</param>
	public Clouds (JToken? data)
		{
		if (data == null)
			{
			return;
			}

		// One Call may provide `clouds` as a number; Current Weather provides `{ "all": <percent> }`.
		JToken? token = data.Type switch
			{
			JTokenType.Object => data["all"],
			JTokenType.Integer or JTokenType.Float or JTokenType.String => data,
			_ => null
			};

		if (token != null && double.TryParse (token.ToString (), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
			{
			Cloudiness = value;
			}
		}

	/// <summary>
	/// Cloudiness, %
	/// </summary>
	public double? Cloudiness
		{
		get;
		}
	}

