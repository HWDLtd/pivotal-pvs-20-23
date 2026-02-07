namespace TabletController.Core.DTOs
{
    public class LockerConfiguration
    {
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public int UnitId { get; set; }
        public bool InvertOpenClosed { get; set; } = false;
    }
}
