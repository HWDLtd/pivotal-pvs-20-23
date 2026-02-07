using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        private readonly INavigationService _navigationService;
        private readonly IProductDataService _productDataService;
        private readonly IInactivityTimeoutService _timeoutService;
        private readonly ILockerService _lockerService;
        private string? _orderJson;
        private string? _collectCode;
        private Order? _order;
        private string _drawerDisplayName = string.Empty;
        private string _lockerDisplayName = string.Empty;

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

                // Step 1: Disable timeout
                _timeoutService.SetEnabled(false);
                System.Diagnostics.Debug.WriteLine($"Locker sequence started: Disabled timeout");

                // Step 2: Pulse the latch port (Collect app doesn't have ReedSwitch, so just pulse directly)
                await _lockerService.PulseLatchAsync(latchArrayNumber).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"Pulsed latch port {latchArrayNumber}");

                // Step 3: Re-enable timeout
                System.Diagnostics.Debug.WriteLine("Re-enabling timeout...");
                _timeoutService.SetEnabled(true);
                _timeoutService.ResetTimer(); // Also reset the timer to start fresh
                System.Diagnostics.Debug.WriteLine("Locker sequence completed: Re-enabled timeout and reset timer");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to trigger locker: {ex.Message}");
                // Ensure timeout is re-enabled even on error
                _timeoutService.SetEnabled(true);
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
            base.OnPageDisappearing();
        }
    }
}
