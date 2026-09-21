// Copyright (c) 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE in the repository root.

namespace SimpleWeather;

/// <summary>Selects the service for one controller independently of its API key.</summary>
public enum OpenWeatherService
	{
	/// <summary>Prefer 3.0, then 4.0, then free endpoints when access is denied.</summary>
	Automatic,
	/// <summary>Use One Call 3.0 only, with no fallback.</summary>
	OneCall3,
	/// <summary>Use One Call 4.0 only, with no fallback.</summary>
	OneCall4,
	/// <summary>Use free current-weather and five-day forecast endpoints only.</summary>
	Free
	}
