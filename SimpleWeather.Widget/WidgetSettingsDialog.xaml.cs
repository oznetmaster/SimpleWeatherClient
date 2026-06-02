using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SimpleWeather.Widget.Services;

namespace SimpleWeather.Widget;

public sealed partial class WidgetSettingsDialog : Page
	{
	public event EventHandler<WidgetSettings>? SaveRequested;
	public event EventHandler? CancelRequested;

	public string City { get; set; }
	public string? State { get; set; }
	public string? Country { get; set; }
	public string Units { get; set; }
	public double RefreshMinutes { get; set; }
	public string? ApiKey { get; set; }

	public WidgetSettingsDialog (WidgetSettings settings)
		{
		InitializeComponent ();
		City = settings.City;
		State = settings.State;
		Country = settings.Country;
		Units = settings.Units;
		RefreshMinutes = settings.RefreshMinutes;
		ApiKey = settings.ApiKey;
		}

	public WidgetSettings GetUpdatedSettings () => new WidgetSettings
		{
		City = City.Trim (),
		State = string.IsNullOrWhiteSpace (State) ? null : State.Trim (),
		Country = string.IsNullOrWhiteSpace (Country) ? null : Country.Trim (),
		Units = Units,
		RefreshMinutes = (int)Math.Clamp (RefreshMinutes, 5, 240),
		ApiKey = string.IsNullOrWhiteSpace (ApiKey) ? null : ApiKey.Trim ()
		};

	private string FormatMinutes (double value) => $"{value:0}";

    private void SaveButton_OnClick (object sender, RoutedEventArgs e) => SaveRequested?.Invoke (this, GetUpdatedSettings ());

    private void CancelButton_OnClick (object sender, RoutedEventArgs e) => CancelRequested?.Invoke (this, EventArgs.Empty);
    }
