using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TabletController.Shared.Services;
using TabletController.Shared.Utilities;
using TabletController.Core.Interfaces;

namespace TabletController.Shared.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;
        private readonly IInactivityTimeoutService _timeoutService;
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand BackCommand { get; }

        public virtual bool IsTimeoutEnabled => true;

        public bool IsTimeoutPopupVisible => _timeoutService.IsPopupVisible;
        public int TimeoutCountdownSeconds => _timeoutService.CountdownSeconds;
        public double TimeoutCountdownProgress => _timeoutService.CountdownProgress;

        public ICommand DismissTimeoutCommand { get; }
        public ICommand NextCustomerCommand { get; }

        protected BaseViewModel(INavigationService navigationService, IInactivityTimeoutService timeoutService)
        {
            _navigationService = navigationService;
            _timeoutService = timeoutService;

            BackCommand = new Command(() => ExecuteBackCommandAsync().SafeFireAndForget());
            DismissTimeoutCommand = new Command(ExecuteDismissTimeout);
            NextCustomerCommand = new Command(ExecuteNextCustomer);

            // Subscribe to timeout service property changes
            _timeoutService.PropertyChanged += OnTimeoutServicePropertyChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private async Task ExecuteBackCommandAsync()
        {
            ResetInactivityTimer();
            await _navigationService.GoBackAsync().ConfigureAwait(false);
        }

        private void OnTimeoutServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Forward property changes from the service
            switch (e.PropertyName)
            {
                case nameof(IInactivityTimeoutService.IsPopupVisible):
                    OnPropertyChanged(nameof(IsTimeoutPopupVisible));
                    break;
                case nameof(IInactivityTimeoutService.CountdownSeconds):
                    OnPropertyChanged(nameof(TimeoutCountdownSeconds));
                    break;
                case nameof(IInactivityTimeoutService.CountdownProgress):
                    OnPropertyChanged(nameof(TimeoutCountdownProgress));
                    break;
            }
        }

        public virtual void OnPageAppearing()
        {
            _timeoutService.SetEnabled(IsTimeoutEnabled);
            if (IsTimeoutEnabled)
            {
                _timeoutService.ResetTimer();
            }
        }

        public virtual void OnPageDisappearing()
        {
            _timeoutService.StopAll();
        }

        public void ResetInactivityTimer()
        {
            if (IsTimeoutEnabled)
            {
                _timeoutService.ResetTimer();
            }
        }

        private void ExecuteDismissTimeout()
        {
            if (_timeoutService is InactivityTimeoutService service)
            {
                service.DismissPopup();
            }
        }

        private void ExecuteNextCustomer()
        {
            if (_timeoutService is InactivityTimeoutService service)
            {
                service.NavigateToIdleNow();
            }
        }
    }
}
