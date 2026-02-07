namespace TabletController.Core.DTOs
{
    public class ReedSwitchStateChangedEventArgs : EventArgs
    {
        public int ArrayNumber { get; }
        public bool IsOpen { get; }

        public ReedSwitchStateChangedEventArgs(int arrayNumber, bool isOpen)
        {
            ArrayNumber = arrayNumber;
            IsOpen = isOpen;
        }
    }
}
