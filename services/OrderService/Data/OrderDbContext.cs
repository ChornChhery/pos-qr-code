using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain;
using Pos.Shared.Http;

namespace OrderService.Data;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<DiningTable> Tables => Set<DiningTable>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<CatalogProduct> CatalogProducts => Set<CatalogProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiningTable>(e =>
        {
            e.ToTable("tables", t =>
            {
                t.HasCheckConstraint("ck_tables_table_number", "table_number > 0");
                t.HasCheckConstraint("ck_tables_capacity", "capacity > 0");
                t.HasCheckConstraint("ck_tables_status", "status IN ('AVAILABLE','OCCUPIED','RESERVED','CLEANING')");
            });

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.TableNumber).HasColumnName("table_number");
            e.HasIndex(x => x.TableNumber).IsUnique();
            e.Property(x => x.Capacity).HasColumnName("capacity");
            e.Property(x => x.QrCodeUrl).HasColumnName("qr_code_url").HasMaxLength(500);
            e.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasConversion(v => v.ToWire(), v => Enum.Parse<TableStatus>(v, true));
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders", t =>
            {
                t.HasCheckConstraint("ck_orders_status", "status IN ('PENDING','COOKING','READY','COMPLETED','CANCELLED')");
                t.HasCheckConstraint("ck_orders_subtotal", "subtotal >= 0");
                t.HasCheckConstraint("ck_orders_tax", "tax >= 0");
                t.HasCheckConstraint("ck_orders_discount", "discount >= 0");
                t.HasCheckConstraint("ck_orders_total", "total >= 0");
            });

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.TableId).HasColumnName("table_id");
            e.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasConversion(v => v.ToWire(), v => Enum.Parse<OrderStatus>(v, true));
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(10, 2);
            e.Property(x => x.Tax).HasColumnName("tax").HasPrecision(10, 2);
            e.Property(x => x.Discount).HasColumnName("discount").HasPrecision(10, 2);
            e.Property(x => x.Total).HasColumnName("total").HasPrecision(10, 2);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.CompletedAt).HasColumnName("completed_at");
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.TableId);
            e.HasOne(x => x.Table).WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items", t =>
            {
                t.HasCheckConstraint("ck_order_items_quantity", "quantity > 0");
                t.HasCheckConstraint("ck_order_items_unit_price", "unit_price >= 0");
                t.HasCheckConstraint("ck_order_items_total_price", "total_price >= 0");
            });

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 2);
            e.Property(x => x.SelectedModifiers).HasColumnName("selected_modifiers").HasColumnType("jsonb");
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.TotalPrice).HasColumnName("total_price").HasPrecision(10, 2);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.OrderId);
        });

        modelBuilder.Entity<CatalogProduct>(e =>
        {
            e.ToTable("catalog_products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.Price).HasColumnName("price").HasPrecision(10, 2);
            e.Property(x => x.IsAvailable).HasColumnName("is_available");
            e.Property(x => x.StockCount).HasColumnName("stock_count");
            e.Property(x => x.ModifiersJson).HasColumnName("modifiers").HasColumnType("jsonb").IsRequired();
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        });

        // MassTransit transactional outbox and consumer inbox tables.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
