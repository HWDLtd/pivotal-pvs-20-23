using TabletController.Core.Enums;

namespace TabletController.Core.DTOs
{
    /// <summary>
    /// Represents a single line on a receipt with formatting and content information.
    /// </summary>
    public class ReceiptLine
    {
        public string? Text { get; set; }
        public TextSize TextSize { get; set; } = TextSize.Regular;
        public ReceiptAlignment Alignment { get; set; } = ReceiptAlignment.Center;
        public ReceiptLineType LineType { get; set; } = ReceiptLineType.Text;
        public bool IsBold { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
