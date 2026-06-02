using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace SimpleWeather.Widget;

public sealed partial class App : Application
    {
    private Window? _window;

    public App ()
        {
        InitializeComponent ();
        }

    protected override void OnLaunched (LaunchActivatedEventArgs args)
        {
        _window ??= new MainWindow ();
        _window.Activate ();
        }
    }
