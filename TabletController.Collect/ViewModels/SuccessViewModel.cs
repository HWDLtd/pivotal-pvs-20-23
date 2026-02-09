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

namespace TabletController.Collect.ViewModels
{
    public class SuccessViewModel : BaseViewModel
    {
        private const int LockerInputIndex = 0; // Input 0 on the Etd8a12Controller: true = closed, false = open
        private const int CloseLockerMessageDelaySeconds = 15;

        private readonly INavigationService _navigationService;
        private readonly IProductDataService _productDataService;
        private readonly IInactivityTimeoutService _timeoutService;
        private readonly ILockerService _lockerService;
        private string? _orderJson;
        private string? _collectCode;
        private Order? _order;
        private string _drawerDisplayName = string.Empty;
        private string _lockerDisplayName = string.Empty;
        private bool _isLockerClosed = true;
        private bool _showCloseLockerMessage;
        private CancellationTokenSource? _closeMessageDelayCts;
        private CancellationTokenSource? _beepBlinkCts;
        private bool _isMonitoringLocker;

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

        /// <summary>
        /// True when the locker is closed (input 0 = true). Controls footer button visibility and timeout.
        /// </summary>
        public bool IsLockerClosed
        {
            get => _isLockerClosed;
            private set => SetProperty(ref _isLockerClosed, value);
        }

        /// <summary>
        /// True when the locker has been open for more than 5 seconds. Shows "Please Close The Locker" message.
        /// </summary>
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

        public string? CollectCode
        {
            get => _collectCode;
            set
            {
                if (SetProperty(ref _collectCode, value) && !string.IsNullOrEmpty(value))
                {
                    LoadProductByCollectCodeAsync(value).SafeFireAndForget();
                }
            }
        }

        public SuccessViewModel(
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

        private async Task LoadProductByCollectCodeAsync(string collectCode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading product by CollectCode: {collectCode}");

                var product = await _productDataService.GetProductByBarcodeAsync(collectCode).ConfigureAwait(false);
                if (product != null && product.LockerDetails != null)
                {
                    DrawerDisplayName = product.LockerDetails.DrawerDisplayName ?? "Drawer 1";
                    LockerDisplayName = product.LockerDetails.LockerDisplayName ?? "Locker 2";
                    
                    System.Diagnostics.Debug.WriteLine($"Loaded product: Drawer={DrawerDisplayName}, Locker={LockerDisplayName}");
                    
                    // Trigger locker after loading product details
                    await TriggerLockerAsync(product).ConfigureAwait(false);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No product found for CollectCode: {collectCode}");
                    // Set default values if product not found
                    DrawerDisplayName = "Drawer 1";
                    LockerDisplayName = "Locker 2";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load product by CollectCode: {ex.Message}");
                // Set default values on error
                DrawerDisplayName = "Drawer 1";
                LockerDisplayName = "Locker 2";
            }
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

                var latchArrayNumber = product.LockerDetails.LatchArrayNumber;

                // Step 1: Disable timeout while locker is being operated
                _timeoutService.SetEnabled(false);
                System.Diagnostics.Debug.WriteLine($"Locker sequence started: Disabled timeout");

                // Step 2: Pulse the latch port to unlock
                try
                {
                    await _lockerService.PulseLatchAsync(latchArrayNumber).ConfigureAwait(false);
                }
                catch (Exception ee)
                {
                    System.Diagnostics.Debug.WriteLine($"Error happened: {ee}");
                }

                System.Diagnostics.Debug.WriteLine($"Pulsed latch port {latchArrayNumber}");

                // Step 3: Start monitoring the locker state (input 0)
                // Timeout and footer button are now managed by the locker monitor
                StartLockerMonitoring();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to trigger locker: {ex.Message}");
                // Ensure timeout is re-enabled even on error
                _timeoutService.SetEnabled(true);
            }
        }

        private void StartLockerMonitoring()
        {
            if (_isMonitoringLocker)
                return;

            _isMonitoringLocker = true;
            _lockerService.ReedSwitchStateChanged += OnLockerStateChanged;

            // Check current state immediately
            UpdateLockerState();
            System.Diagnostics.Debug.WriteLine("Started locker monitoring on input 0");
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
            if (e.ArrayNumber != LockerInputIndex)
                return;

            System.Diagnostics.Debug.WriteLine($"Locker input {LockerInputIndex} changed: IsOpen={e.IsOpen}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateLockerState();
            });
        }

        private void UpdateLockerState()
        {
            try
            {
                var currentStates = _lockerService.GetCurrentInputStates();
                // Input 0: true = closed, false = open
                bool isClosed = LockerInputIndex < currentStates.Length && currentStates[LockerInputIndex];

                System.Diagnostics.Debug.WriteLine($"Locker state update: Input[{LockerInputIndex}]={isClosed} (closed={isClosed})");

                if (isClosed)
                {
                    OnLockerClosed();
                }
                else
                {
                    OnLockerOpened();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading locker state: {ex.Message}");
            }
        }

        private void OnLockerOpened()
        {
            IsLockerClosed = false;

            // Disable timeout while locker is open
            _timeoutService.SetEnabled(false);
            System.Diagnostics.Debug.WriteLine("Locker opened: Timeout disabled");

            // Cancel any existing delay/beep
            _closeMessageDelayCts?.Cancel();
            _closeMessageDelayCts?.Dispose();
            StopBeepBlinkLoop();

            // Start 5-second delay before showing "Please Close The Locker" message
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
                            System.Diagnostics.Debug.WriteLine("Showing 'Please Close The Locker' message");
                        });

                        // Start the beep + blink loop
                        StartBeepBlinkLoop();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Locker was closed before the delay elapsed
                }
            });
        }

        private void OnLockerClosed()
        {
            // Cancel any pending "close locker" message delay and beep/blink
            _closeMessageDelayCts?.Cancel();
            _closeMessageDelayCts?.Dispose();
            _closeMessageDelayCts = null;
            StopBeepBlinkLoop();

            IsLockerClosed = true;
            ShowCloseLockerMessage = false;

            // Re-enable timeout and reset it
            _timeoutService.SetEnabled(true);
            _timeoutService.ResetTimer();
            System.Diagnostics.Debug.WriteLine("Locker closed: Timeout re-enabled, showing Next Customer button");
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

        private async Task<bool> WaitForReedSwitchToOpenAsync(int reedSwitchArrayNumber)
        {
            System.Diagnostics.Debug.WriteLine($"WaitForReedSwitchToOpenAsync: Waiting for reed switch array {reedSwitchArrayNumber} to open");
            
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<ReedSwitchStateChangedEventArgs>? handler = null;
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout
            var isCompleted = false;

            handler = (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"ReedSwitchStateChanged event FIRED: ArrayNumber={e.ArrayNumber}, IsOpen={e.IsOpen}, Waiting for={reedSwitchArrayNumber}, isCompleted={isCompleted}");
                
                if (e.ArrayNumber == reedSwitchArrayNumber && e.IsOpen && !isCompleted)
                {
                    System.Diagnostics.Debug.WriteLine($"Reed switch {reedSwitchArrayNumber} opened via EVENT!");
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

            // Check current state first - might already be open
            var currentStates = _lockerService.GetCurrentInputStates();
            System.Diagnostics.Debug.WriteLine($"Current input states: [{string.Join(", ", currentStates)}]");
            
            if (reedSwitchArrayNumber < currentStates.Length)
            {
                var isCurrentlyOpen = !currentStates[reedSwitchArrayNumber]; // true = closed, false = open
                System.Diagnostics.Debug.WriteLine($"Reed switch {reedSwitchArrayNumber} current state: Closed={currentStates[reedSwitchArrayNumber]}, Open={isCurrentlyOpen}");
                
                if (isCurrentlyOpen)
                {
                    // Already open (remember: true = closed, false = open)
                    System.Diagnostics.Debug.WriteLine($"Reed switch {reedSwitchArrayNumber} is already open");
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
                    return; // Cancelled, which means we detected the open state
                }

                if (!isCompleted)
                {
                    System.Diagnostics.Debug.WriteLine($"Timeout waiting for reed switch {reedSwitchArrayNumber} to open");
                    isCompleted = true;
                    _lockerService.ReedSwitchStateChanged -= handler;
                    tcs.TrySetResult(false);
                }
            });

            var result = await tcs.Task.ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"WaitForReedSwitchToOpenAsync completed: Result={result}");
            return result;
        }

        private async Task WaitForPortsToCloseAsync(int reedSwitchArrayNumber, int latchArrayNumber)
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<ReedSwitchStateChangedEventArgs>? handler = null;
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout

            handler = (sender, e) =>
            {
                // Check if either port changed to closed
                if ((e.ArrayNumber == reedSwitchArrayNumber || e.ArrayNumber == latchArrayNumber) && !e.IsOpen)
                {
                    // Check if both ports are now closed
                    var currentStates = _lockerService.GetCurrentInputStates();
                    var reedSwitchClosed = reedSwitchArrayNumber < currentStates.Length && currentStates[reedSwitchArrayNumber];
                    var latchClosed = latchArrayNumber < currentStates.Length && currentStates[latchArrayNumber];

                    if (reedSwitchClosed && latchClosed)
                    {
                        // Both ports are closed (true = closed)
                        _lockerService.ReedSwitchStateChanged -= handler;
                        timeoutCts.Cancel();
                        tcs.TrySetResult(true);
                    }
                }
            };

            _lockerService.ReedSwitchStateChanged += handler;

            // Check current state first - might already both be closed
            var currentStates = _lockerService.GetCurrentInputStates();
            var reedSwitchClosed = reedSwitchArrayNumber < currentStates.Length && currentStates[reedSwitchArrayNumber];
            var latchClosed = latchArrayNumber < currentStates.Length && currentStates[latchArrayNumber];

            if (reedSwitchClosed && latchClosed)
            {
                // Both already closed
                _lockerService.ReedSwitchStateChanged -= handler;
                timeoutCts.Cancel();
                return;
            }

            // Poll periodically to check state (in case we miss events)
            var pollTask = Task.Run(async () =>
            {
                while (!tcs.Task.IsCompleted && !timeoutCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(100, timeoutCts.Token).ConfigureAwait(false);
                    
                    var states = _lockerService.GetCurrentInputStates();
                    var reedClosed = reedSwitchArrayNumber < states.Length && states[reedSwitchArrayNumber];
                    var latchClosed = latchArrayNumber < states.Length && states[latchArrayNumber];

                    if (reedClosed && latchClosed)
                    {
                        _lockerService.ReedSwitchStateChanged -= handler;
                        timeoutCts.Cancel();
                        tcs.TrySetResult(true);
                        break;
                    }
                }
            }, timeoutCts.Token);

            // Set up timeout
            timeoutCts.Token.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    _lockerService.ReedSwitchStateChanged -= handler;
                    tcs.TrySetResult(false);
                }
            });

            await tcs.Task.ConfigureAwait(false);
        }

        public override void OnPageDisappearing()
        {
            StopLockerMonitoring();
            base.OnPageDisappearing();
        }
    }
}
