using TabletController.Core.DTOs;

namespace TabletController.Core.Interfaces
{
    public interface ILockerService
    {
        event EventHandler<ReedSwitchStateChangedEventArgs>? ReedSwitchStateChanged;

        Task StartMonitoringAsync();
        bool[] GetCurrentInputStates();
        Task PulseLatchAsync(int latchArrayNumber);
        void Configure(string ipAddress, int port, byte unitId, bool invertOpenClosed = false);
    }
}
