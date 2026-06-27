using Microsoft.EntityFrameworkCore;
using MyEccomerce.Models;

namespace MyEccomerce.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Cart> Carts { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<ProductViewLog> productViewLogs { get; set; }
         
        public DbSet<InventoryLog> InventoryLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }

        public DbSet<UserToken> UserTokens { get; set; }

        public DbSet<OrderLog> OrderLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.ProductVariants)
                .HasForeignKey(pv => pv.ProductId);


            modelBuilder.Entity<Cart>()
        .HasIndex(c => c.UserId);
        }

    }


}
