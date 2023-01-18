namespace ShopService.Models
{
    public class ProductToInvoice
    {
        public Guid Id { get; set; }
        public Material Material { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int StockAmount { get; set; }
    }
}
