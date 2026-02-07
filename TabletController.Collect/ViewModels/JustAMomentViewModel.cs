using System.Windows.Input;
using TabletController.Shared.ViewModels;
using TabletController.Shared.Services;
using TabletController.Shared.Utilities;
using TabletController.Core.Interfaces;
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;

namespace TabletController.Collect.ViewModels
{
    public class JustAMomentViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IBarcodeScanner _barcodeScanner;
        private readonly IProductDataService _productDataService;
        private bool _isProcessingScan;
        private string? _collectCode;

        public override bool IsTimeoutEnabled => false;

        public string? CollectCode
        {
            get => _collectCode;
            set => SetProperty(ref _collectCode, value);
        }

        // Custom BackCommand that returns to idle
        public new ICommand BackCommand { get; }

        public JustAMomentViewModel(
            INavigationService navigationService,
            IInactivityTimeoutService timeoutService,
            IBarcodeScanner barcodeScanner,
            IProductDataService productDataService)
            : base(navigationService, timeoutService)
        {
            _navigationService = navigationService;
            _barcodeScanner = barcodeScanner;
            _productDataService = productDataService;
            
            // Create custom BackCommand that navigates to idle
            BackCommand = new Command(() => ExecuteBackCommandAsync().SafeFireAndForget());
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
            if (_isProcessingScan)
                return;

            _isProcessingScan = true;
            HandleBarcodeScannedAsync(e.Barcode).SafeFireAndForget();
        }

        private async Task HandleBarcodeScannedAsync(string barcode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Barcode scanned on JustAMoment page: {barcode}");

                // Validate both the CollectCode and the AdminCode
                if (_productDataService is Services.DemoProductDataService demoService)
                {
                    // Step 1: Validate the CollectCode (must be a valid collection code - product lookup)
                    bool isCollectCodeValid = false;
                    if (!string.IsNullOrEmpty(_collectCode))
                    {
                        var product = await _productDataService.GetProductByBarcodeAsync(_collectCode).ConfigureAwait(false);
                        isCollectCodeValid = product != null;
                        System.Diagnostics.Debug.WriteLine($"CollectCode validation: {_collectCode} -> {(isCollectCodeValid ? "Valid" : "Invalid")}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("CollectCode is empty, validation failed");
                    }

                    // Step 2: Validate the AdminCode (must be in AdminCodes list)
                    bool isAdminCodeValid = demoService.IsAdminCode(barcode);
                    System.Diagnostics.Debug.WriteLine($"AdminCode validation: {barcode} -> {(isAdminCodeValid ? "Valid" : "Invalid")}");

                    // Both must be valid to proceed to success
                    if (isCollectCodeValid && isAdminCodeValid)
                    {
                        System.Diagnostics.Debug.WriteLine($"Both codes valid, navigating to success");
                        
                        // Pass the CollectCode to Success page so it can look up the product and locker details
                        var parameters = new Dictionary<string, object>();
                        if (!string.IsNullOrEmpty(_collectCode))
                        {
                            parameters.Add("collectCode", _collectCode);
                        }
                        await _navigationService.NavigateToAsync("//success", parameters).ConfigureAwait(false);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Validation failed - CollectCode valid: {isCollectCodeValid}, AdminCode valid: {isAdminCodeValid}, navigating to error");
                        await _navigationService.NavigateToAsync("//error").ConfigureAwait(false);
                    }
                }
                else
                {
                    // Fallback if service is not DemoProductDataService
                    System.Diagnostics.Debug.WriteLine("ProductDataService is not DemoProductDataService, navigating to error");
                    await _navigationService.NavigateToAsync("//error").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling barcode scan: {ex.Message}");
                await _navigationService.NavigateToAsync("//error").ConfigureAwait(false);
            }
            finally
            {
                _isProcessingScan = false;
            }
        }

        private async Task ExecuteBackCommandAsync()
        {
            await _navigationService.NavigateToAsync("//idle").ConfigureAwait(false);
        }

        // Custom NextCustomerCommand that navigates to idle
        public new ICommand NextCustomerCommand => new Command(() => ExecuteNextCustomerAsync().SafeFireAndForget());

        private async Task ExecuteNextCustomerAsync()
        {
            await _navigationService.NavigateToAsync("//idle").ConfigureAwait(false);
        }
    }
}
