using TabletController.CollectAtFixture.ViewModels;
using TabletController.Shared.ViewModels;

namespace TabletController.CollectAtFixture.Pages
{
    [QueryProperty(nameof(ProductId), "productId")]
    public partial class PaymentPage : ContentPage
    {
        public PaymentPage(PaymentViewModel viewModel)
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

            // Add tap gesture for Make Payment button with visual feedback
            var makePaymentTap = new TapGestureRecognizer();
            makePaymentTap.Tapped += OnMakePaymentTapped;
            MakePaymentBorder.GestureRecognizers.Add(makePaymentTap);
        }

        private async void OnMakePaymentTapped(object? sender, TappedEventArgs e)
        {
            // Visual feedback
            await MakePaymentBorder.FadeTo(0.6, 100);
            await MakePaymentBorder.FadeTo(1, 100);

            // Execute command
            if (BindingContext is PaymentViewModel viewModel)
            {
                viewModel.MakePaymentCommand.Execute(null);
            }
        }

        public string? ProductId
        {
            get => (BindingContext as PaymentViewModel)?.ProductId;
            set
            {
                if (BindingContext is PaymentViewModel viewModel)
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
