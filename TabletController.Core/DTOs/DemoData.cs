namespace TabletController.Core.DTOs
{
    public class DemoData
    {
        public int VersionCode { get; set; } = 0;
        public List<Category> Categories { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<string> AdminCodes { get; set; } = new();
        public LockerConfiguration? Locker { get; set; }
    }
}
