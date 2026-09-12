# Testing SimpleWeather

`SimpleWeather.Tests` is an NUnit suite targeting **.NET Framework 4.7.2** and **.NET 10**, with `LangVersion=latest`. It is part of `SimpleWeather.sln` and uses the NUnit Visual Studio adapter. `SimpleWeatherTest` remains the existing interactive console harness.

## Offline tests

From the repository root:

```powershell
dotnet test .\SimpleWeather.Tests\SimpleWeather.Tests.csproj --settings .\SimpleWeather.Tests\unit.runsettings
```

Both target frameworks run on Windows. To run one target, add `-f net472` or `-f net10.0`. You only need to build the test project and its library dependency; the desktop and widget applications are not needed.

The suite checks:

- One Call and free endpoint request routing, authentication fallback, other HTTP failures, cancellation and disposal.
- Geocoding, integral/fractional coordinates, regional settings and query escaping.
- Current and forecast response models, optional measurements, precipitation percentages, wind direction, daily summaries and fractional time zone offsets.
- Private live settings, opt-in behavior and omission of secrets from live request failure messages.

Offline HTTP tests use synthetic JSON and an internal message handler. They never contact OpenWeather or use a real account. The public library constructors and method signatures remain unchanged.

## Live OpenWeather tests

The separate `LiveOpenWeatherTests` fixture has the NUnit category **Live**. Its four read-only tests use a real OpenWeather account to check current weather, current forecast readings, and reverse/direct geocoding. They do not assert a particular temperature or weather condition. They do consume API requests under your account's applicable quota and plan. Current weather and forecast use the library's normal One Call requests and authentication fallback to the free endpoints.

Live tests are disabled by default. They are not marked `Explicit`: once enabled, selecting the fixture or running all tests works normally.

1. Add this local exclusion to `.git/info/exclude` **before** creating the private file:

   ```text
   /SimpleWeather.Tests/LiveTestSettings.json
   ```

2. Copy `SimpleWeather.Tests/LiveTestSettings.example.json` to `SimpleWeather.Tests/LiveTestSettings.json`.
3. Set `apiKey` to your existing OpenWeather key and choose `latitude` and `longitude` near a populated place so reverse geocoding can return a name. Set `units` to `metric`, `imperial` or `standard`. Set `cityName` and optional `countryCode` for the separate direct-geocoding check (the example uses London, GB). If `cityName` is omitted, that check skips. Reverse-geocoded locality names are not guaranteed to appear in the direct city search index, so the tests do not assume a round trip.
4. Leave `enabled` false for normal development. Opt in for a run using:

   ```powershell
   dotnet test .\SimpleWeather.Tests\SimpleWeather.Tests.csproj -f net10.0 --settings .\SimpleWeather.Tests\live.runsettings --filter TestCategory=Live
   ```

   Replace `net10.0` with `net472` to check the other runtime. Alternatively set `enabled` to true in the private JSON.

The existing applications store their key in `.local/openweather-api-key.txt` or `%AppData%\SimpleWeather\desktop-api-key.txt`; use the same account key in the private live settings. No new OpenWeather account or token is required. Changes to that saved key are not automatically copied into the live settings.

### Visual Studio

Open `SimpleWeather.sln` and use Test Explorer. Normal runs skip the live fixture unless the private JSON enables it. For a temporary live opt-in, choose `SimpleWeather.Tests/live.runsettings` through **Test > Configure Run Settings > Select Solution Wide runsettings File**, then run the live fixture. Switch to `unit.runsettings` afterwards to force live tests off, even when the JSON says `enabled: true`.

### Settings lookup and external runners

The NUnit parameter `TestDataDirectory`, when supplied, is authoritative: the fixture reads `LiveTestSettings.json` from that directory. Otherwise it searches the test assembly directory and its parents up to the solution root, then `%LocalAppData%\SimpleWeather\LiveTestSettings.json`. This finds the private file beside the test project during ordinary Visual Studio and command-line runs.

The `EnableLiveTests` parameter overrides the JSON `enabled` flag. `false` always disables live tests; `true` enables them and requires valid settings. A missing key or invalid coordinates is a configuration failure when enabled, rather than a silently skipped live test.

The private settings file is **not copied to build or publish output and is not packaged**. A runner on another machine must supply it at run time through `TestDataDirectory`. Only the placeholder example and credential-free runsettings files belong in Git. Do not put keys in command-line arguments, test names, or test results.

## Continuous integration

The Tests workflow runs both frameworks on Windows with `unit.runsettings`. The release workflow also runs the offline suite before packaging. Neither workflow requires account credentials or executes live tests. The test project is not a NuGet package.
