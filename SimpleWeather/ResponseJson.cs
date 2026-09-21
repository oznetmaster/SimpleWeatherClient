// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleWeather;

// All serializer types remain internal. Public models expose weather values only.
internal static class ResponseJson
	{
	private static readonly JsonSerializerOptions Options = CreateOptions ();
	private static JsonSerializerOptions CreateOptions ()
		{
		var options = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString };
		options.Converters.Add (new OptionalNumberConverter ());
		return options;
		}

	internal static T Read<T> (string json) => JsonSerializer.Deserialize<T> (json, Options)
		?? throw new JsonException ("The response must not be null.");
	internal static T? ReadOptional<T> (string? json) where T : class =>
		json == null ? null : JsonSerializer.Deserialize<T> (json, Options);

	internal static double? ReadCloudiness (string? json)
		{
		if (json == null) return null;
		var reader = new Utf8JsonReader (Encoding.UTF8.GetBytes (json));
		if (!reader.Read ()) throw new JsonException ("A cloudiness value is required.");
		double? value = new CloudinessConverter ().Read (ref reader, typeof (double?), Options);
		if (reader.Read ()) throw new JsonException ("Unexpected trailing JSON.");
		return value;
		}

	internal static double? Number (ref Utf8JsonReader reader)
		{
		if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble (out double number))
			return double.IsNaN (number) || double.IsInfinity (number) ? null : number;
		if (reader.TokenType == JsonTokenType.String && double.TryParse (reader.GetString (), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
			return double.IsNaN (number) || double.IsInfinity (number) ? null : number;
		reader.Skip ();
		return null;
		}
	}

internal sealed class OptionalNumberConverter : JsonConverter<double?>
	{
	public override double? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => ResponseJson.Number (ref reader);
	public override void Write (Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
		{
		if (value.HasValue) writer.WriteNumberValue (value.Value); else writer.WriteNullValue ();
		}
	}

// Current Weather uses { "all": n }; One Call uses a scalar.
internal sealed class CloudinessConverter : JsonConverter<double?>
	{
	public override double? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
		if (reader.TokenType != JsonTokenType.StartObject) return ResponseJson.Number (ref reader);
		return JsonSerializer.Deserialize<CloudResponse> (ref reader, options)?.Cloudiness;
		}
	public override void Write (Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
		{
		if (value.HasValue) writer.WriteNumberValue (value.Value); else writer.WriteNullValue ();
		}
	}

// Numeric UTC offsets and named time zones share the provider's "timezone" field.
internal sealed class TextOrNumberConverter : JsonConverter<string>
	{
	public override string? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
		if (reader.TokenType == JsonTokenType.String) return reader.GetString ();
		if (reader.TokenType == JsonTokenType.Number) return reader.GetDouble ().ToString (CultureInfo.InvariantCulture);
		reader.Skip ();
		return null;
		}
	public override void Write (Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue (value);
	}
