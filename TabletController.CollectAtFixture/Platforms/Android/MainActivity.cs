using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Usb;
using Android.OS;
using Android.Views;

namespace TabletController.CollectAtFixture
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        DirectBootAware = true,
        ScreenOrientation = ScreenOrientation.SensorPortrait,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
    [MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetFullscreenMode();

            // Handle USB device attached intent (for automatic permission granting)
            var intent = Intent;
            if (intent != null && UsbManager.ActionUsbDeviceAttached.Equals(intent.Action))
            {
                var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
                if (device != null)
                {
                    var usbManager = GetSystemService(UsbService) as UsbManager;
                    if (usbManager != null && usbManager.HasPermission(device))
                    {
                        Android.Util.Log.Info("MainActivity", $"USB device attached and permission granted: {device.DeviceName}");
                    }
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            // Services are now initialized at app startup in App.OnStart()
            SetFullscreenMode();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
            {
                SetFullscreenMode();
            }
        }

        private void SetFullscreenMode()
        {
            if (Window?.DecorView == null) return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                // Android 11+ (API 30+)
                Window.SetDecorFitsSystemWindows(false);
                var controller = Window.InsetsController;
                if (controller != null)
                {
                    controller.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                    controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                }
            }
            else
            {
                // Legacy approach for older Android versions
#pragma warning disable CA1422
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
                    SystemUiFlags.ImmersiveSticky |
                    SystemUiFlags.LayoutStable |
                    SystemUiFlags.LayoutHideNavigation |
                    SystemUiFlags.LayoutFullscreen |
                    SystemUiFlags.HideNavigation |
                    SystemUiFlags.Fullscreen);
#pragma warning restore CA1422
            }
        }
    }
}
