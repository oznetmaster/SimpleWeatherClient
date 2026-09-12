// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

using System.IO;

namespace SimpleWeather.Tests;

[TestFixture]
public sealed class LiveTestSettingsTests
	{
	private string _directory = null!;
	private string SettingsPath => Path.Combine (_directory, "LiveTestSettings.json");
	[SetUp]
	public void CreateTemporaryFolder () => _directory = Directory.CreateDirectory (Path.Combine (Path.GetTempPath (), "simpleweather-settings-" + Guid.NewGuid ().ToString ("N"))).FullName;
	[TearDown]
	public void RemoveTemporaryFolder () => Directory.Delete (_directory, true);

	[Test]
	public void MissingSettings_DefaultsToDisabled () => Assert.That (() => LiveTestSupport.LoadForRun (SettingsPath, ""), Throws.TypeOf<NUnit.Framework.IgnoreException> ());
	[Test]
	public void EnabledWithoutSettings_IsConfigurationFailure () => Assert.That (() => LiveTestSupport.LoadForRun (SettingsPath, "true"), Throws.TypeOf<InvalidDataException> ());
	[Test]
	public void ExplicitDisable_WinsEvenOverMalformedSettings ()
		{
		File.WriteAllText (SettingsPath, "invalid-json");
		Assert.That (() => LiveTestSupport.LoadForRun (SettingsPath, "false"), Throws.TypeOf<NUnit.Framework.IgnoreException> ());
		}
	[TestCase ("", true)]
	[TestCase ("true", false)]
	public void JsonOrRunnerFlag_CanEnableTests (string enableOverride, bool enabled)
		{
		WriteValidSettings (enabled);
		Assert.That (LiveTestSupport.LoadForRun (SettingsPath, enableOverride).ApiKey, Is.EqualTo ("synthetic-key"));
		}
	[TestCase ("latitude", "91")]
	[TestCase ("longitude", "-181")]
	[TestCase ("latitude", "null")]
	[TestCase ("apiKey", "\"\"")]
	[TestCase ("units", "\"unknown\"")]
	public void InvalidEnabledSettings_AreRejected (string field, string json)
		{
		JObject settings = WriteValidSettings (true);
		settings[field] = JToken.Parse (json);
		File.WriteAllText (SettingsPath, settings.ToString ());
		Assert.That (() => LiveTestSupport.LoadForRun (SettingsPath, ""), Throws.TypeOf<InvalidDataException> ());
		}
	[Test]
	public void InvalidJson_DoesNotExposeSecretInException ()
		{
		File.WriteAllText (SettingsPath, "{\"latitude\":\"synthetic-secret\"}");
		Exception? exception = Assert.Throws<InvalidDataException> (() => LiveTestSupport.LoadForRun (SettingsPath, "true"));
		Assert.That (exception!.ToString (), Does.Not.Contain ("synthetic-secret"));
		}
	[Test]
	public void InvalidOverride_IsRejected () => Assert.That (() => LiveTestSupport.LoadForRun (SettingsPath, "yes"), Throws.TypeOf<InvalidDataException> ());
	[Test]
	public void RequestFailures_DoNotExposeSecretUris ()
		{
		Exception? exception = Assert.ThrowsAsync<InvalidOperationException> (() => LiveTestSupport.Request (() => Task.FromException<string> (new HttpRequestException ("https://example.invalid/?appid=synthetic-secret"))));
		Assert.That (exception!.ToString (), Does.Not.Contain ("synthetic-secret"));
		}
	private JObject WriteValidSettings (bool enabled)
		{
		var settings = new JObject { ["enabled"] = enabled, ["apiKey"] = "synthetic-key", ["latitude"] = 51.5, ["longitude"] = -0.1, ["units"] = "metric" };
		File.WriteAllText (SettingsPath, settings.ToString ());
		return settings;
		}
	}