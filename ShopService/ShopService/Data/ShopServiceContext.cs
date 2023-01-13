using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShopService.Models;

namespace ShopService.Data
{
    public class ShopServiceContext : DbContext
    {
        public ShopServiceContext (DbContextOptions<ShopServiceContext> options)
            : base(options)
        {
        }

        public DbSet<ShopService.Models.Product> Product { get; set; } = default!;
    }
}
