using TabletController.Collect.ViewModels;
using TabletController.Shared.ViewModels;

namespace TabletController.Collect.Pages
{
    [QueryProperty(nameof(CollectCode), "collectCode")]
    public partial class JustAMomentPage : ContentPage
    {
        public JustAMomentPage(JustAMomentViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        public string? CollectCode
        {
            get => (BindingContext as JustAMomentViewModel)?.CollectCode;
            set
            {
                if (BindingContext is JustAMomentViewModel viewModel)
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
