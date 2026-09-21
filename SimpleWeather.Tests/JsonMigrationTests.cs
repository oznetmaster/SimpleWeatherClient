// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

using System.Reflection;

namespace SimpleWeather.Tests;

[TestFixture]
public sealed class JsonMigrationTests
	{
	[Test]
	public void PublicApiAndRuntimeReferencesDoNotExposeASerializer ()
		{
		Assembly library = typeof (WeatherController).Assembly;
		Assert.That (library.GetReferencedAssemblies ().Select (item => item.Name), Does.Not.Contain ("Newtonsoft.Json"));
		foreach (Type type in library.GetExportedTypes ())
			{
			IEnumerable<Type> types = type.GetMethods (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
				.SelectMany (method => method.GetParameters ().Select (parameter => parameter.ParameterType).Append (method.ReturnType))
				.Concat (type.GetConstructors ().SelectMany (constructor => constructor.GetParameters ().Select (parameter => parameter.ParameterType)));
			foreach (Type exposed in types)
				Assert.That (exposed.ToString (), Does.Not.Contain ("Newtonsoft").And.Not.Contain ("System.Text.Json"), type.FullName);
			}
		}

	[Test]
	public void FragmentFactoriesPreserveOptionalAndDerivedValues ()
		{
		Assert.That (Coordinates.FromJson ("{\"lat\":56.1,\"lon\":-4.8}").Latitude, Is.EqualTo (56.1));
		Assert.That (Rain.FromJson ("{\"1h\":0.3}").ThreeHours, Is.Null);
		Assert.That (Snow.FromJson ("{\"3h\":1.5}").ThreeHours, Is.EqualTo (1.5));
		Assert.That (Wind.FromJson ("{\"deg\":90}").WindDirectionShort, Is.EqualTo ("E"));
		Assert.That (FeelsLike.FromJson ("{\"morn\":12.5}").Morning, Is.EqualTo (12.5));
		Assert.That (Temperature.FromJson ("{\"min\":4,\"max\":12}").Max, Is.EqualTo (12));
		Assert.That (Sys.FromJson ("{\"sunrise\":1700000000,\"country\":\"GB\"}").Sunrise,
			Is.EqualTo (DateTimeOffset.FromUnixTimeSeconds (1700000000).LocalDateTime));
		}

	[TestCase ("25")]
	[TestCase ("{\"all\":25,\"future_field\":true}")]
	public void CloudFragmentFactoryAcceptsBothProviderShapes (string json) =>
		Assert.That (Clouds.FromJson (json).Cloudiness, Is.EqualTo (25));

	[Test]
	public void CoordinateHelperAcceptsJsonWithoutSerializerTypes ()
		{
		LatLong location = GeoUtils.GetCoordinatesFromJson ("{\"lat\":56.1,\"lon\":-4.8}");
		Assert.That (location.Latitude, Is.EqualTo (56.1));
		Assert.That (location.Longitude, Is.EqualTo (-4.8));
		}
	}
