using TabletController.Core.DTOs;
using TabletController.Core.Interfaces;

namespace TabletController.Hardware.Services
{
    /// <summary>
    /// Stub implementation of IPrinterService for non-Android platforms.
    /// The Epson TM-m30 III printer is only supported on Android.
    /// </summary>
    public class StubPrinterService : IPrinterService
    {
        public event EventHandler<PrintCompletedEventArgs>? PrintCompleted;
        public event EventHandler<bool>? ConnectionChanged;

        public bool IsConnected => false;
        public bool IsPrinting => false;
        public string? StatusMessage => "Printer not available on this platform";

        public Task<bool> InitializeAsync() => Task.FromResult(false);
        public Task<bool> ConnectAsync() => Task.FromResult(false);
        public Task DisconnectAsync() => Task.CompletedTask;
        public Task<bool> PrintReceiptAsync(ReceiptData receiptData) => Task.FromResult(false);
    }
}
