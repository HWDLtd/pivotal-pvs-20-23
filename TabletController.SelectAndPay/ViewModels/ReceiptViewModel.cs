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
    public class ReceiptViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IReceiptService _receiptService;
        private readonly IInactivityTimeoutService _timeoutService;
        private string? _orderJson;
        private ReceiptData? _receiptData;

        public ReceiptData? ReceiptData
        {
            get => _receiptData;
            private set => SetProperty(ref _receiptData, value);
        }

        public string? OrderJson
        {
            get => _orderJson;
            set
            {
                if (SetProperty(ref _orderJson, value) && !string.IsNullOrEmpty(value))
                {
                    LoadReceiptAsync(value).SafeFireAndForget();
                }
            }
        }

        public ReceiptViewModel(
            INavigationService navigationService,
            IReceiptService receiptService,
            IInactivityTimeoutService timeoutService)
            : base(navigationService, timeoutService)
        {
            _navigationService = navigationService;
            _receiptService = receiptService;
            _timeoutService = timeoutService;
        }

        private async Task LoadReceiptAsync(string orderJson)
        {
            try
            {
                IsBusy = true;
                var decodedJson = WebUtility.UrlDecode(orderJson) ?? orderJson;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(), new DateTimeConverter() }
                };
                var order = JsonSerializer.Deserialize<Order>(decodedJson, options);

                if (order != null)
                {
                    // Generate receipt data
                    ReceiptData = await _receiptService.GenerateReceiptAsync(order).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load receipt: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public override void OnPageDisappearing()
        {
            base.OnPageDisappearing();
        }
    }
}
