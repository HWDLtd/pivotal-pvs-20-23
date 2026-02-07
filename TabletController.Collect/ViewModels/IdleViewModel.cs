using System.Windows.Input;
using TabletController.Shared.ViewModels;
using TabletController.Shared.Services;
using TabletController.Shared.Utilities;
using TabletController.Core.Interfaces;
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;

namespace TabletController.Collect.ViewModels
{
    public class IdleViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IBarcodeScanner _barcodeScanner;

        public override bool IsTimeoutEnabled => false;

        public IdleViewModel(
            INavigationService navigationService,
            IInactivityTimeoutService timeoutService,
            IBarcodeScanner barcodeScanner)
            : base(navigationService, timeoutService)
        {
            _navigationService = navigationService;
            _barcodeScanner = barcodeScanner;
        }

        public override void OnPageAppearing()
        {
            base.OnPageAppearing();
            _barcodeScanner.BarcodeScanned += OnBarcodeScanned;
        }

        public override void OnPageDisappearing()
        {
            base.OnPageDisappearing();
            _barcodeScanner.BarcodeScanned -= OnBarcodeScanned;
        }

        private void OnBarcodeScanned(object? sender, BarcodeScannedEventArgs e)
        {
            HandleBarcodeScannedAsync(e.Barcode).SafeFireAndForget();
        }

        private async Task HandleBarcodeScannedAsync(string barcode)
        {
            try
            {
                // Navigate to "Just a Moment" page with the scanned CollectCode
                var parameters = new Dictionary<string, object>
                {
                    { "collectCode", barcode }
                };
                await _navigationService.NavigateToAsync("//justAMoment", parameters).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
            }
        }
    }
}
