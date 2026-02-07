using TabletController.Shared.ViewModels;
using TabletController.Shared.Services;
using TabletController.Core.Interfaces;
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;

namespace TabletController.Collect.ViewModels
{
    public class ErrorViewModel : BaseViewModel
    {
        public override bool IsTimeoutEnabled => false;

        public ErrorViewModel(
            INavigationService navigationService,
            IInactivityTimeoutService timeoutService)
            : base(navigationService, timeoutService)
        {
        }

        public override void OnPageAppearing()
        {
            base.OnPageAppearing();
        }
    }
}
