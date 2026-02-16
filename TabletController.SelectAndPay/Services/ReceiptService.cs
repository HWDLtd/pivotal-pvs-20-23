using TabletController.Core.DTOs;
using TabletController.Core.Enums;
using TabletController.Core.Interfaces;

namespace TabletController.SelectAndPay.Services
{
    /// <summary>
    /// Service for generating receipt data from orders.
    /// </summary>
    public class ReceiptService : IReceiptService
    {
        private readonly IProductDataService _productDataService;

        private static readonly int LineLength = 48;

        public ReceiptService(IProductDataService productDataService)
        {
            _productDataService = productDataService;
        }

        public async Task<ReceiptData> GenerateReceiptAsync(Order order)
        {
            var receiptData = new ReceiptData();
            var lines = receiptData.Lines;


            lines.Add(new ReceiptLine
            {
                Text = "receipt_logo.png",
                LineType = ReceiptLineType.Image,
                Width = 371,
                Height = 173
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });
            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = "48 Boston Road"
            });

            lines.Add(new ReceiptLine
            {
                Text = "Beaumont Leys"
            });

            lines.Add(new ReceiptLine
            {
                Text = "Leicester"
            });

            lines.Add(new ReceiptLine
            {
                Text = "LE4 1AA"
            });

            lines.Add(new ReceiptLine
            {
                Text = "info@pivotal-retail.com   +44 (0) 116 320 0025"
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = "B.V - Herengracht 449 A - 1017 BR - Amsterdam"
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            var transaction = order.TransactionDetails;
            lines.Add(new ReceiptLine
            {
                Text = $"   Merchant ID: {transaction.MerchantId}          "
            });

            lines.Add(new ReceiptLine
            {
                Text = $"Transaction ID: {transaction.TransactionId}"
            });

            lines.Add(new ReceiptLine
            {
                Text = $"   Receipt No.: {transaction.ReceiptNumber}"
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = $"Sale",
                IsBold = true
            });

            lines.Add(new ReceiptLine
            {
                Text = transaction.TransactionDate.ToString("dd-MM-yyyy HH:mm")
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            decimal subtotal = 0;
            string? collectionCode = null;

            foreach (var basketItem in order.Basket)
            {
                var product = await _productDataService.GetProductByIdAsync(basketItem.ProductId);
                if (product != null)
                {
                    var itemTotal = basketItem.TotalPrice;
                    subtotal += itemTotal;

                    if (string.IsNullOrEmpty(collectionCode))
                    {
                        collectionCode = product.CollectionCode;
                    }

                    var priceText = $"€{itemTotal:F2}";
                    var receiptName = !string.IsNullOrEmpty(product.ReceiptName) 
                        ? product.ReceiptName 
                        : product.Name.ToUpperInvariant();
                    if (receiptName.Length > 40)
                    {
                        receiptName = receiptName.Substring(0, 40);
                    }
                    lines.Add(new ReceiptLine
                    {
                        Text = receiptName.PadRight(LineLength - priceText.Length) + priceText,
                        Alignment = ReceiptAlignment.Left,
                        LineType = ReceiptLineType.Text
                    });
                }
            }

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = $"TOTAL     €{subtotal:F2}",
                Alignment = ReceiptAlignment.Right,
                IsBold = true
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = transaction.PaymentMethod
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = "PLEASE RETAIN FOR YOUR RECORDS."
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = "---------------------------------------"
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = "Remember to collect your item",
                IsBold = true
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = "Take this receipt to the Select and Collect"
            });
            
            lines.Add(new ReceiptLine
            {
                Text = "lockers at the front of store"
            });
            
            lines.Add(new ReceiptLine
            {
                Text = "and follow the instructions onscreen."
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            if (!string.IsNullOrEmpty(collectionCode))
            {
                lines.Add(new ReceiptLine
                {
                    Text = "Your collection code is"
                });

                lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

                lines.Add(new ReceiptLine
                {
                    Text = collectionCode,
                    TextSize = TextSize.Large,
                    IsBold = true
                });

                lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });
            }

            if (!string.IsNullOrEmpty(collectionCode))
            {
                lines.Add(new ReceiptLine
                {
                    Text = collectionCode,
                    LineType = ReceiptLineType.QrCode,
                    Width = 8
                });

                lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });
            }

            lines.Add(new ReceiptLine
            {
                Text = "Age verification is required before collection",
                IsBold = true
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = "---------------------------------------"
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = "Thank you for shopping with us.",
                Alignment = ReceiptAlignment.Center,
                LineType = ReceiptLineType.Text,
                IsBold = true
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                Text = "Visit us online at www.pivotal-retail.com",
                Alignment = ReceiptAlignment.Center,
                LineType = ReceiptLineType.Text
            });

            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });
            lines.Add(new ReceiptLine { LineType = ReceiptLineType.BlankLine });

            lines.Add(new ReceiptLine
            {
                LineType = ReceiptLineType.Cut
            });

            return receiptData;
        }
    }
}
