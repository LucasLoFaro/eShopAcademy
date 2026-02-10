using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore;
using Domain.Orders.Entities;
using Domain.Orders.Enums;


namespace Infrastructure.Data;

public class OrderDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        var toStringOrder = new EnumToStringConverter<OrderStatus>();
        var toStringShip = new EnumToStringConverter<ShippingStatus>();
        var toStringPay = new EnumToStringConverter<PaymentStatus>();
        var toStringBilling = new EnumToStringConverter<BillingStatus>();

        builder.Entity<Order>(o =>
        {
            o.HasKey(x => x.Id);

            o.Property(x => x.Status).HasConversion(toStringOrder);

            o.HasMany(x => x.Items)
             .WithOne()
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            o.OwnsOne(o => o.Customer, c =>
            {
                c.Property(x => x.Name).HasColumnName("customer_name");
                c.Property(x => x.Email).HasColumnName("customer_email");
                c.Property(x => x.Phone).HasColumnName("customer_phone");
                c.OwnsOne(c => c.Address, a =>
                {
                    a.Property(x => x.Street).HasColumnName("customer_address_street");
                    a.Property(x => x.Number).HasColumnName("customer_address_number");
                    a.Property(x => x.City).HasColumnName("customer_address_city");
                    a.Property(x => x.ZipCode).HasColumnName("customer_address_zipcode");
                    a.Property(x => x.AdditionalInformation).HasColumnName("customer_address_additionalinformation");
                });
            });

            o.OwnsOne(o => o.Payment, p =>
            {
                p.Property(x => x.Id).HasColumnName("payment_id");
                p.Property(x => x.Status).HasColumnName("payment_status").HasConversion(toStringPay);
                p.Property(x => x.ProviderTransactionId).HasColumnName("payment_provider_id");
                p.Property(x => x.Amount).HasColumnName("payment_amount");
                p.Property(x => x.PaidAt).HasColumnName("payment_paid_at");
            });

            o.OwnsOne(o => o.Shipping, s =>
            {
                s.Property(x => x.Status).HasColumnName("shipping_status").HasConversion(toStringShip);
                s.Property(x => x.DestinationAddress).HasColumnName("shipping_destination_address");
                s.Property(x => x.TrackingNumber).HasColumnName("shipping_tracking_number");
                s.Property(x => x.Carrier).HasColumnName("shipping_carrier");
                s.Property(x => x.ReadyForPickupAt).HasColumnName("shipping_ready_for_pickup_at");
                s.Property(x => x.ShippedAt).HasColumnName("shipping_shipped_at");
                s.Property(x => x.DeliveredAt).HasColumnName("shipping_delivered_at");
            });

            o.OwnsOne(o => o.Stock, st =>
            {
                st.Property(x => x.ReservationId).HasColumnName("stock_reservation_id");
                st.Property(x => x.CommittedAt).HasColumnName("stock_committed_at");
            });

            o.OwnsOne(o => o.Operations, op =>
            {
                op.Property(x => x.OperatorName).HasColumnName("operations_operator_name");
                op.Property(x => x.PackedAt).HasColumnName("operations_packed_at");
            });

            o.OwnsOne(o => o.Billing, b =>
            {
                b.Property(x => x.Status).HasColumnName("billing_status").HasConversion(toStringBilling);
                b.Property(x => x.InvoiceId).HasColumnName("billing_invoice_id");
                b.Property(x => x.BilledAt).HasColumnName("billing_billed_at");
            });
        });

        builder.Entity<OrderItem>(oi =>
        {
            oi.OwnsOne(x => x.Product, p =>
            {
                p.Property(x => x.ID).HasColumnName("product_id");
                p.Property(x => x.Name).HasColumnName("product_name");
                p.Property(x => x.Price).HasColumnName("product_price");
                p.Property(x => x.Stock).HasColumnName("product_stock");
                p.Property(x => x.Description).HasColumnName("product_description");
                p.Property(x => x.Image).HasColumnName("product_image");
                p.Property(x => x.CategoryName).HasColumnName("product_category").IsRequired(false);
            });

            oi.Ignore(x => x.Price);
        });

        base.OnModelCreating(builder);
    }


    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.ModifiedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = now;
            }
        }
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
