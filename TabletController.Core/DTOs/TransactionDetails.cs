namespace TabletController.Core.DTOs
{
    /// <summary>
    /// Represents transaction details from a Payment Electronic Device (PED).
    /// </summary>
    public class TransactionDetails
    {
        public string MerchantId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.Now;
    }
}
