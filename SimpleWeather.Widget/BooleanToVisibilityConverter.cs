using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace SimpleWeather.Widget;

public sealed class BooleanToVisibilityConverter : IValueConverter
	{
	public object Convert (object value, Type targetType, object parameter, string language)
		{
		var invert = parameter is string s && string.Equals (s, "Invert", StringComparison.OrdinalIgnoreCase);
		var isTrue = value is bool b && b;
		if (invert)
			{
			isTrue = !isTrue;
			}

		return isTrue ? Visibility.Visible : Visibility.Collapsed;
		}

	public object ConvertBack (object value, Type targetType, object parameter, string language)
		{
		var invert = parameter is string s && string.Equals (s, "Invert", StringComparison.OrdinalIgnoreCase);
		var isTrue = value is Visibility v && v == Visibility.Visible;
		return invert ? !isTrue : isTrue;
		}
	}
