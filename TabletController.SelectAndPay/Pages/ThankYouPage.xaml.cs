using TabletController.SelectAndPay.ViewModels;
using TabletController.Shared.ViewModels;

namespace TabletController.SelectAndPay.Pages
{
    [QueryProperty(nameof(OrderJson), "orderJson")]
    public partial class ThankYouPage : ContentPage
    {
        public ThankYouPage(ThankYouViewModel viewModel)
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

        public string? OrderJson
        {
            get => (BindingContext as ThankYouViewModel)?.OrderJson;
            set
            {
                if (BindingContext is ThankYouViewModel viewModel)
                {
                    viewModel.OrderJson = value;
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as BaseViewModel)?.OnPageAppearing();
            // Printing will be triggered automatically when OrderJson is set and order is loaded
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            (BindingContext as BaseViewModel)?.OnPageDisappearing();
        }
    }
}
