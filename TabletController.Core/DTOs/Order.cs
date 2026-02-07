namespace TabletController.Core.DTOs
{
    /// <summary>
    /// Represents a completed order with basket items and transaction details.
    /// </summary>
    public class Order
    {
        public List<BasketItem> Basket { get; set; } = new();
        public TransactionDetails TransactionDetails { get; set; } = new();
        public DateTime OrderDate { get; set; } = DateTime.Now;
    }
}
