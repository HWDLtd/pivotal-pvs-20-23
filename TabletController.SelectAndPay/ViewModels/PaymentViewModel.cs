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
    public class PaymentViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IProductDataService _productDataService;
        private ProductSummary? _product;
        private string? _productId;
        private Product? _fullProduct;

        public ProductSummary? Product
        {
            get => _product;
            set => SetProperty(ref _product, value);
        }

        public string? ProductId
        {
            get => _productId;
            set
            {
                if (SetProperty(ref _productId, value) && !string.IsNullOrEmpty(value))
                {
                    if (int.TryParse(value, out int id))
                    {
                        LoadProductAsync(id).SafeFireAndForget();
                    }
                }
            }
        }

        public ICommand MakePaymentCommand { get; }

        public PaymentViewModel(INavigationService navigationService, IProductDataService productDataService, IInactivityTimeoutService timeoutService)
            : base(navigationService, timeoutService)
        {
            _navigationService = navigationService;
            _productDataService = productDataService;
            MakePaymentCommand = new Command(() => ExecuteMakePaymentCommandAsync().SafeFireAndForget());
        }

        private async Task LoadProductAsync(int productId)
        {
            IsBusy = true;
            try
            {
                var product = await _productDataService.GetProductByIdAsync(productId).ConfigureAwait(false);
                if (product != null)
                {
                    _fullProduct = product;
                    Product = new ProductSummary
                    {
                        ProductId = product.ProductId,
                        Name = product.Name,
                        ShortDescription = product.ShortDescription,
                        Price = product.Price,
                        ImageUrl = product.ImageUrl
                    };
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteMakePaymentCommandAsync()
        {
            ResetInactivityTimer();
            try
            {
                if (_fullProduct == null || Product == null)
                    return;

                // Create transaction details (simulating PED response)
                var transactionDetails = new TransactionDetails
                {
                    MerchantId = "PIV0116",
                    TransactionId = $"PTX{DateTime.Now:yyyyMMddHHmmss}",
                    ReceiptNumber = $"PIV{DateTime.Now:yyyyMMddHHmmss}",
                    TransactionType = "Sale",
                    PaymentMethod = "Total paid by card.",
                    TransactionDate = DateTime.Now
                };

                // Create basket item
                var basketItem = new BasketItem
                {
                    ProductId = _fullProduct.ProductId,
                    Quantity = 1,
                    UnitPrice = _fullProduct.Price,
                };

                // Create order
                var order = new Order
                {
                    Basket = new List<BasketItem> { basketItem },
                    TransactionDetails = transactionDetails,
                    OrderDate = DateTime.Now
                };

                // Serialize order to JSON for navigation
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Converters = { new JsonStringEnumConverter(), new DateTimeConverter() }
                };
                var orderJson = JsonSerializer.Serialize(order, options);

                // Navigate with order
                var parameters = new Dictionary<string, object>
                {
                    { "orderJson", orderJson }
                };

                await _navigationService.NavigateToAsync("//thankYou", parameters).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation to thank you failed: {ex.Message}");
            }
        }
    }
}

