# Configuration

This repository is set up so tracked configuration files can remain safe for publication while local development still works.

## OpenWeather API Key Lookup

The desktop app and test app resolve the OpenWeather API key in this order:

1. `App.config`
2. solution-local key file: `.local/openweather-api-key.txt`
3. AppData fallback: `%AppData%\\SimpleWeather\\desktop-api-key.txt`

A tracked `App.config` should contain only placeholder values.

## Recommended Local Setup

For portable local development, create this file in the solution root:

```text
.local/openweather-api-key.txt
```

The file contents should be only the raw API key:

```text
YOUR_OPENWEATHER_API_KEY_HERE
```

## Why the Solution-Local File Exists

The `.local` key file allows the solution folder to be copied between machines while keeping the real key out of the published repository.

## Local-Only Files

The following local-only paths are intentionally excluded from publication:

- `.local/`
- local machine-specific test configuration paths excluded through git local exclude settings

These are intended for your machine only and should not be committed.

## Tracked Placeholder Configuration

Tracked `App.config` files may still exist for:
- runtime settings
- binding redirects
- placeholder API-key values

That allows the applications to run correctly once a real key is provided through one of the supported local-only sources.
