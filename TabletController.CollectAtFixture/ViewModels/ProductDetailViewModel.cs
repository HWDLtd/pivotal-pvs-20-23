using System.Collections.ObjectModel;
using System.Windows.Input;
using TabletController.CollectAtFixture.Services;
using TabletController.Shared.ViewModels;
using TabletController.Shared.Services;
using TabletController.Shared.Utilities;
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;
using TabletController.Core.DTOs;
using TabletController.Core.Interfaces;

namespace TabletController.CollectAtFixture.ViewModels
{
    public class ProductDetailViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IProductDataService _productDataService;
        private Product? _product;
        private string? _productId;

        public ObservableCollection<string> HighlightsList { get; } = new();

        public Product? Product
        {
            get => _product;
            set
            {
                if (SetProperty(ref _product, value))
                {
                    UpdateHighlightsList();
                }
            }
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

        public ICommand BuyNowCommand { get; }

        public ProductDetailViewModel(INavigationService navigationService, IProductDataService productDataService, IInactivityTimeoutService timeoutService)
            : base(navigationService, timeoutService)
        {
            _navigationService = navigationService;
            _productDataService = productDataService;
            BuyNowCommand = new Command(() => ExecuteBuyNowCommandAsync().SafeFireAndForget());
        }

        public async Task LoadProductAsync(int productId)
        {
            IsBusy = true;
            try
            {
                Product = await _productDataService.GetProductByIdAsync(productId).ConfigureAwait(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateHighlightsList()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                HighlightsList.Clear();
                if (!string.IsNullOrEmpty(Product?.Highlights))
                {
                    var highlights = Product.Highlights.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var highlight in highlights)
                    {
                        HighlightsList.Add(highlight.Trim());
                    }
                }
            });
        }

        private async Task ExecuteBuyNowCommandAsync()
        {
            ResetInactivityTimer();
            try
            {
                if (Product == null)
                    return;

                var parameters = new Dictionary<string, object>
                {
                    { "productId", Product.ProductId.ToString() }
                };
                await _navigationService.NavigateToAsync("//payment", parameters).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation to payment failed: {ex.Message}");
            }
        }
    }
}

