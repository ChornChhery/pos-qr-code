using CatalogService.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Modifier> Modifiers => Set<Modifier>();

    public DbSet<ModifierOption> ModifierOptions => Set<ModifierOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.DisplayOrder).HasColumnName("display_order");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasMany(x => x.Products).WithOne(x => x.Category).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products", t =>
            {
                t.HasCheckConstraint("ck_products_price", "price >= 0");
                t.HasCheckConstraint("ck_products_stock_count", "stock_count >= 0");
            });

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.CategoryId).HasColumnName("category_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Price).HasColumnName("price").HasPrecision(10, 2);
            e.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
            e.Property(x => x.IsAvailable).HasColumnName("is_available");
            e.Property(x => x.StockCount).HasColumnName("stock_count");
            e.Property(x => x.LowStockThreshold).HasColumnName("low_stock_threshold");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
            e.Ignore(x => x.IsOrderable);
            e.HasIndex(x => x.CategoryId);
            e.HasMany(x => x.Modifiers).WithOne().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Modifier>(e =>
        {
            e.ToTable("modifiers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.GroupName).HasColumnName("group_name").HasMaxLength(100).IsRequired();
            e.Property(x => x.IsRequired).HasColumnName("is_required");
            e.Property(x => x.MinSelections).HasColumnName("min_selections");
            e.Property(x => x.MaxSelections).HasColumnName("max_selections");
            e.Property(x => x.DisplayOrder).HasColumnName("display_order");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.ProductId);
            e.HasMany(x => x.Options).WithOne().HasForeignKey(x => x.ModifierId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ModifierOption>(e =>
        {
            e.ToTable("modifier_options", t => t.HasCheckConstraint("ck_modifier_options_price_add", "price_add >= 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.ModifierId).HasColumnName("modifier_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.PriceAdd).HasColumnName("price_add").HasPrecision(10, 2);
            e.Property(x => x.DisplayOrder).HasColumnName("display_order");
            e.Property(x => x.IsAvailable).HasColumnName("is_available");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.ModifierId);
        });

        // MassTransit transactional outbox and consumer inbox tables.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
