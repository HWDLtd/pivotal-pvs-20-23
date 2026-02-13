using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Widget;
using Java.Lang;
using TabletController.Core.Interfaces;

namespace TabletController.Hardware.Platforms.Android.Services
{
    /// <summary>
    /// Android implementation of IBarcodeScanner using the Newland FM430 SDK.
    /// </summary>
    public class BarcodeScannerService : Java.Lang.Object, IBarcodeScanner, Com.Nlscan.Nlsdk.NewlandScannerBridge.IScannerCallback
    {
        private const string Tag = "BarcodeScannerService";
        private const string ActionUsbPermission = "com.tabletcontroller.USB_PERMISSION";

        private Com.Nlscan.Nlsdk.NewlandScannerBridge? _scannerBridge;
        private UsbManager? _usbManager;
        private PendingIntent? _permissionIntent;
        private UsbPermissionReceiver? _usbReceiver;
        private bool _isInitialized;
        private bool _isConnected;
        private bool _permissionRequested;
        private readonly object _lock = new();

        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;
        public event EventHandler<bool>? ConnectionChanged;

        public bool IsConnected => _isConnected;
        public string? LastBarcode { get; private set; }

        public Task<bool> InitializeAsync()
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_isInitialized)
                        return true;

                    try
                    {
                        var context = Platform.CurrentActivity ?? Platform.AppContext;
                        if (context == null)
                        {
                            global::Android.Util.Log.Error(Tag, "No Android context available");
                            return false;
                        }

                        _usbManager = (UsbManager?)context.GetSystemService(Context.UsbService);
                        if (_usbManager == null)
                        {
                            global::Android.Util.Log.Error(Tag, "USB Manager not available");
                            return false;
                        }

                        // Create permission intent
                        var intent = new Intent(ActionUsbPermission);
                        var flags = Build.VERSION.SdkInt >= BuildVersionCodes.S
                            ? PendingIntentFlags.Mutable
                            : PendingIntentFlags.UpdateCurrent;
                        _permissionIntent = PendingIntent.GetBroadcast(context, 0, intent, flags);

                        // Register USB permission receiver
                        _usbReceiver = new UsbPermissionReceiver(this);
                        var filter = new IntentFilter(ActionUsbPermission);
                        filter.AddAction(UsbManager.ActionUsbDeviceAttached);
                        filter.AddAction(UsbManager.ActionUsbDeviceDetached);

                        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                        {
                            context.RegisterReceiver(_usbReceiver, filter, ReceiverFlags.Exported);
                        }
                        else
                        {
                            context.RegisterReceiver(_usbReceiver, filter);
                        }

                        // Create scanner bridge
                        _scannerBridge = new Com.Nlscan.Nlsdk.NewlandScannerBridge();
                        _scannerBridge.SetCallback(this);

                        _isInitialized = true;
                        global::Android.Util.Log.Info(Tag, "Barcode scanner service initialized");

                        return true;
                    }
                    catch (System.Exception ex)
                    {
                        global::Android.Util.Log.Error(Tag, $"Failed to initialize scanner: {ex.Message}");
                        return false;
                    }
                }
            });
        }

        public Task<bool> StartAsync()
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!_isInitialized)
                    {
                        global::Android.Util.Log.Warn(Tag, "Scanner not initialized");
                        return false;
                    }

                    try
                    {
                        var context = Platform.CurrentActivity ?? Platform.AppContext;
                        if (context == null)
                        {
                            global::Android.Util.Log.Error(Tag, "No Android context available");
                            return false;
                        }

                        // Check for USB permission and request if needed
                        var newlandDevice = FindNewlandDevice();
                        if (newlandDevice != null && _usbManager != null)
                        {
                            if (!_usbManager.HasPermission(newlandDevice))
                            {
                                if (!_permissionRequested)
                                {
                                    _permissionRequested = true;
                                    global::Android.Util.Log.Info(Tag, "Requesting USB permission");
                                    _usbManager.RequestPermission(newlandDevice, _permissionIntent);
                                }
                                return false; // Permission pending
                            }
                        }

                        var result = _scannerBridge?.Open(context) ?? false;

                        if (result)
                        {
                            global::Android.Util.Log.Info(Tag, "Scanner started successfully");
                            ShowToast("Scanner connected");
                        }
                        else
                        {
                            global::Android.Util.Log.Warn(Tag, "Failed to start scanner - permission may be pending");
                        }

                        return result;
                    }
                    catch (System.Exception ex)
                    {
                        global::Android.Util.Log.Error(Tag, $"Failed to start scanner: {ex.Message}");
                        return false;
                    }
                }
            });
        }

        public Task StopAsync()
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        _scannerBridge?.Close();
                        _isConnected = false;
                        global::Android.Util.Log.Info(Tag, "Scanner stopped");
                    }
                    catch (System.Exception ex)
                    {
                        global::Android.Util.Log.Error(Tag, $"Failed to stop scanner: {ex.Message}");
                    }
                }
            });
        }

        public Task TriggerScanAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    _scannerBridge?.TriggerScan();
                }
                catch (System.Exception ex)
                {
                    global::Android.Util.Log.Error(Tag, $"Failed to trigger scan: {ex.Message}");
                }
            });
        }

        // Implementation of IScannerCallback
        public void OnBarcodeScanned(string? barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                return;

            LastBarcode = barcode.Trim();
            global::Android.Util.Log.Info(Tag, $"Barcode received: {LastBarcode}");

            // Show toast on UI thread
            //ShowToast($"Scanned: {LastBarcode}");

            // Raise event on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(LastBarcode));
            });
        }

        public void OnConnectionChanged(bool connected)
        {
            _isConnected = connected;
            global::Android.Util.Log.Info(Tag, $"Connection changed: {connected}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectionChanged?.Invoke(this, connected);
            });

            if (connected)
            {
                ShowToast("Scanner connected");
            }
            else
            {
                ShowToast("Scanner disconnected");
            }
        }

        private UsbDevice? FindNewlandDevice()
        {
            if (_usbManager == null)
                return null;

            const int NewlandVendorId = 0x1EAB; // 7851 decimal

            var deviceList = _usbManager.DeviceList;
            if (deviceList == null)
                return null;

            foreach (var device in deviceList.Values)
            {
                if (device?.VendorId == NewlandVendorId)
                {
                    global::Android.Util.Log.Info(Tag, $"Found Newland device: VID={device.VendorId}, PID={device.ProductId}");
                    return device;
                }
            }

            return null;
        }

        private void ShowToast(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var context = Platform.CurrentActivity ?? Platform.AppContext;
                    if (context != null)
                    {
                        Toast.MakeText(context, message, ToastLength.Short)?.Show();
                    }
                }
                catch (System.Exception ex)
                {
                    global::Android.Util.Log.Error(Tag, $"Failed to show toast: {ex.Message}");
                }
            });
        }

        internal void OnUsbPermissionGranted(UsbDevice device)
        {
            _permissionRequested = false;
            global::Android.Util.Log.Info(Tag, $"USB permission granted for device: {device.DeviceName}");
            _ = StartAsync();
        }

        internal void OnUsbPermissionDenied()
        {
            _permissionRequested = false;
        }

        internal async void OnUsbDeviceAttached(UsbDevice device)
        {
            global::Android.Util.Log.Info(Tag, $"USB device attached: {device.DeviceName}");

            if (device.VendorId == 0x1EAB && _usbManager != null)
            {
                // Give the Activity intent filter a moment to auto-grant permission
                await Task.Delay(500);

                if (_usbManager.HasPermission(device))
                {
                    _ = StartAsync();
                }
                else if (!_permissionRequested)
                {
                    global::Android.Util.Log.Info(Tag, "Permission not auto-granted, requesting manually");
                    _permissionRequested = true;
                    _usbManager.RequestPermission(device, _permissionIntent);
                }
            }
        }

        internal void OnUsbDeviceDetached(UsbDevice device)
        {
            global::Android.Util.Log.Info(Tag, $"USB device detached: {device.DeviceName}");

            if (device.VendorId == 0x1EAB)
            {
                _scannerBridge?.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _scannerBridge?.Close();

                    var context = Platform.CurrentActivity ?? Platform.AppContext;
                    if (context != null && _usbReceiver != null)
                    {
                        context.UnregisterReceiver(_usbReceiver);
                    }
                }
                catch (System.Exception ex)
                {
                    global::Android.Util.Log.Error(Tag, $"Error disposing scanner service: {ex.Message}");
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Broadcast receiver for USB permission and device events.
        /// </summary>
        private class UsbPermissionReceiver : BroadcastReceiver
        {
            private readonly BarcodeScannerService _service;

            public UsbPermissionReceiver(BarcodeScannerService service)
            {
                _service = service;
            }

            public override void OnReceive(Context? context, Intent? intent)
            {
                if (intent == null)
                    return;

                var action = intent.Action;

                if (action == ActionUsbPermission)
                {
                    var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
                    var granted = intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false);

                    if (device != null && granted)
                    {
                        _service.OnUsbPermissionGranted(device);
                    }
                    else
                    {
                        _service.OnUsbPermissionDenied();
                        global::Android.Util.Log.Warn(Tag, "USB permission denied");
                        _service.ShowToast("USB permission denied");
                    }
                }
                else if (action == UsbManager.ActionUsbDeviceAttached)
                {
                    var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
                    if (device != null)
                    {
                        _service.OnUsbDeviceAttached(device);
                    }
                }
                else if (action == UsbManager.ActionUsbDeviceDetached)
                {
                    var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
                    if (device != null)
                    {
                        _service.OnUsbDeviceDetached(device);
                    }
                }
            }
        }
    }
}
