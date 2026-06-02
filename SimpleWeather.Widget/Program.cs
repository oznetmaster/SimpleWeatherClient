using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace SimpleWeather.Widget;

public static class Program
	{
	private const uint WINDOWS_APP_SDK_MAJOR_MINOR_VERSION = 0x00010008; // 1.8

	[STAThread]
	public static void Main (string[] args)
		{
		Bootstrap.Initialize (WINDOWS_APP_SDK_MAJOR_MINOR_VERSION);
		try
			{
			WinRT.ComWrappersSupport.InitializeComWrappers ();
			Application.Start (appInitializationCallbackParams =>
			{
				var context = new DispatcherQueueSynchronizationContext (DispatcherQueue.GetForCurrentThread ());
				SynchronizationContext.SetSynchronizationContext (context);
				_ = new App ();
			});
			}
		finally
			{
			Bootstrap.Shutdown ();
			}
		}
	}
