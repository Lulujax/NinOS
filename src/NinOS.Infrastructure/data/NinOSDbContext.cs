using Microsoft.EntityFrameworkCore;
using NinOS.Domain;

namespace NinOS.Infrastructure.Data
{
    public class NinOSDbContext : DbContext
    {
        public DbSet<seller> sellers { get; set; }
        public DbSet<note_type> note_types { get; set; }
        public DbSet<customer> customers { get; set; }
        public DbSet<product> products { get; set; }
        public DbSet<delivery_note> delivery_notes { get; set; }
        public DbSet<note_detail> note_details { get; set; }
        public DbSet<payment> payments { get; set; }
        public DbSet<commission> commissions { get; set; }
        public DbSet<commission_payment> commission_payments { get; set; }
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

            model_builder.Entity<note_type>(entity =>
            {
                entity.ToTable("note_type");
                entity.HasKey(e => e.id_note_type);
                entity.Property(e => e.id_note_type).HasColumnName("id_note_type").UseIdentityByDefaultColumn();
                entity.Property(e => e.name).HasColumnName("name").IsRequired().HasMaxLength(100);
                entity.Property(e => e.code).HasColumnName("code").IsRequired().HasMaxLength(50);
                entity.Property(e => e.id_seller).HasColumnName("id_seller").IsRequired(false);
                entity.Property(e => e.header_title).HasColumnName("header_title").IsRequired().HasMaxLength(100);
                entity.Property(e => e.calculation_type).HasColumnName("calculation_type").IsRequired().HasMaxLength(50);
                entity.Property(e => e.discount_configurable).HasColumnName("discount_configurable").IsRequired();
                entity.Property(e => e.default_discount_percentage).HasColumnName("default_discount_percentage").IsRequired().HasPrecision(18, 3);
                entity.Property(e => e.promo_discount_percentage).HasColumnName("promo_discount_percentage").HasPrecision(18, 3);
                entity.Property(e => e.mandatory_discount_percentage).HasColumnName("mandatory_discount_percentage").HasPrecision(18, 3);
                entity.Property(e => e.conditions_template).HasColumnName("conditions_template");
                entity.Property(e => e.discount_conditions_template).HasColumnName("discount_conditions_template");
                entity.Property(e => e.is_active).HasColumnName("is_active").IsRequired();
                entity.Property(e => e.sort_order).HasColumnName("sort_order").IsRequired();

                entity.HasOne<seller>().WithMany().HasForeignKey(e => e.id_seller).OnDelete(DeleteBehavior.Restrict);
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
                entity.Property(e => e.unit_price_usd).HasColumnName("unit_price_usd").IsRequired().HasPrecision(18, 2);
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
                entity.Property(e => e.total_amount_usd).HasColumnName("total_amount_usd").IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.adjusted_total_usd).HasColumnName("adjusted_total_usd").IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.status).HasColumnName("status").IsRequired().HasMaxLength(50);
                entity.Property(e => e.cxc_observations).HasColumnName("cxc_observations").HasMaxLength(500);
                entity.Property(e => e.sales_observations).HasColumnName("sales_observations").HasMaxLength(500);
                entity.Property(e => e.discount_percentage).HasColumnName("discount_percentage").HasPrecision(18, 3);
                entity.Property(e => e.note_type_id).HasColumnName("note_type_id").IsRequired(false);
                entity.Property(e => e.promo_discount_percentage).HasColumnName("promo_discount_percentage").HasPrecision(18, 3);
                entity.Property(e => e.volume_discount_percentage).HasColumnName("volume_discount_percentage").HasPrecision(18, 3);
                entity.Property(e => e.original_discount_percentage).HasColumnName("original_discount_percentage").HasPrecision(18, 3);
                entity.Property(e => e.original_volume_discount_percentage).HasColumnName("original_volume_discount_percentage").HasPrecision(18, 3);

                entity.HasOne<customer>().WithMany().HasForeignKey(e => e.id_customer).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<seller>().WithMany().HasForeignKey(e => e.id_seller).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<note_type>().WithMany().HasForeignKey(e => e.note_type_id).OnDelete(DeleteBehavior.Restrict);
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
                entity.Property(e => e.unit_price_usd).HasColumnName("unit_price_usd").IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.subtotal_usd).HasColumnName("subtotal_usd").IsRequired().HasPrecision(18, 2);

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
                entity.Property(e => e.amount_usd).HasColumnName("amount_usd").IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.amount_bs).HasColumnName("amount_bs").IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.exchange_rate).HasColumnName("exchange_rate").HasPrecision(18, 4);
                entity.Property(e => e.payment_type).HasColumnName("payment_type").IsRequired().HasMaxLength(50);
                entity.Property(e => e.reference_number).HasColumnName("reference_number").HasMaxLength(100);
                entity.Property(e => e.bank_name).HasColumnName("bank_name").HasMaxLength(100);
                entity.Property(e => e.observations).HasColumnName("observations");
                entity.Property(e => e.created_at).HasColumnName("created_at").IsRequired();
                entity.Property(e => e.updated_at).HasColumnName("updated_at");

                entity.HasOne<delivery_note>().WithMany().HasForeignKey(e => e.id_delivery_note).OnDelete(DeleteBehavior.Cascade);
            });

            model_builder.Entity<commission>(entity =>
            {
                entity.ToTable("commission");
                entity.HasKey(e => e.id_commission);
                entity.Property(e => e.id_commission).HasColumnName("id_commission").UseIdentityByDefaultColumn();
                entity.Property(e => e.id_seller).HasColumnName("id_seller").IsRequired();
                entity.Property(e => e.id_delivery_note).HasColumnName("id_delivery_note").IsRequired();
                entity.Property(e => e.commission_percentage).HasColumnName("commission_percentage").IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.amount_usd).HasColumnName("amount_usd").IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.amount_bs).HasColumnName("amount_bs").IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.exchange_rate).HasColumnName("exchange_rate").IsRequired().HasPrecision(18, 4);
                entity.Property(e => e.reference_number).HasColumnName("reference_number").HasMaxLength(100);
                entity.Property(e => e.is_paid).HasColumnName("is_paid").IsRequired();
                entity.Property(e => e.payout_date).HasColumnName("payout_date");

                entity.HasOne<seller>().WithMany().HasForeignKey(e => e.id_seller).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<delivery_note>().WithMany().HasForeignKey(e => e.id_delivery_note).OnDelete(DeleteBehavior.Cascade);
            });

            model_builder.Entity<commission_payment>(entity =>
            {
                entity.ToTable("commission_payment");
                entity.HasKey(e => e.id_commission_payment);
                entity.Property(e => e.id_commission_payment).HasColumnName("id_commission_payment").UseIdentityByDefaultColumn();
                entity.Property(e => e.id_commission).HasColumnName("id_commission").IsRequired();
                entity.Property(e => e.amount_usd).HasColumnName("amount_usd").IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.amount_bs).HasColumnName("amount_bs").IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.exchange_rate).HasColumnName("exchange_rate").IsRequired().HasPrecision(18, 4);
                entity.Property(e => e.payment_type).HasColumnName("payment_type").IsRequired().HasMaxLength(50);
                entity.Property(e => e.reference_number).HasColumnName("reference_number").HasMaxLength(100);
                entity.Property(e => e.bank_name).HasColumnName("bank_name").HasMaxLength(100);
                entity.Property(e => e.observations).HasColumnName("observations");
                entity.Property(e => e.payment_date).HasColumnName("payment_date").IsRequired();

                entity.HasOne<commission>().WithMany().HasForeignKey(e => e.id_commission).OnDelete(DeleteBehavior.Cascade);
            });

            model_builder.Entity<promotion>(entity =>
            {
                entity.ToTable("promotion");
                entity.HasKey(e => e.id_promotion);
                entity.Property(e => e.id_promotion).HasColumnName("id_promotion").UseIdentityByDefaultColumn();
                entity.Property(e => e.promotion_code).HasColumnName("promotion_code").IsRequired().HasMaxLength(50);
                entity.Property(e => e.name).HasColumnName("name").IsRequired().HasMaxLength(200);
                entity.Property(e => e.category).HasColumnName("category").IsRequired().HasMaxLength(100);
                entity.Property(e => e.unit_price_usd).HasColumnName("unit_price_usd").IsRequired().HasPrecision(18, 2);
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