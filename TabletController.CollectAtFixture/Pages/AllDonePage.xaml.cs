using TabletController.CollectAtFixture.ViewModels;
using TabletController.Shared.ViewModels;

namespace TabletController.CollectAtFixture.Pages
{
    [QueryProperty(nameof(OrderJson), "orderJson")]
    public partial class AllDonePage : ContentPage
    {
        public AllDonePage(AllDoneViewModel viewModel)
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
            get => (BindingContext as AllDoneViewModel)?.OrderJson;
            set
            {
                if (BindingContext is AllDoneViewModel viewModel)
                {
                    viewModel.OrderJson = value;
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
