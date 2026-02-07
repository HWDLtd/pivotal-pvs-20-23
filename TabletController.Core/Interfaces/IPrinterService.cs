using TabletController.Core.DTOs;

namespace TabletController.Core.Interfaces
{
    /// <summary>
    /// Event arguments for print completed events.
    /// </summary>
    public class PrintCompletedEventArgs : EventArgs
    {
        public bool Success { get; }
        public string? ErrorMessage { get; }
        public DateTime CompletedAt { get; }

        public PrintCompletedEventArgs(bool success, string? errorMessage = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
            CompletedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Service interface for thermal receipt printer integration.
    /// Implementations handle device connection and receipt printing.
    /// </summary>
    public interface IPrinterService
    {
        /// <summary>
        /// Raised when a print job completes.
        /// </summary>
        event EventHandler<PrintCompletedEventArgs>? PrintCompleted;

        /// <summary>
        /// Raised when the printer connection status changes.
        /// </summary>
        event EventHandler<bool>? ConnectionChanged;

        /// <summary>
        /// Gets whether the printer is currently connected and ready.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets whether the printer is currently printing.
        /// </summary>
        bool IsPrinting { get; }

        /// <summary>
        /// Gets the printer status message (e.g., "Ready", "Paper Low", "Offline").
        /// </summary>
        string? StatusMessage { get; }

        /// <summary>
        /// Initializes the printer service and requests necessary permissions.
        /// </summary>
        /// <returns>True if initialization was successful.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Connects to the printer.
        /// </summary>
        /// <returns>True if connection was successful.</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnects from the printer.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Prints a receipt with the provided data.
        /// </summary>
        /// <param name="receiptData">The receipt data to print.</param>
        /// <returns>True if print job was successfully sent.</returns>
        Task<bool> PrintReceiptAsync(ReceiptData receiptData);
    }
}
