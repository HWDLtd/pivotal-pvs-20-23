namespace TabletController.Core.DTOs
{
    /// <summary>
    /// Represents receipt data to be printed in a manufacturer-agnostic format.
    /// </summary>
    public class ReceiptData
    {
        public List<ReceiptLine> Lines { get; set; } = new();
    }
}
