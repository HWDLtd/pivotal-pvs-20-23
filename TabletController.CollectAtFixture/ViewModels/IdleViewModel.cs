using System.Windows.Input;
using TabletController.Shared.ViewModels;
using TabletController.Shared.Services;
using TabletController.Shared.Utilities;
using TabletController.Core.Interfaces;
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;

namespace TabletController.CollectAtFixture.ViewModels
{
    public class IdleViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IBarcodeScanner _barcodeScanner;
        private readonly IProductDataService _productDataService;

        public override bool IsTimeoutEnabled => false;

        public IdleViewModel(
            INavigationService navigationService,
            IInactivityTimeoutService timeoutService,
            IBarcodeScanner barcodeScanner,
            IProductDataService productDataService)
            : base(navigationService, timeoutService)
        {
            _navigationService = navigationService;
            _barcodeScanner = barcodeScanner;
            _productDataService = productDataService;

            BrowseCommand = new Command(() => ExecuteBrowseCommandAsync().SafeFireAndForget());
        }

        public ICommand BrowseCommand { get; }

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
                var product = await _productDataService.GetProductByBarcodeAsync(barcode).ConfigureAwait(false);
                if (product == null)
                {
                    System.Diagnostics.Debug.WriteLine($"No product found for barcode: {barcode}");
                    return;
                }

                // Push productList as back destination before navigating
                _navigationService.PushHistoryEntry("//productList");

                var parameters = new Dictionary<string, object>
                {
                    { "productId", product.ProductId.ToString() }
                };
                await _navigationService.NavigateToAsync("//productDetail", parameters, skipHistoryPush: true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Barcode navigation failed: {ex.Message}");
            }
        }

        private async Task ExecuteBrowseCommandAsync()
        {
            try
            {
                await _navigationService.NavigateToAsync("//productList").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
            }
        }
    }
}
