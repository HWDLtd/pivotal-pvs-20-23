using TabletController.Collect.ViewModels;
using TabletController.Shared.ViewModels;

namespace TabletController.Collect.Pages
{
    public partial class IdlePage : ContentPage
    {
        private CancellationTokenSource? _animationCancellationTokenSource;

        public IdlePage(IdleViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            StartArrowsAnimation();
            (BindingContext as BaseViewModel)?.OnPageAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopArrowsAnimation();
            (BindingContext as BaseViewModel)?.OnPageDisappearing();
        }

        private void StartArrowsAnimation()
        {
            _animationCancellationTokenSource = new CancellationTokenSource();
            _ = AnimateArrowsLoop(_animationCancellationTokenSource.Token);
        }

        private void StopArrowsAnimation()
        {
            _animationCancellationTokenSource?.Cancel();
            _animationCancellationTokenSource?.Dispose();
            _animationCancellationTokenSource = null;
            
            if (ArrowsImage != null)
            {
                ArrowsImage.TranslationX = 0;
            }
            if (ScanQrLabel != null)
            {
                ScanQrLabel.TranslationX = 0;
            }
        }

        private async Task AnimateArrowsLoop(CancellationToken cancellationToken)
        {
            const double slideDistance = -12;
            const uint slideDuration = 600;
            const int pauseDuration = 3000;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.WhenAll(
                        ArrowsImage.TranslateTo(slideDistance, 0, slideDuration, Easing.SinInOut),
                        ScanQrLabel.TranslateTo(slideDistance, 0, slideDuration, Easing.SinInOut)
                    );
                    
                    if (cancellationToken.IsCancellationRequested) break;

                    await Task.WhenAll(
                        ArrowsImage.TranslateTo(0, 0, slideDuration, Easing.SinInOut),
                        ScanQrLabel.TranslateTo(0, 0, slideDuration, Easing.SinInOut)
                    );
                    
                    if (cancellationToken.IsCancellationRequested) break;

                    await Task.Delay(pauseDuration, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
