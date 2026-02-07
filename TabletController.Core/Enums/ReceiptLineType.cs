namespace TabletController.Core.Enums
{
    /// <summary>
    /// Specifies the type of receipt line.
    /// </summary>
    public enum ReceiptLineType
    {
        Text,
        Image,
        Barcode,
        QrCode,
        BlankLine,
        Cut,
        Feed
    }
}
