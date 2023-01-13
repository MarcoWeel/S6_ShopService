using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopService.Models
{
    public class Product
    {
        [Column(TypeName = "char(36)")]
        [Key]
        public Guid Id { get; set; }
        public Material Material { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int StockAmount { get; set; }
        //List<byte[]> bytes { get; set; }
    }
}
