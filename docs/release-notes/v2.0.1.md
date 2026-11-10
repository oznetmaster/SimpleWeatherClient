# SimpleWeatherClient 2.0.1

This patch corrects the audited reliability issues while retaining the existing public API and supported frameworks.

- Capability probes are serialized per controller. Temporary service failures return fallback capabilities without caching a subscription downgrade, allowing a later successful probe to establish the real capabilities. Confirmed capabilities are retained, and late probes cannot repopulate the cache after disposal.

The source also retains the previously committed widget dependency alignment and test SDK update; the NuGet library target frameworks are unchanged.

## Validation

- Offline workstation tests passed in Debug and Release on .NET 10 and .NET Framework 4.7.2: SimpleWeather net10.0: 177; SimpleWeather net472: 177. No failures or skips.
- 42 selected tests passed in the shared 374-case processor run on Mono 6.12.0.107 (32-bit), with no failures or skips. The merged host's resolved dependency graph was tested; these are not eight independent release-package tests.
- Fake transports and credentials exercised ordering, cancellation, shutdown and failure recovery. No new household-device or cloud control was performed. Pairing storage is unchanged.
- Strict XML, formatting and style checks passed. Package and companion-app preparation is verified separately by the shared release pipeline.

See [the changelog](https://github.com/oznetmaster/SimpleWeatherClient/blob/v2.0.1/CHANGELOG.md) and [validation provenance](https://github.com/oznetmaster/SimpleWeatherClient/blob/v2.0.1/release-validation/v2.0.1.json).
