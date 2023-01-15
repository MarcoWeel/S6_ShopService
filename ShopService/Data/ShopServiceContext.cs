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
        //public ShopServiceContext (DbContextOptions<ShopServiceContext> options)
        //    : base(options)
        //{
        //}
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(databaseName: "ShopDB");
        }

        public DbSet<ShopService.Models.Product> Product { get; set; } = default!;
        public DbSet<ShopService.Models.Material> Material { get; set; } = default!;
        public DbSet<ShopService.Models.ProductMapping> ProductMappings { get; set; } = default!;
        public DbSet<ShopService.Models.Order> Order { get; set; } = default!;
    }
}
