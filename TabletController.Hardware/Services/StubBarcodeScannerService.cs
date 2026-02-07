using TabletController.Core.Interfaces;

namespace TabletController.Hardware.Services
{
    /// <summary>
    /// Stub implementation of IBarcodeScanner for non-Android platforms.
    /// The Newland FM430 scanner is only supported on Android.
    /// </summary>
    public class StubBarcodeScannerService : IBarcodeScanner
    {
        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;
        public event EventHandler<bool>? ConnectionChanged;

        public bool IsConnected => false;
        public string? LastBarcode => null;

        public Task<bool> InitializeAsync()
        {
            // Scanner not available on this platform
            return Task.FromResult(false);
        }

        public Task<bool> StartAsync()
        {
            // Scanner not available on this platform
            return Task.FromResult(false);
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        public Task TriggerScanAsync()
        {
            return Task.CompletedTask;
        }
    }
}
