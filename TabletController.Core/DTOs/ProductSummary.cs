namespace TabletController.Core.DTOs
{
    public class ProductSummary
    {
        public int ProductId { get; set; }
        public required string Name { get; set; }
        public string? ShortDescription { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }
}
