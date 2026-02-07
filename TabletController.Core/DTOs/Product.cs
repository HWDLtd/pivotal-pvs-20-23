namespace TabletController.Core.DTOs
{
    public class Product
    {
        public int ProductId { get; set; }
        public required string Name { get; set; }
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string? Highlights { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public string? CollectionCode { get; set; }
        public string? Barcode { get; set; }
        public LockerDetails? LockerDetails { get; set; }
    }
}
