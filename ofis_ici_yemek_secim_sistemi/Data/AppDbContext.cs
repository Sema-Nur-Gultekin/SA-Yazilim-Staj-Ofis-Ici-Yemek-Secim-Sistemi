using System.Data.Entity;
using ofis_ici_yemek_secim_sistemi.Models;

namespace ofis_ici_yemek_secim_sistemi.Data
{

    public class AppDbContext : DbContext
    {
       
        public AppDbContext() : base("name=DefaultConnection")
        {
        }

        public DbSet<Company> Companies { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<FoodCategory> FoodCategories { get; set; }
        public DbSet<Food> Foods { get; set; }

        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Selection> Selections { get; set; }

        public DbSet<FoodRating> FoodRatings { get; set; }

        public DbSet<ProductionRecord> ProductionRecords { get; set; }
        public DbSet<StockItem> StockItems { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StockItem>()
                .Property(s => s.CurrentQuantity)
                .HasPrecision(18, 2);

            modelBuilder.Entity<StockItem>()
                .Property(s => s.MinimumQuantity)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RecipeIngredient>()
                .Property(r => r.RequiredQuantity)
                .HasPrecision(18, 2);
        }
    }
}
