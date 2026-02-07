using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using TabletController.SelectAndPay.Services;
using TabletController.Shared.ViewModels;
using TabletController.Shared.Services;
using TabletController.Shared.Utilities;
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;
using TabletController.Core.DTOs;
using TabletController.Core.Interfaces;

namespace TabletController.SelectAndPay.ViewModels
{
    public class ThankYouViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IProductDataService _productDataService;
        private readonly IInactivityTimeoutService _timeoutService;
        private readonly IReceiptService _receiptService;
        private readonly IPrinterService _printerService;
        private string? _orderJson;
        private Order? _order;
        private string _orderNumber = string.Empty;

        public string OrderNumber
        {
            get => _orderNumber;
            private set => SetProperty(ref _orderNumber, value);
        }

        public string? OrderJson
        {
            get => _orderJson;
            set
            {
                if (SetProperty(ref _orderJson, value) && !string.IsNullOrEmpty(value))
                {
                    LoadOrderAsync(value).SafeFireAndForget();
                }
            }
        }

        public ThankYouViewModel(
            INavigationService navigationService,
            IProductDataService productDataService,
            IInactivityTimeoutService timeoutService,
            IReceiptService receiptService,
            IPrinterService printerService)
            : base(navigationService, timeoutService)
        {
            _navigationService = navigationService;
            _productDataService = productDataService;
            _timeoutService = timeoutService;
            _receiptService = receiptService;
            _printerService = printerService;
        }

        private async Task LoadOrderAsync(string orderJson)
        {
            try
            {
                var decodedJson = WebUtility.UrlDecode(orderJson) ?? orderJson;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(), new DateTimeConverter() }
                };
                _order = JsonSerializer.Deserialize<Order>(decodedJson, options);

                if (_order != null && _order.Basket.Count > 0)
                {
                    var product = await _productDataService.GetProductByIdAsync(_order.Basket[0].ProductId);
                    if (product != null)
                    {
                        OrderNumber = product.CollectionCode ?? "N/A";

                        // Generate and print receipt
                        await PrintReceiptAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load order: {ex.Message}");
            }
        }

        private async Task PrintReceiptAsync()
        {
            if (_order == null)
                return;

            try
            {
                // Ensure printer is connected
                var isConnected = await _printerService.ConnectAsync().ConfigureAwait(false);
                if (!isConnected)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to connect to printer");
                    return;
                }

                // Generate receipt data
                var receiptData = await _receiptService.GenerateReceiptAsync(_order).ConfigureAwait(false);

                // Print receipt
                var printSuccess = await _printerService.PrintReceiptAsync(receiptData).ConfigureAwait(false);
                if (!printSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to print receipt");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error printing receipt: {ex.Message}");
            }
        }

        public override void OnPageDisappearing()
        {
            base.OnPageDisappearing();
        }
    }
}

