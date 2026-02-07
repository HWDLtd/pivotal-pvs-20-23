using TabletController.Core.DTOs;

namespace TabletController.Core.Interfaces
{
    /// <summary>
    /// Service interface for generating receipt data from orders.
    /// </summary>
    public interface IReceiptService
    {
        /// <summary>
        /// Generates receipt data from an order.
        /// </summary>
        /// <param name="order">The order containing basket items and transaction details.</param>
        /// <returns>Receipt data ready to be printed.</returns>
        Task<ReceiptData> GenerateReceiptAsync(Order order);
    }
}
