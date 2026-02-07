using TabletController.SelectAndPay.ViewModels;
using TabletController.Shared.ViewModels;

namespace TabletController.SelectAndPay.Pages
{
    [QueryProperty(nameof(ProductId), "productId")]
    public partial class ProductDetailPage : ContentPage
    {
        public ProductDetailPage(ProductDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            // Add touch gesture to reset inactivity timer
            var pointerGesture = new PointerGestureRecognizer();
            pointerGesture.PointerMoved += (s, e) => (BindingContext as BaseViewModel)?.ResetInactivityTimer();
            PageContent.GestureRecognizers.Add(pointerGesture);

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => (BindingContext as BaseViewModel)?.ResetInactivityTimer();
            PageContent.GestureRecognizers.Add(tapGesture);
        }

        public string? ProductId
        {
            get => (BindingContext as ProductDetailViewModel)?.ProductId;
            set
            {
                if (BindingContext is ProductDetailViewModel viewModel)
                {
                    viewModel.ProductId = value;
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as BaseViewModel)?.OnPageAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            (BindingContext as BaseViewModel)?.OnPageDisappearing();
        }
    }
}
