// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace SimpleWeather.Tests;

[TestFixture]
public sealed class CapabilityConcurrencyTests
{
    [Test]
    public async Task TemporaryFailureCannotOverwriteConfirmedCapabilities()
    {
        using var handler = new CapabilityService();
        using var client = new WeatherController("fake", handler);
        Task<OpenWeatherCapabilities> first = client.ProbeCapabilitiesAsync();
        try
        {
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Task<OpenWeatherCapabilities> second = client.ProbeCapabilitiesAsync();
            Assert.That(handler.Requests, Is.EqualTo(1));
            Assert.That(second.IsCompleted, Is.False);
            _ = handler.Release.TrySetResult(true);
            await first;
            OpenWeatherCapabilities confirmed = await second.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.That(confirmed.Has(OpenWeatherFeatures.OneCall), Is.True);
            Assert.That((await client.ProbeCapabilitiesAsync()).Has(OpenWeatherFeatures.OneCall), Is.True);
            Assert.That(handler.Requests, Is.EqualTo(2));
        }
        finally { _ = handler.Release.TrySetResult(true); }
    }
    private sealed class CapabilityService : HttpMessageHandler
    {
        internal int Requests;
        internal TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref Requests) == 1)
            {
                _ = Started.TrySetResult(true);
                await Release.Task;
                return new(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("temporary outage") };
            }
            return new(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }
    }
}
