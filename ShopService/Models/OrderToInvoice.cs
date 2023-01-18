namespace ShopService.Models
{
    public class OrderToInvoice
    {
        public Guid Id { get; set; }
        public Guid UserGuid { get; set; }
        public double TotalPrice { get; set; }
        public List<ProductToInvoice> Products { get; set; }
    }
}
