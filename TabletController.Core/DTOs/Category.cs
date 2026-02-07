namespace TabletController.Core.DTOs
{
    public class Category
    {
        public int CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public int Order { get; set; }
    }
}
