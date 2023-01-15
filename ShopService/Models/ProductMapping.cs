using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopService.Models
{
    public class ProductMapping
    {
        [Column(TypeName = "char(36)")]
        [Key]
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
    }
}
