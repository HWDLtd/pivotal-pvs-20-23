using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Plugin.Maui.Audio;
using TabletController.Shared.ViewModels;
using TabletController.Shared.Services;
using TabletController.Shared.Utilities;
using IInactivityTimeoutService = TabletController.Shared.Services.IInactivityTimeoutService;
using TabletController.Core.DTOs;
using TabletController.Core.Interfaces;

namespace TabletController.CollectAtFixture.ViewModels
{
    public class AllDoneViewModel : BaseViewModel
    {
        private const int CloseLockerMessageDelaySeconds = 15;

        private readonly INavigationService _navigationService;
        private readonly IProductDataService _productDataService;
        private readonly IInactivityTimeoutService _timeoutService;
        private readonly ILockerService _lockerService;
        private string? _orderJson;
        private Order? _order;
        private string _drawerDisplayName = string.Empty;
        private string _lockerDisplayName = string.Empty;
        private bool _isLockerClosed = false;
        private bool _showCloseLockerMessage;
        private CancellationTokenSource? _closeMessageDelayCts;
        private CancellationTokenSource? _beepBlinkCts;
        private bool _isMonitoringLocker;
        private bool _hasTriggeredLocker;
        private int _reedSwitchArrayNumber;
        private int _latchArrayNumber;

        public string DrawerDisplayName
        {
            get => _drawerDisplayName;
            private set => SetProperty(ref _drawerDisplayName, value);
        }

        public string LockerDisplayName
        {
            get => _lockerDisplayName;
            private set => SetProperty(ref _lockerDisplayName, value);
        }

        public bool IsLockerClosed
        {
            get => _isLockerClosed;
            private set => SetProperty(ref _isLockerClosed, value);
        }

        public bool ShowCloseLockerMessage
        {
            get => _showCloseLockerMessage;
            private set => SetProperty(ref _showCloseLockerMessage, value);
        }

        public string? OrderJson
        {
            get => _orderJson;
            set
            {
                if (SetProperty(ref _orderJson, value) && !string.IsNullOrEmpty(value))
                {
                    LoadOrderAsync(value).SafeFireAndForget();
                }
            }
        }

        public AllDoneViewModel(
            INavigationService navigationService,
            IProductDataService productDataService,
            IInactivityTimeoutService timeoutService,
            ILockerService lockerService)
            : base(navigationService, timeoutService)
        {
            _navigationService = navigationService;
            _productDataService = productDataService;
            _timeoutService = timeoutService;
            _lockerService = lockerService;
        }

        public override void OnPageAppearing()
        {
            base.OnPageAppearing();
        }

        private async Task LoadOrderAsync(string orderJson)
        {
            try
            {
                var decodedJson = WebUtility.UrlDecode(orderJson) ?? orderJson;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(), new DateTimeConverter() }
                };
                _order = JsonSerializer.Deserialize<Order>(decodedJson, options);

                if (_order != null && _order.Basket.Count > 0)
                {
                    var product = await _productDataService.GetProductByIdAsync(_order.Basket[0].ProductId);
                    if (product != null && product.LockerDetails != null)
                    {
                        DrawerDisplayName = product.LockerDetails.DrawerDisplayName ?? "Drawer 1";
                        LockerDisplayName = product.LockerDetails.LockerDisplayName ?? "Locker 2";

                        // Trigger locker after loading order and product details
                        await TriggerLockerAsync(product).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load order: {ex.Message}");
            }
        }

        private async Task TriggerLockerAsync(Product product)
        {
            try
            {
                if (product?.LockerDetails == null)
                    return;

                // Guard: only trigger once per page lifecycle
                if (_hasTriggeredLocker)
                {
                    System.Diagnostics.Debug.WriteLine("TriggerLockerAsync: Already triggered, skipping");
                    return;
                }
                _hasTriggeredLocker = true;

                _reedSwitchArrayNumber = product.LockerDetails.ReedSwitchArrayNumber;
                _latchArrayNumber = product.LockerDetails.LatchArrayNumber;

                // Step 1: Disable timeout
                _timeoutService.SetEnabled(false);
                System.Diagnostics.Debug.WriteLine($"Locker sequence started: Disabled timeout");

                // Step 2: Wait for ReedSwitch to change from open to closed (once)
                var reedSwitchClosed = await WaitForReedSwitchToCloseAsync(_reedSwitchArrayNumber).ConfigureAwait(false);

                if (!reedSwitchClosed)
                {
                    System.Diagnostics.Debug.WriteLine("ReedSwitch did not close, re-enabling timeout and aborting");
                    _hasTriggeredLocker = false;
                    _timeoutService.SetEnabled(true);
                    return;
                }

                // Step 3: Wait half a second
                await Task.Delay(500).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine("Waited 500ms after ReedSwitch closed");

                // Step 4: Pulse the latch port (once)
                await _lockerService.PulseLatchAsync(_latchArrayNumber).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"Pulsed latch port {_latchArrayNumber}");

                // Step 5: Wait for both reed switch and latch to close
                StartLockerMonitoring();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to trigger locker: {ex.Message}");
                _hasTriggeredLocker = false;
                _timeoutService.SetEnabled(true);
            }
        }

        private void StartLockerMonitoring()
        {
            if (_isMonitoringLocker)
                return;

            _isMonitoringLocker = true;
            _lockerService.ReedSwitchStateChanged += OnLockerStateChanged;

            // Start the "please close the locker" message timer (runs once)
            _closeMessageDelayCts = new CancellationTokenSource();
            var token = _closeMessageDelayCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(CloseLockerMessageDelaySeconds), token).ConfigureAwait(false);

                    if (!token.IsCancellationRequested)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ShowCloseLockerMessage = true;
                            System.Diagnostics.Debug.WriteLine("Showing 'Please close the locker' message");
                        });

                        StartBeepBlinkLoop();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Locker was closed before the delay elapsed
                }
            });

            // Check current state immediately in case already closed
            CheckIfBothClosed();
            System.Diagnostics.Debug.WriteLine("Started locker monitoring - waiting for both reed switch and latch to close");
        }

        private void StopLockerMonitoring()
        {
            if (!_isMonitoringLocker)
                return;

            _isMonitoringLocker = false;
            _lockerService.ReedSwitchStateChanged -= OnLockerStateChanged;
            _closeMessageDelayCts?.Cancel();
            _closeMessageDelayCts?.Dispose();
            _closeMessageDelayCts = null;
            StopBeepBlinkLoop();
            System.Diagnostics.Debug.WriteLine("Stopped locker monitoring");
        }

        private void OnLockerStateChanged(object? sender, ReedSwitchStateChangedEventArgs e)
        {
            if (e.ArrayNumber != _reedSwitchArrayNumber && e.ArrayNumber != _latchArrayNumber)
                return;

            System.Diagnostics.Debug.WriteLine($"Locker state changed: ArrayNumber={e.ArrayNumber}, IsOpen={e.IsOpen}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CheckIfBothClosed();
            });
        }

        private void CheckIfBothClosed()
        {
            try
            {
                var currentStates = _lockerService.GetCurrentInputStates();
                // true = closed, false = open
                bool reedSwitchClosed = _reedSwitchArrayNumber < currentStates.Length && currentStates[_reedSwitchArrayNumber];
                bool latchClosed = _latchArrayNumber < currentStates.Length && currentStates[_latchArrayNumber];

                System.Diagnostics.Debug.WriteLine($"Locker state check: ReedSwitch[{_reedSwitchArrayNumber}]={reedSwitchClosed}, Latch[{_latchArrayNumber}]={latchClosed}");

                if (reedSwitchClosed && latchClosed)
                {
                    OnBothClosed();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading locker state: {ex.Message}");
            }
        }

        private void OnBothClosed()
        {
            System.Diagnostics.Debug.WriteLine("Both reed switch and latch closed - enabling Next Customer");

            // Stop monitoring - we're done
            StopLockerMonitoring();

            IsLockerClosed = true;
            ShowCloseLockerMessage = false;

            // Re-enable timeout and reset it
            _timeoutService.SetEnabled(true);
            _timeoutService.ResetTimer();
        }

        private void StartBeepBlinkLoop()
        {
            StopBeepBlinkLoop();
            _beepBlinkCts = new CancellationTokenSource();
            var token = _beepBlinkCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        // Play beep sound
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            PlayBeepSound();
                        });

                        // Blink: hide text for 0.5 seconds
                        MainThread.BeginInvokeOnMainThread(() => ShowCloseLockerMessage = false);
                        await Task.Delay(500, token).ConfigureAwait(false);
                        MainThread.BeginInvokeOnMainThread(() => ShowCloseLockerMessage = true);

                        // Wait remaining 4.5 seconds to complete the 5-second cycle
                        await Task.Delay(4500, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Loop cancelled - locker was closed
                }
            });
        }

        private void StopBeepBlinkLoop()
        {
            _beepBlinkCts?.Cancel();
            _beepBlinkCts?.Dispose();
            _beepBlinkCts = null;
        }

        private async void PlayBeepSound()
        {
            try
            {
                var stream = await FileSystem.OpenAppPackageFileAsync("beep-09.wav");
                var player = AudioManager.Current.CreatePlayer(stream);
                player.PlaybackEnded += (s, e) =>
                {
                    player.Dispose();
                };
                player.Play();
                System.Diagnostics.Debug.WriteLine("Played beep-09.wav");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to play beep sound: {ex.Message}");
            }
        }

        private async Task<bool> WaitForReedSwitchToCloseAsync(int reedSwitchArrayNumber)
        {
            System.Diagnostics.Debug.WriteLine($"WaitForReedSwitchToCloseAsync: Waiting for reed switch array {reedSwitchArrayNumber} to close");

            var tcs = new TaskCompletionSource<bool>();
            EventHandler<ReedSwitchStateChangedEventArgs>? handler = null;
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout
            var isCompleted = false;

            handler = (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"ReedSwitchStateChanged event FIRED: ArrayNumber={e.ArrayNumber}, IsOpen={e.IsOpen}, Waiting for={reedSwitchArrayNumber}, isCompleted={isCompleted}");

                if (e.ArrayNumber == reedSwitchArrayNumber && !e.IsOpen && !isCompleted)
                {
                    System.Diagnostics.Debug.WriteLine($"Reed switch {reedSwitchArrayNumber} closed via EVENT!");
                    isCompleted = true;
                    _lockerService.ReedSwitchStateChanged -= handler;
                    timeoutCts.Cancel();
                    tcs.TrySetResult(true);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Event ignored: ArrayNumber match={e.ArrayNumber == reedSwitchArrayNumber}, IsOpen={e.IsOpen}, isCompleted={isCompleted}");
                }
            };

            System.Diagnostics.Debug.WriteLine($"Subscribing to ReedSwitchStateChanged event for array {reedSwitchArrayNumber}");
            _lockerService.ReedSwitchStateChanged += handler;

            // Check current state first - might already be closed
            var currentStates = _lockerService.GetCurrentInputStates();
            System.Diagnostics.Debug.WriteLine($"Current input states: [{string.Join(", ", currentStates)}]");

            if (reedSwitchArrayNumber < currentStates.Length)
            {
                var isCurrentlyClosed = currentStates[reedSwitchArrayNumber]; // true = closed, false = open
                System.Diagnostics.Debug.WriteLine($"Reed switch {reedSwitchArrayNumber} current state: Closed={isCurrentlyClosed}, Open={!isCurrentlyClosed}");

                if (isCurrentlyClosed)
                {
                    // Already closed
                    System.Diagnostics.Debug.WriteLine($"Reed switch {reedSwitchArrayNumber} is already closed");
                    isCompleted = true;
                    _lockerService.ReedSwitchStateChanged -= handler;
                    timeoutCts.Cancel();
                    return true;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Reed switch array number {reedSwitchArrayNumber} is out of range (array length: {currentStates.Length})");
            }

            // Set up timeout
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return; // Cancelled, which means we detected the closed state
                }

                if (!isCompleted)
                {
                    System.Diagnostics.Debug.WriteLine($"Timeout waiting for reed switch {reedSwitchArrayNumber} to close");
                    isCompleted = true;
                    _lockerService.ReedSwitchStateChanged -= handler;
                    tcs.TrySetResult(false);
                }
            });

            var result = await tcs.Task.ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"WaitForReedSwitchToCloseAsync completed: Result={result}");
            return result;
        }

        public override void OnPageDisappearing()
        {
            StopLockerMonitoring();
            base.OnPageDisappearing();
        }
    }
}
