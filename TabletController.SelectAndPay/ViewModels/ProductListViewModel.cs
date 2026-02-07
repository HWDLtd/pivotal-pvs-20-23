using System.Collections.ObjectModel;
using System.Windows.Input;
using TabletController.SelectAndPay.Services;
using TabletController.Shared.ViewModels;
using TabletController.Shared.Services;
using TabletController.Shared.Utilities;
using TabletController.Shared.Controls;
using TabletController.SelectAndPay.Converters;
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;
using TabletController.Core.DTOs;
using TabletController.Core.Interfaces;

namespace TabletController.SelectAndPay.ViewModels
{
    public class ProductListViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IProductDataService _productDataService;
        private Category? _selectedCategory;
        private bool _preserveCategoryOnReturn;

        public ObservableCollection<Category> Categories { get; }
        public ObservableCollection<ProductSummary> Products { get; }
        public ICommand SelectCategoryCommand { get; }
        public ICommand SelectProductCommand { get; }

        public ProductListViewModel(INavigationService navigationService, IProductDataService productDataService, IInactivityTimeoutService timeoutService)
            : base(navigationService, timeoutService)
        {
            _navigationService = navigationService;
            _productDataService = productDataService;
            Categories = new ObservableCollection<Category>();
            Products = new ObservableCollection<ProductSummary>();
            SelectCategoryCommand = new Command<Category>(ExecuteSelectCategoryCommand);
            SelectProductCommand = new Command<ProductSummary>((product) => ExecuteSelectProductCommandAsync(product).SafeFireAndForget());
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync().ConfigureAwait(false);

            if (SelectedCategory != null)
            {
                await LoadProductsForCategoryAsync(SelectedCategory.CategoryId).ConfigureAwait(false);
            }
        }

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        private async Task LoadDataAsync()
        {
            // If categories already loaded, only preserve selection when returning from ProductDetail
            if (Categories.Any())
            {
                if (!_preserveCategoryOnReturn && Categories.Any())
                {
                    SelectedCategory = Categories.First();
                }
                _preserveCategoryOnReturn = false;
                return;
            }

            IsBusy = true;
            try
            {
                var categories = await _productDataService.GetCategoriesAsync().ConfigureAwait(false);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Categories.Clear();
                    foreach (var category in categories)
                    {
                        Categories.Add(category);
                    }

                    if (Categories.Any())
                    {
                        SelectedCategory = Categories.First();
                    }
                });
            }
            finally
            {
                IsBusy = false;
                _preserveCategoryOnReturn = false;
            }
        }

        private async Task LoadProductsForCategoryAsync(int categoryId)
        {
            IsBusy = true;
            try
            {
                var products = await _productDataService.GetProductsByCategoryAsync(categoryId).ConfigureAwait(false);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Products.Clear();
                    foreach (var product in products)
                    {
                        Products.Add(new ProductSummary
                        {
                            ProductId = product.ProductId,
                            Name = product.Name,
                            ShortDescription = product.ShortDescription,
                            Price = product.Price,
                            ImageUrl = product.ImageUrl
                        });
                    }
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteSelectCategoryCommand(Category category)
        {
            ResetInactivityTimer();
            SelectedCategory = category;
            LoadProductsForCategoryAsync(category.CategoryId).SafeFireAndForget();
        }

        private async Task ExecuteSelectProductCommandAsync(ProductSummary product)
        {
            ResetInactivityTimer();
            _preserveCategoryOnReturn = true;
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "productId", product.ProductId.ToString() }
                };
                await _navigationService.NavigateToAsync("//productDetail", parameters).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation to product detail failed: {ex.Message}");
            }
        }
    }
}