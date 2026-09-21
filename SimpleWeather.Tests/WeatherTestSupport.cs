// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Net;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Text.Json.Nodes;
global using System.Text.Json;
global using NUnit.Framework;

namespace SimpleWeather.Tests;

internal sealed class ScriptedWeather : HttpMessageHandler
	{
	private readonly Queue<Func<CancellationToken, Task<HttpResponseMessage>>> _replies = new ();
	internal List<Uri> Requests { get; } = [];
	internal List<HttpMethod> Methods { get; } = [];
	internal List<string> AcceptHeaders { get; } = [];
	internal List<ObservedContent> Contents { get; } = [];
	internal bool Disposed { get; private set; }
	internal void Reply (string json, HttpStatusCode status = HttpStatusCode.OK)
		{
		var content = new ObservedContent (json);
		Contents.Add (content);
		_replies.Enqueue (_ => Task.FromResult (new HttpResponseMessage (status) { Content = content }));
		}
	internal void Throw (Exception exception) => _replies.Enqueue (_ => Task.FromException<HttpResponseMessage> (exception));
	internal void WaitForCancellation () => _replies.Enqueue (async token =>
		{
		await Task.Delay (Timeout.Infinite, token);
		throw new InvalidOperationException ("Cancelled request unexpectedly completed.");
		});
	protected override Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
		{
		Requests.Add (request.RequestUri!);
		Methods.Add (request.Method);
		AcceptHeaders.Add (request.Headers.Accept.ToString ());
		if (_replies.Count == 0)
			throw new InvalidOperationException ("Unexpected HTTP request; this test never uses the network.");
		return _replies.Dequeue () (cancellationToken);
		}
	protected override void Dispose (bool disposing)
		{
		Disposed = true;
		base.Dispose (disposing);
		}
	internal GeoLocator Locator (string key = "synthetic-key") => new (key, () => new HttpClient (this, false));
	internal static Dictionary<string, string> Query (Uri uri) => uri.Query.TrimStart ('?').Split ('&').Select (part => part.Split (['='], 2)).ToDictionary (part => Uri.UnescapeDataString (part[0]), part => Uri.UnescapeDataString (part[1]));
	}

internal sealed class ObservedContent (string value) : StringContent (value)
	{
	internal bool Disposed { get; private set; }
	protected override void Dispose (bool disposing)
		{
		Disposed = true;
		base.Dispose (disposing);
		}
	}

internal static class Payloads
	{
	internal const string Current = """
		{"lat":55.5,"lon":-3.25,"timezone":"Europe/London","timezone_offset":3600,"current":{"dt":1700000000,"sunrise":1699948800,"sunset":1699981200,"temp":12.5,"feels_like":11.25,"pressure":1012,"humidity":73,"wind_speed":4.5,"wind_deg":90,"wind_gust":6.5,"clouds":25,"visibility":9000,"weather":[{"id":801,"main":"Clouds","description":"few clouds","icon":"02d"}]}}
		""";
	internal const string FreeCurrent = """
		{"cod":200,"coord":{"lat":55.5,"lon":-3.25},"name":"Synthetic City","id":42,"base":"stations","timezone":3600,"main":{"temp":12.5,"feels_like":11.25,"temp_min":10,"temp_max":15,"pressure":1012,"humidity":73},"wind":{"speed":4.5,"deg":90,"gust":6.5},"clouds":{"all":25},"visibility":9000,"sys":{"country":"GB","sunrise":1699948800,"sunset":1699981200},"weather":[{"id":801,"main":"Clouds","description":"few clouds","icon":"02d"}]}
		""";
	internal const string Forecast = """
		{"lat":55.5,"lon":-3.25,"timezone":"Europe/London","timezone_offset":3600,"hourly":[{"dt":1700000000,"temp":12.5,"wind_speed":4.5,"wind_deg":90,"pop":0.65}],"daily":[{"dt":1700000000,"temp":{"min":10,"max":15},"pop":0.65,"moon_phase":0.5}],"alerts":[{"sender_name":"Synthetic agency","event":"Wind","start":1700000000,"end":1700003600,"description":"Synthetic test alert","tags":["Wind"]}]}
		""";
	internal const string FreeForecast = """
		{"cod":"200","city":{"name":"Synthetic City","timezone":3600,"coord":{"lat":55.5,"lon":-3.25}},"list":[{"dt":1700000000,"main":{"temp":12.5,"feels_like":11.25,"temp_min":10,"temp_max":15,"pressure":1012,"humidity":73},"wind":{"speed":4.5,"deg":90,"gust":6.5},"clouds":{"all":25},"pop":0.65,"weather":[{"id":801,"main":"Clouds","icon":"02d"}]}]}
		""";
	}
internal static class JsonTest
	{
	internal static JsonObject ParseObject (string json) => JsonNode.Parse (json)!.AsObject ();
	internal static JsonArray ParseArray (string json) => JsonNode.Parse (json)!.AsArray ();
	}
