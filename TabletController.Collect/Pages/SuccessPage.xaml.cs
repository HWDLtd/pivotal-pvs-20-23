using TabletController.Collect.ViewModels;
using TabletController.Shared.ViewModels;

namespace TabletController.Collect.Pages
{
    [QueryProperty(nameof(OrderJson), "orderJson")]
    [QueryProperty(nameof(CollectCode), "collectCode")]
    public partial class SuccessPage : ContentPage
    {
        public SuccessPage(SuccessViewModel viewModel)
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
            get => (BindingContext as SuccessViewModel)?.OrderJson;
            set
            {
                if (BindingContext is SuccessViewModel viewModel)
                {
                    viewModel.OrderJson = value;
                }
            }
        }

        public string? CollectCode
        {
            get => (BindingContext as SuccessViewModel)?.CollectCode;
            set
            {
                if (BindingContext is SuccessViewModel viewModel)
                {
                    viewModel.CollectCode = value;
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
