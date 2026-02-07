namespace TabletController.Core.Interfaces
{
    /// <summary>
    /// Event arguments for barcode scanned events.
    /// </summary>
    public class BarcodeScannedEventArgs : EventArgs
    {
        public string Barcode { get; }
        public DateTime ScannedAt { get; }

        public BarcodeScannedEventArgs(string barcode)
        {
            Barcode = barcode;
            ScannedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Service interface for barcode scanner integration.
    /// Implementations handle device connection and barcode data reception.
    /// </summary>
    public interface IBarcodeScanner
    {
        /// <summary>
        /// Raised when a barcode is successfully scanned.
        /// </summary>
        event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

        /// <summary>
        /// Raised when the scanner connection status changes.
        /// </summary>
        event EventHandler<bool>? ConnectionChanged;

        /// <summary>
        /// Gets whether the scanner is currently connected and ready.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the last scanned barcode value, if any.
        /// </summary>
        string? LastBarcode { get; }

        /// <summary>
        /// Initializes the scanner and requests necessary permissions.
        /// </summary>
        /// <returns>True if initialization was successful.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Starts listening for barcode scans.
        /// </summary>
        /// <returns>True if scanner was successfully started.</returns>
        Task<bool> StartAsync();

        /// <summary>
        /// Stops listening for barcode scans.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Triggers a scan (for scanners that support programmatic triggering).
        /// </summary>
        Task TriggerScanAsync();
    }
}
