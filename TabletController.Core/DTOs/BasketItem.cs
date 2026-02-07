namespace TabletController.Core.DTOs
{
    /// <summary>
    /// Represents a lightweight item in the shopping basket.
    /// </summary>
    public class BasketItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
