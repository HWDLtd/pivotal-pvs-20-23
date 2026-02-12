using System.ComponentModel;
using System.Runtime.CompilerServices;
using TabletController.Core.Interfaces;

namespace TabletController.Shared.Services
{
    public interface IInactivityTimeoutService : INotifyPropertyChanged
    {
        bool IsPopupVisible { get; }
        int CountdownSeconds { get; }
        double CountdownProgress { get; }
        void ResetTimer();
        void StartCountdown();
        void StopAll();
        void SetEnabled(bool enabled);
    }

    public class InactivityTimeoutService : IInactivityTimeoutService
    {
        private const int InactivityTimeoutSeconds = 40;
        private const int CountdownDurationSeconds = 10;
        private const int CountdownTickIntervalMs = 50;

        private readonly INavigationService _navigationService;
        private System.Timers.Timer? _inactivityTimer;
        private System.Timers.Timer? _countdownTimer;
        private DateTime _countdownStartTime;
        private bool _isEnabled;
        private bool _isPopupVisible;
        private int _countdownSeconds;
        private double _countdownProgress;

        public event PropertyChangedEventHandler? PropertyChanged;

        public InactivityTimeoutService(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _countdownSeconds = CountdownDurationSeconds;
            _countdownProgress = 1.0;
        }

        public bool IsPopupVisible
        {
            get => _isPopupVisible;
            private set
            {
                if (_isPopupVisible != value)
                {
                    _isPopupVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CountdownSeconds
        {
            get => _countdownSeconds;
            private set
            {
                if (_countdownSeconds != value)
                {
                    _countdownSeconds = value;
                    OnPropertyChanged();
                }
            }
        }

        public double CountdownProgress
        {
            get => _countdownProgress;
            private set
            {
                if (Math.Abs(_countdownProgress - value) > 0.001)
                {
                    _countdownProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled)
            {
                StopAll();
            }
        }

        public void ResetTimer()
        {
            if (!_isEnabled) return;

            // Stop countdown if running
            StopCountdown();
            IsPopupVisible = false;

            // Reset and restart inactivity timer
            _inactivityTimer?.Stop();
            _inactivityTimer?.Dispose();

            _inactivityTimer = new System.Timers.Timer(InactivityTimeoutSeconds * 1000);
            _inactivityTimer.Elapsed += OnInactivityTimeout;
            _inactivityTimer.AutoReset = false;
            _inactivityTimer.Start();
        }

        public void StartCountdown()
        {
            if (!_isEnabled) return;

            // Stop inactivity timer
            _inactivityTimer?.Stop();

            // Reset countdown values
            CountdownSeconds = CountdownDurationSeconds;
            CountdownProgress = 1.0;
            IsPopupVisible = true;

            // Record start time for smooth animation
            _countdownStartTime = DateTime.Now;

            // Start countdown timer with fast tick interval for smooth animation
            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();

            _countdownTimer = new System.Timers.Timer(CountdownTickIntervalMs);
            _countdownTimer.Elapsed += OnCountdownTick;
            _countdownTimer.AutoReset = true;
            _countdownTimer.Start();
        }

        public void StopAll()
        {
            _inactivityTimer?.Stop();
            _inactivityTimer?.Dispose();
            _inactivityTimer = null;

            StopCountdown();
            IsPopupVisible = false;

            // Reset countdown values
            CountdownSeconds = CountdownDurationSeconds;
            CountdownProgress = 1.0;
        }

        private void StopCountdown()
        {
            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();
            _countdownTimer = null;
        }

        private void OnInactivityTimeout(object? sender, System.Timers.ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StartCountdown();
            });
        }

        private void OnCountdownTick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var elapsedMs = (DateTime.Now - _countdownStartTime).TotalMilliseconds;
                var totalMs = CountdownDurationSeconds * 1000.0;
                var remainingMs = totalMs - elapsedMs;

                if (remainingMs <= 0)
                {
                    NavigateToIdle();
                    return;
                }

                // Smooth progress update
                CountdownProgress = remainingMs / totalMs;

                // Update seconds display only when whole-second value changes
                var newSeconds = (int)Math.Ceiling(remainingMs / 1000.0);
                if (newSeconds != CountdownSeconds)
                {
                    CountdownSeconds = newSeconds;
                }
            });
        }

        private void NavigateToIdle()
        {
            StopAll();
            _navigationService.NavigateToAsync("//idle");
        }

        public void DismissPopup()
        {
            StopCountdown();
            IsPopupVisible = false;
            ResetTimer();
        }

        public void NavigateToIdleNow()
        {
            NavigateToIdle();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
