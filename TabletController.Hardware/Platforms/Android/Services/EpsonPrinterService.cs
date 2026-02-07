using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Usb;
using Android.OS;
using Android.Widget;
using TabletController.Core.DTOs;
using TabletController.Core.Enums;
using TabletController.Core.Interfaces;
using Com.Epson.Epos2.Printer;
using Com.Epson.Epos2;

namespace TabletController.Hardware.Platforms.Android.Services
{
    /// <summary>
    /// Android implementation of IPrinterService using Epson ePOS2 SDK.
    /// </summary>
    public class EpsonPrinterService : Java.Lang.Object, IPrinterService, IReceiveListener
    {
        private const string Tag = "EpsonPrinterService";
        private const string ActionUsbPermission = "com.tabletcontroller.PRINTER_USB_PERMISSION";
        private const int EpsonVendorId = 0x04B8; // 1208 decimal

        // Epson SDK constants (from ePOS2 SDK documentation)
        private const int ParamDefault = -2;
        private const int ModelAnk = 0;
        private const int AlignCenter = 1;
        private const int AlignLeft = 0;
        private const int AlignRight = 2;
        private const int CutFeed = 1;

        // Barcode type constants (ePOS2 SDK values)
        private const int BarcodeCode128 = 10;
        private const int BarcodeCode39 = 6;
        private const int BarcodeEan13 = 2;
        private const int BarcodeEan8 = 4;
        private const int BarcodeItf = 7;
        private const int BarcodeUpcA = 0;
        private const int BarcodeUpcE = 1;
        private const int BarcodeQr = 50;

        // Symbol type constants (ePOS2 SDK values)
        private const int SymbolPdf417Standard = 0;
        private const int SymbolPdf417Truncated = 1;
        private const int SymbolQrcodeModel1 = 2;
        private const int SymbolQrcodeModel2 = 3;
        private const int SymbolQrcodeMicro = 4;
        private const int SymbolMaxicodeMode2 = 5;
        private const int SymbolMaxicodeMode3 = 6;
        private const int SymbolMaxicodeMode4 = 7;
        private const int SymbolMaxicodeMode5 = 8;
        private const int SymbolMaxicodeMode6 = 9;
        private const int SymbolGs1DatabarStacked = 10;
        private const int SymbolGs1DatabarStackedOmnidirectional = 11;
        private const int SymbolGs1DatabarExpandedStacked = 12;
        private const int SymbolAzteccodeFullrange = 13;
        private const int SymbolAzteccodeCompact = 14;
        private const int SymbolDatamatrixSquare = 15;
        private const int SymbolDatamatrixRectangle8 = 16;
        private const int SymbolDatamatrixRectangle12 = 17;
        private const int SymbolDatamatrixRectangle16 = 18;

        // Symbol error correction level constants (ePOS2 SDK values)
        private const int LevelDefault = 0;
        private const int LevelL = 1;  // ~7% error correction
        private const int LevelM = 2;  // ~15% error correction
        private const int LevelQ = 3;  // ~25% error correction
        private const int LevelH = 4;  // ~30% error correction

        // Barcode HRI (Human Readable Interpretation) positions
        private const int HriNone = 0;
        private const int HriAbove = 1;
        private const int HriBelow = 2;
        private const int HriAboveAndBelow = 3;

        // Barcode font
        private const int FontA = 0;
        private const int FontB = 1;

        // Image color constants
        private const int Color1 = 1; // Monochrome
        private const int Color2 = 2;
        private const int Color3 = 3;
        private const int Color4 = 4;

        // Image halftone mode constants
        private const int HalftoneDither = 0;
        private const int HalftoneErrorDiffusion = 1;
        private const int HalftoneThreshold = 2;

        // Image brightness (0.0 to 1.0)
        private const double ImageBrightness = 1.0;

        // Image compression
        private const int CompressNone = 0;
        private const int CompressDeflate = 1;

        // Line style constants (ePOS2 SDK values)
        private const int LineThin = 0;
        private const int LineMedium = 1;
        private const int LineThick = 2;
        private const int LineDouble = 3;

        private Printer? _printer;
        private UsbManager? _usbManager;
        private PendingIntent? _permissionIntent;
        private PrinterUsbReceiver? _usbReceiver;
        private bool _isInitialized;
        private bool _isConnected;
        private bool _isPrinting;
        private string? _statusMessage;
        private readonly object _lock = new();

        public event EventHandler<PrintCompletedEventArgs>? PrintCompleted;
        public event EventHandler<bool>? ConnectionChanged;

        public bool IsConnected => _isConnected;
        public bool IsPrinting => _isPrinting;
        public string? StatusMessage => _statusMessage;

        public Task<bool> InitializeAsync()
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_isInitialized) return true;

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
                        _usbReceiver = new PrinterUsbReceiver(this);
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

                        _isInitialized = true;
                        _statusMessage = "Initialized";
                        global::Android.Util.Log.Info(Tag, "Printer service initialized");
                        return true;
                    }
                    catch (System.Exception ex)
                    {
                        global::Android.Util.Log.Error(Tag, $"Failed to initialize printer: {ex.Message}");
                        _statusMessage = $"Init failed: {ex.Message}";
                        return false;
                    }
                }
            });
        }

        public Task<bool> ConnectAsync()
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!_isInitialized)
                    {
                        global::Android.Util.Log.Warn(Tag, "Printer not initialized");
                        return false;
                    }

                    if (_isConnected)
                    {
                        global::Android.Util.Log.Info(Tag, "Printer already connected");
                        return true;
                    }

                    try
                    {
                        var context = Platform.CurrentActivity ?? Platform.AppContext;
                        if (context == null) return false;

                        // Check for USB permission
                        var epsonDevice = FindEpsonDevice();
                        if (epsonDevice != null && _usbManager != null)
                        {
                            if (!_usbManager.HasPermission(epsonDevice))
                            {
                                global::Android.Util.Log.Info(Tag, "Requesting USB permission");
                                _usbManager.RequestPermission(epsonDevice, _permissionIntent);
                                _statusMessage = "Requesting permission...";
                                return false;
                            }
                        }
                        else if (epsonDevice == null)
                        {
                            global::Android.Util.Log.Warn(Tag, "No Epson printer found");
                            _statusMessage = "Printer not found";
                            return false;
                        }

                        // Create and connect printer
                        _printer = new Printer(Printer.TmM30iii, ModelAnk, context);
                        _printer.SetReceiveEventListener(this);
                        _printer.Connect("USB:", ParamDefault);

                        _isConnected = true;
                        _statusMessage = "Connected";
                        global::Android.Util.Log.Info(Tag, "Printer connected");
                        ShowToast("Printer connected");

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ConnectionChanged?.Invoke(this, true);
                        });

                        return true;
                    }
                    catch (System.Exception ex)
                    {
                        global::Android.Util.Log.Error(Tag, $"Failed to connect: {ex.Message}");
                        _statusMessage = $"Connection failed: {ex.Message}";
                        _printer?.Dispose();
                        _printer = null;
                        return false;
                    }
                }
            });
        }

        public Task DisconnectAsync()
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        if (_printer != null)
                        {
                            try
                            {
                                _printer.Disconnect();
                            }
                            catch { }

                            _printer.ClearCommandBuffer();
                            _printer.SetReceiveEventListener(null);
                            _printer.Dispose();
                            _printer = null;
                        }

                        _isConnected = false;
                        _statusMessage = "Disconnected";
                        global::Android.Util.Log.Info(Tag, "Printer disconnected");

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ConnectionChanged?.Invoke(this, false);
                        });
                    }
                    catch (System.Exception ex)
                    {
                        global::Android.Util.Log.Error(Tag, $"Failed to disconnect: {ex.Message}");
                    }
                }
            });
        }

        public Task<bool> PrintReceiptAsync(ReceiptData receiptData)
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!_isConnected || _printer == null)
                    {
                        global::Android.Util.Log.Warn(Tag, "Printer not connected");
                        return false;
                    }

                    if (_isPrinting)
                    {
                        global::Android.Util.Log.Warn(Tag, "Print job already in progress");
                        return false;
                    }

                    try
                    {
                        _isPrinting = true;
                        _statusMessage = "Printing...";
                        _printer.ClearCommandBuffer();

                        foreach (var line in receiptData.Lines)
                        {
                            ProcessReceiptLine(line);
                        }

                        _printer.SendData(ParamDefault);
                        return true;
                    }
                    catch (System.Exception ex)
                    {
                        global::Android.Util.Log.Error(Tag, $"Failed to print: {ex.Message}");
                        _isPrinting = false;
                        _statusMessage = $"Print failed: {ex.Message}";
                        _printer?.ClearCommandBuffer();
                        return false;
                    }
                }
            });
        }

        private void ProcessReceiptLine(ReceiptLine line)
        {
            switch (line.LineType)
            {
                case ReceiptLineType.Text:
                    ProcessTextLine(line);
                    break;

                case ReceiptLineType.BlankLine:
                    _printer?.AddFeedLine(1);
                    break;

                case ReceiptLineType.Cut:
                    _printer?.AddCut(CutFeed);
                    break;

                case ReceiptLineType.Feed:
                    _printer?.AddFeedLine(1);
                    break;

                case ReceiptLineType.Image:
                    ProcessImageLine(line);
                    break;

                case ReceiptLineType.Barcode:
                    ProcessBarcodeLine(line);
                    break;

                case ReceiptLineType.QrCode:
                    ProcessQrCodeLine(line);
                    break;
            }
        }


        private void ProcessTextLine(ReceiptLine line)
        {
            if (_printer == null || string.IsNullOrEmpty(line.Text))
                return;

            switch (line.Alignment)
            {
                case ReceiptAlignment.Left:
                    _printer.AddTextAlign(AlignLeft);
                    break;
                case ReceiptAlignment.Center:
                    _printer.AddTextAlign(AlignCenter);
                    break;
                case ReceiptAlignment.Right:
                    _printer.AddTextAlign(AlignRight);
                    break;
            }

            switch (line.TextSize)
            {
                case TextSize.Regular:
                    _printer.AddTextSize(1, 1);
                    break;
                case TextSize.Large:
                    _printer.AddTextSize(2, 2);
                    break;
            }

            // Format is reverse (bit), underline (bit), bold (bit), color (int)
            int reverse = 0;
            int underline = 0;
            int bold = line.IsBold ? 1 : 0;
            int colour = 1;

            _printer.AddTextStyle(reverse, underline, bold, colour);

            _printer.AddText(line.Text + "\n");

            // Reset styling
            _printer.AddTextStyle(0, 0, 0, 1);
            _printer.AddTextSize(1, 1);
        }

        private void ProcessImageLine(ReceiptLine line)
        {
            if (_printer == null || string.IsNullOrEmpty(line.Text))
                return;

            try
            {
                // Set alignment
                switch (line.Alignment)
                {
                    case ReceiptAlignment.Left:
                        _printer.AddTextAlign(AlignLeft);
                        break;
                    case ReceiptAlignment.Center:
                        _printer.AddTextAlign(AlignCenter);
                        break;
                    case ReceiptAlignment.Right:
                        _printer.AddTextAlign(AlignRight);
                        break;
                }

                // Load image from resources
                var context = Platform.CurrentActivity ?? Platform.AppContext;
                if (context == null)
                {
                    global::Android.Util.Log.Error(Tag, "No Android context available for image loading");
                    return;
                }

                // Get image resource ID
                var imageName = System.IO.Path.GetFileNameWithoutExtension(line.Text);
                var resourceId = context.Resources?.GetIdentifier(imageName, "drawable", context.PackageName) ?? 0;

                if (resourceId == 0)
                {
                    global::Android.Util.Log.Warn(Tag, $"Image resource not found: {line.Text}");
                    return;
                }

                // Load bitmap from resource
                using var bitmap = BitmapFactory.DecodeResource(context.Resources, resourceId);
                if (bitmap == null)
                {
                    global::Android.Util.Log.Warn(Tag, $"Failed to decode image: {line.Text}");
                    return;
                }

                // Use specified width/height or bitmap dimensions
                var width = line.Width ?? bitmap.Width;
                var height = line.Height ?? bitmap.Height;

                // Scale bitmap if dimensions are specified
                Bitmap scaledBitmap = bitmap;
                if (line.Width.HasValue || line.Height.HasValue)
                {
                    scaledBitmap = Bitmap.CreateScaledBitmap(bitmap, width, height, true);
                }

                // Add image to printer
                // Parameters: bitmap, x, y, width, height, color, halftone, brightness, compress
                // Signature: AddImage(Bitmap, int x, int y, int width, int height, int color, int halftone, int ?, double brightness, int compress)
                // x=0, y=0 means use current position
                // color: Color1 (monochrome)
                // halftone: HalftoneDither (0)
                // brightness: 1.0 (full brightness)
                // compress: CompressNone (0)
                // Note: Parameter 7 (after halftone) is unknown - using 0 as default
                _printer.AddImage(scaledBitmap, 0, 0, width, height, Color1, HalftoneDither, 0, ImageBrightness, CompressNone);

                // Dispose scaled bitmap if it was created
                if (scaledBitmap != bitmap)
                {
                    scaledBitmap?.Dispose();
                }
            }
            catch (System.Exception ex)
            {
                global::Android.Util.Log.Error(Tag, $"Failed to process image: {ex.Message}");
            }
        }

        private void ProcessBarcodeLine(ReceiptLine line)
        {
            if (_printer == null || string.IsNullOrEmpty(line.Text))
                return;

            try
            {
                switch (line.Alignment)
                {
                    case ReceiptAlignment.Left:
                        _printer.AddTextAlign(AlignLeft);
                        break;
                    case ReceiptAlignment.Center:
                        _printer.AddTextAlign(AlignCenter);
                        break;
                    case ReceiptAlignment.Right:
                        _printer.AddTextAlign(AlignRight);
                        break;
                }

                var barcodeType = BarcodeCode128;

                var width = line.Width ?? 2;
                var height = line.Height ?? 100; // Default height in dots

                // Add barcode
                // Parameters: data, type, hri, font, width, height
                // HRI = Human Readable Interpretation (text below barcode)
                // Font = FontA or FontB
                _printer.AddSymbol(line.Text, barcodeType, HriBelow, FontA, width, height);
            }
            catch (System.Exception ex)
            {
                global::Android.Util.Log.Error(Tag, $"Failed to process barcode: {ex.Message}");
            }
        }

        private void ProcessQrCodeLine(ReceiptLine line)
        {
            if (_printer == null || string.IsNullOrEmpty(line.Text))
                return;

            try
            {
                switch (line.Alignment)
                {
                    case ReceiptAlignment.Left:
                        _printer.AddTextAlign(AlignLeft);
                        break;
                    case ReceiptAlignment.Center:
                        _printer.AddTextAlign(AlignCenter);
                        break;
                    case ReceiptAlignment.Right:
                        _printer.AddTextAlign(AlignRight);
                        break;
                }

                var width = line.Width ?? 8;

                // Add symbol (QR code, PDF417, DataMatrix, etc.)
                // Parameters: data, type, level, width, height, size
                // Height and Size are ignored for QR codes
                _printer.AddSymbol(line.Text, SymbolQrcodeModel1, ParamDefault, width, 8, 100);
            }
            catch (System.Exception ex)
            {
                global::Android.Util.Log.Error(Tag, $"Failed to process barcode: {ex.Message}");
            }
        }

        // IReceiveListener implementation - Epson SDK callback
        public void OnPtrReceive(Printer? printerObj, int code, PrinterStatusInfo? status, string? printJobId)
        {
            global::Android.Util.Log.Info(Tag, $"Print callback: code={code}");

            _isPrinting = false;
            var success = (code == Epos2CallbackCode.CodeSuccess);
            _statusMessage = success ? "Ready" : $"Error: {code}";

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PrintCompleted?.Invoke(this, new PrintCompletedEventArgs(success,
                    success ? null : $"Print error code: {code}"));
            });

            if (success)
            {
                //ShowToast("Receipt printed");
            }
            else
            {
                ShowToast($"Print failed: {code}");
            }
        }


        private UsbDevice? FindEpsonDevice()
        {
            if (_usbManager == null) return null;

            var deviceList = _usbManager.DeviceList;
            if (deviceList == null) return null;

            foreach (var device in deviceList.Values)
            {
                if (device?.VendorId == EpsonVendorId)
                {
                    global::Android.Util.Log.Info(Tag, $"Found Epson device: VID={device.VendorId}, PID={device.ProductId}");
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
                catch { }
            });
        }

        internal void OnUsbPermissionGranted(UsbDevice device)
        {
            global::Android.Util.Log.Info(Tag, $"USB permission granted for: {device.DeviceName}");
            _ = ConnectAsync();
        }

        internal void OnUsbDeviceAttached(UsbDevice device)
        {
            global::Android.Util.Log.Info(Tag, $"USB device attached: VID={device.VendorId}");
            if (device.VendorId == EpsonVendorId && _usbManager != null)
            {
                if (!_usbManager.HasPermission(device))
                {
                    _usbManager.RequestPermission(device, _permissionIntent);
                }
                else
                {
                    _ = ConnectAsync();
                }
            }
        }

        internal void OnUsbDeviceDetached(UsbDevice device)
        {
            global::Android.Util.Log.Info(Tag, $"USB device detached: VID={device.VendorId}");
            if (device.VendorId == EpsonVendorId)
            {
                _ = DisconnectAsync();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _printer?.Disconnect();
                    _printer?.Dispose();

                    var context = Platform.CurrentActivity ?? Platform.AppContext;
                    if (context != null && _usbReceiver != null)
                    {
                        context.UnregisterReceiver(_usbReceiver);
                    }
                }
                catch { }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// USB broadcast receiver for printer permission and device events.
        /// </summary>
        private class PrinterUsbReceiver : BroadcastReceiver
        {
            private readonly EpsonPrinterService _service;

            public PrinterUsbReceiver(EpsonPrinterService service) => _service = service;

            public override void OnReceive(Context? context, Intent? intent)
            {
                if (intent == null) return;
                var action = intent.Action;

                if (action == ActionUsbPermission)
                {
                    var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
                    var granted = intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false);

                    if (device != null && granted)
                        _service.OnUsbPermissionGranted(device);
                    else
                    {
                        global::Android.Util.Log.Warn(Tag, "USB permission denied");
                        _service.ShowToast("Printer permission denied");
                    }
                }
                else if (action == UsbManager.ActionUsbDeviceAttached)
                {
                    var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
                    if (device != null) _service.OnUsbDeviceAttached(device);
                }
                else if (action == UsbManager.ActionUsbDeviceDetached)
                {
                    var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
                    if (device != null) _service.OnUsbDeviceDetached(device);
                }
            }
        }
    }
}
