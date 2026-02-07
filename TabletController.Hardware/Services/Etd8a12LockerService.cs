using TabletController.Core.DTOs;
using TabletController.Core.Interfaces;

namespace TabletController.Hardware.Services
{
    public sealed class Etd8a12LockerService : ILockerService, IDisposable
    {
        private const int PulseDurationMs = 200;

        private readonly Etd8a12Controller _controller;
        private bool _disposed;
        private bool _invertOpenClosed = false;

        public event EventHandler<ReedSwitchStateChangedEventArgs>? ReedSwitchStateChanged;

        public Etd8a12LockerService()
        {
            _controller = new Etd8a12Controller();
            _controller.InputStateChanged += OnControllerInputStateChanged;
            _controller.PollingError += OnControllerPollingError;
        }

        public void Configure(string ipAddress, int port, byte unitId, bool invertOpenClosed = false)
        {
            _controller.Configure(ipAddress, port, unitId);
            _invertOpenClosed = invertOpenClosed;
        }

        public Task StartMonitoringAsync()
        {
            _controller.Start();
            return Task.CompletedTask;
        }

        public bool[] GetCurrentInputStates()
        {
            var states = _controller.GetInputs();
            // If invertOpenClosed is true, we need to invert the states for the callers
            // But actually, the states returned here are raw controller states
            // The inversion is handled in OnControllerInputStateChanged for events
            // For consistency, we should also invert here if needed
            // However, looking at the usage, GetCurrentInputStates is used to check current state
            // and the code does: var isCurrentlyOpen = !currentStates[reedSwitchArrayNumber]
            // So if we invert here, we'd need to change that logic too
            // Let's keep GetCurrentInputStates returning raw states, and handle inversion in event handlers
            return states;
        }

        public Task PulseLatchAsync(int latchArrayNumber)
        {
            // Controller uses 1-based indexing for outputs
            int outputNumber = latchArrayNumber + 1;
            _controller.PulseOutput(outputNumber, PulseDurationMs);
            return Task.CompletedTask;
        }

        private void OnControllerInputStateChanged(object? sender, InputStateChangedEventArgs e)
        {
            // Fire events for each array number that changed
            // Input state true = closed, false = open
            // IsOpen = !inputState (inverted by default, but can be double-inverted if _invertOpenClosed is true)
            System.Diagnostics.Debug.WriteLine($"OnControllerInputStateChanged: OldState=[{string.Join(", ", e.OldState)}], NewState=[{string.Join(", ", e.NewState)}], InvertOpenClosed={_invertOpenClosed}");
            
            for (int i = 0; i < e.OldState.Length && i < e.NewState.Length; i++)
            {
                if (e.OldState[i] != e.NewState[i])
                {
                    // Default: true (closed) -> false (open), false (open) -> true (open)
                    // If invertOpenClosed is true, we double-invert: true (closed) -> true (closed), false (open) -> false (open)
                    bool isOpen = _invertOpenClosed ? e.NewState[i] : !e.NewState[i];
                    var args = new ReedSwitchStateChangedEventArgs(i, isOpen);
                    System.Diagnostics.Debug.WriteLine($"Firing ReedSwitchStateChanged event: ArrayNumber={i}, IsOpen={isOpen}, Subscribers={ReedSwitchStateChanged?.GetInvocationList().Length ?? 0}");
                    ReedSwitchStateChanged?.Invoke(this, args);
                    System.Diagnostics.Debug.WriteLine($"ReedSwitchStateChanged event fired for array {i}");
                }
            }
        }

        private void OnControllerPollingError(object? sender, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Locker controller polling error: {ex.Message}");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _controller.InputStateChanged -= OnControllerInputStateChanged;
                _controller.PollingError -= OnControllerPollingError;
                _controller.Stop();
                _disposed = true;
            }
        }
    }
}
