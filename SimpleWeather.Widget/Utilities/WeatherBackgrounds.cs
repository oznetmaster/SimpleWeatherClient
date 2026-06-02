using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.UI.Xaml.Media.Imaging;

namespace SimpleWeather.Widget.Utilities;

public static class WeatherBackgrounds
	{
	private static readonly ConcurrentDictionary<string, BitmapImage> _cache = new (StringComparer.OrdinalIgnoreCase);
	private static int _initialized;

	public static void Initialize ()
		{
		if (Interlocked.Exchange (ref _initialized, 1) == 1)
			{
			return;
			}

		foreach (var icon in SupportedIcons ())
			{
            _ = _cache.TryAdd (icon, CreateBitmap (icon));
			}
		}


	public static BitmapImage FromIcon (string? icon)
		{
		var key = NormalizeIcon (icon);
		return _cache.GetOrAdd (key, CreateBitmap);
		}

    private static BitmapImage CreateBitmap (string normalizedIcon) => new BitmapImage (new Uri ($"ms-appx:///Assets/Backgrounds/bg_{normalizedIcon}.png"));

    private static IEnumerable<string> SupportedIcons ()
		{
		// Available assets in `Assets/Backgrounds`: bg_01d..bg_50n
		var groups = new[] { "01", "02", "03", "04", "09", "10", "11", "13", "50" };
		foreach (var g in groups)
			{
			yield return g + "d";
			yield return g + "n";
			}
		}
	
	private static string NormalizeIcon (string? icon)
		{
		if (string.IsNullOrWhiteSpace (icon))
			{
			return "01d";
			}

		var trimmed = icon.Trim ();
		if (trimmed.Length < 3)
			{
			return "01d";
			}

		// Prefer full icon code (01d/10n/etc.) if it's in our supported set.
		var suffix = trimmed.EndsWith ("n", StringComparison.OrdinalIgnoreCase) ? "n" : "d";
		var group = trimmed[..2];
		var full = group + suffix;
		return Array.Exists (SupportedGroupCodes, g => string.Equals (g, group, StringComparison.Ordinal))
			? full
			: "01" + suffix;
		}
	
	private static readonly string[] SupportedGroupCodes = [ "01", "02", "03", "04", "09", "10", "11", "13", "50" ];
	}
