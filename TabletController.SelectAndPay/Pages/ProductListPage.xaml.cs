using TabletController.SelectAndPay.ViewModels;
using TabletController.Shared.ViewModels;
using TabletController.Shared.Utilities;

namespace TabletController.SelectAndPay.Pages
{
    public partial class ProductListPage : ContentPage
    {
        public ProductListPage(ProductListViewModel viewModel)
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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as BaseViewModel)?.OnPageAppearing();

            if (BindingContext is ProductListViewModel viewModel)
            {
                viewModel.InitializeAsync().SafeFireAndForget();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            (BindingContext as BaseViewModel)?.OnPageDisappearing();
        }
    }
}
