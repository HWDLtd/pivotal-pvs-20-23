using TabletController.Collect.ViewModels;
using TabletController.Shared.ViewModels;

namespace TabletController.Collect.Pages
{
    public partial class ErrorPage : ContentPage
    {
        public ErrorPage(ErrorViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
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
