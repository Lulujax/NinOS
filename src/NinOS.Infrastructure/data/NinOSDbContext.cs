using Microsoft.EntityFrameworkCore;
using NinOS.Domain;

namespace NinOS.Infrastructure.Data
{
    public class NinOSDbContext : DbContext
    {
        public DbSet<seller> sellers { get; set; }
        public DbSet<customer> customers { get; set; }
        public DbSet<product> products { get; set; }
        public DbSet<delivery_note> delivery_notes { get; set; }
        public DbSet<note_detail> note_details { get; set; }
        public DbSet<payment> payments { get; set; }
        public DbSet<commission> commissions { get; set; }
        public DbSet<promotion> promotions { get; set; }
        public DbSet<promotion_item> promotion_items { get; set; }

        public NinOSDbContext(DbContextOptions<NinOSDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder model_builder)
        {
            model_builder.Entity<seller>(entity =>
            {
                entity.ToTable("seller");
                entity.HasKey(e => e.id_seller);
                entity.Property(e => e.id_seller).HasColumnName("id_seller").UseIdentityByDefaultColumn();
                entity.Property(e => e.full_name).HasColumnName("full_name").IsRequired().HasMaxLength(150);
                entity.Property(e => e.seller_code).HasColumnName("seller_code").IsRequired().HasMaxLength(50);
                entity.Property(e => e.customer_code_prefix).HasColumnName("customer_code_prefix").IsRequired().HasMaxLength(50);
            });

            model_builder.Entity<customer>(entity =>
            {
                entity.ToTable("customer");
                entity.HasKey(e => e.id_customer);
                entity.Property(e => e.id_customer).HasColumnName("id_customer").UseIdentityByDefaultColumn();
                entity.Property(e => e.customer_code).HasColumnName("customer_code").IsRequired().HasMaxLength(50);
                entity.Property(e => e.business_name).HasColumnName("business_name").IsRequired().HasMaxLength(200);
                entity.Property(e => e.rif).HasColumnName("rif").HasMaxLength(50);
                entity.Property(e => e.contact_name).HasColumnName("contact_name").HasMaxLength(100);
                entity.Property(e => e.phone_number).HasColumnName("phone_number").HasMaxLength(50);
                entity.Property(e => e.fiscal_address).HasColumnName("fiscal_address");
                entity.Property(e => e.delivery_address).HasColumnName("delivery_address");
                entity.Property(e => e.seller_name).HasColumnName("seller_name").HasMaxLength(100);
            });

            model_builder.Entity<product>(entity =>
            {
                entity.ToTable("product");
                entity.HasKey(e => e.id_product);
                entity.Property(e => e.id_product).HasColumnName("id_product").UseIdentityByDefaultColumn();
                entity.Property(e => e.product_code).HasColumnName("product_code").IsRequired().HasMaxLength(50);
                entity.Property(e => e.name).HasColumnName("name").IsRequired().HasMaxLength(250);
                entity.Property(e => e.category).HasColumnName("category").IsRequired().HasMaxLength(100);
                entity.Property(e => e.unit_price_usd).HasColumnName("unit_price_usd").IsRequired();
                entity.Property(e => e.stock_quantity).HasColumnName("stock_quantity").IsRequired();
            });

            model_builder.Entity<delivery_note>(entity =>
            {
                entity.ToTable("delivery_note");
                entity.HasKey(e => e.id_delivery_note);
                entity.Property(e => e.id_delivery_note).HasColumnName("id_delivery_note").UseIdentityByDefaultColumn();
                entity.Property(e => e.note_number).HasColumnName("note_number").IsRequired().HasMaxLength(50);
                entity.Property(e => e.creation_date).HasColumnName("creation_date").IsRequired();
                entity.Property(e => e.id_seller).HasColumnName("id_seller").IsRequired();
                entity.Property(e => e.id_customer).HasColumnName("id_customer").IsRequired();
                entity.Property(e => e.total_amount_usd).HasColumnName("total_amount_usd").IsRequired();
                entity.Property(e => e.status).HasColumnName("status").IsRequired().HasMaxLength(50);

                entity.HasOne<customer>().WithMany().HasForeignKey(e => e.id_customer).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<seller>().WithMany().HasForeignKey(e => e.id_seller).OnDelete(DeleteBehavior.Restrict);
            });

            model_builder.Entity<note_detail>(entity =>
            {
                entity.ToTable("note_detail");
                entity.HasKey(e => e.id_note_detail);
                entity.Property(e => e.id_note_detail).HasColumnName("id_note_detail").UseIdentityByDefaultColumn();
                entity.Property(e => e.id_delivery_note).HasColumnName("id_delivery_note").IsRequired();
                entity.Property(e => e.id_product).HasColumnName("id_product").IsRequired(false);
                entity.Property(e => e.id_promotion).HasColumnName("id_promotion").IsRequired(false);
                entity.Property(e => e.quantity).HasColumnName("quantity").IsRequired();
                entity.Property(e => e.unit_price_usd).HasColumnName("unit_price_usd").IsRequired();
                entity.Property(e => e.subtotal_usd).HasColumnName("subtotal_usd").IsRequired();

                entity.HasOne<delivery_note>().WithMany().HasForeignKey(e => e.id_delivery_note).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<product>().WithMany().HasForeignKey(e => e.id_product).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<promotion>().WithMany().HasForeignKey(e => e.id_promotion).OnDelete(DeleteBehavior.Restrict);
            });

            model_builder.Entity<payment>(entity =>
            {
                entity.ToTable("payment");
                entity.HasKey(e => e.id_payment);
                entity.Property(e => e.id_payment).HasColumnName("id_payment").UseIdentityByDefaultColumn();
                entity.Property(e => e.id_delivery_note).HasColumnName("id_delivery_note").IsRequired();
                entity.Property(e => e.payment_date).HasColumnName("payment_date").IsRequired();
                entity.Property(e => e.amount_usd).HasColumnName("amount_usd").IsRequired();
                entity.Property(e => e.amount_bs).HasColumnName("amount_bs").IsRequired();
                entity.Property(e => e.exchange_rate).HasColumnName("exchange_rate").IsRequired();

                entity.HasOne<delivery_note>().WithMany().HasForeignKey(e => e.id_delivery_note).OnDelete(DeleteBehavior.Cascade);
            });

            model_builder.Entity<commission>(entity =>
            {
                entity.ToTable("commission");
                entity.HasKey(e => e.id_commission);
                entity.Property(e => e.id_commission).HasColumnName("id_commission").UseIdentityByDefaultColumn();
                entity.Property(e => e.id_seller).HasColumnName("id_seller").IsRequired();
                entity.Property(e => e.id_delivery_note).HasColumnName("id_delivery_note").IsRequired();
                entity.Property(e => e.commission_percentage).HasColumnName("commission_percentage").IsRequired();
                entity.Property(e => e.amount_usd).HasColumnName("amount_usd").IsRequired();
                entity.Property(e => e.is_paid).HasColumnName("is_paid").IsRequired();
                entity.Property(e => e.payout_date).HasColumnName("payout_date");

                entity.HasOne<seller>().WithMany().HasForeignKey(e => e.id_seller).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<delivery_note>().WithMany().HasForeignKey(e => e.id_delivery_note).OnDelete(DeleteBehavior.Cascade);
            });

            model_builder.Entity<promotion>(entity =>
            {
                entity.ToTable("promotion");
                entity.HasKey(e => e.id_promotion);
                entity.Property(e => e.id_promotion).HasColumnName("id_promotion").UseIdentityByDefaultColumn();
                entity.Property(e => e.promotion_code).HasColumnName("promotion_code").IsRequired().HasMaxLength(50);
                entity.Property(e => e.name).HasColumnName("name").IsRequired().HasMaxLength(200);
                entity.Property(e => e.category).HasColumnName("category").IsRequired().HasMaxLength(100);
                entity.Property(e => e.unit_price_usd).HasColumnName("unit_price_usd").IsRequired();
            });

            model_builder.Entity<promotion_item>(entity =>
            {
                entity.ToTable("promotion_item");
                entity.HasKey(e => e.id_promotion_item);
                entity.Property(e => e.id_promotion_item).HasColumnName("id_promotion_item").UseIdentityByDefaultColumn();
                entity.Property(e => e.id_promotion).HasColumnName("id_promotion").IsRequired();
                entity.Property(e => e.id_product).HasColumnName("id_product").IsRequired();
                entity.Property(e => e.quantity_required).HasColumnName("quantity_required").IsRequired();

                entity.HasOne(e => e.promotion)
                    .WithMany(p => p.items)
                    .HasForeignKey(e => e.id_promotion)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.product)
                    .WithMany()
                    .HasForeignKey(e => e.id_product)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}